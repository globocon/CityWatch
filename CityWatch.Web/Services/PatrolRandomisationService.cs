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
    /// What the handset is told about Patrol Randomisation, in the scan response, from
    /// api/Scanner/GetPatrolRandomisationStatus and api/Scanner/PatrolRandomisationBypass, or pushed
    /// over SignalR ("PatrolRandomisationNextCheckpoint"). Plain JSON, so Android and iOS read it the
    /// same way. The handset caches it so it can keep randomising while offline.
    /// </summary>
    public class PatrolRandomisationInstruction
    {
        public int ClientSiteId { get; set; }
        public int GuardId { get; set; }

        /// <summary>The site has randomisation switched on (STND mode), whatever the time.</summary>
        public bool IsConfigured { get; set; }

        /// <summary>A window is open right now: the handset shows "Tour: RNDM" and the GoTo line.</summary>
        public bool IsActive { get; set; }

        /// <summary>The open window in site-local time, and its end in UTC for a countdown.</summary>
        public DateTime? WindowStartLocal { get; set; }
        public DateTime? WindowEndLocal { get; set; }
        public DateTime? WindowEndUtc { get; set; }

        /// <summary>The week's windows, e.g. "Mon 02:00-03:59, Thu 10:00-12:00 (site time)".</summary>
        public string ScheduleDescription { get; set; }

        /// <summary>
        /// Every window from now to a week ahead, in UTC. The handset flips between RNDM and STND
        /// from this list, so it needs neither the site's time zone nor a connection to do so.
        /// </summary>
        public List<PatrolRandomisationWindowUtc> UpcomingWindowsUtc { get; set; } = new();

        /// <summary>Checkpoints missed in the last 72 hours; the handset weights its own offline picks with them.</summary>
        public List<string> PriorityTagUIds { get; set; } = new();

        /// <summary>Null when not active, or when the site has too few tags to pick from.</summary>
        public PatrolRandomisationCheckpoint NextCheckpoint { get; set; }

        /// <summary>
        /// The acknowledgement for the strike or bypass that produced this instruction: "Completed"
        /// (the guard struck the checkpoint they were sent to), "Skipped", "Bypassed", or null.
        /// </summary>
        public string PreviousCheckpointResult { get; set; }
        public string PreviousCheckpointLabel { get; set; }

        public string Message { get; set; }
    }

    public class PatrolRandomisationWindowUtc
    {
        public DateTime StartUtc { get; set; }
        public DateTime EndUtc { get; set; }
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

    /// <summary>Outcome of a bypass, for the controller's logbook entry.</summary>
    public class PatrolRandomisationBypassResult
    {
        public bool IsConfigured { get; set; }
        public string BypassedTagLabel { get; set; }
        /// <summary>The same bypass was already recorded (a resend): nothing changed, no new logbook entry.</summary>
        public bool AlreadyRecorded { get; set; }
        public PatrolRandomisationInstruction Instruction { get; set; }
    }

    public interface IPatrolRandomisationService
    {
        /// <summary>
        /// Called after every strike of one of the site's own tags, online or offline-synced.
        /// Records it in ClientSitePatrolRandomisationLogs: it answers the guard's instruction
        /// (Completed / Skipped), or is logged as Unguided when there was none to follow. A live
        /// strike inside a window is answered with the next random checkpoint. Returns null when
        /// randomisation is not set up for the site, or for an offline strike (nobody is waiting).
        /// </summary>
        PatrolRandomisationInstruction OnWandStrike(PatrolRandomisationStrike strike);

        /// <summary>
        /// The guard tapped "Tour: RNDM" to bypass the checkpoint on screen. Records it as Bypassed
        /// and, when live, hands out another checkpoint. Replays of offline bypasses keep their time.
        /// </summary>
        PatrolRandomisationBypassResult Bypass(PatrolRandomisationBypassRequest request);

        /// <summary>
        /// Current state without striking anything: for the handset on login/reconnect and when a
        /// window opens. When a window is open and the guard has no instruction yet, startNewIfNone
        /// hands one out.
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
            var scanned = ScannedFields(site, strike, scannedLabel, hitUtc);
            string previousResult = null, previousLabel = null;
            var answered = false;

            var pending = CloseIfWindowEnded(_wandDataProvider.GetPendingPatrolRandomisationLog(strike.ClientSiteId, strike.GuardId), hitUtc);

            // An offline strike made before the instruction existed does not answer it.
            if (pending != null && pending.AssignedUtc.HasValue && AsUtc(pending.AssignedUtc.Value) > hitUtc)
                pending = null;

            // The handset was showing a different checkpoint than the one on record. Offline, it moved
            // on without striking it, which only a bypass does (the bypass replay fills in its time).
            // Live, a bypass is recorded the moment it happens, so the handset simply never got this
            // instruction (a lost response): it is cancelled, not held against the guard.
            if (pending != null && !string.IsNullOrEmpty(strike.ExpectedTagUId)
                && !string.Equals(pending.AssignedTagUId, strike.ExpectedTagUId, StringComparison.OrdinalIgnoreCase))
            {
                _wandDataProvider.ResolvePatrolRandomisationLog(new ClientSitePatrolRandomisationLog
                {
                    Id = pending.Id,
                    Status = strike.IsOffline ? PatrolRandomisationLogStatus.Bypassed : PatrolRandomisationLogStatus.Cancelled,
                    ResolvedUtc = hitUtc
                });
                pending = null;
            }

            if (pending != null)
            {
                var completed = string.Equals(pending.AssignedTagUId, strike.TagUId, StringComparison.OrdinalIgnoreCase);
                scanned.Id = pending.Id;
                scanned.Status = completed ? PatrolRandomisationLogStatus.Completed : PatrolRandomisationLogStatus.Skipped;
                answered = _wandDataProvider.ResolvePatrolRandomisationLog(scanned);
                if (answered)
                {
                    previousResult = scanned.Status.ToString();
                    previousLabel = pending.AssignedTagLabel;
                }
            }
            else if (window != null && !string.IsNullOrEmpty(strike.ExpectedTagUId))
            {
                // The checkpoint came from the handset itself (picked offline); record it with its answer.
                var expected = tags.FirstOrDefault(t => string.Equals(t.UId, strike.ExpectedTagUId, StringComparison.OrdinalIgnoreCase));
                var expectedUtc = AsUtc(strike.ExpectedAssignedUtc ?? hitUtc);
                var completed = string.Equals(strike.ExpectedTagUId, strike.TagUId, StringComparison.OrdinalIgnoreCase);
                var row = NewLogRow(site, window, strike.ClientSiteId, strike.GuardId, strike.UserId, strike.SmartWandId, expectedUtc);
                row.AssignedTagId = expected?.Id;
                row.AssignedTagUId = strike.ExpectedTagUId;
                row.AssignedTagLabel = LabelOf(tags, strike.ExpectedTagUId);
                row.AssignedUtc = expectedUtc;
                row.AssignedLocal = site.ToLocal(expectedUtc);
                CopyScanned(scanned, row);
                row.Status = completed ? PatrolRandomisationLogStatus.Completed : PatrolRandomisationLogStatus.Skipped;
                _wandDataProvider.AddPatrolRandomisationLog(row);
                answered = true;
                previousResult = row.Status.ToString();
                previousLabel = row.AssignedTagLabel;
            }

            if (strike.IsOffline)
            {
                if (window != null && !answered)
                {
                    // On record even with nothing to follow.
                    var row = NewLogRow(site, window, strike.ClientSiteId, strike.GuardId, strike.UserId, strike.SmartWandId, hitUtc);
                    CopyScanned(scanned, row);
                    row.Status = PatrolRandomisationLogStatus.Unguided;
                    _wandDataProvider.AddPatrolRandomisationLog(row);
                }

                // The handset picked the next checkpoint itself; hold it as pending so the next strike is judged against it.
                if (window != null && !string.IsNullOrEmpty(strike.NextTagUId))
                    AddPending(site, window, tags, strike.ClientSiteId, strike.GuardId, strike.UserId, strike.SmartWandId,
                        strike.NextTagUId, AsUtc(strike.NextAssignedUtc ?? hitUtc), strike, scannedLabel);
                return null;
            }

            var result = NewInstruction(site, window, strike.ClientSiteId, strike.GuardId);
            result.PreviousCheckpointResult = previousResult;
            result.PreviousCheckpointLabel = previousLabel;
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

        public PatrolRandomisationBypassResult Bypass(PatrolRandomisationBypassRequest request)
        {
            if (request == null || request.GuardId <= 0 || request.ClientSiteId <= 0 || string.IsNullOrWhiteSpace(request.TagUId))
                return new PatrolRandomisationBypassResult();

            var site = LoadSite(request.ClientSiteId);
            var tags = _wandDataProvider.GetActiveSmartWandTagsForSite(request.ClientSiteId);
            var label = LabelOf(tags, request.TagUId);
            if (site == null)
                return new PatrolRandomisationBypassResult { BypassedTagLabel = label };

            var bypassUtc = AsUtc(request.BypassUtc == default ? DateTime.UtcNow : request.BypassUtc);
            var window = site.WindowAt(bypassUtc);

            if (window != null && _wandDataProvider.GetPatrolRandomisationLogsForGuard(request.ClientSiteId, request.GuardId, window.StartUtc.AddHours(-1))
                    .Any(l => l.Status == PatrolRandomisationLogStatus.Bypassed && l.ScannedUtc.HasValue
                              && Math.Abs((AsUtc(l.ScannedUtc.Value) - bypassUtc).TotalSeconds) < 1.5
                              && string.Equals(l.AssignedTagUId, request.TagUId, StringComparison.OrdinalIgnoreCase)))
            {
                var again = NewInstruction(site, window, request.ClientSiteId, request.GuardId);
                return new PatrolRandomisationBypassResult { IsConfigured = true, BypassedTagLabel = label, AlreadyRecorded = true, Instruction = again };
            }
            var pending = CloseIfWindowEnded(_wandDataProvider.GetPendingPatrolRandomisationLog(request.ClientSiteId, request.GuardId), bypassUtc);

            var resolution = new ClientSitePatrolRandomisationLog
            {
                Status = PatrolRandomisationLogStatus.Bypassed,
                ScannedUtc = bypassUtc,
                ScannedLocal = site.ToLocal(bypassUtc),
                ScannedIsOffline = request.IsOffline,
                ScannedUserId = request.UserId > 0 ? request.UserId : null,
                ResolvedUtc = bypassUtc
            };

            if (pending != null && string.Equals(pending.AssignedTagUId, request.TagUId, StringComparison.OrdinalIgnoreCase))
            {
                resolution.Id = pending.Id;
                _wandDataProvider.ResolvePatrolRandomisationLog(resolution);
            }
            else if (window != null)
            {
                // Already closed as Bypassed by a later offline strike (see OnWandStrike)? Give it the real time.
                var inferred = _wandDataProvider.GetPatrolRandomisationLogsForGuard(request.ClientSiteId, request.GuardId, window.StartUtc)
                    .LastOrDefault(l => l.Status == PatrolRandomisationLogStatus.Bypassed && l.ScannedUtc == null
                                        && string.Equals(l.AssignedTagUId, request.TagUId, StringComparison.OrdinalIgnoreCase));
                if (inferred != null)
                {
                    resolution.Id = inferred.Id;
                    _wandDataProvider.ResolvePatrolRandomisationLog(resolution, onlyIfPending: false);
                }
                else
                {
                    var assignedUtc = AsUtc(request.TagAssignedUtc ?? bypassUtc);
                    var tag = tags.FirstOrDefault(t => string.Equals(t.UId, request.TagUId, StringComparison.OrdinalIgnoreCase));
                    var row = NewLogRow(site, window, request.ClientSiteId, request.GuardId, request.UserId, request.SmartWandId, assignedUtc);
                    row.AssignedTagId = tag?.Id;
                    row.AssignedTagUId = request.TagUId;
                    row.AssignedTagLabel = label;
                    row.AssignedUtc = assignedUtc;
                    row.AssignedLocal = site.ToLocal(assignedUtc);
                    CopyScanned(resolution, row);
                    row.Status = PatrolRandomisationLogStatus.Bypassed;
                    _wandDataProvider.AddPatrolRandomisationLog(row);
                }
            }

            var result = NewInstruction(site, window, request.ClientSiteId, request.GuardId);
            result.PreviousCheckpointResult = PatrolRandomisationLogStatus.Bypassed.ToString();
            result.PreviousCheckpointLabel = label;

            if (window != null)
            {
                if (!request.IsOffline)
                {
                    // Live: another checkpoint straight away. The bypassed one is already in this window's
                    // deck, so it is not handed out again until every other point has been.
                    result.NextCheckpoint = AssignNext(site, window, tags,
                        new PatrolRandomisationStrike { ClientSiteId = request.ClientSiteId, GuardId = request.GuardId, UserId = request.UserId, SmartWandId = request.SmartWandId, HitUtc = bypassUtc },
                        null, bypassUtc, excludeTagUId: request.TagUId);
                }
                else if (!string.IsNullOrEmpty(request.NextTagUId)
                         && _wandDataProvider.GetPendingPatrolRandomisationLog(request.ClientSiteId, request.GuardId) == null)
                {
                    // Replay: the checkpoint the handset moved on to, unless a later strike already accounted for it.
                    AddPending(site, window, tags, request.ClientSiteId, request.GuardId, request.UserId, request.SmartWandId,
                        request.NextTagUId, AsUtc(request.NextAssignedUtc ?? bypassUtc), null, null);
                }
            }

            result.Message = result.NextCheckpoint != null
                ? $"Proceed to: {result.NextCheckpoint.Label}"
                : window == null ? "Patrol Randomisation is not active now." : "Bypass recorded.";
            return new PatrolRandomisationBypassResult { IsConfigured = true, BypassedTagLabel = label, Instruction = result };
        }

        public PatrolRandomisationInstruction GetStatus(int clientSiteId, int guardId, DateTime utcNow, bool startNewIfNone = false)
        {
            var site = LoadSite(clientSiteId);
            if (site == null)
                return new PatrolRandomisationInstruction { ClientSiteId = clientSiteId, GuardId = guardId, Message = "Patrol Randomisation is not set up for this site." };

            utcNow = AsUtc(utcNow);
            var window = site.WindowAt(utcNow);
            var result = NewInstruction(site, window, clientSiteId, guardId);

            var pending = CloseIfWindowEnded(_wandDataProvider.GetPendingPatrolRandomisationLog(clientSiteId, guardId), utcNow);

            if (window == null)
            {
                result.Message = "Patrol Randomisation is not active now.";
                return result;
            }

            if (pending != null && !string.IsNullOrEmpty(pending.AssignedTagUId))
            {
                var tag = _wandDataProvider.GetActiveSmartWandTagsForSite(clientSiteId)
                    .FirstOrDefault(t => string.Equals(t.UId, pending.AssignedTagUId, StringComparison.OrdinalIgnoreCase));
                result.NextCheckpoint = new PatrolRandomisationCheckpoint
                {
                    AssignmentId = pending.Id,
                    TagId = pending.AssignedTagId ?? tag?.Id ?? 0,
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

        /// <summary>A pending instruction whose window has closed is marked Missed and dropped.</summary>
        private ClientSitePatrolRandomisationLog CloseIfWindowEnded(ClientSitePatrolRandomisationLog pending, DateTime utc)
        {
            if (pending == null || AsUtc(pending.WindowEndUtc) > utc)
                return pending;

            _wandDataProvider.ResolvePatrolRandomisationLog(new ClientSitePatrolRandomisationLog
            {
                Id = pending.Id,
                Status = PatrolRandomisationLogStatus.Missed,
                ResolvedUtc = pending.WindowEndUtc
            });
            return null;
        }

        /// <summary>Picks (weighted towards checkpoints missed in the last 72 hours) and records the next checkpoint.</summary>
        private PatrolRandomisationCheckpoint AssignNext(SiteContext site, Window window, List<ClientSiteSmartWandTags> tags,
            PatrolRandomisationStrike strike, string fromLabel, DateTime utcNow, string excludeTagUId = null)
        {
            var used = _wandDataProvider.GetPatrolRandomisationAssignedTagUIds(strike.ClientSiteId, strike.GuardId, window.StartUtc);
            var priority = site.PriorityTagUIds(_wandDataProvider, strike.ClientSiteId, utcNow);
            var next = PatrolRandomisationHelper.PickNextTag(tags, excludeTagUId ?? strike.TagUId, used, priorityTagUIds: priority);
            if (next == null)
                return null;

            var row = AddPending(site, window, tags, strike.ClientSiteId, strike.GuardId, strike.UserId, strike.SmartWandId,
                next.UId, utcNow, string.IsNullOrEmpty(strike.TagUId) ? null : strike, fromLabel);

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

        private ClientSitePatrolRandomisationLog AddPending(SiteContext site, Window window, List<ClientSiteSmartWandTags> tags,
            int clientSiteId, int guardId, int? userId, int? smartWandId, string tagUId, DateTime assignedUtc,
            PatrolRandomisationStrike from, string fromLabel)
        {
            var tag = tags.FirstOrDefault(t => string.Equals(t.UId, tagUId, StringComparison.OrdinalIgnoreCase));
            var row = NewLogRow(site, window, clientSiteId, guardId, userId, smartWandId, assignedUtc);
            if (from != null)
            {
                row.FromTagUId = from.TagUId;
                row.FromTagLabel = fromLabel;
                row.FromHitLogId = from.HitLogId > 0 ? from.HitLogId : null;
                row.FromScanUtc = AsUtc(from.HitUtc);
                row.FromScanLocal = site.ToLocal(from.HitUtc);
                row.FromIsOffline = from.IsOffline;
            }
            row.AssignedTagId = tag?.Id;
            row.AssignedTagUId = tagUId;
            row.AssignedTagLabel = LabelOf(tags, tagUId);
            row.AssignedUtc = assignedUtc;
            row.AssignedLocal = site.ToLocal(assignedUtc);
            row.Status = PatrolRandomisationLogStatus.Pending;
            _wandDataProvider.AddPatrolRandomisationLog(row);
            return row;
        }

        private static ClientSitePatrolRandomisationLog ScannedFields(SiteContext site, PatrolRandomisationStrike strike, string label, DateTime hitUtc) => new()
        {
            ScannedTagUId = strike.TagUId,
            ScannedTagLabel = label,
            ScannedHitLogId = strike.HitLogId > 0 ? strike.HitLogId : null,
            ScannedUtc = hitUtc,
            ScannedLocal = site.ToLocal(hitUtc),
            ScannedIsOffline = strike.IsOffline,
            ScannedUserId = strike.UserId > 0 ? strike.UserId : null,
            ResolvedUtc = DateTime.UtcNow
        };

        private static void CopyScanned(ClientSitePatrolRandomisationLog from, ClientSitePatrolRandomisationLog to)
        {
            to.ScannedTagUId = from.ScannedTagUId;
            to.ScannedTagLabel = from.ScannedTagLabel;
            to.ScannedHitLogId = from.ScannedHitLogId;
            to.ScannedUtc = from.ScannedUtc;
            to.ScannedLocal = from.ScannedLocal;
            to.ScannedIsOffline = from.ScannedIsOffline;
            to.ScannedUserId = from.ScannedUserId;
            to.ResolvedUtc = from.ResolvedUtc;
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

        private PatrolRandomisationInstruction NewInstruction(SiteContext site, Window window, int clientSiteId, int guardId)
        {
            var now = DateTime.UtcNow;
            return new PatrolRandomisationInstruction
            {
                ClientSiteId = clientSiteId,
                GuardId = guardId,
                IsConfigured = true,
                IsActive = window != null,
                WindowStartLocal = window?.StartLocal,
                WindowEndLocal = window?.EndLocal,
                WindowEndUtc = window == null ? null : AsUtc(window.EndUtc),
                ScheduleDescription = site.ScheduleDescription,
                UpcomingWindowsUtc = site.WindowsBetween(now, now.AddDays(8)),
                PriorityTagUIds = site.PriorityTagUIds(_wandDataProvider, clientSiteId, now)
            };
        }

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
            private List<string> _priority;

            public ClientSitePatrolRandomisationSetting Setting { get; set; }
            public TimeZoneInfo Zone { get; set; }
            public string ScheduleDescription { get; set; }

            public DateTime ToLocal(DateTime utc) => SiteTimeZoneHelper.UtcToSiteLocal(utc, Zone);

            /// <summary>Loaded once per request: one aggregate query.</summary>
            public List<string> PriorityTagUIds(IClientSiteWandDataProvider provider, int clientSiteId, DateTime utcNow) =>
                _priority ??= provider.GetPatrolRandomisationPriorityTagUIds(clientSiteId, utcNow - PatrolRandomisationHelper.PriorityLookback);

            /// <summary>The window open at that instant, or null.</summary>
            public Window WindowAt(DateTime utc)
            {
                var local = ToLocal(utc);
                var schedule = PatrolRandomisationHelper.FindActiveWindow(Setting, PatrolTouringMode.STND, local);
                return schedule == null ? null : Build(local.Date, schedule);
            }

            /// <summary>Every window that ends after fromUtc and starts before toUtc.</summary>
            public List<PatrolRandomisationWindowUtc> WindowsBetween(DateTime fromUtc, DateTime toUtc)
            {
                var list = new List<PatrolRandomisationWindowUtc>();
                var firstDay = ToLocal(fromUtc).Date.AddDays(-1);
                var lastDay = ToLocal(toUtc).Date;
                for (var day = firstDay; day <= lastDay; day = day.AddDays(1))
                {
                    foreach (var s in Setting.Schedules.Where(s => s.DayOfWeek == day.DayOfWeek))
                    {
                        var w = Build(day, s);
                        if (w.EndUtc > fromUtc && w.StartUtc < toUtc)
                            list.Add(new PatrolRandomisationWindowUtc { StartUtc = AsUtc(w.StartUtc), EndUtc = AsUtc(w.EndUtc) });
                    }
                }
                return list.OrderBy(w => w.StartUtc).ToList();
            }

            private Window Build(DateTime localDate, ClientSitePatrolRandomisationSchedule schedule)
            {
                var start = localDate + schedule.StartTime;
                var end = localDate + schedule.EndTime;
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
