using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    /// <summary>
    /// Patrol Randomisation switch for a site (DbScript/387). One row per site; the windows are in
    /// <see cref="Schedules"/>. Only applies in Standard (STND) touring mode; see
    /// PatrolRandomisationHelper for the rules.
    /// </summary>
    [Table("ClientSitePatrolRandomisationSettings")]
    public class ClientSitePatrolRandomisationSetting
    {
        [Key]
        public int Id { get; set; }
        public int ClientSiteId { get; set; }
        public bool IsEnabled { get; set; }
        public DateTime LastUpdatedUtc { get; set; }
        public int? LastUpdatedByUserId { get; set; }
        public int? LastUpdatedByGuardId { get; set; }

        public List<ClientSitePatrolRandomisationSchedule> Schedules { get; set; } = new();
    }

    /// <summary>One day's randomisation window, site local time (DbScript/387).</summary>
    [Table("ClientSitePatrolRandomisationSchedules")]
    public class ClientSitePatrolRandomisationSchedule
    {
        [Key]
        public int Id { get; set; }
        public int SettingId { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        /// <summary>Exclusive.</summary>
        public TimeSpan EndTime { get; set; }

        [ForeignKey(nameof(SettingId))]
        public ClientSitePatrolRandomisationSetting Setting { get; set; }
    }

    public enum PatrolRandomisationLogStatus : byte
    {
        /// <summary>Instruction given; the guard has not struck a tag yet.</summary>
        Pending = 0,
        /// <summary>The guard's next strike was the tag they were sent to.</summary>
        Completed = 1,
        /// <summary>The guard's next strike was a different tag; the assigned one was skipped.</summary>
        Skipped = 2,
        /// <summary>The window closed before the guard struck any tag.</summary>
        Missed = 3,
        /// <summary>Randomisation was switched off or deleted for the site.</summary>
        Cancelled = 4,
        /// <summary>A strike in the window with no instruction to follow (e.g. an offline scan synced later).</summary>
        Unguided = 5,
        /// <summary>The guard tapped "Tour: RNDM" to skip a checkpoint that is missing, faulty or blocked.</summary>
        Bypassed = 6
    }

    /// <summary>
    /// Patrol Randomisation scan log (DbScript/387): one instruction handed to a guard and the
    /// strike that answered it. Times are held in UTC and site local.
    /// </summary>
    [Table("ClientSitePatrolRandomisationLogs")]
    public class ClientSitePatrolRandomisationLog
    {
        [Key]
        public int Id { get; set; }
        public int ClientSiteId { get; set; }
        public int GuardId { get; set; }
        public int? UserId { get; set; }
        public int? SmartWandId { get; set; }

        public DateTime BusinessDate { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public DateTime WindowStartLocal { get; set; }
        public DateTime WindowEndLocal { get; set; }
        public DateTime WindowEndUtc { get; set; }
        public string SiteUtcOffset { get; set; }

        public string FromTagUId { get; set; }
        public string FromTagLabel { get; set; }
        public int? FromHitLogId { get; set; }
        public DateTime? FromScanUtc { get; set; }
        public DateTime? FromScanLocal { get; set; }
        public bool? FromIsOffline { get; set; }

        public int? AssignedTagId { get; set; }
        public string AssignedTagUId { get; set; }
        public string AssignedTagLabel { get; set; }
        public DateTime? AssignedUtc { get; set; }
        public DateTime? AssignedLocal { get; set; }

        public PatrolRandomisationLogStatus Status { get; set; }

        public string ScannedTagUId { get; set; }
        public string ScannedTagLabel { get; set; }
        public int? ScannedHitLogId { get; set; }
        public DateTime? ScannedUtc { get; set; }
        public DateTime? ScannedLocal { get; set; }
        public bool? ScannedIsOffline { get; set; }
        public int? ScannedUserId { get; set; }

        public DateTime? ResolvedUtc { get; set; }
        public DateTime CreatedUtc { get; set; }
    }

    /// <summary>A wand strike as the randomisation service sees it, online or offline.</summary>
    public class PatrolRandomisationStrike
    {
        public int ClientSiteId { get; set; }
        public int GuardId { get; set; }
        public int? UserId { get; set; }
        public int? SmartWandId { get; set; }
        public string TagUId { get; set; }
        public int? HitLogId { get; set; }
        public DateTime HitUtc { get; set; }
        public bool IsOffline { get; set; }

        /// <summary>The "GoTo" checkpoint the handset showed when this tag was struck (null from older builds).</summary>
        public string ExpectedTagUId { get; set; }
        public DateTime? ExpectedAssignedUtc { get; set; }

        /// <summary>Offline only: the checkpoint the handset picked itself after this strike.</summary>
        public string NextTagUId { get; set; }
        public DateTime? NextAssignedUtc { get; set; }
    }

    /// <summary>
    /// The guard tapped "Tour: RNDM" to bypass the checkpoint on screen (missing, faulty or blocked).
    /// Posted live, or replayed by the handset after an offline spell with the original time.
    /// </summary>
    public class PatrolRandomisationBypassRequest
    {
        public int ClientSiteId { get; set; }
        public int GuardId { get; set; }
        public int UserId { get; set; }
        public int? SmartWandId { get; set; }
        /// <summary>The checkpoint being bypassed.</summary>
        public string TagUId { get; set; }
        public DateTime? TagAssignedUtc { get; set; }
        public DateTime BypassUtc { get; set; }
        public bool IsOffline { get; set; }
        public string Gps { get; set; }
        /// <summary>Offline replay: the checkpoint the handset moved on to.</summary>
        public string NextTagUId { get; set; }
        public DateTime? NextAssignedUtc { get; set; }

        // Handset time, for the logbook entry (same fields the scan posts carry).
        public DateTime? EventDateTimeLocal { get; set; }
        public DateTimeOffset? EventDateTimeLocalWithOffset { get; set; }
        public string EventDateTimeZone { get; set; }
        public string EventDateTimeZoneShort { get; set; }
        public int? EventDateTimeUtcOffsetMinute { get; set; }
    }

    /// <summary>Filters for the Randomisation report (Audit Site Log > Randomisation). Multi-values are comma separated.</summary>
    public class PatrolRandomisationReportRequest
    {
        public DateTime LogFromDate { get; set; }
        public DateTime LogToDate { get; set; }
        public string ClientSiteId { get; set; }
        public int[] ClientSiteIds => SplitInts(ClientSiteId);
        /// <summary>PatrolRandomisationLogStatus values; empty means all.</summary>
        public string Status { get; set; }
        public int[] Statuses => SplitInts(Status);
        /// <summary>"", "online" or "offline".</summary>
        public string ScanMode { get; set; }
        public string GuardName { get; set; }
        public string GuardSecurityNo { get; set; }

        private static int[] SplitInts(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Array.Empty<int>();
            var list = new List<int>();
            foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                if (int.TryParse(part, out var id)) list.Add(id);
            return list.ToArray();
        }
    }

    /// <summary>One row of the Randomisation report.</summary>
    public class PatrolRandomisationReportViewModel
    {
        public int Id { get; set; }
        public int ClientSiteId { get; set; }
        public string SiteName { get; set; }
        public DateTime BusinessDate { get; set; }
        public string BusinessDateText => BusinessDate.ToString("dd MMM yyyy");
        public string DayName { get; set; }
        public string WindowText { get; set; }
        public string GuardName { get; set; }
        public string GuardSecurityNo { get; set; }
        public string GuardDisplay => string.IsNullOrEmpty(GuardSecurityNo) ? GuardName : $"{GuardName} [{GuardSecurityNo}]";
        public string UserName { get; set; }
        public int? SmartWandId { get; set; }

        public string FromTagUId { get; set; }
        public string FromTagLabel { get; set; }
        public DateTime? FromScanLocal { get; set; }
        public string FromScanLocalText => FromScanLocal?.ToString("HH:mm:ss");

        public string AssignedTagUId { get; set; }
        public string AssignedTagLabel { get; set; }
        public DateTime? AssignedLocal { get; set; }
        public string AssignedLocalText => AssignedLocal?.ToString("HH:mm:ss");

        public string ScannedTagUId { get; set; }
        public string ScannedTagLabel { get; set; }
        public DateTime? ScannedLocal { get; set; }
        public string ScannedLocalText => ScannedLocal?.ToString("HH:mm:ss");
        public bool? ScannedIsOffline { get; set; }
        public string ScanMode => ScannedIsOffline == null ? "" : ScannedIsOffline.Value ? "Offline" : "Online";

        /// <summary>Minutes from instruction to strike.</summary>
        public double? ResponseMinutes { get; set; }
        public string ResponseText => ResponseMinutes.HasValue ? TimeSpan.FromMinutes(ResponseMinutes.Value).ToString(@"hh\:mm\:ss") : "";

        public PatrolRandomisationLogStatus Status { get; set; }
        public string StatusText => Status.ToString();
        public string SiteUtcOffset { get; set; }
        public DateTime SortKey { get; set; }
    }
}
