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
