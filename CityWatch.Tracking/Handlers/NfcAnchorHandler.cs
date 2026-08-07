using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CityWatch.Events;
using CityWatch.Events.Events;
using CityWatch.Tracking.Contracts;
using CityWatch.Tracking.Data;
using CityWatch.Tracking.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CityWatch.Tracking.Handlers
{
    /// <summary>
    /// Turns an NFC scan into the highest-trust point in the system (§20.3). The scan is
    /// already committed by the platform before this runs — the anchor is derived evidence,
    /// and losing one is recoverable from the scan record itself.
    ///
    /// No active tracking session ⇒ no anchor. The scan's own GPS still lives on the
    /// platform's hit log; tracking simply has nothing to add for an untracked unit.
    /// </summary>
    public sealed class NfcAnchorHandler : IDomainEventHandler<NfcCheckpointScanned>
    {
        private readonly TrackingDbContext _db;
        private readonly Services.ILiveStateStore _liveState;
        private readonly Services.ISessionService _sessions;
        private readonly ChannelWriter<TrackPoint> _writer;
        private readonly ILogger<NfcAnchorHandler> _logger;

        public NfcAnchorHandler(TrackingDbContext db, Services.ILiveStateStore liveState,
            Services.ISessionService sessions, ChannelWriter<TrackPoint> writer,
            ILogger<NfcAnchorHandler> logger)
        {
            _db = db;
            _liveState = liveState;
            _sessions = sessions;
            _writer = writer;
            _logger = logger;
        }

        /// <summary>
        /// An in-car tag is one that belongs to the officer's own fleet site — the site they
        /// logged in to ("Citywatch M1 - Romeo Patrol Cars"). Client-site checkpoints belong
        /// to a different site. The "(in-car)" label is accepted as a secondary signal so a
        /// tag filed against the wrong site still behaves sensibly.
        /// </summary>
        internal static bool IsInCarTag(NfcCheckpointScanned e)
        {
            if (!string.IsNullOrWhiteSpace(e.LabelDescription) &&
                e.LabelDescription.IndexOf("in-car", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return e.LoggedInClientSiteId > 0 && e.ClientSiteId == e.LoggedInClientSiteId;
        }

        public async Task HandleAsync(NfcCheckpointScanned e, CancellationToken ct)
        {
            /* Find the session by GUARD, not by device. The scanning phone and the tracked
               unit are different things — the unit is a car or a person — and a scan may
               carry no wand id at all. The officer is the reliable link between the two. */
            if (e.GuardId is not { } guardId || guardId <= 0)
                return;

            var session = await _db.TrackingSessions
                .FirstOrDefaultAsync(s => s.GuardId == guardId && s.Status == "Active", ct);
            if (session == null)
                return;

            /* Update where the car is: in-car tag ⇒ departing (transit), site tag ⇒ arrived.
               This only labels the trail — GPS sampling is continuous either way, so a
               forgotten in-car scan can never lose a journey. Live scans only: a replayed
               offline scan must not rewrite the car's current position. */
            if (!e.IsOfflineRecord)
            {
                await _sessions.ApplyScanAsync(session.UnitId, e.ClientSiteId, e.TagSiteName,
                    IsInCarTag(e), e.OccurredUtc, ct);
            }

            if (!TryParseGps(e.GpsCoordinates, out var lat, out var lon))
            {
                _logger.LogDebug("Scan {Tag} on unit {Unit} carried no usable GPS; anchor skipped.",
                    e.TagUid, session.UnitId);
                return;
            }

            var flags = e.IsOfflineRecord ? TrackPointFlags.Backfilled : TrackPointFlags.None;
            var point = new TrackPoint
            {
                UnitId = session.UnitId,
                SessionId = session.Id,
                /* Server-generated anchors use negative sequence numbers so they can never
                   collide with the device's own positive Seq counter in the dedupe index. */
                Seq = -(int)(e.OccurredUtc.Ticks / TimeSpan.TicksPerMillisecond % int.MaxValue),
                RecordedUtc = e.OccurredUtc,
                ReceivedUtc = e.PublishedUtc == default ? DateTime.UtcNow : e.PublishedUtc,
                Latitude = lat,
                Longitude = lon,
                SourceType = (byte)TrackPointSource.NfcAnchor,
                ModeAtCapture = (byte)TrackingMode.Normal,
                Flags = (byte)flags,
                AnchorTagUid = e.TagUid
            };

            _writer.TryWrite(point);

            if (!e.IsOfflineRecord)
            {
                _liveState.Update(new Services.UnitLiveState
                {
                    UnitId = session.UnitId,
                    SessionId = session.Id,
                    Lat = lat,
                    Lon = lon,
                    Mode = TrackingMode.Normal,
                    Source = TrackPointSource.NfcAnchor,
                    Flags = flags,
                    RecordedUtc = e.OccurredUtc,
                    ReceivedUtc = point.ReceivedUtc
                });
            }
        }

        /// <summary>Platform GPS strings are "lat,lon"; empty and malformed are common
        /// (no fix, permission denied) and simply mean no anchor.</summary>
        internal static bool TryParseGps(string? gps, out decimal lat, out decimal lon)
        {
            lat = lon = 0;
            if (string.IsNullOrWhiteSpace(gps))
                return false;

            var parts = gps.Split(',');
            if (parts.Length != 2)
                return false;

            if (!decimal.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out lat) ||
                !decimal.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out lon))
                return false;

            return lat != 0 || lon != 0;
        }
    }
}
