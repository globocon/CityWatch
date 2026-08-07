using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CityWatch.Tracking.Configuration;
using CityWatch.Tracking.Contracts;
using CityWatch.Tracking.Data;
using Microsoft.EntityFrameworkCore;

namespace CityWatch.Tracking.Services
{
    public sealed record IdleUnitDto(
        int UnitId, string Kind, int GuardId, string? GuardName,
        decimal Lat, decimal Lon, DateTime IdleSinceUtc, int IdleMinutes)
    {
        /// <summary>Callsign from the login screen, when the unit declared one.</summary>
        public string? Callsign { get; init; }
    }

    public interface IIdleDetectionService
    {
        /// <summary>Units on an active session that have not moved beyond the idle radius
        /// for at least the threshold. An idle DURESS unit is excluded — it is an emergency,
        /// not a loiter.</summary>
        Task<IReadOnlyList<IdleUnitDto>> GetIdleUnitsAsync(TimeSpan threshold, CancellationToken ct);
    }

    /// <summary>
    /// "Sitting in one location for a long time" made precise: from the unit's newest fix,
    /// walk its recent trail backwards; the first fix that lies OUTSIDE the idle radius ends
    /// the idle spell, and everything after it is time-in-place. A patrol that crawls around
    /// a car park stays "idle" (all fixes inside the radius); a patrol that drove off and
    /// came back is idle only since it came back.
    ///
    /// This is an operator-attention feature, not a disciplinary record: it reads the same
    /// evidentiary points everything else reads and stores nothing new.
    /// </summary>
    public sealed class IdleDetectionService : IIdleDetectionService
    {
        /// <summary>Look no further back than this: a whole shift parked is reported as
        /// "idle 4h+" rather than paying for an unbounded scan.</summary>
        private static readonly TimeSpan MaxLookback = TimeSpan.FromHours(4);

        private readonly TrackingDbContext _db;
        private readonly TrackingOptions _options;
        private readonly Func<DateTime> _utcNow;

        public IdleDetectionService(TrackingDbContext db, TrackingOptions options, Func<DateTime>? utcNow = null)
        {
            _db = db;
            _options = options;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public async Task<IReadOnlyList<IdleUnitDto>> GetIdleUnitsAsync(TimeSpan threshold, CancellationToken ct)
        {
            var now = _utcNow();
            var since = now - MaxLookback;

            var sessions = await _db.TrackingSessions
                .Where(s => s.Status == "Active")
                .Select(s => new { s.Id, s.UnitId, s.GuardId, s.IsPatrolCar, s.Callsign })
                .ToListAsync(ct);
            if (sessions.Count == 0)
                return Array.Empty<IdleUnitDto>();

            var result = new List<IdleUnitDto>();

            foreach (var session in sessions)
            {
                /* Newest-first trail for the session, bounded by the lookback window.
                   Backfilled points are history replays, not presence — skip them. */
                var trail = await _db.TrackPoints
                    .Where(p => p.SessionId == session.Id && p.RecordedUtc >= since
                                && (p.Flags & (byte)TrackPointFlags.Backfilled) == 0)
                    .OrderByDescending(p => p.RecordedUtc)
                    .Select(p => new { p.RecordedUtc, p.Latitude, p.Longitude, p.ModeAtCapture })
                    .ToListAsync(ct);
                if (trail.Count == 0)
                    continue;

                var newest = trail[0];
                if (newest.ModeAtCapture == (byte)TrackingMode.Duress)
                    continue;   // an emergency is not a loiter

                /* Walk backwards while fixes stay inside the radius of the CURRENT position. */
                var idleSince = newest.RecordedUtc;
                var brokeOut = false;
                foreach (var fix in trail.Skip(1))
                {
                    var metres = GeoMath.HaversineKm(newest.Latitude, newest.Longitude, fix.Latitude, fix.Longitude) * 1000;
                    if (metres > _options.IdleRadiusM)
                    {
                        brokeOut = true;
                        break;
                    }
                    idleSince = fix.RecordedUtc;
                }

                /* If the whole bounded trail is in-radius and reaches the window edge, the
                   true idle start is unknown-but-older: report the window edge honestly. */
                if (!brokeOut && trail.Count > 1)
                    idleSince = trail[^1].RecordedUtc;

                var idleFor = now - idleSince;
                if (idleFor < threshold)
                    continue;

                result.Add(new IdleUnitDto(
                    session.UnitId,
                    /* The guard's own login toggle wins; the wand is only the fallback. */
                    session.IsPatrolCar == true ? "car" : "guard",
                    session.GuardId, null,
                    newest.Latitude, newest.Longitude,
                    idleSince, (int)idleFor.TotalMinutes)
                {
                    Callsign = string.IsNullOrWhiteSpace(session.Callsign) ? null : session.Callsign
                });
            }

            if (result.Count == 0)
                return result;

            /* Resolve kind + guard names in two bounded lookups. */
            var unitIds = result.Select(r => r.UnitId).ToList();
            var kinds = await _db.PlatformSmartWands
                .Where(w => unitIds.Contains(w.Id))
                .Select(w => new { w.Id, IsCar = w.PatrolCarId != null })
                .ToDictionaryAsync(w => w.Id, w => w.IsCar, ct);
            var guardIds = result.Select(r => r.GuardId).Distinct().ToList();
            var names = await _db.PlatformGuards
                .Where(g => guardIds.Contains(g.Id))
                .ToDictionaryAsync(g => g.Id, g => g.Name, ct);

            return result
                .Select(r => r with
                {
                    /* Only fall back to the wand's PatrolCarId when the session made no
                       declaration (sessions opened before that was captured). */
                    Kind = r.Kind == "car" || (kinds.TryGetValue(r.UnitId, out var isCar) && isCar) ? "car" : "guard",
                    GuardName = names.TryGetValue(r.GuardId, out var name) ? name : null
                })
                .OrderByDescending(r => r.IdleMinutes)
                .ToList();
        }
    }
}
