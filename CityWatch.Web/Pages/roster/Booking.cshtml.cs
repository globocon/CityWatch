using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using CityWatch.Data;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Enums;
using CityWatch.Web.Services;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using CityWatch.Data.Helpers;
using CityWatch.Web.Helpers;

namespace CityWatch.Web.Pages.roster
{
    public class BookingModel : PageModel
    {
        private readonly ILogger<BookingModel> _logger;
        private readonly IViewDataService _viewDataService;
        private readonly CityWatchDbContext _context;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IRosterReportGenerator _rosterReportGenerator;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IAlertEmailServices _alertEmailServices;

        public BookingModel(
            ILogger<BookingModel> logger,
            IViewDataService viewDataService,
            CityWatchDbContext context,
            IClientDataProvider clientDataProvider,
            IRosterReportGenerator rosterReportGenerator,
            IWebHostEnvironment webHostEnvironment,
            IAlertEmailServices alertEmailServices)
        {
            _logger = logger;
            _viewDataService = viewDataService;
            _context = context;
            _clientDataProvider = clientDataProvider;
            _rosterReportGenerator = rosterReportGenerator;
            _webHostEnvironment = webHostEnvironment;
            _alertEmailServices = alertEmailServices;
        }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string WeekRange { get; set; }
        public DateTime PreviousWeek { get; set; }
        public DateTime NextWeek { get; set; }
        public int? SelectedGroupId { get; set; }
        public int? SelectedBinderId { get; set; }
        public List<PayRate> PayRatesList { get; set; }
        public List<IncidentReportField> CallsignList { get; set; }
        public bool IsLocked { get; set; }
        public string ActiveTab { get; set; }
        public string RosterAccessRole { get; set; }
        public int? RestrictedSiteId { get; set; }
        public List<PublicHolidayDayInfo> WeeklyHolidays { get; set; }

        public class PublicHolidayDayInfo
        {
            public DateTime Date { get; set; }
            public bool IsPublicHoliday { get; set; }
            public List<string> States { get; set; }
            public string Reasons { get; set; }
        }

        public void OnGet(DateTime? startDate, int? groupId, int? binderId, string tab)
        {
            var today = DateTime.Today;
            var timesheet = _clientDataProvider.GetTimesheetDetails();
            DayOfWeek firstDayOfWeek = GetFirstDayOfWeek();

            if (startDate == null)
            {
                int diff = (7 + (today.DayOfWeek - firstDayOfWeek)) % 7;
                StartDate = today.AddDays(-1 * diff).Date;
            }
            else
            {
                StartDate = startDate.Value;
            }

            EndDate = StartDate.AddDays(6);
            WeekRange = $"{StartDate:dd MMM yyyy} - {EndDate:dd MMM yyyy}";
            PreviousWeek = StartDate.AddDays(-7);
            NextWeek = StartDate.AddDays(7);
            SelectedGroupId = groupId;
            SelectedBinderId = binderId;
            ActiveTab = tab ?? "projects";
            
            // Clear session restrictions if Administrator is loading the page directly without siteId
            var qSiteId = Request.Query["siteId"];
            if (User.IsInRole("Administrator") && string.IsNullOrEmpty(qSiteId))
            {
                HttpContext.Session.Remove("RestrictedSiteId");
                HttpContext.Session.Remove("BookingAccessRole");
                RestrictedSiteId = null;
                RosterAccessRole = "GSS";
            }
            else
            {
                RestrictedSiteId = HttpContext.Session.GetInt32("RestrictedSiteId");
                RosterAccessRole = HttpContext.Session.GetString("BookingAccessRole") ?? "GSS";
            }
            
            // If siteId is passed, check if it's an ROEditor access and set session
            if (!string.IsNullOrEmpty(qSiteId) && int.TryParse(qSiteId, out int sid))
            {
                // Verify if the user has ROEditor access for this site/global
                // For now, if they are coming from the Logbook (which we assume is trusted here), 
                // we set the restriction.
                HttpContext.Session.SetInt32("RestrictedSiteId", sid);
                HttpContext.Session.SetString("BookingAccessRole", "ROEditor");
                RestrictedSiteId = sid;
                RosterAccessRole = "ROEditor";
            }

            PayRatesList = _context.PayRates
                .Where(x => !x.IsDeleted)
                .ToList();

            CallsignList = _context.IncidentReportFields
                .Where(x => x.TypeId == ReportFieldType.CallSign)
                .OrderBy(x => x.Name)
                .ToList();

            // Locking logic: Dec is locked if it's Jan.
            var firstDayOfCurrentMonth = new DateTime(today.Year, today.Month, 1);
            IsLocked = EndDate < firstDayOfCurrentMonth;

            PopulateWeeklyHolidays();
        }

        private void PopulateWeeklyHolidays()
        {
            // Fetch all public holidays to handle recurring matching (Month/Day)
            // Filtering for RepeatYearly or overlapping dates
            var holidays = _context.BroadcastBannerCalendarEvents
                .Where(x => x.IsPublicHoliday && (x.RepeatYearly || (x.ExpiryDate >= StartDate && x.StartDate <= EndDate)))
                .ToList();

            var eventIds = holidays.Select(x => x.id).ToList();
            var holidayStates = _context.PublicHolidayStates
                .Where(x => eventIds.Contains(x.CalendarEventId) && !x.IsDeleted)
                .ToList();

            WeeklyHolidays = new List<PublicHolidayDayInfo>();
            for (int i = 0; i < 7; i++)
            {
                var date = StartDate.AddDays(i).Date;
                
                // Matching logic: 
                // 1. Exact date range match
                // 2. If RepeatYearly is true, match Month and Day
                var dayHolidays = holidays.Where(h => 
                    (date >= h.StartDate.Date && date <= h.ExpiryDate.Date) || 
                    (h.RepeatYearly && h.StartDate.Month == date.Month && h.StartDate.Day == date.Day)
                ).ToList();
                
                var states = new List<string>();
                var reasonsList = new List<string>();
                bool isPh = false;
                
                foreach (var h in dayHolidays)
                {
                    var hStates = holidayStates.Where(s => s.CalendarEventId == h.id).Select(s => s.State).ToList();
                    var reasonLabel = h.TextMessage;
                    if (hStates.Count > 0)
                    {
                        reasonLabel += " (" + string.Join(", ", hStates) + ")";
                    }
                    else
                    {
                        states.Add("ALL");
                    }
                    
                    reasonsList.Add(reasonLabel);
                    isPh = true;
                    states.AddRange(hStates);
                }

                WeeklyHolidays.Add(new PublicHolidayDayInfo
                {
                    Date = date,
                    IsPublicHoliday = isPh,
                    States = states.Distinct().ToList(),
                    Reasons = string.Join("; ", reasonsList)
                });
            }
        }

        private bool[] GetPublicHolidayFlags(string siteState, DateTime start)
        {
            var flags = new bool[7];
            for (int i = 0; i < 7; i++)
            {
                var date = start.AddDays(i).Date;
                
                var phInfo = WeeklyHolidays.FirstOrDefault(x => x.Date == date);
                if (phInfo != null && phInfo.IsPublicHoliday)
                {
                    // Applies if "ALL" is in states or if site's state matches (trimmed & case-insensitive)
                    var trimmedSiteState = siteState?.Trim().ToUpper();
                    if (phInfo.States.Contains("ALL") || (!string.IsNullOrEmpty(trimmedSiteState) && phInfo.States.Any(s => s.Trim().ToUpper() == trimmedSiteState)))
                    {
                        flags[i] = true;
                    }
                }
            }
            return flags;
        }

        private string[] GetPublicHolidayReasons(string siteState, DateTime start)
        {
            var reasons = new string[7];
            for (int i = 0; i < 7; i++)
            {
                var date = start.AddDays(i).Date;

                var phInfo = WeeklyHolidays.FirstOrDefault(x => x.Date == date);
                if (phInfo != null && phInfo.IsPublicHoliday)
                {
                    var trimmedSiteState = siteState?.Trim().ToUpper();
                    if (phInfo.States.Contains("ALL") || (!string.IsNullOrEmpty(trimmedSiteState) && phInfo.States.Any(s => s.Trim().ToUpper() == trimmedSiteState)))
                    {
                        reasons[i] = phInfo.Reasons;
                    }
                }
                reasons[i] = reasons[i] ?? "";
            }
            return reasons;
        }

        public JsonResult OnGetSearchProjects(string search)
        {
            var projects = _context.RosterGroups
                .Where(x => !x.IsDeleted && (string.IsNullOrEmpty(search) || x.Name.Contains(search)))
                .OrderBy(x => x.Name)
                .Select(x => new { id = x.Id, text = x.Name })
                .ToList();

            return new JsonResult(new { results = projects });
        }

        public async Task<JsonResult> OnGetLoadRoster(int groupId, DateTime startDate)
        {
            this.StartDate = startDate;
            this.EndDate = startDate.AddDays(6);
            PopulateWeeklyHolidays();

            var endDate = startDate.AddDays(6).AddDays(1).AddSeconds(-1);

            var groupSitesQuery = _context.RosterGroupSites
                .Where(x => x.RosterGroupId == groupId);

            // Filter by Restricted Site if ROEditor
            var rSiteId = HttpContext.Session.GetInt32("RestrictedSiteId");
            var role = HttpContext.Session.GetString("BookingAccessRole");
            if (role == "ROEditor" && rSiteId.HasValue)
            {
                groupSitesQuery = groupSitesQuery.Where(x => x.ClientSiteId == rSiteId.Value);
            }

            var groupSites = await groupSitesQuery
                .Include(x => x.ClientSite)
                .ThenInclude(x => x.ClientType)
                .ToListAsync();

            var schedules = await _context.RosterSchedules
                .Where(x => x.RosterGroupId == groupId && !x.IsDeleted && x.ShiftStart >= startDate && x.ShiftStart <= endDate)
                .Include(x => x.Guard)
                .Include(x => x.ReliefGuard)
                .Include(x => x.Callsign)
                .Include(x => x.PayRate)
                .ToListAsync();

            // Site Status Integration (Read-Only)
            // Fetches the current roster status (Paid, Invoiced, Cancelled) for each site from RosterSiteWeekStatuses.
            // This is used to display status stamps in the Booking grid for informational purposes.
            var siteIds = groupSites.Select(gs => gs.ClientSiteId).ToList();
            var weekStatuses = await _context.RosterSiteWeekStatuses
                .Where(x => siteIds.Contains(x.ClientSiteId) && x.StartDate == startDate)
                .ToListAsync();

            var rosterData = groupSites.Select(gs => new
            {
                siteId = gs.ClientSiteId,
                siteName = gs.ClientSite.Name,
                clientTypeName = gs.ClientSite.ClientType?.Name ?? "N/A",
                // Injection of status for UI stamp rendering
                status = weekStatuses.FirstOrDefault(ws => ws.ClientSiteId == gs.ClientSiteId)?.Status ?? (schedules.Any(s => s.ClientSiteId == gs.ClientSiteId) ? "Live" : ""),
                isPublicHoliday = GetPublicHolidayFlags(gs.ClientSite.State, startDate),
                publicHolidayReasons = GetPublicHolidayReasons(gs.ClientSite.State, startDate),
                days = Enumerable.Range(0, 7).Select(dayOffset =>
                {
                    var targetDate = startDate.AddDays(dayOffset);
                    return schedules
                        .Where(s => s.ClientSiteId == gs.ClientSiteId && s.ShiftStart.Date == targetDate.Date)
                        .OrderBy(s => s.ShiftStart)
                        .Select(s => new
                        {
                            id = s.Id,
                            guardId = s.GuardId,
                            guardName = s.GuardId.HasValue ? s.Guard.Name : (s.ProviderName ?? "Unassigned"),
                            guardLicense = s.GuardId.HasValue ? (s.Guard.SecurityNo ?? "N/A") : (string.IsNullOrEmpty(s.ProviderName) ? "N/A" : "External"),
                            guardState = s.GuardId.HasValue ? (s.Guard.State ?? "N/A") : "N/A",
                            guardProvider = !string.IsNullOrEmpty(s.ProviderName) ? s.ProviderName : (s.GuardId.HasValue ? (s.Guard.Provider ?? "N/A") : "N/A"),
                            providerName = s.ProviderName,
                            payRateId = s.PayRateId,
                            payRateName = s.PayRate != null ? s.PayRate.Description : "N/A",
                            shiftStart = s.ShiftStart.ToString("HH:mm"),
                            shiftEnd = s.ShiftEnd.ToString("HH:mm"),
                            callsignId = s.CallsignId,
                            callsignName = s.Callsign?.Name ?? "",
                            status = (int)s.Status,
                            durationHours = DateTimeHelper.CalculateDisplayDuration(s.ShiftStart, s.ShiftEnd),
                            payRate = s.PayRate != null ? s.PayRate.GuardPayRate : 0,
                            sellRate = s.PayRate != null ? s.PayRate.SellRateToClient : 0,
                            reliefGuardId = s.ReliefGuardId,
                            reliefGuardName = s.ReliefGuard?.Name ?? "",
                            reliefGuardLicense = s.ReliefGuardId.HasValue ? (s.ReliefGuard.SecurityNo ?? "N/A") : "",
                            reliefProviderName = s.ReliefProviderName ?? "",
                            reliefReason = s.ReliefReason ?? "",
                            reliefReasonOther = s.ReliefReasonOther ?? "",
                            shiftType = s.ShiftType ?? "Regular",
                            adhocOffsiteText = s.AdhocOffsiteText ?? ""
                        })
                        .ToList();
                }).ToList()
            }).ToList();

            return new JsonResult(new { results = rosterData, projectStatus = rosterData.FirstOrDefault()?.status ?? "Live" });
        }

        public JsonResult OnGetSearchProviders(string search, int? siteId)
        {
            var providerNames = new List<string>();

            if (siteId.HasValue && siteId.Value > 0)
            {
                var mainProviders = _context.RosterSchedules
                    .Where(x => x.ClientSiteId == siteId.Value && !x.IsDeleted && !string.IsNullOrEmpty(x.ProviderName))
                    .Select(x => x.ProviderName)
                    .Distinct()
                    .ToList();

                var reliefProviders = _context.RosterSchedules
                    .Where(x => x.ClientSiteId == siteId.Value && !x.IsDeleted && !string.IsNullOrEmpty(x.ReliefProviderName))
                    .Select(x => x.ReliefProviderName)
                    .Distinct()
                    .ToList();

                providerNames = mainProviders.Union(reliefProviders).Distinct().ToList();
            }

            var query = _viewDataService.ProviderList
                .Where(x => !string.IsNullOrEmpty(x.Text) && x.Text != "Select");

            if (providerNames.Any())
            {
                query = query.Where(x => providerNames.Contains(x.Text, StringComparer.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(x => x.Text.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            var providers = query
                .Select(x => new { id = x.Value, text = x.Text })
                .ToList();

            return new JsonResult(new { results = providers });
        }

        public async Task<IActionResult> OnPostDeleteSiteFromGroup(int groupId, int siteId)
        {
            // 1. Remove site from group
            var groupSite = await _context.RosterGroupSites
                .FirstOrDefaultAsync(x => x.RosterGroupId == groupId && x.ClientSiteId == siteId);

            if (groupSite != null)
            {
                _context.RosterGroupSites.Remove(groupSite);

                // 2. Cascade delete all shifts for this site in this group
                var shifts = await _context.RosterSchedules
                    .Where(x => x.RosterGroupId == groupId && x.ClientSiteId == siteId)
                    .ToListAsync();

                if (shifts.Any())
                {
                    _context.RosterSchedules.RemoveRange(shifts);
                }

                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true });
            }

            return new JsonResult(new { success = false, message = "Site not found in group." });
        }

        public async Task<IActionResult> OnPostAddSiteToGroup(int groupId, int siteId)
        {
            if (groupId <= 0)
            {
                return new JsonResult(new { success = false, message = "Invalid Project ID. Please select a valid project." });
            }

            var exists = await _context.RosterGroupSites.AnyAsync(x => x.RosterGroupId == groupId && x.ClientSiteId == siteId);
            if (!exists)
            {
                _context.RosterGroupSites.Add(new RosterGroupSite
                {
                    RosterGroupId = groupId,
                    ClientSiteId = siteId
                });
                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true });
            }
            return new JsonResult(new { success = false, message = "This site is already added to the group." });
        }

        public async Task<IActionResult> OnPostAddShift(int groupId, int siteId, DateTime start, DateTime end, int? guardId, string providerName, int? payRateId, int? shiftId, int? callsignId, int? reliefGuardId, string reliefProviderName, string reliefReason, string reliefReasonOther, string shiftType, int? status, string adhocOffsiteText, decimal? payRate = null, bool ignoreUnavailability = false)
        {
            if (payRateId.HasValue && payRate.HasValue)
            {
                var pr = await _context.PayRates.FindAsync(payRateId.Value);
                if (pr != null && pr.GuardPayRate != payRate.Value)
                {
                    pr.GuardPayRate = payRate.Value;
                    // No need for separate save here, it will be saved with the shift
                }
            }
            // Lock Check
            var today = DateTime.Today;
            var firstDayOfCurrentMonth = new DateTime(today.Year, today.Month, 1);
            var weekEndDate = StartOfWeek(start, GetFirstDayOfWeek()).AddDays(6);
            if (weekEndDate < firstDayOfCurrentMonth)
            {
                return new JsonResult(new { success = false, message = "Changes to previous months are locked." });
            }

            // Validation: Time range check (00:01 - 23:59)
            if (start.TimeOfDay == TimeSpan.Zero || end.TimeOfDay == TimeSpan.Zero)
            {
                return new JsonResult(new { success = false, message = "Time values of 00:00 or 24:00 are not allowed. Minimum value is 00:01 and maximum is 23:59." });
            }

            // Validation 1: Start Date < End Date
            if (start >= end)
            {
                return new JsonResult(new { success = false, message = "Shift End Time must be greater than Start Time." });
            }

            // Validation 2: Guard OR Provider must be selected (Unless Cancelled)
            if (!guardId.HasValue && string.IsNullOrEmpty(providerName) && shiftType != "Cancelled")
            {
                return new JsonResult(new { success = false, message = "Please select a Guard or a Subcontractor Provider." });
            }

            // Validation 3: Conflict Detection (If Guard is selected)
            // If a relief guard or relief provider is assigned, the main guard is not actually working this shift.
            // Therefore, we skip the conflict and unavailability checks for the main guard for this specific shift instance.
            if (guardId.HasValue && !reliefGuardId.HasValue && string.IsNullOrEmpty(reliefProviderName))
            {
                var conflict = await _context.RosterSchedules
                    .Where(x => ((x.GuardId == guardId && x.ReliefGuardId == null) || x.ReliefGuardId == guardId) &&
                                !x.IsDeleted && x.Id != (shiftId ?? 0) && x.Status != RosterShiftStatus.Cancelled &&
                                ((start >= x.ShiftStart && start < x.ShiftEnd) ||
                                 (end > x.ShiftStart && end <= x.ShiftEnd) ||
                                 (start <= x.ShiftStart && end >= x.ShiftEnd)))
                    .Include(x => x.ClientSite)
                    .FirstOrDefaultAsync();

                if (conflict != null)
                {
                    var guard = await _context.Guards.FindAsync(guardId);
                    return new JsonResult(new { success = false, message = $"Conflict: Guard {guard.Name} is currently assigned to {conflict.ClientSite.Name} from {conflict.ShiftStart:HH:mm} to {conflict.ShiftEnd:HH:mm}." });
                }

                // Check Guard Unavailability
                var unavailGuard = await _context.GuardUnavailabilities
                    .Where(u => u.GuardId == guardId && start.Date <= u.ToDate.Date && end.Date >= u.FromDate.Date)
                    .FirstOrDefaultAsync();
                
                if (unavailGuard != null && !ignoreUnavailability)
                {
                    var guard = await _context.Guards.FindAsync(guardId);
                    return new JsonResult(new { success = false, isUnavailConflict = true, message = $"{guard.Name} cannot be rostered on as they are marked unavailable during this period (reasons {unavailGuard.Reason}, {unavailGuard.FromDate:dd MMMM yyyy} – {unavailGuard.ToDate:dd MMMM yyyy}). Please select another guard or adjust their HR records." });
                }
            }

            if (reliefGuardId.HasValue)
            {
                var reliefConflict = await _context.RosterSchedules
                    .Where(x => ((x.GuardId == reliefGuardId && x.ReliefGuardId == null) || x.ReliefGuardId == reliefGuardId) &&
                                !x.IsDeleted && x.Id != (shiftId ?? 0) && x.Status != RosterShiftStatus.Cancelled &&
                                ((start >= x.ShiftStart && start < x.ShiftEnd) ||
                                 (end > x.ShiftStart && end <= x.ShiftEnd) ||
                                 (start <= x.ShiftStart && end >= x.ShiftEnd)))
                    .Include(x => x.ClientSite)
                    .FirstOrDefaultAsync();

                if (reliefConflict != null)
                {
                    var guard = await _context.Guards.FindAsync(reliefGuardId);
                    return new JsonResult(new { success = false, message = $"Conflict: Relief Guard {guard.Name} is currently assigned to {reliefConflict.ClientSite.Name} from {reliefConflict.ShiftStart:HH:mm} to {reliefConflict.ShiftEnd:HH:mm}." });
                }

                var unavailRelief = await _context.GuardUnavailabilities
                    .Where(u => u.GuardId == reliefGuardId && start.Date <= u.ToDate.Date && end.Date >= u.FromDate.Date)
                    .FirstOrDefaultAsync();

                if (unavailRelief != null && !ignoreUnavailability)
                {
                    var guard = await _context.Guards.FindAsync(reliefGuardId);
                    return new JsonResult(new { success = false, isUnavailConflict = true, message = $"Relief Guard {guard.Name} cannot be rostered on as they are marked unavailable during this period (reasons {unavailRelief.Reason}, {unavailRelief.FromDate:dd MMMM yyyy} – {unavailRelief.ToDate:dd MMMM yyyy}). Please select another guard or adjust their HR records." });
                }
            }

            if (shiftId.HasValue && shiftId.Value > 0)
            {
                var existing = await _context.RosterSchedules.FindAsync(shiftId.Value);
                if (existing == null) return new JsonResult(new { success = false, message = "Shift not found." });

                // Capture old values for detailed logging
                var oldGuardId = existing.GuardId;
                var oldReliefGuardId = existing.ReliefGuardId;
                var oldStart = existing.ShiftStart;
                var oldEnd = existing.ShiftEnd;
                int oldStatusVal = (int)existing.Status;

                // Update the entity
                existing.ClientSiteId = siteId;
                existing.ShiftStart = start;
                existing.ShiftEnd = end;
                existing.GuardId = guardId;
                existing.ProviderName = providerName;
                existing.ReliefGuardId = reliefGuardId;
                existing.ReliefProviderName = reliefProviderName;
                existing.ReliefReason = reliefReason;
                existing.PayRateId = payRateId;
                existing.CallsignId = callsignId;
                existing.ReliefReasonOther = reliefReasonOther;
                existing.AdhocOffsiteText = adhocOffsiteText;

                var finalShiftType = shiftType;
                var finalStatus = RosterShiftStatus.Pushed;

                if (shiftType == "RegularAccepted") { finalShiftType = "Regular"; finalStatus = RosterShiftStatus.Accepted; }
                else if (shiftType == "AdhocAccepted") { finalShiftType = "Adhoc"; finalStatus = RosterShiftStatus.Accepted; }
                else if (shiftType == "Declined") { finalShiftType = "Regular"; finalStatus = RosterShiftStatus.Declined; }
                else if (shiftType == "Missed") { finalShiftType = "Regular"; finalStatus = RosterShiftStatus.Missed; }
                else if (shiftType == "Cancelled") { finalShiftType = "Regular"; finalStatus = RosterShiftStatus.Cancelled; }
                else if (shiftType == "Adhoc") { finalShiftType = "Adhoc"; finalStatus = RosterShiftStatus.Pushed; }
                else { finalShiftType = "Regular"; finalStatus = RosterShiftStatus.Pushed; }

                existing.ShiftType = finalShiftType;
                existing.Status = finalStatus;



                // 1. Save the actual shift change first
                await _context.SaveChangesAsync();
                
                // Queue web cancellation email if applicable
                if (finalStatus == RosterShiftStatus.Declined || finalStatus == RosterShiftStatus.Cancelled)
                {
                    try
                    {
                        var userName = HttpContext.User.Identity?.Name ?? "Admin";
                        bool isGuard = HttpContext.User.IsInRole("Guard");
                        string cancelledBy = isGuard ? $"Guard|{userName}" : userName;
                        
                        await _alertEmailServices.QueueWebShiftCancellation(existing, "Cancelled via Web Portal", cancelledBy);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to queue web shift cancellation email.");
                    }
                }
                else
                {
                    try { await _alertEmailServices.RemoveFromQueue(existing); } catch { }
                }

                // 2. Build a detailed change message
                try
                {
                    var changes = new List<string>();
                    if (oldGuardId != guardId) changes.Add("Guard changed");
                    if (oldReliefGuardId != reliefGuardId) 
                    {
                        if (reliefGuardId.HasValue) changes.Add("Relief Guard added");
                        else changes.Add("Relief Guard removed");
                    }
                    if (oldStart != start || oldEnd != end) changes.Add("Time changed");
                    if (oldStatusVal != (int)finalStatus) changes.Add($"Status changed to {finalStatus}");

                    string detailedMessage = changes.Count > 0 ? string.Join(", ", changes) : "Shift updated (no major changes)";

                    var userIdString = HttpContext.User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Sid)?.Value;
                    int? parsedUserId = null;
                    if (!string.IsNullOrEmpty(userIdString) && int.TryParse(userIdString, out int uid))
                    {
                        parsedUserId = uid;
                    }

                    _context.RosterScheduleAuditLogs.Add(new RosterScheduleAuditLog
                    {
                        RosterScheduleId = existing.Id,
                        ActionTime = DateTime.Now,
                        UserId = parsedUserId,
                        ActionSource = "Web",
                        Action = "Edited",
                        Details = detailedMessage,
                        IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        Platform = Request.Headers["User-Agent"].ToString(),
                        OldStatus = oldStatusVal,
                        NewStatus = (int)existing.Status
                    });
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create detailed roster audit log for Edit.");
                }
                
                // Real-time broadcast
                var hub = (Microsoft.AspNetCore.SignalR.IHubContext<CityWatch.Common.Models.UpdateHub>)HttpContext.RequestServices.GetService(typeof(Microsoft.AspNetCore.SignalR.IHubContext<CityWatch.Common.Models.UpdateHub>));
                if (hub != null && guardId.HasValue)
                {
                    await hub.Clients.All.SendAsync("RefreshRoster", guardId.Value.ToString());
                }

                var mobileHub = (Microsoft.AspNetCore.SignalR.IHubContext<CityWatch.Data.Services.MobileAppSignalRHub>)HttpContext.RequestServices.GetService(typeof(Microsoft.AspNetCore.SignalR.IHubContext<CityWatch.Data.Services.MobileAppSignalRHub>));
                if (mobileHub != null)
                {
                    await mobileHub.Clients.All.SendAsync("RefreshRoster", new { siteId = existing.ClientSiteId });
                }

                return new JsonResult(new { success = true, id = existing.Id });
            }
            else
            {
                var finalShiftType = shiftType;
                var finalStatus = RosterShiftStatus.Pushed;

                if (shiftType == "RegularAccepted") { finalShiftType = "Regular"; finalStatus = RosterShiftStatus.Accepted; }
                else if (shiftType == "AdhocAccepted") { finalShiftType = "Adhoc"; finalStatus = RosterShiftStatus.Accepted; }
                else if (shiftType == "Declined") { finalShiftType = "Regular"; finalStatus = RosterShiftStatus.Declined; }
                else if (shiftType == "Missed") { finalShiftType = "Regular"; finalStatus = RosterShiftStatus.Missed; }
                else if (shiftType == "Cancelled") { finalShiftType = "Regular"; finalStatus = RosterShiftStatus.Cancelled; }
                else if (shiftType == "Adhoc") { finalShiftType = "Adhoc"; finalStatus = RosterShiftStatus.Pushed; }
                else { finalShiftType = "Regular"; finalStatus = RosterShiftStatus.Pushed; }



                var schedule = new RosterSchedule
                {
                    RosterGroupId = groupId,
                    ClientSiteId = siteId,
                    ShiftStart = start,
                    ShiftEnd = end,
                    GuardId = guardId,
                    ProviderName = providerName,
                    ReliefGuardId = reliefGuardId,
                    ReliefProviderName = reliefProviderName,
                    ReliefReason = reliefReason,
                    Status = finalStatus,
                    PayRateId = payRateId,
                    CallsignId = callsignId,
                    ReliefReasonOther = reliefReasonOther,
                    ShiftType = finalShiftType,
                    AdhocOffsiteText = adhocOffsiteText
                };
                _context.RosterSchedules.Add(schedule);
                await _context.SaveChangesAsync();

                try
                {
                    var userIdString = HttpContext.User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Sid)?.Value;
                    int? parsedUserId = null;
                    if (!string.IsNullOrEmpty(userIdString) && int.TryParse(userIdString, out int uid))
                    {
                        parsedUserId = uid;
                    }

                    _context.RosterScheduleAuditLogs.Add(new RosterScheduleAuditLog
                    {
                        RosterScheduleId = schedule.Id, // Captured after SaveChanges
                        ActionTime = DateTime.Now,
                        UserId = parsedUserId,
                        ActionSource = "Web",
                        Action = "Created",
                        Details = "Shift created by admin.",
                        IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        Platform = Request.Headers["User-Agent"].ToString(),
                        OldStatus = null,
                        NewStatus = (int)schedule.Status
                    });
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create roster audit log for Add.");
                }

                // Real-time broadcast
                var hubNew = (Microsoft.AspNetCore.SignalR.IHubContext<CityWatch.Common.Models.UpdateHub>)HttpContext.RequestServices.GetService(typeof(Microsoft.AspNetCore.SignalR.IHubContext<CityWatch.Common.Models.UpdateHub>));
                if (hubNew != null && guardId.HasValue)
                {
                    await hubNew.Clients.All.SendAsync("RefreshRoster", guardId.Value.ToString());
                }

                var mobileHubNew = (Microsoft.AspNetCore.SignalR.IHubContext<CityWatch.Data.Services.MobileAppSignalRHub>)HttpContext.RequestServices.GetService(typeof(Microsoft.AspNetCore.SignalR.IHubContext<CityWatch.Data.Services.MobileAppSignalRHub>));
                if (mobileHubNew != null)
                {
                    await mobileHubNew.Clients.All.SendAsync("RefreshRoster", new { siteId = schedule.ClientSiteId });
                }

                return new JsonResult(new { success = true, id = schedule.Id });
            }
        }

        public JsonResult OnGetSearchPayRates(string search, int? groupId, int? id)
        {
            var query = _context.PayRates.Include(x => x.PayRateGroup).Where(x => !x.IsDeleted);

            if (id.HasValue)
            {
                query = query.Where(x => x.Id == id.Value);
            }
            else
            {
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.Description.Contains(search));
                
                if (groupId.HasValue)
                    query = query.Where(x => x.PayRateGroupId == groupId);
            }

            var rates = query.Select(x => new
            {
                id = x.Id,
                text = x.Description,
                guardPayRate = x.GuardPayRate,
                groupId = x.PayRateGroupId,
                groupName = x.PayRateGroup != null ? x.PayRateGroup.Name : "No Group"
            }).ToList();

            return new JsonResult(new { results = rates });
        }

        public JsonResult OnGetSearchPayRateGroups(string search, int? siteId)
        {
            var query = _context.PayRateGroups.Where(x => !x.IsDeleted);

            if (siteId.HasValue)
            {
                // Filter by site assignment if it exists
                var assignedGroupIds = _context.PayRateGroupSites
                    .Where(x => x.ClientSiteId == siteId.Value)
                    .Select(x => x.PayRateGroupId)
                    .ToList();

                if (assignedGroupIds.Any())
                {
                    query = query.Where(x => assignedGroupIds.Contains(x.Id));
                }
            }

            var groups = query
                .Where(x => string.IsNullOrEmpty(search) || x.Name.Contains(search))
                .OrderBy(x => x.Name)
                .Select(x => new
                {
                    id = x.Id,
                    text = x.Name
                })
                .ToList();

            return new JsonResult(new { results = groups });
        }

        public async Task<JsonResult> OnGetSiteDefaultAssignment(int siteId)
        {
            var assignment = await _context.PayRateGroupSites
                .Where(x => x.ClientSiteId == siteId)
                .Include(x => x.PayRateGroup)
                .FirstOrDefaultAsync();

            if (assignment != null && assignment.PayRateGroup != null)
            {
                return new JsonResult(new { 
                    success = true, 
                    groupId = assignment.PayRateGroup.Id, 
                    groupName = assignment.PayRateGroup.Name 
                });
            }

            return new JsonResult(new { success = false });
        }

        public async Task<IActionResult> OnPostUpdateStatus(int id, int status)
        {
            var schedule = await _context.RosterSchedules.FindAsync(id);
            if (schedule != null)
            {
                var today = DateTime.Today;
                var firstDayOfCurrentMonth = new DateTime(today.Year, today.Month, 1);
                var weekEndDate = StartOfWeek(schedule.ShiftStart, GetFirstDayOfWeek()).AddDays(6);
                if (weekEndDate < firstDayOfCurrentMonth)
                {
                    return new JsonResult(new { success = false, message = "Changes to previous months are locked." });
                }

                schedule.Status = (RosterShiftStatus)status;
                await _context.SaveChangesAsync();

                // Real-time broadcast
                var hub = (Microsoft.AspNetCore.SignalR.IHubContext<CityWatch.Common.Models.UpdateHub>)HttpContext.RequestServices.GetService(typeof(Microsoft.AspNetCore.SignalR.IHubContext<CityWatch.Common.Models.UpdateHub>));
                if (hub != null && schedule.GuardId.HasValue)
                {
                    await hub.Clients.All.SendAsync("RefreshRoster", schedule.GuardId.Value.ToString());
                }

                var mobileHub = (Microsoft.AspNetCore.SignalR.IHubContext<CityWatch.Data.Services.MobileAppSignalRHub>)HttpContext.RequestServices.GetService(typeof(Microsoft.AspNetCore.SignalR.IHubContext<CityWatch.Data.Services.MobileAppSignalRHub>));
                if (mobileHub != null)
                {
                    await mobileHub.Clients.All.SendAsync("RefreshRoster", new { siteId = schedule.ClientSiteId });
                }
            }
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostDeleteShift(int id)
        {
            var schedule = await _context.RosterSchedules.FindAsync(id);
            if (schedule != null)
            {
                var today = DateTime.Today;
                var firstDayOfCurrentMonth = new DateTime(today.Year, today.Month, 1);
                var weekEndDate = StartOfWeek(schedule.ShiftStart, GetFirstDayOfWeek()).AddDays(6);
                if (weekEndDate < firstDayOfCurrentMonth)
                {
                    return new JsonResult(new { success = false, message = "Changes to previous months are locked." });
                }

                int oldStatusVal = (int)schedule.Status;
                schedule.IsDeleted = true;

                // 1. Save the actual shift deletion first
                await _context.SaveChangesAsync();

                // 2. Separately try to log the audit entry
                try
                {
                    var userIdString = HttpContext.User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Sid)?.Value;
                    int? parsedUserId = null;
                    if (!string.IsNullOrEmpty(userIdString) && int.TryParse(userIdString, out int uid))
                    {
                        parsedUserId = uid;
                    }

                    _context.RosterScheduleAuditLogs.Add(new RosterScheduleAuditLog
                    {
                        RosterScheduleId = schedule.Id,
                        ActionTime = DateTime.Now,
                        UserId = parsedUserId,
                        ActionSource = "Web",
                        Action = "Deleted",
                        Details = "Shift was deleted by admin.",
                        IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        Platform = Request.Headers["User-Agent"].ToString(),
                        OldStatus = oldStatusVal,
                        NewStatus = null
                    });
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create roster audit log for Delete.");
                }

                var mobileHub = (Microsoft.AspNetCore.SignalR.IHubContext<CityWatch.Data.Services.MobileAppSignalRHub>)HttpContext.RequestServices.GetService(typeof(Microsoft.AspNetCore.SignalR.IHubContext<CityWatch.Data.Services.MobileAppSignalRHub>));
                if (mobileHub != null)
                {
                    await mobileHub.Clients.All.SendAsync("RefreshRoster", new { siteId = schedule.ClientSiteId });
                }
            }
            return new JsonResult(new { success = true });
        }

        public JsonResult OnGetSearchGuards(string search, int? siteId)
        {
            var providerList = _viewDataService.ProviderList;
            var query = _context.Guards.Where(x => x.IsActive);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(x => x.Name.Contains(search) || (x.SecurityNo != null && x.SecurityNo.Contains(search)));
            }

            if (siteId.HasValue && siteId.Value > 0)
            {
                var limitDate = DateTime.Now.AddDays(-120);
                var siteGuardIds = _context.GuardLogins
                    .Where(x => x.ClientSiteId == siteId.Value && x.LoginDate >= limitDate)
                    .Select(x => x.GuardId)
                    .Distinct()
                    .ToList();

                if (siteGuardIds.Any())
                {
                    query = query.Where(x => siteGuardIds.Contains(x.Id));
                }
            }

            var guards = query
                .Select(x => new {
                    id = x.Id,
                    text = x.Name + (string.IsNullOrEmpty(x.SecurityNo) ? "" : " - " + x.SecurityNo),
                    license = x.SecurityNo ?? "N/A",
                    state = x.State ?? "N/A",
                    provider = x.Provider ?? "N/A"
                })
                .ToList()
                .Select(g =>
                {
                    var pId = providerList
                        .FirstOrDefault(p => string.Equals(p.Text, g.provider, StringComparison.OrdinalIgnoreCase))?.Value;

                    return new
                    {
                        id = g.id,
                        text = g.text,
                        license = g.license,
                        state = g.state,
                        provider = g.provider,
                        providerId = pId // Add ProviderId to response
                    };
                })
                .ToList();

            return new JsonResult(new { results = guards });
        }

        public JsonResult OnGetSearchCallsigns(string search, int? siteId)
        {
            var query = _context.IncidentReportFields
                .Where(x => x.TypeId == ReportFieldType.CallSign);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(x => x.Name.Contains(search));
            }

            var results = query
                .Select(x => new {
                    id = x.Id,
                    text = x.Name
                })
                .ToList();

            return new JsonResult(new { results = results });
        }

        public async Task<IActionResult> OnPostDeleteGroup(int groupId)
        {
            var group = await _context.RosterGroups.FindAsync(groupId);
            if (group != null)
            {
                // Delete all associated data
                var sites = await _context.RosterGroupSites.Where(x => x.RosterGroupId == groupId).ToListAsync();
                _context.RosterGroupSites.RemoveRange(sites);

                var schedules = await _context.RosterSchedules.Where(x => x.RosterGroupId == groupId).ToListAsync();
                _context.RosterSchedules.RemoveRange(schedules);

                _context.RosterGroups.Remove(group);
                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true });
            }
            return new JsonResult(new { success = false, message = "Project not found." });
        }

        public async Task<IActionResult> OnPostEditGroup(int groupId, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return new JsonResult(new { success = false, message = "Project name is required." });
            }

            var group = await _context.RosterGroups.FindAsync(groupId);
            if (group != null)
            {
                group.Name = name;
                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true });
            }
            return new JsonResult(new { success = false, message = "Project not found." });
        }

        public async Task<IActionResult> OnPostCreateGroup(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return new JsonResult(new { success = false, message = "Project name is required." });
            }

            var group = new RosterGroup
            {
                Name = name,
                IsDeleted = false
            };

            _context.RosterGroups.Add(group);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, id = group.Id });
        }

        public async Task<IActionResult> OnGetDownloadPdf(int? groupId, int? binderId, DateTime startDate, int weeks = 1, bool includeFinancials = false, bool includeSuppliers = false, string rateType = "guard")
        {
            byte[] pdfBytes = null;
            string fileName = "";

            if (binderId.HasValue && binderId.Value > 0)
            {
                pdfBytes = await _rosterReportGenerator.GenerateBinderRosterPdfAsync(binderId.Value, startDate, weeks, includeFinancials, includeSuppliers, rateType);
                var binderName = await _context.RosterBinders.Where(x => x.Id == binderId.Value).Select(x => x.Name).FirstOrDefaultAsync();
                fileName = $"Roster_Group_{binderName}_{startDate:yyyyMMdd}.pdf";
            }
            else if (groupId.HasValue && groupId.Value > 0)
            {
                pdfBytes = await _rosterReportGenerator.GenerateRosterPdfAsync(groupId.Value, startDate, weeks, includeFinancials, includeSuppliers, rateType);
                var groupName = await _context.RosterGroups.Where(x => x.Id == groupId.Value).Select(x => x.Name).FirstOrDefaultAsync();
                fileName = $"Roster_{groupName}_{startDate:yyyyMMdd}.pdf";
            }

            if (pdfBytes != null)
            {
                return File(pdfBytes, "application/pdf", fileName);
            }
            return NotFound();
        }

        public JsonResult OnGetSearchBinders(string search)
        {
            var binders = _context.RosterBinders
                .Where(x => !x.IsDeleted && (string.IsNullOrEmpty(search) || x.Name.Contains(search)))
                .OrderBy(x => x.Name)
                .Select(x => new { id = x.Id, text = x.Name })
                .ToList();

            return new JsonResult(new { results = binders });
        }

        public async Task<JsonResult> OnGetLoadBinderRoster(int binderId, DateTime startDate)
        {
            this.StartDate = startDate;
            this.EndDate = startDate.AddDays(6);
            PopulateWeeklyHolidays();

            var endDate = startDate.AddDays(6).AddDays(1).AddSeconds(-1);

            var binderProjects = await _context.RosterBinderProjects
                .Where(x => x.RosterBinderId == binderId)
                .Include(x => x.RosterGroup)
                .ToListAsync();

            var projectIds = binderProjects.Select(bp => bp.RosterGroupId).ToList();

            var schedules = await _context.RosterSchedules
                .Where(x => projectIds.Contains(x.RosterGroupId) && !x.IsDeleted && x.ShiftStart >= startDate && x.ShiftStart <= endDate)
                .Include(x => x.Guard)
                .Include(x => x.ReliefGuard)
                .Include(x => x.Callsign)
                .Include(x => x.PayRate)
                .ToListAsync();

            var rosterData = new List<object>();

            foreach (var bp in binderProjects.OrderBy(x => x.SortOrder).ThenBy(x => x.Id))
            {
                var groupSites = await _context.RosterGroupSites
                    .Where(x => x.RosterGroupId == bp.RosterGroupId)
                    .Include(x => x.ClientSite)
                    .ThenInclude(x => x.ClientType)
                    .ToListAsync();

                // Binder Site Status Sync
                // Retrieves site-specific statuses for the binder/group view to ensure consistency with single project views.
                var groupSiteIds = groupSites.Select(gs => gs.ClientSiteId).ToList();
                var groupWeekStatuses = await _context.RosterSiteWeekStatuses
                    .Where(x => groupSiteIds.Contains(x.ClientSiteId) && x.StartDate == startDate)
                    .ToListAsync();

                var projectSites = groupSites.Select(gs => new
                {
                    siteId = gs.ClientSiteId,
                    siteName = gs.ClientSite.Name,
                    clientTypeName = gs.ClientSite.ClientType?.Name ?? "N/A",
                    // Display status indicator in Binder grid
                    status = groupWeekStatuses.FirstOrDefault(ws => ws.ClientSiteId == gs.ClientSiteId)?.Status ?? "Live",
                    projectId = bp.RosterGroupId,
                    projectName = bp.RosterGroup.Name,
                    isPublicHoliday = GetPublicHolidayFlags(gs.ClientSite.State, startDate),
                    publicHolidayReasons = GetPublicHolidayReasons(gs.ClientSite.State, startDate),
                    days = Enumerable.Range(0, 7).Select(dayOffset =>
                    {
                        var targetDate = startDate.AddDays(dayOffset);
                        return schedules
                            .Where(s => s.RosterGroupId == bp.RosterGroupId && s.ClientSiteId == gs.ClientSiteId && s.ShiftStart.Date == targetDate.Date)
                            .OrderBy(s => s.ShiftStart)
                            .Select(s => new
                            {
                                id = s.Id,
                                guardId = s.GuardId,
                                guardName = s.GuardId.HasValue ? s.Guard.Name : (s.ProviderName ?? "Unassigned"),
                                guardLicense = s.GuardId.HasValue ? (s.Guard.SecurityNo ?? "N/A") : "External",
                                guardState = s.GuardId.HasValue ? (s.Guard.State ?? "N/A") : "N/A",
                                guardProvider = !string.IsNullOrEmpty(s.ProviderName) ? s.ProviderName : (s.GuardId.HasValue ? (s.Guard.Provider ?? "N/A") : "N/A"),
                                providerName = s.ProviderName,
                                payRateId = s.PayRateId,
                                payRateName = s.PayRate != null ? s.PayRate.Description : "N/A",
                                shiftStart = s.ShiftStart.ToString("HH:mm"),
                                shiftEnd = s.ShiftEnd.ToString("HH:mm"),
                                callsignId = s.CallsignId,
                                callsignName = s.Callsign?.Name ?? "",
                                status = (int)s.Status,
                                durationHours = DateTimeHelper.CalculateDisplayDuration(s.ShiftStart, s.ShiftEnd),
                                payRate = s.PayRate != null ? s.PayRate.GuardPayRate : 0,
                                sellRate = s.PayRate != null ? s.PayRate.SellRateToClient : 0,
                                reliefGuardId = s.ReliefGuardId,
                                reliefGuardName = s.ReliefGuard?.Name ?? "",
                                reliefGuardLicense = s.ReliefGuardId.HasValue ? (s.ReliefGuard.SecurityNo ?? "N/A") : "",
                                reliefProviderName = s.ReliefProviderName ?? "",
                                reliefReason = s.ReliefReason ?? "",
                                reliefReasonOther = s.ReliefReasonOther ?? "",
                                shiftType = s.ShiftType ?? "Regular",
                                adhocOffsiteText = s.AdhocOffsiteText ?? ""
                            })
                            .ToList();
                    }).ToList()
                }).ToList();

                rosterData.Add(new { 
                    projectName = bp.RosterGroup.Name, 
                    projectId = bp.RosterGroupId, 
                    sites = projectSites,
                    projectStatus = projectSites.FirstOrDefault()?.status ?? "Live"
                });
            }

            return new JsonResult(new { results = rosterData });
        }

        public async Task<IActionResult> OnPostCreateBinder(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return new JsonResult(new { success = false, message = "Group name is required." });
            }

            var exists = await _context.RosterBinders.AnyAsync(x => x.Name == name.Trim() && !x.IsDeleted);
            if (exists)
            {
                return new JsonResult(new { success = false, message = "A group with this name already exists." });
            }

            var binder = new RosterBinder
            {
                Name = name.Trim(),
                IsDeleted = false
            };

            _context.RosterBinders.Add(binder);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, id = binder.Id });
        }

        public async Task<IActionResult> OnPostEditBinder(int binderId, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return new JsonResult(new { success = false, message = "Group name is required." });
            }

            var exists = await _context.RosterBinders.AnyAsync(x => x.Name == name.Trim() && x.Id != binderId && !x.IsDeleted);
            if (exists)
            {
                return new JsonResult(new { success = false, message = "A group with this name already exists." });
            }

            var binder = await _context.RosterBinders.FindAsync(binderId);
            if (binder != null)
            {
                binder.Name = name.Trim();
                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true });
            }
            return new JsonResult(new { success = false, message = "Group not found." });
        }

        public async Task<IActionResult> OnPostDeleteBinder(int binderId)
        {
            var binder = await _context.RosterBinders.FindAsync(binderId);
            if (binder != null)
            {
                var projects = await _context.RosterBinderProjects.Where(x => x.RosterBinderId == binderId).ToListAsync();
                _context.RosterBinderProjects.RemoveRange(projects);

                _context.RosterBinders.Remove(binder);
                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true });
            }
            return new JsonResult(new { success = false, message = "Group not found." });
        }

        public async Task<IActionResult> OnPostAddProjectToBinder(int binderId, int projectId)
        {
            var projectExists = await _context.RosterGroups.AnyAsync(x => x.Id == projectId && !x.IsDeleted);
            if (!projectExists)
            {
                return new JsonResult(new { success = false, message = "Project not found or already deleted." });
            }

            var exists = await _context.RosterBinderProjects.AnyAsync(x => x.RosterBinderId == binderId && x.RosterGroupId == projectId);
            if (!exists)
            {
                var maxSortOrder = await _context.RosterBinderProjects
                    .Where(x => x.RosterBinderId == binderId)
                    .Select(x => (int?)x.SortOrder)
                    .MaxAsync() ?? 0;

                _context.RosterBinderProjects.Add(new RosterBinderProject
                {
                    RosterBinderId = binderId,
                    RosterGroupId = projectId,
                    SortOrder = maxSortOrder + 1
                });
                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true });
            }
            return new JsonResult(new { success = false, message = "This project is already added to this group." });
        }

        public async Task<IActionResult> OnPostDeleteProjectFromBinder(int binderId, int projectId)
        {
            var item = await _context.RosterBinderProjects.FirstOrDefaultAsync(x => x.RosterBinderId == binderId && x.RosterGroupId == projectId);
            if (item != null)
            {
                _context.RosterBinderProjects.Remove(item);
                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true });
            }
            return new JsonResult(new { success = false, message = "Project not found in group." });
        }

        public async Task<IActionResult> OnPostCheckFutureData(int groupId, DateTime startDate, string option)
        {
            try
            {
                var endDate = startDate.AddDays(7);
                var hasSourceData = await _context.RosterSchedules
                    .AnyAsync(x => x.RosterGroupId == groupId && !x.IsDeleted && x.ShiftStart >= startDate && x.ShiftStart < endDate);

                DateTime copyUntil;
                if (option == "NextWeek") copyUntil = startDate.AddDays(14);
                else if (option == "Month") copyUntil = new DateTime(startDate.Year, startDate.Month, 1).AddMonths(1);
                else if (option == "Year") copyUntil = new DateTime(startDate.Year, 12, 31).AddDays(1);
                else return new JsonResult(new { success = false, message = "Invalid option." });

                var targetStart = startDate.AddDays(7);
                var hasData = await _context.RosterSchedules
                    .Where(x => x.RosterGroupId == groupId && !x.IsDeleted && x.ShiftStart >= targetStart && x.ShiftStart < copyUntil)
                    .AnyAsync();

                return new JsonResult(new { success = true, hasSourceData, hasData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking future roster data");
                return new JsonResult(new { success = false, message = "An error occurred checking future data." });
            }
        }

        public async Task<IActionResult> OnPostRolloverRoster(int groupId, DateTime startDate, string option, bool eraseFuture = false, bool clearNames = false)
        {
            try
            {
                var endDate = startDate.AddDays(7);
                var sourceSchedules = await _context.RosterSchedules
                    .Where(x => x.RosterGroupId == groupId && !x.IsDeleted && x.ShiftStart >= startDate && x.ShiftStart < endDate)
                    .ToListAsync();

                var today = DateTime.Today;
                var firstDayOfCurrentMonth = new DateTime(today.Year, today.Month, 1);

                DateTime copyUntil;
                if (option == "NextWeek")
                {
                    copyUntil = startDate.AddDays(14);
                }
                else if (option == "Month")
                {
                    // End of current month
                    copyUntil = new DateTime(startDate.Year, startDate.Month, 1).AddMonths(1);
                }
                else if (option == "Year")
                {
                    // End of current year
                    copyUntil = new DateTime(startDate.Year, 12, 31).AddDays(1);
                }
                else
                {
                    return new JsonResult(new { success = false, message = "Invalid option." });
                }

                var targetWeeks = new List<DateTime>();
                var currentTargetStart = startDate.AddDays(7);
                var targetStart = currentTargetStart;

                if (eraseFuture)
                {
                    var shiftsToDelete = await _context.RosterSchedules
                        .Where(x => x.RosterGroupId == groupId && !x.IsDeleted && x.ShiftStart >= targetStart && x.ShiftStart < copyUntil)
                        .ToListAsync();
                    
                    // Do not delete shifts that are in previous/locked months
                    var firstDayOfWeek = GetFirstDayOfWeek();
                    var shiftsAllowedToDelete = shiftsToDelete.Where(x => StartOfWeek(x.ShiftStart, firstDayOfWeek).AddDays(6) >= firstDayOfCurrentMonth).ToList();
                    
                    foreach (var shift in shiftsAllowedToDelete)
                    {
                        shift.IsDeleted = true;
                    }
                    await _context.SaveChangesAsync();
                }

                while (currentTargetStart < copyUntil)
                {
                    targetWeeks.Add(currentTargetStart);
                    currentTargetStart = currentTargetStart.AddDays(7);
                }

                foreach (var targetWeekStart in targetWeeks)
                {
                    if (targetWeekStart.AddDays(6) < firstDayOfCurrentMonth) continue; // Safety check

                    foreach (var source in sourceSchedules)
                    {
                        var dayOffset = (source.ShiftStart - startDate).Days;
                        var newStart = targetWeekStart.AddDays(dayOffset).Add(source.ShiftStart.TimeOfDay);
                        var newEnd = targetWeekStart.AddDays(dayOffset).Add(source.ShiftEnd.TimeOfDay);

                        // Handle overnight shifts
                        if (source.ShiftEnd.Date > source.ShiftStart.Date)
                        {
                            var crossoverDays = (source.ShiftEnd - source.ShiftStart).Days;
                            newEnd = newStart.Add(source.ShiftEnd - source.ShiftStart);
                        }

                        // Check for duplicate or existing shift to update (for Merge support)
                        var checkGuardId = clearNames ? null : source.GuardId;
                        var checkProviderName = clearNames ? null : source.ProviderName;

                        var existingShift = await _context.RosterSchedules.FirstOrDefaultAsync(x =>
                            x.RosterGroupId == groupId &&
                            x.ClientSiteId == source.ClientSiteId &&
                            x.ShiftStart == newStart &&
                            x.GuardId == checkGuardId &&
                            x.ProviderName == checkProviderName &&
                            x.CallsignId == source.CallsignId &&
                            !x.IsDeleted);

                        if (existingShift == null)
                        {
                            _context.RosterSchedules.Add(new RosterSchedule
                            {
                                RosterGroupId = groupId,
                                ClientSiteId = source.ClientSiteId,
                                GuardId = checkGuardId,
                                ProviderName = checkProviderName,
                                ShiftStart = newStart,
                                ShiftEnd = newEnd,
                                Status = RosterShiftStatus.Pushed, // Reset status to Pushed for new shifts
                                PayRateId = source.PayRateId,
                                CallsignId = source.CallsignId,
                                ReliefGuardId = clearNames ? null : source.ReliefGuardId,
                                ReliefProviderName = clearNames ? null : source.ReliefProviderName,
                                ReliefReason = clearNames ? null : source.ReliefReason,
                                ReliefReasonOther = clearNames ? null : source.ReliefReasonOther,
                                ShiftType = source.ShiftType
                            });
                        }
                        else
                        {
                            // Update existing shift with relief details and type (Support for Preserving Relief on Merge)
                            existingShift.ShiftEnd = newEnd; // Sync end time if it changed
                            existingShift.ReliefGuardId = clearNames ? null : source.ReliefGuardId;
                            existingShift.ReliefProviderName = clearNames ? null : source.ReliefProviderName;
                            existingShift.ReliefReason = clearNames ? null : source.ReliefReason;
                            existingShift.ReliefReasonOther = clearNames ? null : source.ReliefReasonOther;
                            existingShift.ShiftType = source.ShiftType;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Roster Rollover");
                return new JsonResult(new { success = false, message = "An error occurred during rollover." });
            }
        }

        public async Task<JsonResult> OnGetGuardDailySchedule(int guardId, DateTime date)
        {
            var schedules = await _context.RosterSchedules
                .Include(x => x.ClientSite)
                .Include(x => x.Guard)
                .Include(x => x.ReliefGuard)
                .Where(x => (x.GuardId == guardId || x.ReliefGuardId == guardId) &&
                            !x.IsDeleted && x.Status != RosterShiftStatus.Cancelled &&
                            x.ShiftStart.Date <= date.Date && x.ShiftEnd.Date >= date.Date)
                .OrderBy(x => x.ShiftStart)
                .Select(x => new
                {
                    projectName = x.ClientSite != null ? x.ClientSite.Name : "Unknown",
                    date = x.ShiftStart.ToString("dd MMM yyyy"),
                    startTime = x.ShiftStart.ToString("HH:mm"),
                    endTime = x.ShiftEnd.ToString("HH:mm"),
                    isRelief = x.ReliefGuardId == guardId,
                    reliefName = x.ReliefGuardId != null ? x.ReliefGuard.Name : x.ReliefProviderName,
                    mainGuardName = x.GuardId != null ? x.Guard.Name : x.ProviderName
                })
                .ToListAsync();

            return new JsonResult(new { success = true, data = schedules });
        }

        public JsonResult OnGetSearchSites(string search)
        {
            var results = _viewDataService.GetUserClientSites(string.Empty, search);

            var select2Data = results.Select(x => new
            {
                id = x.Id,
                text = $"{x.Name} ({x.ClientType?.Name ?? "N/A"})"
            }).ToList();

            return new JsonResult(new { results = select2Data });
        }
        public async Task<JsonResult> OnGetLoadSettingsProjects()
        {
            var projects = await _context.RosterGroups
                .Where(x => !x.IsDeleted)
                .Select(x => new {
                    x.Id,
                    x.Name,
                    x.CoverFileName,
                    CoverFileDate = x.CoverFileDate.HasValue ? x.CoverFileDate.Value.ToString("dd MMM yyyy @ HH:mm") : null
                })
                .OrderBy(x => x.Name)
                .ToListAsync();

            return new JsonResult(projects);
        }

        public async Task<JsonResult> OnGetLoadSettingsGroups()
        {
            var groups = await _context.RosterBinders
                .Where(x => !x.IsDeleted)
                .Select(x => new {
                    x.Id,
                    x.Name,
                    x.CoverFileName,
                    x.AccessKey,
                    CoverFileDate = x.CoverFileDate.HasValue ? x.CoverFileDate.Value.ToString("dd MMM yyyy @ HH:mm") : null
                })
                .OrderBy(x => x.Name)
                .ToListAsync();

            return new JsonResult(groups);
        }

        public async Task<IActionResult> OnPostUploadProjectCover(int id, IFormFile file)
        {
            if (file == null || file.Length == 0) return new JsonResult(new { success = false, message = "No file uploaded." });
            if (Path.GetExtension(file.FileName).ToLower() != ".pdf") return new JsonResult(new { success = false, message = "Only PDF files are allowed." });

            var project = await _context.RosterGroups.FindAsync(id);
            if (project == null) return new JsonResult(new { success = false, message = "Project not found." });

            string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "Uploads", "RosterCovers", "Projects");
            if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

            if (!string.IsNullOrEmpty(project.CoverFileName))
            {
                string oldPath = Path.Combine(uploadDir, project.CoverFileName);
                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
            }

            string fileName = $"Project_{id}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            string filePath = Path.Combine(uploadDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            project.CoverFileName = fileName;
            project.CoverFileDate = DateTime.Now;
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostUploadGroupCover(int id, IFormFile file)
        {
            if (file == null || file.Length == 0) return new JsonResult(new { success = false, message = "No file uploaded." });
            if (Path.GetExtension(file.FileName).ToLower() != ".pdf") return new JsonResult(new { success = false, message = "Only PDF files are allowed." });

            var binder = await _context.RosterBinders.FindAsync(id);
            if (binder == null) return new JsonResult(new { success = false, message = "Group not found." });

            string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "Uploads", "RosterCovers", "Groups");
            if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

            if (!string.IsNullOrEmpty(binder.CoverFileName))
            {
                string oldPath = Path.Combine(uploadDir, binder.CoverFileName);
                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
            }

            string fileName = $"Group_{id}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            string filePath = Path.Combine(uploadDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            binder.CoverFileName = fileName;
            binder.CoverFileDate = DateTime.Now;
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostDeleteProjectCover(int id)
        {
            var project = await _context.RosterGroups.FindAsync(id);
            if (project == null) return new JsonResult(new { success = false, message = "Project not found." });

            if (!string.IsNullOrEmpty(project.CoverFileName))
            {
                string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "Uploads", "RosterCovers", "Projects", project.CoverFileName);
                if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
            }

            project.CoverFileName = null;
            project.CoverFileDate = null;
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostDeleteGroupCover(int id)
        {
            var binder = await _context.RosterBinders.FindAsync(id);
            if (binder == null) return new JsonResult(new { success = false, message = "Group not found." });

            if (!string.IsNullOrEmpty(binder.CoverFileName))
            {
                string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "Uploads", "RosterCovers", "Groups", binder.CoverFileName);
                if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
            }

            binder.CoverFileName = null;
            binder.CoverFileDate = null;
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnGetDownloadCover(string type, int id)
        {
            string fileName = "";
            string subDir = "";

            if (type == "project")
            {
                var project = await _context.RosterGroups.FindAsync(id);
                if (project == null || string.IsNullOrEmpty(project.CoverFileName)) return NotFound();
                fileName = project.CoverFileName;
                subDir = "Projects";
            }
            else
            {
                var binder = await _context.RosterBinders.FindAsync(id);
                if (binder == null || string.IsNullOrEmpty(binder.CoverFileName)) return NotFound();
                fileName = binder.CoverFileName;
                subDir = "Groups";
            }

            string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "Uploads", "RosterCovers", subDir, fileName);
            if (!System.IO.File.Exists(filePath)) return NotFound();

            byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, "application/pdf", fileName);
        }

        public async Task<IActionResult> OnGetPreviewCover(string type, int id)
        {
            byte[] fileBytes = await _rosterReportGenerator.GeneratePreviewRosterPdfAsync(type, id);
            if (fileBytes == null) return NotFound();
            return File(fileBytes, "application/pdf");
        }

        public async Task<IActionResult> OnPostMoveBinderProject(int binderId, int projectId, string direction)
        {
            var projects = await _context.RosterBinderProjects
                .Where(x => x.RosterBinderId == binderId)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();

            var currentIdx = projects.FindIndex(x => x.RosterGroupId == projectId);
            if (currentIdx == -1) return new JsonResult(new { success = false, message = "Project not found in group." });

            // Normalize all sort orders to their current index to ensure movement is consistent
            for (int i = 0; i < projects.Count; i++)
            {
                projects[i].SortOrder = i;
            }

            if (direction == "up" && currentIdx > 0)
            {
                projects[currentIdx].SortOrder = currentIdx - 1;
                projects[currentIdx - 1].SortOrder = currentIdx;
            }
            else if (direction == "down" && currentIdx < projects.Count - 1)
            {
                projects[currentIdx].SortOrder = currentIdx + 1;
                projects[currentIdx + 1].SortOrder = currentIdx;
            }
            else
            {
                return new JsonResult(new { success = true });
            }

            await _context.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostCycleShiftType(int id)
        {
            var schedule = await _context.RosterSchedules.FindAsync(id);
            if (schedule == null) return new JsonResult(new { success = false, message = "Shift not found." });

            var today = DateTime.Today;
            var firstDayOfCurrentMonth = new DateTime(today.Year, today.Month, 1);
            var weekEndDate = StartOfWeek(schedule.ShiftStart, GetFirstDayOfWeek()).AddDays(6);
            if (weekEndDate < firstDayOfCurrentMonth)
            {
                return new JsonResult(new { success = false, message = "Changes to previous months are locked." });
            }

            // Cycle: Regular -> Adhoc -> RegularAccepted -> AdhocAccepted -> Declined -> Regular
            var currentType = schedule.ShiftType ?? "Regular";
            var currentStatus = schedule.Status;

            if (currentStatus == RosterShiftStatus.Cancelled)
            {
                return new JsonResult(new { success = false, message = "Cancelled shifts cannot be cycled. Use Edit to change status." });
            }
            
            var nextType = "Regular";
            var nextStatus = RosterShiftStatus.Pushed;

            if (currentType == "Regular" && currentStatus == RosterShiftStatus.Pushed)
            {
                nextType = "Adhoc";
                nextStatus = RosterShiftStatus.Pushed;
            }
            else if (currentType == "Adhoc" && currentStatus == RosterShiftStatus.Pushed)
            {
                nextType = "Regular";
                nextStatus = RosterShiftStatus.Accepted;
            }
            else if (currentType == "Regular" && currentStatus == RosterShiftStatus.Accepted)
            {
                nextType = "Adhoc";
                nextStatus = RosterShiftStatus.Accepted;
            }
            else if (currentType == "Adhoc" && currentStatus == RosterShiftStatus.Accepted)
            {
                nextType = "Regular";
                nextStatus = RosterShiftStatus.Declined;
            }
            else if (currentType == "Regular" && currentStatus == RosterShiftStatus.Declined)
            {
                nextType = "Regular";
                nextStatus = RosterShiftStatus.Missed;
            }
            else
            {
                nextType = "Regular";
                nextStatus = RosterShiftStatus.Pushed;
            }

            schedule.ShiftType = nextType;
            schedule.Status = nextStatus;

            await _context.SaveChangesAsync();

            // Queue web cancellation email if applicable
            if (nextStatus == RosterShiftStatus.Declined || nextStatus == RosterShiftStatus.Cancelled)
            {
                try
                {
                    var userName = HttpContext.User.Identity?.Name ?? "Admin";
                    bool isGuard = HttpContext.User.IsInRole("Guard");
                    string cancelledBy = isGuard ? $"Guard|{userName}" : userName;

                    await _alertEmailServices.QueueWebShiftCancellation(schedule, "Cancelled via Status Cycle", cancelledBy);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to queue web shift cancellation email from cycle.");
                }
            }
            else
            {
                try { await _alertEmailServices.RemoveFromQueue(schedule); } catch { }
            }

            // Real-time broadcast
            var hub = (Microsoft.AspNetCore.SignalR.IHubContext<CityWatch.Common.Models.UpdateHub>)HttpContext.RequestServices.GetService(typeof(Microsoft.AspNetCore.SignalR.IHubContext<CityWatch.Common.Models.UpdateHub>));
            if (hub != null && schedule.GuardId.HasValue)
            {
                await hub.Clients.All.SendAsync("RefreshRoster", schedule.GuardId.Value.ToString());
            }

            var mobileHub = (Microsoft.AspNetCore.SignalR.IHubContext<CityWatch.Data.Services.MobileAppSignalRHub>)HttpContext.RequestServices.GetService(typeof(Microsoft.AspNetCore.SignalR.IHubContext<CityWatch.Data.Services.MobileAppSignalRHub>));
            if (mobileHub != null)
            {
                await mobileHub.Clients.All.SendAsync("RefreshRoster", new { siteId = schedule.ClientSiteId });
            }

            return new JsonResult(new { success = true, shiftType = nextType, status = (int)nextStatus });
        }

        public async Task<IActionResult> OnPostSaveProjectRosterStatus(int projectId, DateTime startDate, string status)
        {
            var projectSites = await _context.RosterGroupSites
                .Where(x => x.RosterGroupId == projectId)
                .Select(x => x.ClientSiteId)
                .ToListAsync();

            if (!projectSites.Any()) return new JsonResult(new { success = false, message = "No sites found in this project." });

            foreach (var siteId in projectSites)
            {
                var statusObj = await _context.RosterSiteWeekStatuses
                    .FirstOrDefaultAsync(x => x.ClientSiteId == siteId && x.StartDate.Date == startDate.Date);

                if (statusObj == null)
                {
                    statusObj = new RosterSiteWeekStatus
                    {
                        ClientSiteId = siteId,
                        StartDate = startDate.Date,
                        Status = status,
                        UpdatedDate = DateTime.Now,
                        UpdatedBy = AuthUserHelper.GetLoggedInUserId?.ToString() ?? "System"
                    };
                    _context.RosterSiteWeekStatuses.Add(statusObj);
                }
                else
                {
                    statusObj.Status = status;
                    statusObj.UpdatedDate = DateTime.Now;
                    statusObj.UpdatedBy = AuthUserHelper.GetLoggedInUserId?.ToString() ?? "System";
                }
            }

            await _context.SaveChangesAsync();

            // Real-time broadcast
            var hub = (Microsoft.AspNetCore.SignalR.IHubContext<CityWatch.Common.Models.UpdateHub>)HttpContext.RequestServices.GetService(typeof(Microsoft.AspNetCore.SignalR.IHubContext<CityWatch.Common.Models.UpdateHub>));
            var mobileHub = (Microsoft.AspNetCore.SignalR.IHubContext<CityWatch.Data.Services.MobileAppSignalRHub>)HttpContext.RequestServices.GetService(typeof(Microsoft.AspNetCore.SignalR.IHubContext<CityWatch.Data.Services.MobileAppSignalRHub>));
            
            foreach (var siteId in projectSites)
            {
                if (hub != null)
                {
                    // For Web, passing siteId triggers a refresh if they are on that site
                    await hub.Clients.All.SendAsync("UpdateRoster", new { siteId = siteId });
                }
                if (mobileHub != null)
                {
                    await mobileHub.Clients.All.SendAsync("RefreshRoster", new { siteId = siteId });
                }
            }

            return new JsonResult(new { success = true });
        }
        private DayOfWeek GetFirstDayOfWeek()
        {
            var timesheet = _clientDataProvider.GetTimesheetDetails();
            if (timesheet != null && !string.IsNullOrEmpty(timesheet.weekName))
            {
                if (Enum.TryParse<DayOfWeek>(timesheet.weekName, true, out var parsedDay))
                {
                    return parsedDay;
                }
            }
            return DayOfWeek.Monday;
        }

        private DateTime StartOfWeek(DateTime dt, DayOfWeek startOfWeek)
        {
            int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }
    }
}
