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
                .Select(s => new { s.Id, s.UnitId, s.GuardId })
                .ToListAsync(ct);
            if (sessions.Count == 0)
                return Array.Empty<LiveUnitDto>();

            /* Kind + name resolution: two bounded lookups against the read-only platform
               projections. A wand allocated to a patrol car renders as a car; everything
               else — a guard on foot with a wand — renders as a guard. */
            var unitIds = sessions.Select(s => s.UnitId).ToList();
            var carUnits = await _db.PlatformSmartWands
                .Where(w => unitIds.Contains(w.Id) && w.PatrolCarId != null)
                .Select(w => w.Id)
                .ToListAsync(ct);
            var guardIds = sessions.Select(s => s.GuardId).Distinct().ToList();
            var guardNames = await _db.PlatformGuards
                .Where(g => guardIds.Contains(g.Id))
                .ToDictionaryAsync(g => g.Id, g => g.Name, ct);
            var carSet = carUnits.ToHashSet();
            var guardBySession = sessions.ToDictionary(s => s.Id, s => s.GuardId);

            LiveUnitDto Decorate(LiveUnitDto dto)
            {
                var guardId = guardBySession.TryGetValue(dto.SessionId, out var g) ? g : 0;
                return dto with
                {
                    Kind = carSet.Contains(dto.UnitId) ? "car" : "guard",
                    GuardId = guardId,
                    GuardName = guardNames.TryGetValue(guardId, out var name) ? name : null
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
                        (int)Math.Max(0, (now - warm.ReceivedUtc).TotalSeconds))));
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

            return result.OrderBy(u => u.UnitId).ToList();
        }
    }
}
