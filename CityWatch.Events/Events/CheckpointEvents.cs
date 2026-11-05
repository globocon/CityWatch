using System;

namespace CityWatch.Events.Events
{
    /// <summary>
    /// An NFC checkpoint tag was scanned. The most important event on the bus.
    /// </summary>
    /// <remarks>
    /// A scan already establishes officer, vehicle, checkpoint, site and time with physical
    /// certainty, and the existing scan path already carries GPS coordinates. That makes this event
    /// a complete, self-verifying anchor: the tracking pack writes it as a TrackPoint with
    /// SourceType = NfcAnchor and needs no continuous location capture to do so.
    ///
    /// The timezone block mirrors PcarVisitHistory. It travels with the event because a subscriber
    /// must be able to render a site-local time without querying back into the platform's data
    /// layer — which would reintroduce exactly the coupling the bus removes.
    /// </remarks>
    public sealed class NfcCheckpointScanned : DomainEvent
    {
        public NfcCheckpointScanned(
            int smartWandId,
            string tagUid,
            int clientSiteId,
            int? guardId,
            int? loginUserId,
            string? gpsCoordinates,
            DateTime occurredUtc,
            int scanningType,
            bool isOfflineRecord)
            : base(occurredUtc)
        {
            SmartWandId = smartWandId;
            TagUid = tagUid;
            ClientSiteId = clientSiteId;
            GuardId = guardId;
            LoginUserId = loginUserId;
            GpsCoordinates = gpsCoordinates;
            ScanningType = scanningType;
            IsOfflineRecord = isOfflineRecord;
        }

        public int SmartWandId { get; }

        public string TagUid { get; }

        public int ClientSiteId { get; }

        public int? GuardId { get; }

        public int? LoginUserId { get; }

        /// <summary>
        /// Raw "lat,lon" as captured by the app. Kept in the platform's existing string form here
        /// because that is what the scan path holds; the tracking pack parses it into typed columns
        /// on write. Null when the device had no fix.
        /// </summary>
        public string? GpsCoordinates { get; }

        /// <summary>Existing ScanningType enum value, passed through untranslated.</summary>
        public int ScanningType { get; }

        /// <summary>
        /// The site the officer LOGGED IN to (their fleet base, e.g. "Citywatch M1 - Romeo
        /// Patrol Cars"). When the scanned tag belongs to this same site it is an in-car tag —
        /// the officer is back in the vehicle — rather than a checkpoint at a client site.
        /// </summary>
        public int LoggedInClientSiteId { get; init; }

        /// <summary>Tag label, e.g. "Romeo 03 (in-car)". Used as a secondary signal.</summary>
        public string? LabelDescription { get; init; }

        /// <summary>Name of the site the tag belongs to, for display on the map.</summary>
        public string? TagSiteName { get; init; }

        /// <summary>
        /// True when this scan is being replayed from the device's offline cache. Subscribers
        /// should not treat a backfilled scan as a live position.
        /// </summary>
        public bool IsOfflineRecord { get; }

        /* Timezone context, mirroring PcarVisitHistory. Optional: set where the scan path has it. */
        public DateTime? EventDateTimeLocal { get; init; }
        public DateTimeOffset? EventDateTimeLocalWithOffset { get; init; }
        public string? EventDateTimeZone { get; init; }
        public string? EventDateTimeZoneShort { get; init; }
        public int? EventDateTimeUtcOffsetMinute { get; init; }
    }
}
