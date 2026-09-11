namespace CityWatch.Tracking.Configuration
{
    /// <summary>
    /// Bound from the "Tracking" configuration section. Everything here is deployment
    /// configuration; per-customer and per-unit enablement is data (TrackingUnitEnrolment),
    /// so that enabling a customer is an INSERT, not a deployment.
    /// </summary>
    public sealed class TrackingOptions
    {
        public const string SectionName = "Tracking";

        /// <summary>
        /// Master switch. When false, AddCityWatchTracking registers nothing beyond this
        /// options object and MapCityWatchTracking maps nothing: "off" is indistinguishable
        /// from the module not being deployed. This is Level-1 rollback.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Phase 1 leader election: exactly one instance runs the ticking hosted services.
        /// Replaced by a distributed lock when Redis arrives in Phase 2 (D11).
        /// </summary>
        public bool IsLeaderInstance { get; set; } = true;

        /// <summary>Live Mode is a spotlight, not a floodlight (§5.3).</summary>
        public int MaxConcurrentLiveUnits { get; set; } = 10;

        /// <summary>A forgotten live session must expire on its own.</summary>
        public int LiveModeTtlSeconds { get; set; } = 900;

        /// <summary>Per-unit ingest rate limit; a runaway device cannot flood the pipeline.</summary>
        public int IngestRateLimitPerUnitPerMinute { get; set; } = 30;

        /// <summary>Ingest validation: fixes worse than this are flagged, not trusted.</summary>
        public int MaxAcceptedAccuracyMetres { get; set; } = 100;

        /// <summary>Implied speed above this between consecutive fixes flags a teleport.</summary>
        public int PlausibilityMaxSpeedKph { get; set; } = 250;

        /// <summary>
        /// Geographic envelope for accepted fixes. Defaults to Australia, which is where the
        /// service operates — a fix from the other side of the world is a device fault or a
        /// spoof, not data. It is CONFIGURABLE because hard-coded geography in a validator
        /// blocks legitimate cases: testing from another country, and any future expansion.
        /// Set EnforceServiceArea=false to accept fixes from anywhere.
        /// </summary>
        public bool EnforceServiceArea { get; set; } = true;

        public ServiceAreaOptions ServiceArea { get; set; } = new();

        public sealed class ServiceAreaOptions
        {
            public decimal MinLat { get; set; } = -45.5m;
            public decimal MaxLat { get; set; } = -8.8m;
            public decimal MinLon { get; set; } = 111.0m;
            public decimal MaxLon { get; set; } = 156.5m;
        }

        /// <summary>A unit that has stayed within this radius counts as sitting in place.</summary>
        public int IdleRadiusM { get; set; } = 75;

        /// <summary>Default time-in-place before a unit appears on the idle list.</summary>
        public int IdleThresholdMinutes { get; set; } = 15;

        public RetentionOptions RetentionDays { get; set; } = new();

        public SamplingPolicyOptions Policy { get; set; } = new();

        /// <summary>Remote nudge via FCM. The accelerator, never the guarantee (§5.3
        /// discipline): a nudge only "succeeds" when a fresh position arrives on the
        /// ingest path. Unset ServiceAccountJsonPath ⇒ a no-op sender is registered and
        /// /ping answers with a machine-readable refusal.</summary>
        public FcmOptions Fcm { get; set; } = new();

        public sealed class FcmOptions
        {
            /// <summary>Absolute path to the Firebase service-account JSON on the SERVER,
            /// outside the site folder and outside source control. Never committed.</summary>
            public string? ServiceAccountJsonPath { get; set; }

            /// <summary>Repeated pings of one unit inside this window are refused — this
            /// protects Android's high-priority delivery quota, not just the database.</summary>
            public int PingCooldownSeconds { get; set; } = 30;

            /// <summary>Repeated custom messages to one unit inside this window are refused
            /// (double-click guard); a broadcast SKIPS cooled-down units instead of failing.</summary>
            public int MessageCooldownSeconds { get; set; } = 5;
        }

        /// <summary>Abandoned-session expiry (§13.5 hygiene). A login nobody closed must not
        /// haunt the live map as a permanently-stationary unit the next day.</summary>
        public ReaperOptions Reaper { get; set; } = new();

        public sealed class ReaperOptions
        {
            public bool Enabled { get; set; } = true;

            /// <summary>A session with no point RECEIVED for this long is abandoned. Longer
            /// than any real shift on purpose: mid-shift quiet spells must survive it.</summary>
            public int StaleAfterHours { get; set; } = 12;

            /// <summary>How often the leader sweeps.</summary>
            public int SweepMinutes { get; set; } = 15;
        }

        /// <summary>GPS site-arrival detection. Without it "arrived at site" depends entirely
        /// on an officer remembering to tag, so a car can sit at a site all night while the
        /// control room reads "in transit".</summary>
        public SiteGeofenceOptions SiteGeofence { get; set; } = new();

        public sealed class SiteGeofenceOptions
        {
            public bool Enabled { get; set; } = true;

            /// <summary>The NFC scan is the source of truth (17 Aug decision): site tag =
            /// entered, in-car tag = left. GPS dwell detection stays implemented behind this
            /// flag for sites where tagging is impractical, but it is OFF by default — the
            /// scan is deliberate, unambiguous evidence; a radius calculation is not.</summary>
            public bool UseGpsDetection { get; set; } = false;

            /// <summary>Crossing INTO this radius of a site's coordinate opens a candidate
            /// visit. Sites are recorded as a single point, so this has to cover the whole
            /// property — 150 m is a large industrial block, not a GPS tolerance.</summary>
            public int EnterRadiusM { get; set; } = 150;

            /// <summary>Leaving is only believed past this LARGER radius. The gap between the
            /// two is deliberate hysteresis: without it a fix wobbling across a single
            /// boundary produces arrive/depart/arrive all night.</summary>
            public int ExitRadiusM { get; set; } = 250;

            /// <summary>How long the unit must stay inside before the arrival is announced.
            /// This is what separates "arrived" from "drove past on the main road" — at
            /// 60 km/h a car clears a 150 m radius in about 18 seconds.</summary>
            public int DwellSeconds { get; set; } = 120;

            /// <summary>Sites move rarely; the catalogue is re-read on this interval rather
            /// than per fix (about 800 active sites carry coordinates).</summary>
            public int CatalogueRefreshMinutes { get; set; } = 15;

            /// <summary>Patrol cars only, by default. A guard posted at a site is stationary
            /// there for a whole shift — announcing that as an arrival is noise, and noise is
            /// what makes an alert feed get ignored.</summary>
            public bool CarsOnly { get; set; } = true;

            /// <summary>How far back the control room's bell reads arrivals on load.</summary>
            public int FeedHours { get; set; } = 12;
        }

        /// <summary>The Insights drawer (analytics plan A1). Its own switch inside the pack:
        /// analytics can be turned off while tracking stays up, and vice versa the master
        /// Enabled=false removes it with everything else. Default ON — it is a passive,
        /// read-only layer that costs nothing until an operator opens the drawer.</summary>
        public AnalyticsOptions Analytics { get; set; } = new();

        public sealed class AnalyticsOptions
        {
            public bool Enabled { get; set; } = true;
        }

        public GeocodingOptions Geocoding { get; set; } = new();

        /// <summary>Reverse geocoding (§Phase 2.1). The cache carries the load; the provider
        /// is a trickle. Disabled ⇒ the address endpoint answers null and the UI falls back
        /// to site name / coordinates — nothing else changes.</summary>
        public sealed class GeocodingOptions
        {
            public bool Enabled { get; set; } = true;

            /// <summary>Provider endpoint. Nominatim-compatible.</summary>
            public string BaseUrl { get; set; } = "https://nominatim.openstreetmap.org/";

            /// <summary>Provider policy floor — Nominatim asks for ≤1 req/s.</summary>
            public int MinIntervalMs { get; set; } = 1100;

            /// <summary>How long a resolved address stays true enough. Streets rarely move.</summary>
            public int CacheDays { get; set; } = 45;

            /// <summary>How long a FAILED lookup is remembered before one retry is allowed.</summary>
            public int FailureRetryMinutes { get; set; } = 30;
        }

        public sealed class RetentionOptions
        {
            /// <summary>Raw points kept hot. Confirm against contractual requirements (D12).</summary>
            public int Points { get; set; } = 90;

            /// <summary>Compressed archive partition window.</summary>
            public int Archive { get; set; } = 365;

            /// <summary>Segments are the evidentiary record: 7 years.</summary>
            public int Segments { get; set; } = 2555;
        }

        /// <summary>
        /// Server-pushed sampling thresholds (§5.2). Returned to the device on every ingest
        /// response so tuning never requires an app-store release.
        /// </summary>
        public sealed class SamplingPolicyOptions
        {
            public int TransitSteadySec { get; set; } = 10;
            public int TransitManoeuvreSec { get; set; } = 4;
            public int StationarySec { get; set; } = 60;
            public int OnSiteSec { get; set; } = 30;
            public int ApproachingSiteSec { get; set; } = 5;
            public int LiveModeSec { get; set; } = 3;
            public int DuressSec { get; set; } = 2;
            public int DistanceFilterM { get; set; } = 25;
            public int UploadBatchSec { get; set; } = 60;
            public int LiveUploadBatchSec { get; set; } = 5;
        }
    }
}
