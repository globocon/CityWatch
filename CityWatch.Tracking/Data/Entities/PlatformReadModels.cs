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

        public int LoggedInGuardId { get; set; }

        public int LoggedInClientSiteId { get; set; }

        /// <summary>The site the scanned tag belongs to — the site the scan is evidence FOR.</summary>
        public int? TagLinkedClientSiteId { get; set; }

        public System.DateTime HitUtcDateTime { get; set; }
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
