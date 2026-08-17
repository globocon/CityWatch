using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CityWatch.Tracking.Configuration;
using CityWatch.Tracking.Contracts;
using CityWatch.Tracking.Data;
using CityWatch.Tracking.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CityWatch.Tracking.Services
{
    public interface IIngestService
    {
        Task<IngestResponse> IngestAsync(PositionBatch batch, CancellationToken ct);
    }

    /// <summary>
    /// The §7.3 pipeline: authorise → rate-limit → validate → flag → live state → enqueue.
    /// The HTTP response never waits for a database write of points — the channel decouples
    /// ingest latency from storage, and the bounded writer drains it in bulk.
    /// </summary>
    public sealed class IngestService : IIngestService
    {
        /// <summary>Device clocks ahead of the server beyond this are rejected, not flagged —
        /// a future timestamp cannot be evidence of anything.</summary>
        private static readonly TimeSpan MaxFutureSkew = TimeSpan.FromMinutes(5);

        private readonly TrackingDbContext _db;
        private readonly ILiveStateStore _liveState;
        private readonly ChannelWriter<TrackPoint> _writer;
        private readonly UnitRateLimiter _rateLimiter;
        private readonly TrackingOptions _options;
        private readonly IModeCommandService? _commands;
        private readonly Geofencing.ISiteArrivalDetector? _arrivals;
        private readonly ILogger<IngestService> _logger;
        private readonly Func<DateTime> _utcNow;

        public IngestService(
            TrackingDbContext db,
            ILiveStateStore liveState,
            ChannelWriter<TrackPoint> writer,
            UnitRateLimiter rateLimiter,
            TrackingOptions options,
            ILogger<IngestService> logger,
            IModeCommandService? commands = null,
            Func<DateTime>? utcNow = null,
            Geofencing.ISiteArrivalDetector? arrivals = null)
        {
            _db = db;
            _liveState = liveState;
            _writer = writer;
            _rateLimiter = rateLimiter;
            _options = options;
            _commands = commands;
            _arrivals = arrivals;
            _logger = logger;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public async Task<IngestResponse> IngestAsync(PositionBatch batch, CancellationToken ct)
        {
            var serverUtc = _utcNow();
            var response = new IngestResponse
            {
                ServerUtc = serverUtc,
                Policy = _options.Policy,
                DesiredMode = TrackingMode.Normal   // M1.8 replaces this with the command channel
            };

            /* ---- Gate 1: the unit must be enrolled, enabled, and have consent on file.
               Consent is the structural guarantee (§13.5): IsEnabled without consent is
               refused exactly like no enrolment at all. ---- */
            var enrolment = await _db.TrackingUnitEnrolments
                .FirstOrDefaultAsync(e => e.UnitId == batch.UnitId, ct);
            if (enrolment is not { IsEnabled: true } || enrolment.ConsentRecordedUtc == null)
            {
                response.Rejected = batch.Points.Count;
                return response;   // 200 with zero accepted: the device backs off to Normal
            }

            /* ---- Gate 2: no session, no tracking (§6.5). ---- */
            var session = await _db.TrackingSessions
                .FirstOrDefaultAsync(s => s.Id == batch.SessionId && s.UnitId == batch.UnitId, ct);
            if (session is not { Status: "Active" })
            {
                response.Rejected = batch.Points.Count;
                /* The device's session was closed because another officer signed into this
                   unit: say so. Without the flag the superseded phone keeps uploading
                   rejected batches all shift while its officer believes they are tracked. */
                response.SessionSuperseded = session?.EndReason == "SupersededByNewSession";
                return response;
            }

            /* ---- Gate 3: a runaway device cannot flood the pipeline. ---- */
            if (!_rateLimiter.TryAcquire(batch.UnitId, serverUtc))
            {
                response.Rejected = batch.Points.Count;
                response.RetryAfterSeconds = 60;
                return response;
            }

            var previous = _liveState.Get(batch.UnitId);
            var geoFixes = new List<Geofencing.GeoFix>();

            foreach (var p in batch.Points)
            {
                ct.ThrowIfCancellationRequested();

                if (!IsAcceptable(p, serverUtc))
                {
                    response.Rejected++;
                    continue;
                }

                var flags = ComputeFlags(p, previous);
                var point = ToEntity(batch, p, flags, serverUtc);

                /* TryWrite on a DropOldest bounded channel fails only at shutdown. */
                if (!_writer.TryWrite(point))
                {
                    response.Rejected++;
                    continue;
                }

                response.Accepted++;

                /* Backfilled history must never drive the live picture (§6.4); the store
                   also rejects clock regressions on its own. */
                if (!p.Backfilled)
                {
                    var state = ToLiveState(batch, p, flags, serverUtc);
                    _liveState.Update(state);
                    previous = state;

                    /* A fix the ingest pipeline does not trust is not a fix the geofence
                       should draw a conclusion from: a 500 m accuracy reading sits "inside"
                       half the suburb. */
                    if ((flags & TrackPointFlags.LowAccuracy) == 0 &&
                        (flags & TrackPointFlags.Implausible) == 0 &&
                        (flags & TrackPointFlags.MockProvider) == 0)
                    {
                        geoFixes.Add(new Geofencing.GeoFix(p.Lat, p.Lon, p.Utc));
                    }
                }
            }

            /* One cheap UPDATE per batch, not per point: the reaper reads this. */
            if (response.Accepted > 0)
            {
                session.LastFixUtc = serverUtc;
                _db.TrackingSessions.Update(session);
                await _db.SaveChangesAsync(ct);
            }

            /* Site geofence (§5.1): decide whether this batch means the car arrived somewhere.
               Deliberately AFTER the session update and wrapped: an arrival alert is worth
               having, but never at the cost of rejecting positions that were already accepted
               and written. A failure here loses a notification, not evidence. */
            if (_arrivals != null && geoFixes.Count > 0)
            {
                try
                {
                    var isCar = session.IsPatrolCar ?? TrackingUnitKey.IsPosition(batch.UnitId);
                    await _arrivals.EvaluateAsync(batch.UnitId, session.Id, isCar, geoFixes, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Site geofence evaluation failed for unit {Unit}; positions were still accepted.",
                        batch.UnitId);
                }
            }

            /* Authoritative mode delivery on the device's own heartbeat (§5.3, D5): the
               batch's ack is applied and the current command comes back with the response.
               Push only ever accelerates this; it never replaces it. */
            if (_commands != null)
            {
                var resolution = await _commands.ResolveAsync(batch.UnitId, batch.CommandSeqSeen, ct);
                response.DesiredMode = resolution.DesiredMode;
                response.CommandSeq = resolution.CommandSeq;
                response.CommandTtlSeconds = resolution.TtlSecondsRemaining;
            }

            return response;
        }

        private bool IsAcceptable(PositionPoint p, DateTime serverUtc)
        {
            if (p.Lat == 0 && p.Lon == 0)
                return false;                                  // the null island fix
            if (p.Lat < -90 || p.Lat > 90 || p.Lon < -180 || p.Lon > 180)
                return false;                                  // not a coordinate at all
            if (p.Utc > serverUtc + MaxFutureSkew)
                return false;                                  // future timestamps are not evidence

            /* Configurable service envelope (defaults to Australia). Off by configuration
               when testing from elsewhere, or if the service ever operates outside it. */
            if (_options.EnforceServiceArea)
            {
                var area = _options.ServiceArea;
                if (p.Lat < area.MinLat || p.Lat > area.MaxLat ||
                    p.Lon < area.MinLon || p.Lon > area.MaxLon)
                    return false;
            }

            return true;
        }

        /// <summary>Flag, never drop (§13.6): a flagged anomaly is evidence; a dropped point
        /// is a gap that cannot be explained later.</summary>
        private TrackPointFlags ComputeFlags(PositionPoint p, UnitLiveState? previous)
        {
            var flags = TrackPointFlags.None;

            if (p.IsMock)
                flags |= TrackPointFlags.MockProvider;
            if (p.Backfilled)
                flags |= TrackPointFlags.Backfilled;
            if (p.AccuracyM is > 0 && p.AccuracyM > _options.MaxAcceptedAccuracyMetres)
                flags |= TrackPointFlags.LowAccuracy;

            if (previous != null && !p.Backfilled && p.Utc > previous.RecordedUtc)
            {
                var impliedKph = ImpliedSpeedKph(previous.Lat, previous.Lon, p.Lat, p.Lon,
                    (p.Utc - previous.RecordedUtc).TotalHours);
                if (impliedKph > _options.PlausibilityMaxSpeedKph)
                    flags |= TrackPointFlags.Implausible;
            }

            return flags;
        }

        /// <summary>Haversine, sufficient at patrol scale. Kept as a passthrough so existing
        /// tests keep their entry point; the maths lives in GeoMath (shared with segments).</summary>
        internal static double ImpliedSpeedKph(decimal lat1, decimal lon1, decimal lat2, decimal lon2, double hours)
            => GeoMath.ImpliedSpeedKph(lat1, lon1, lat2, lon2, hours);

        private static TrackPoint ToEntity(PositionBatch batch, PositionPoint p, TrackPointFlags flags, DateTime serverUtc)
            => new()
            {
                UnitId = batch.UnitId,
                SessionId = batch.SessionId,
                Seq = p.Seq,
                RecordedUtc = p.Utc,
                ReceivedUtc = serverUtc,
                Latitude = p.Lat,
                Longitude = p.Lon,
                SpeedKph = p.SpeedKph is { } s ? (short)Math.Clamp(s, short.MinValue, short.MaxValue) : null,
                HeadingDeg = p.HeadingDeg is { } h ? (short)Math.Clamp(h, 0, 359) : null,
                AccuracyM = p.AccuracyM is { } a ? (short)Math.Clamp(a, 0, short.MaxValue) : null,
                BatteryPct = p.BatteryPct,
                SourceType = (byte)ParseSource(p.Source),
                ModeAtCapture = (byte)SourceToMode(ParseSource(p.Source)),
                Flags = (byte)flags,
                AnchorTagUid = p.TagUid
            };

        private UnitLiveState ToLiveState(PositionBatch batch, PositionPoint p, TrackPointFlags flags, DateTime serverUtc)
        {
            /* Speed fallback (§Phase 2.3): device speed when given; otherwise implied from
               the previous live fix — only when the interval is sane and the result is
               plausible, and always marked derived. No value beats a misleading one. */
            short? speed = p.SpeedKph is { } s ? (short)Math.Clamp(s, short.MinValue, short.MaxValue) : null;
            var derived = false;
            var previous = _liveState.Get(batch.UnitId);
            if (speed == null && previous != null && !p.Backfilled &&
                (flags & TrackPointFlags.Implausible) == 0 &&
                (flags & TrackPointFlags.LowAccuracy) == 0)
            {
                var dtSec = (p.Utc - previous.RecordedUtc).TotalSeconds;
                if (dtSec is >= 3 and <= 180)
                {
                    var implied = ImpliedSpeedKph(previous.Lat, previous.Lon, p.Lat, p.Lon, dtSec / 3600.0);
                    if (implied <= _options.PlausibilityMaxSpeedKph)
                    {
                        speed = (short)Math.Round(implied);
                        derived = true;
                    }
                }
            }

            return new()
            {
                UnitId = batch.UnitId,
                SessionId = batch.SessionId,
                Lat = p.Lat,
                Lon = p.Lon,
                SpeedKph = speed,
                SpeedDerived = derived,
                HeadingDeg = p.HeadingDeg is { } h ? (short)Math.Clamp(h, 0, 359) : null,
                AccuracyM = p.AccuracyM is { } a ? (short)Math.Clamp(a, 0, short.MaxValue) : null,
                BatteryPct = p.BatteryPct,
                Mode = SourceToMode(ParseSource(p.Source)),
                Source = ParseSource(p.Source),
                Flags = flags,
                RecordedUtc = p.Utc,
                ReceivedUtc = serverUtc
            };
        }

        internal static TrackPointSource ParseSource(string? source) => source?.ToLowerInvariant() switch
        {
            "nfcanchor" => TrackPointSource.NfcAnchor,
            "live" => TrackPointSource.Live,
            "duress" => TrackPointSource.Duress,
            _ => TrackPointSource.Transit
        };

        private static TrackingMode SourceToMode(TrackPointSource source) => source switch
        {
            TrackPointSource.NfcAnchor => TrackingMode.Normal,
            TrackPointSource.Live => TrackingMode.Live,
            TrackPointSource.Duress => TrackingMode.Duress,
            _ => TrackingMode.Transit
        };
    }
}
