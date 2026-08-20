using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CityWatch.Tracking.Contracts;
using CityWatch.Tracking.Data;
using Microsoft.EntityFrameworkCore;

namespace CityWatch.Tracking.Services
{
    /// <summary>
    /// The live picture, wherever it is asked for.
    /// </summary>
    /// <remarks>
    /// Ingest runs in CityWatch.Web, so only that process has a warm <see cref="ILiveStateStore"/>.
    /// The control room runs in CityWatch.RadioCheck — a different process — where the store is
    /// empty. This service serves memory when it is fresh and falls back to the database (latest
    /// point per active session; a handful of clustered-index seeks at patrol fleet scale) when
    /// it is not. Redis replaces the fallback as the shared store in Phase 2 (D11) without
    /// changing this interface or its callers.
    /// </remarks>
    public interface ILiveSnapshotService
    {
        Task<IReadOnlyList<LiveUnitDto>> GetSnapshotAsync(CancellationToken ct);
    }

    public sealed record LiveUnitDto(
        int UnitId, Guid SessionId, decimal Lat, decimal Lon,
        short? SpeedKph, short? HeadingDeg, short? AccuracyM, byte? BatteryPct,
        byte Mode, byte Flags, DateTime RecordedUtc, int AgeSeconds)
    {
        /// <summary>"car" when the wand is allocated to a patrol car, else "guard" —
        /// this is what picks the map symbol.</summary>
        public string Kind { get; init; } = "guard";

        public int GuardId { get; init; }

        public string? GuardName { get; init; }

        /// <summary>Callsign from the login screen ("Romeo 1") — the label operators use.</summary>
        public string? Callsign { get; init; }

        /// <summary>The car itself — "Mobile Patrols (Car) M1". The tracked unit's identity.</summary>
        public string? PatrolCar { get; init; }

        /// <summary>AtSite | Transit — from NFC scans; does not gate GPS.</summary>
        public string TravelState { get; init; } = "Transit";

        /// <summary>Site the car is at right now; null while travelling.</summary>
        public string? CurrentSiteName { get; init; }

        /// <summary>Minutes in the current state — "at Martha Cove 12 min".</summary>
        public int StateMinutes { get; init; }

        /// <summary>When this session opened — the map's "full journey so far" window, and
        /// the boundary a live trail must reset on when the session changes hands.</summary>
        public DateTime SessionStartedUtc { get; init; }

        /// <summary>True when SpeedKph is computed from fixes, not device-reported —
        /// the UI shows it as approximate ("~42 km/h").</summary>
        public bool SpeedDerived { get; init; }
    }

    public sealed class LiveSnapshotService : ILiveSnapshotService
    {
        private readonly ILiveStateStore _memory;
        private readonly TrackingDbContext _db;
        private readonly Func<DateTime> _utcNow;

        public LiveSnapshotService(ILiveStateStore memory, TrackingDbContext db, Func<DateTime>? utcNow = null)
        {
            _memory = memory;
            _db = db;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public async Task<IReadOnlyList<LiveUnitDto>> GetSnapshotAsync(CancellationToken ct)
        {
            var now = _utcNow();

            var sessions = await _db.TrackingSessions
                .Where(s => s.Status == "Active")
                .Select(s => new
                {
                    s.Id, s.UnitId, s.GuardId, s.IsPatrolCar, s.Callsign,
                    s.PatrolCarPositionName, s.TravelState, s.CurrentSiteName, s.TravelStateSinceUtc,
                    s.StartedUtc, s.ClientSiteId
                })
                .ToListAsync(ct);
            if (sessions.Count == 0)
                return Array.Empty<LiveUnitDto>();

            /* Kind + name resolution: two bounded lookups against the read-only platform
               projections. A wand allocated to a patrol car renders as a car; everything
               else — a guard on foot with a wand — renders as a guard. */
            var unitIds = sessions.Select(s => s.UnitId).ToList();
            /* Legacy fallback only: units keyed on a wand device are no longer issued, but a
               session created before the change may still be open. New units say what they
               are in the key itself (TrackingUnitKey) and in the session's IsPatrolCar. */
            var legacyWandIds = unitIds.Where(id => !TrackingUnitKey.IsGuard(id) && !TrackingUnitKey.IsPosition(id)).ToList();
            var carUnits = legacyWandIds.Count == 0
                ? new List<int>()
                : await _db.PlatformSmartWands
                    .Where(w => legacyWandIds.Contains(w.Id) && w.PatrolCarId != null)
                    .Select(w => w.Id)
                    .ToListAsync(ct);
            var guardIds = sessions.Select(s => s.GuardId).Distinct().ToList();
            var guardNames = await _db.PlatformGuards
                .Where(g => guardIds.Contains(g.Id))
                .ToDictionaryAsync(g => g.Id, g => g.Name, ct);
            /* A car whose login declared no position (#153 Part 1) still needs its home name
               under the callsign — the login site IS the car in the platform's model
               ("Mobile Patrols (Car) M1" is a ClientSite). Bounded to the sessions missing it. */
            var unnamedSiteIds = sessions
                .Where(s => string.IsNullOrWhiteSpace(s.PatrolCarPositionName))
                .Select(s => s.ClientSiteId)
                .Distinct()
                .ToList();
            var loginSiteNames = unnamedSiteIds.Count == 0
                ? new Dictionary<int, string?>()
                : await _db.PlatformClientSites
                    .Where(cs => unnamedSiteIds.Contains(cs.Id))
                    .ToDictionaryAsync(cs => cs.Id, cs => cs.Name, ct);
            var carSet = carUnits.ToHashSet();
            var sessionById = sessions.ToDictionary(s => s.Id);

            LiveUnitDto Decorate(LiveUnitDto dto)
            {
                sessionById.TryGetValue(dto.SessionId, out var session);
                var guardId = session?.GuardId ?? 0;

                /* Kind precedence: the officer's own "Mobile Patrol Car" toggle for THIS
                   shift wins; then the unit key itself; the wand's PatrolCarId is only a
                   fallback for legacy sessions opened before the change. */
                var isCar = session?.IsPatrolCar
                            ?? (TrackingUnitKey.IsPosition(dto.UnitId) ? true
                                : TrackingUnitKey.IsGuard(dto.UnitId) ? false
                                : carSet.Contains(dto.UnitId));

                var stateMinutes = session?.TravelStateSinceUtc is { } since
                    ? (int)Math.Max(0, (now - since).TotalMinutes)
                    : 0;

                return dto with
                {
                    Kind = isCar ? "car" : "guard",
                    GuardId = guardId,
                    GuardName = guardNames.TryGetValue(guardId, out var name) ? name : null,
                    Callsign = string.IsNullOrWhiteSpace(session?.Callsign) ? null : session!.Callsign,
                    PatrolCar = !string.IsNullOrWhiteSpace(session?.PatrolCarPositionName)
                        ? session!.PatrolCarPositionName
                        : isCar && loginSiteNames.TryGetValue(session?.ClientSiteId ?? 0, out var homeSite)
                            && !string.IsNullOrWhiteSpace(homeSite)
                            ? homeSite
                            : null,
                    TravelState = string.IsNullOrWhiteSpace(session?.TravelState) ? "Transit" : session!.TravelState,
                    CurrentSiteName = string.IsNullOrWhiteSpace(session?.CurrentSiteName) ? null : session!.CurrentSiteName,
                    StateMinutes = stateMinutes,
                    SessionStartedUtc = session?.StartedUtc ?? default
                };
            }

            var result = new List<LiveUnitDto>(sessions.Count);
            var missing = new List<(int UnitId, Guid SessionId)>();

            foreach (var session in sessions)
            {
                var warm = _memory.Get(session.UnitId);
                if (warm != null && warm.SessionId == session.Id)
                {
                    result.Add(Decorate(new LiveUnitDto(
                        warm.UnitId, warm.SessionId, warm.Lat, warm.Lon,
                        warm.SpeedKph, warm.HeadingDeg, warm.AccuracyM, warm.BatteryPct,
                        (byte)warm.Mode, (byte)warm.Flags, warm.RecordedUtc,
                        (int)Math.Max(0, (now - warm.ReceivedUtc).TotalSeconds))
                    { SpeedDerived = warm.SpeedDerived }));
                }
                else
                {
                    missing.Add((session.UnitId, session.Id));
                }
            }

            /* Cold path: this process has not seen the unit's traffic. Latest stored point
               per session — bounded by fleet size, served by CX_TrackPoint_Unit_Time. */
            foreach (var (unitId, sessionId) in missing)
            {
                var point = await _db.TrackPoints
                    .Where(p => p.UnitId == unitId && p.SessionId == sessionId
                                && (p.Flags & (byte)TrackPointFlags.Backfilled) == 0)
                    .OrderByDescending(p => p.RecordedUtc)
                    .FirstOrDefaultAsync(ct);
                if (point == null)
                    continue;   // session open, no fix yet — the unit is not on the map

                result.Add(Decorate(new LiveUnitDto(
                    point.UnitId, point.SessionId, point.Latitude, point.Longitude,
                    point.SpeedKph, point.HeadingDeg, point.AccuracyM, point.BatteryPct,
                    point.ModeAtCapture, point.Flags, point.RecordedUtc,
                    (int)Math.Max(0, (now - point.ReceivedUtc).TotalSeconds))));
            }

            /* The live map shows NOW; a point's ModeAtCapture is history. A marker keeps its
               DURESS accent only while the platform's ClientSiteDuress rows say the alarm is
               still on — the control room deactivating it deletes those rows, and the map must
               stand down with them even when the phone is unreachable and cannot ack (§5.4). */
            if (result.Any(u => u.Mode == (byte)TrackingMode.Duress))
            {
                var alarmedGuards = (await _db.PlatformClientSiteDuress
                    .Where(d => d.IsEnabled)
                    .Select(d => d.EnabledBy)
                    .ToListAsync(ct)).ToHashSet();
                for (var i = 0; i < result.Count; i++)
                    if (result[i].Mode == (byte)TrackingMode.Duress && !alarmedGuards.Contains(result[i].GuardId))
                        result[i] = result[i] with { Mode = (byte)TrackingMode.Transit };
            }

            return result.OrderBy(u => u.UnitId).ToList();
        }
    }
}
