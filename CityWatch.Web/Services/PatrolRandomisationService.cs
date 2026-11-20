using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Web.Services
{
    /// <summary>
    /// What the handset is told about Patrol Randomisation, either in the scan response, from
    /// api/Scanner/GetPatrolRandomisationStatus, or pushed over SignalR
    /// ("PatrolRandomisationNextCheckpoint"). Plain JSON, so Android and iOS read it the same way.
    /// </summary>
    public class PatrolRandomisationInstruction
    {
        public int ClientSiteId { get; set; }
        public int GuardId { get; set; }

        /// <summary>The site has randomisation switched on (STND mode), whatever the time.</summary>
        public bool IsConfigured { get; set; }

        /// <summary>A window is open right now: the guard should follow NextCheckpoint.</summary>
        public bool IsActive { get; set; }

        /// <summary>The open window in site-local time, and its end in UTC for a countdown.</summary>
        public DateTime? WindowStartLocal { get; set; }
        public DateTime? WindowEndLocal { get; set; }
        public DateTime? WindowEndUtc { get; set; }

        /// <summary>The week's windows, e.g. "Mon 02:00-03:59, Thu 10:00-12:00 (site time)".</summary>
        public string ScheduleDescription { get; set; }

        /// <summary>Null when not active, or when the site has too few tags to pick from.</summary>
        public PatrolRandomisationCheckpoint NextCheckpoint { get; set; }

        /// <summary>
        /// How the strike that produced this instruction compared with the previous one:
        /// "Completed" (the guard struck the tag they were sent to), "Skipped", or null.
        /// </summary>
        public string PreviousCheckpointResult { get; set; }

        public string Message { get; set; }
    }

    public class PatrolRandomisationCheckpoint
    {
        /// <summary>ClientSitePatrolRandomisationLogs.Id.</summary>
        public int AssignmentId { get; set; }
        public int TagId { get; set; }
        public string TagUId { get; set; }
        public string Label { get; set; }
        public int TagsTypeId { get; set; }
        public DateTime AssignedUtc { get; set; }
    }

    public interface IPatrolRandomisationService
    {
        /// <summary>
        /// Called after every strike of one of the site's own tags, online or offline-synced.
        /// Records it in ClientSitePatrolRandomisationLogs: it answers the guard's pending
        /// instruction (Completed / Skipped), or is logged as Unguided when there was none to
        /// follow (offline). A live strike inside a window is answered with the next random
        /// checkpoint. Returns null when randomisation is not set up for the site, or for an
        /// offline strike (nobody is waiting for an answer).
        /// </summary>
        PatrolRandomisationInstruction OnWandStrike(PatrolRandomisationStrike strike);

        /// <summary>
        /// Current state without striking anything: for the handset on login/reconnect. When a
        /// window is open and the guard has no instruction yet, startNewIfNone hands one out.
        /// </summary>
        PatrolRandomisationInstruction GetStatus(int clientSiteId, int guardId, DateTime utcNow, bool startNewIfNone = false);
    }

    public class PatrolRandomisationService : IPatrolRandomisationService
    {
        private readonly IClientSiteWandDataProvider _wandDataProvider;
        private readonly IClientDataProvider _clientDataProvider;

        public PatrolRandomisationService(IClientSiteWandDataProvider wandDataProvider, IClientDataProvider clientDataProvider)
        {
            _wandDataProvider = wandDataProvider;
            _clientDataProvider = clientDataProvider;
        }

        public PatrolRandomisationInstruction OnWandStrike(PatrolRandomisationStrike strike)
        {
            if (strike == null || strike.GuardId <= 0 || string.IsNullOrWhiteSpace(strike.TagUId))
                return null;

            var site = LoadSite(strike.ClientSiteId);
            if (site == null)
                return null;

            var hitUtc = AsUtc(strike.HitUtc);
            var window = site.WindowAt(hitUtc);
            var tags = _wandDataProvider.GetActiveSmartWandTagsForSite(strike.ClientSiteId);
            var scannedLabel = LabelOf(tags, strike.TagUId);
            string previousResult = null;
            var answeredPending = false;

            var pending = _wandDataProvider.GetPendingPatrolRandomisationLog(strike.ClientSiteId, strike.GuardId);
            if (pending != null)
            {
                if (AsUtc(pending.WindowEndUtc) <= hitUtc)
                {
                    // Its window closed before this strike: the instruction was never followed.
                    _wandDataProvider.ResolvePatrolRandomisationLog(new ClientSitePatrolRandomisationLog
                    {
                        Id = pending.Id,
                        Status = PatrolRandomisationLogStatus.Missed,
                        ResolvedUtc = pending.WindowEndUtc
                    });
                }
                else if (pending.AssignedUtc.HasValue && AsUtc(pending.AssignedUtc.Value) <= hitUtc)
                {
                    var completed = string.Equals(pending.AssignedTagUId, strike.TagUId, StringComparison.OrdinalIgnoreCase);
                    answeredPending = _wandDataProvider.ResolvePatrolRandomisationLog(new ClientSitePatrolRandomisationLog
                    {
                        Id = pending.Id,
                        Status = completed ? PatrolRandomisationLogStatus.Completed : PatrolRandomisationLogStatus.Skipped,
                        ScannedTagUId = strike.TagUId,
                        ScannedTagLabel = scannedLabel,
                        ScannedHitLogId = strike.HitLogId > 0 ? strike.HitLogId : null,
                        ScannedUtc = hitUtc,
                        ScannedLocal = site.ToLocal(hitUtc),
                        ScannedIsOffline = strike.IsOffline,
                        ScannedUserId = strike.UserId > 0 ? strike.UserId : null,
                        ResolvedUtc = DateTime.UtcNow
                    });
                    if (answeredPending)
                        previousResult = completed ? "Completed" : "Skipped";
                }
                // else: an offline strike made before this instruction existed; it does not answer it.
            }

            if (strike.IsOffline)
            {
                // Nobody waits on an offline sync; just make sure every strike in a window is on record.
                if (window != null && !answeredPending)
                {
                    var row = NewLogRow(site, window, strike.ClientSiteId, strike.GuardId, strike.UserId, strike.SmartWandId, hitUtc);
                    row.Status = PatrolRandomisationLogStatus.Unguided;
                    row.ScannedTagUId = strike.TagUId;
                    row.ScannedTagLabel = scannedLabel;
                    row.ScannedHitLogId = strike.HitLogId > 0 ? strike.HitLogId : null;
                    row.ScannedUtc = hitUtc;
                    row.ScannedLocal = site.ToLocal(hitUtc);
                    row.ScannedIsOffline = true;
                    row.ScannedUserId = strike.UserId > 0 ? strike.UserId : null;
                    row.ResolvedUtc = DateTime.UtcNow;
                    _wandDataProvider.AddPatrolRandomisationLog(row);
                }
                return null;
            }

            var result = NewInstruction(site, window, strike.ClientSiteId, strike.GuardId);
            result.PreviousCheckpointResult = previousResult;
            if (window == null)
            {
                result.Message = "Patrol Randomisation is not active now.";
                return result;
            }

            result.NextCheckpoint = AssignNext(site, window, tags, strike, scannedLabel, hitUtc);
            result.Message = result.NextCheckpoint != null
                ? $"Proceed to: {result.NextCheckpoint.Label}"
                : "Not enough checkpoints registered on this site to randomise the patrol.";
            return result;
        }

        public PatrolRandomisationInstruction GetStatus(int clientSiteId, int guardId, DateTime utcNow, bool startNewIfNone = false)
        {
            var site = LoadSite(clientSiteId);
            if (site == null)
                return new PatrolRandomisationInstruction { ClientSiteId = clientSiteId, GuardId = guardId, Message = "Patrol Randomisation is not set up for this site." };

            utcNow = AsUtc(utcNow);
            var window = site.WindowAt(utcNow);
            var result = NewInstruction(site, window, clientSiteId, guardId);

            var pending = _wandDataProvider.GetPendingPatrolRandomisationLog(clientSiteId, guardId);
            if (pending != null && AsUtc(pending.WindowEndUtc) <= utcNow)
            {
                _wandDataProvider.ResolvePatrolRandomisationLog(new ClientSitePatrolRandomisationLog
                {
                    Id = pending.Id,
                    Status = PatrolRandomisationLogStatus.Missed,
                    ResolvedUtc = pending.WindowEndUtc
                });
                pending = null;
            }

            if (window == null)
            {
                result.Message = "Patrol Randomisation is not active now.";
                return result;
            }

            if (pending != null && pending.AssignedTagId.HasValue)
            {
                var tag = _wandDataProvider.GetActiveSmartWandTagsForSite(clientSiteId)
                    .FirstOrDefault(t => t.Id == pending.AssignedTagId);
                result.NextCheckpoint = new PatrolRandomisationCheckpoint
                {
                    AssignmentId = pending.Id,
                    TagId = pending.AssignedTagId.Value,
                    TagUId = pending.AssignedTagUId,
                    Label = pending.AssignedTagLabel,
                    TagsTypeId = tag?.TagsTypeId ?? 0,
                    AssignedUtc = AsUtc(pending.AssignedUtc ?? utcNow)
                };
            }
            else if (startNewIfNone)
            {
                var tags = _wandDataProvider.GetActiveSmartWandTagsForSite(clientSiteId);
                result.NextCheckpoint = AssignNext(site, window, tags,
                    new PatrolRandomisationStrike { ClientSiteId = clientSiteId, GuardId = guardId, HitUtc = utcNow }, null, utcNow);
            }

            result.Message = result.NextCheckpoint != null
                ? $"Proceed to: {result.NextCheckpoint.Label}"
                : "Strike any checkpoint to receive your next one.";
            return result;
        }

        /// <summary>Picks and records the next checkpoint. strike.TagUId empty = no strike behind it (status call).</summary>
        private PatrolRandomisationCheckpoint AssignNext(SiteContext site, Window window, List<ClientSiteSmartWandTags> tags,
            PatrolRandomisationStrike strike, string fromLabel, DateTime utcNow)
        {
            var used = _wandDataProvider.GetPatrolRandomisationAssignedTagUIds(strike.ClientSiteId, strike.GuardId, window.StartUtc);
            var next = PatrolRandomisationHelper.PickNextTag(tags, strike.TagUId, used);
            if (next == null)
                return null;

            var row = NewLogRow(site, window, strike.ClientSiteId, strike.GuardId, strike.UserId, strike.SmartWandId, utcNow);
            if (!string.IsNullOrEmpty(strike.TagUId))
            {
                row.FromTagUId = strike.TagUId;
                row.FromTagLabel = fromLabel;
                row.FromHitLogId = strike.HitLogId > 0 ? strike.HitLogId : null;
                row.FromScanUtc = AsUtc(strike.HitUtc);
                row.FromScanLocal = site.ToLocal(strike.HitUtc);
                row.FromIsOffline = strike.IsOffline;
            }
            row.AssignedTagId = next.Id;
            row.AssignedTagUId = next.UId;
            row.AssignedTagLabel = string.IsNullOrWhiteSpace(next.LabelDescription) ? next.UId : next.LabelDescription;
            row.AssignedUtc = utcNow;
            row.AssignedLocal = site.ToLocal(utcNow);
            row.Status = PatrolRandomisationLogStatus.Pending;
            _wandDataProvider.AddPatrolRandomisationLog(row);

            return new PatrolRandomisationCheckpoint
            {
                AssignmentId = row.Id,
                TagId = next.Id,
                TagUId = next.UId,
                Label = row.AssignedTagLabel,
                TagsTypeId = next.TagsTypeId,
                AssignedUtc = AsUtc(utcNow)
            };
        }

        private static ClientSitePatrolRandomisationLog NewLogRow(SiteContext site, Window window, int clientSiteId, int guardId,
            int? userId, int? smartWandId, DateTime utc) => new()
        {
            ClientSiteId = clientSiteId,
            GuardId = guardId,
            UserId = userId > 0 ? userId : null,
            SmartWandId = smartWandId > 0 ? smartWandId : null,
            BusinessDate = window.StartLocal.Date,
            DayOfWeek = window.StartLocal.DayOfWeek,
            WindowStartLocal = window.StartLocal,
            WindowEndLocal = window.EndLocal,
            WindowEndUtc = window.EndUtc,
            SiteUtcOffset = SiteTimeZoneHelper.FormatOffset(utc, site.Zone),
            CreatedUtc = DateTime.UtcNow
        };

        private static string LabelOf(IEnumerable<ClientSiteSmartWandTags> tags, string uid)
        {
            var tag = tags?.FirstOrDefault(t => string.Equals(t.UId, uid, StringComparison.OrdinalIgnoreCase));
            return string.IsNullOrWhiteSpace(tag?.LabelDescription) ? uid : tag.LabelDescription;
        }

        private static PatrolRandomisationInstruction NewInstruction(SiteContext site, Window window, int clientSiteId, int guardId) => new()
        {
            ClientSiteId = clientSiteId,
            GuardId = guardId,
            IsConfigured = true,
            IsActive = window != null,
            WindowStartLocal = window?.StartLocal,
            WindowEndLocal = window?.EndLocal,
            WindowEndUtc = window == null ? null : AsUtc(window.EndUtc),
            ScheduleDescription = site.ScheduleDescription
        };

        /// <summary>DB and request times are UTC but come back Unspecified; mark them so JSON carries the Z.</summary>
        private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

        private class Window
        {
            public DateTime StartLocal { get; set; }
            public DateTime EndLocal { get; set; }
            public DateTime StartUtc { get; set; }
            public DateTime EndUtc { get; set; }
        }

        private class SiteContext
        {
            public ClientSitePatrolRandomisationSetting Setting { get; set; }
            public TimeZoneInfo Zone { get; set; }
            public string ScheduleDescription { get; set; }

            public DateTime ToLocal(DateTime utc) => SiteTimeZoneHelper.UtcToSiteLocal(utc, Zone);

            /// <summary>The window open at that instant, or null.</summary>
            public Window WindowAt(DateTime utc)
            {
                var local = ToLocal(utc);
                var schedule = PatrolRandomisationHelper.FindActiveWindow(Setting, PatrolTouringMode.STND, local);
                if (schedule == null)
                    return null;
                var start = local.Date + schedule.StartTime;
                var end = local.Date + schedule.EndTime;
                return new Window
                {
                    StartLocal = start,
                    EndLocal = end,
                    StartUtc = SiteTimeZoneHelper.SiteLocalToUtc(start, Zone),
                    EndUtc = SiteTimeZoneHelper.SiteLocalToUtc(end, Zone)
                };
            }
        }

        /// <summary>Null when the site has no enabled STND randomisation with at least one window.</summary>
        private SiteContext LoadSite(int clientSiteId)
        {
            var setting = _wandDataProvider.GetPatrolRandomisationSetting(clientSiteId);
            if (setting == null || !setting.IsEnabled || setting.Schedules == null || setting.Schedules.Count == 0)
                return null;

            var mode = _clientDataProvider.GetClientSiteDetailsWithId(clientSiteId).FirstOrDefault()?.PatrolTourMode;
            if (mode != PatrolTouringMode.STND)
                return null;

            return new SiteContext
            {
                Setting = setting,
                Zone = SiteTimeZoneHelper.Resolve(_wandDataProvider.GetClientSiteTimezoneString(clientSiteId)),
                ScheduleDescription = PatrolRandomisationHelper.Describe(setting.Schedules) + " (site time)"
            };
        }
    }
}
