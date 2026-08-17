using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CityWatch.Tracking.Contracts;
using CityWatch.Tracking.Data;
using CityWatch.Tracking.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CityWatch.Tracking.Services
{
    public interface ISegmentBuilder
    {
        /// <summary>Rolls a closed session's points into TrackSegment legs. Idempotent —
        /// rebuilding replaces the session's existing segments.</summary>
        Task<int> BuildForSessionAsync(Guid sessionId, CancellationToken ct);
    }

    /// <summary>
    /// The §8.3 roll-up. Legs are split at NFC anchor scans — the checkpoint spine — so each
    /// segment is "from this verified touch to the next one", which is exactly the shape
    /// Verified Proof of Patrol reconciles in Phase 3. Reporting reads these rows and never
    /// the point stream.
    /// </summary>
    public sealed class SegmentBuilder : ISegmentBuilder
    {
        private readonly TrackingDbContext _db;
        private readonly ILogger<SegmentBuilder> _logger;

        public SegmentBuilder(TrackingDbContext db, ILogger<SegmentBuilder> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<int> BuildForSessionAsync(Guid sessionId, CancellationToken ct)
        {
            var points = await _db.TrackPoints
                .Where(p => p.SessionId == sessionId)
                .OrderBy(p => p.RecordedUtc).ThenBy(p => p.Seq)
                .ToListAsync(ct);
            if (points.Count < 2)
                return 0;

            /* Rebuild: segments are derived data; the points are the record. */
            var existing = await _db.TrackSegments.Where(s => s.SessionId == sessionId).ToListAsync(ct);
            if (existing.Count > 0)
                _db.TrackSegments.RemoveRange(existing);

            var legs = SplitAtAnchors(points);
            var segments = legs
                .Where(leg => leg.Count >= 2)
                .Select(leg => BuildSegment(sessionId, leg))
                .Where(s => s.DurationSec > 0)
                .ToList();

            _db.TrackSegments.AddRange(segments);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Session {Session}: {Points} points rolled into {Segments} segments.",
                sessionId, points.Count, segments.Count);
            return segments.Count;
        }

        /// <summary>Anchor points are both the end of one leg and the start of the next.</summary>
        internal static List<List<TrackPoint>> SplitAtAnchors(List<TrackPoint> points)
        {
            var legs = new List<List<TrackPoint>>();
            var current = new List<TrackPoint>();

            foreach (var point in points)
            {
                current.Add(point);
                if (point.SourceType == (byte)TrackPointSource.NfcAnchor && current.Count > 1)
                {
                    legs.Add(current);
                    current = new List<TrackPoint> { point };
                }
            }
            if (current.Count > 1)
                legs.Add(current);

            return legs;
        }

        private static TrackSegment BuildSegment(Guid sessionId, List<TrackPoint> leg)
        {
            /* Implausible points stay in the evidentiary record but must not inflate the
               distance a patrol is credited with — and that means bridging OVER them:
               skipping only the hop into a teleport still counts the (equally bogus) hop
               back out. Distance accumulates between consecutive PLAUSIBLE fixes. */
            double distanceKm = 0;
            TrackPoint? lastPlausible = null;
            foreach (var point in leg)
            {
                if ((point.Flags & (byte)TrackPointFlags.Implausible) != 0)
                    continue;
                if (lastPlausible != null)
                {
                    distanceKm += GeoMath.HaversineKm(
                        lastPlausible.Latitude, lastPlausible.Longitude, point.Latitude, point.Longitude);
                }
                lastPlausible = point;
            }

            var start = leg[0];
            var end = leg[^1];
            var duration = (int)Math.Max(0, (end.RecordedUtc - start.RecordedUtc).TotalSeconds);
            var speeds = leg.Where(p => p.SpeedKph.HasValue).Select(p => p.SpeedKph!.Value).ToList();

            byte aggregateFlags = 0;
            foreach (var point in leg)
                aggregateFlags |= point.Flags;

            return new TrackSegment
            {
                UnitId = start.UnitId,
                SessionId = sessionId,
                StartUtc = start.RecordedUtc,
                EndUtc = end.RecordedUtc,
                DistanceM = (int)Math.Round(distanceKm * 1000),
                DurationSec = duration,
                MaxSpeedKph = speeds.Count > 0 ? speeds.Max() : null,
                AvgSpeedKph = duration > 0 ? (short?)Math.Round(distanceKm / (duration / 3600.0)) : null,
                PointCount = leg.Count,
                AnchorScanCount = leg.Count(p => p.SourceType == (byte)TrackPointSource.NfcAnchor),
                /* FromSiteId/ToSiteId resolve from anchor tags in the Phase 3 reconciliation
                   pass; the leg boundaries themselves are already anchor-true. */
                Flags = aggregateFlags
            };
        }
    }
}
