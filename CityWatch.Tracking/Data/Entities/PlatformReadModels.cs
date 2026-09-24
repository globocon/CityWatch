using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Tracking.Data.Entities
{
    /* Read-only projections of two PLATFORM tables, mapped column-subset only.
       This is how the pack resolves unit kind (car vs guard) and guard names without a
       project reference to CityWatch.Data — the same one-way boundary §13.3 uses for scope.
       Nothing here is ever written: the platform owns these tables; the pack reads them. */

    /// <summary>ClientSiteSmartWands, read-only. PatrolCarId decides the map symbol:
    /// a wand allocated to a patrol car renders as a car, anything else as a guard.</summary>
    [Table("ClientSiteSmartWands")]
    public class PlatformSmartWand
    {
        [Key]
        public int Id { get; set; }

        public int ClientSiteId { get; set; }

        public int? PatrolCarId { get; set; }

        public bool IsDeleted { get; set; }

        /// <summary>The platform's "SmartWandId" column is the wand's display name
        /// ("Dell 5430"), not a key — mapped for the analytics wand card (A2).</summary>
        [Column("SmartWandId")]
        public string? WandName { get; set; }
    }

    /// <summary>Guards, read-only. Identity for the control-room display: with a hundred
    /// Muhammads on the books a name alone identifies nobody — the licence does (#153 Part 2).
    /// The HR pin is deliberately NOT mapped: it is an HR credential, not an identity.</summary>
    [Table("Guards")]
    public class PlatformGuard
    {
        [Key]
        public int Id { get; set; }

        public string? Name { get; set; }

        /// <summary>Security licence number — the HR screen's "License No (Primary)".</summary>
        public string? SecurityNo { get; set; }

        /// <summary>Issuing state of the licence ("VIC") — a licence number means
        /// nothing without it.</summary>
        public string? State { get; set; }

        public string? Mobile { get; set; }

        public string? Email { get; set; }
    }

    /// <summary>GuardMobileAppVersions, read-only (P4#153): the app build each guard's phone
    /// last reported at login. The table ships with DbScript 371 — every reader must tolerate
    /// its absence, because an older database still has to answer.</summary>
    [Table("GuardMobileAppVersions")]
    public class PlatformGuardAppVersion
    {
        [Key]
        public int Id { get; set; }

        public int GuardId { get; set; }

        /// <summary>"1.54.3" — exactly as the APK reports itself.</summary>
        public string? AppVersion { get; set; }

        public string? Platform { get; set; }

        public System.DateTime LastSeen { get; set; }
    }

    /// <summary>ClientSiteDuress, read-only. THE truth table for duress: raising the alarm
    /// inserts rows (and only then publishes DuressActivated); the control room deactivating
    /// it DELETES them. Tracking keeps a unit in Duress Mode exactly as long as a row backs
    /// it — a command that outlives its rows is a stuck alarm, not an emergency.</summary>
    [Table("ClientSiteDuress")]
    public class PlatformClientSiteDuress
    {
        [Key]
        public int Id { get; set; }

        public int ClientSiteId { get; set; }

        public bool IsEnabled { get; set; }

        /// <summary>The guard who raised the alarm — the association tracking mirrors,
        /// because DuressActivated escalates that guard's active session.</summary>
        public int EnabledBy { get; set; }
    }

    /// <summary>ClientSiteSmartWandTagsHitLogs, read-only (analytics A1). Every NFC scan the
    /// platform has recorded — written by the scanner path even when no tracking session is
    /// running, which is what makes it the complete record of wand activity. Column subset:
    /// the drawer counts and groups; it never needs the tag label or GPS text here.</summary>
    [Table("ClientSiteSmartWandTagsHitLogs")]
    public class PlatformWandScan
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Nullable in the wild — PCAR logins routinely select no wand.</summary>
        public int? SmartWandId { get; set; }

        /// <summary>The physical tag scanned — the join key to <c>ClientSiteSmartWandTags.UId</c>.
        /// How a hit is matched to a specific required FQ tag (#153 FQ scan summary).</summary>
        public string? TagUId { get; set; }

        public int LoggedInGuardId { get; set; }

        public int LoggedInClientSiteId { get; set; }

        /// <summary>The site the scanned tag belongs to — the site the scan is evidence FOR.</summary>
        public int? TagLinkedClientSiteId { get; set; }

        public System.DateTime HitUtcDateTime { get; set; }
    }

    /// <summary>ClientSiteSmartWandTags, read-only (#153 FQ scan summary). The site's checkpoint
    /// catalogue — one row per installed tag. The REQUIRED FQ set for a site is
    /// <c>IsDeleted = 0 AND FqBypass = 0</c>: FqBypass marks a tag deliberately excluded from the
    /// round (decommissioned or optional), so it is neither required nor counted. A hit joins to
    /// a tag by <see cref="UId"/>, which is what makes the summary guard-independent — the tag is
    /// scanned on the strength of any hit on its UId, whoever's wand made it.</summary>
    [Table("ClientSiteSmartWandTags")]
    public class PlatformWandTag
    {
        [Key]
        public int Id { get; set; }

        public int ClientSiteId { get; set; }

        /// <summary>The physical NFC/BLE tag id — the join key to a hit log's TagUId.</summary>
        public string? UId { get; set; }

        /// <summary>The checkpoint's human name ("Point 1 - Front ADMIN Door").</summary>
        public string? LabelDescription { get; set; }

        /// <summary>True = excluded from the required FQ round — not required, not counted.</summary>
        public bool FqBypass { get; set; }

        public bool IsDeleted { get; set; }
    }

    /// <summary>IncidentReportPositions, read-only. THE CAR catalogue: a patrol-car
    /// Position is a tracked unit's identity (unit key = Id + 2,000,000). Read at
    /// session/start so the CALLSIGN can name the car authoritatively — phones
    /// auto-restore a stale saved Position, and six Romeo cars all keyed to the one
    /// old shared position collapsed to a single map marker (25 Aug 2026).</summary>
    [Table("IncidentReportPositions")]
    public class PlatformPosition
    {
        [Key]
        public int Id { get; set; }

        public string? Name { get; set; }

        public bool IsPatrolCar { get; set; }
    }

    /// <summary>ClientSiteKpiSettings, read-only (analytics A4). MinPatrolFreq is the
    /// AGREED patrol frequency — the number the weekly grid holds each day against.
    /// The same field the map's FQ badge reads.</summary>
    [Table("ClientSiteKpiSettings")]
    public class PlatformSiteKpi
    {
        [Key]
        public int Id { get; set; }

        public int ClientSiteId { get; set; }

        public int? MinPatrolFreq { get; set; }
    }

    /// <summary>DailyWandFq, read-only (analytics A4): traditional-wand patrol rounds per
    /// site per LOCAL date — the platform's own durable per-day rounds record.</summary>
    [Table("DailyWandFq")]
    public class PlatformDailyWandFq
    {
        [Key]
        public int Id { get; set; }

        public int ClientSiteId { get; set; }

        public int Fq { get; set; }

        public System.DateTime FqDate { get; set; }
    }

    /// <summary>SmartWandScanGuardHistory, read-only (analytics A4): one row per COMPLETED
    /// smart-wand inspection round, with LOCAL start time — the historical record behind
    /// the board's CompletedRounds. Counting rows per guard per day mirrors the board.</summary>
    [Table("SmartWandScanGuardHistory")]
    public class PlatformWandRound
    {
        [Key]
        public int Id { get; set; }

        public int ClientSiteId { get; set; }

        public int GuardId { get; set; }

        public System.DateTime InspectionStartDatetimeLocal { get; set; }
    }

    /// <summary>ClientSites, read-only. The geofence catalogue: where the sites ARE, so an
    /// arrival can be detected from GPS instead of waiting for a tag that may never be
    /// scanned. Gps is the platform's own free-text "lat,lon" column — parsed defensively,
    /// never written back.</summary>
    [Table("ClientSites")]
    public class PlatformClientSite
    {
        [Key]
        public int Id { get; set; }

        public string? Name { get; set; }

        /// <summary>"-37.81805,145.1849757". Free text: blank, malformed and out-of-range
        /// values all exist in the wild and are simply skipped.</summary>
        public string? Gps { get; set; }

        public bool IsActive { get; set; }
    }
}
