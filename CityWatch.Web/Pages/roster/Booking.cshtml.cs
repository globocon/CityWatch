using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

        public BookingModel(
            ILogger<BookingModel> logger,
            IViewDataService viewDataService,
            CityWatchDbContext context,
            IClientDataProvider clientDataProvider,
            IRosterReportGenerator rosterReportGenerator,
            IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _viewDataService = viewDataService;
            _context = context;
            _clientDataProvider = clientDataProvider;
            _rosterReportGenerator = rosterReportGenerator;
            _webHostEnvironment = webHostEnvironment;
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
            DayOfWeek firstDayOfWeek = DayOfWeek.Monday;

            if (timesheet != null && !string.IsNullOrEmpty(timesheet.weekName))
            {
                if (Enum.TryParse<DayOfWeek>(timesheet.weekName, true, out var parsedDay))
                {
                    firstDayOfWeek = parsedDay;
                }
            }

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

            PayRatesList = _context.PayRates
                .Where(x => !x.IsDeleted)
                .ToList();

            CallsignList = _context.IncidentReportFields
                .Where(x => x.TypeId == ReportFieldType.CallSign)
                .OrderBy(x => x.Name)
                .ToList();

            // Locking logic: Dec is locked if it's Jan.
            var firstDayOfCurrentMonth = new DateTime(today.Year, today.Month, 1);
            IsLocked = StartDate < firstDayOfCurrentMonth;

            PopulateWeeklyHolidays();
        }

        private void PopulateWeeklyHolidays()
        {
            var holidays = _context.BroadcastBannerCalendarEvents
                .Where(x => x.IsPublicHoliday && x.ExpiryDate >= StartDate && x.StartDate <= EndDate)
                .ToList();

            var eventIds = holidays.Select(x => x.id).ToList();
            var holidayStates = _context.PublicHolidayStates
                .Where(x => eventIds.Contains(x.CalendarEventId) && !x.IsDeleted)
                .ToList();

            WeeklyHolidays = new List<PublicHolidayDayInfo>();
            for (int i = 0; i < 7; i++)
            {
                var date = StartDate.AddDays(i).Date;
                var dayHolidays = holidays.Where(h => date >= h.StartDate.Date && date <= h.ExpiryDate.Date).ToList();
                
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
                
                // Weekend check: Saturday (5) or Sunday (6) if week starts on Mon
                // Actually, logic is Mon-Fri only for PH highlight
                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                {
                    flags[i] = false;
                    continue;
                }

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
                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                {
                    reasons[i] = "";
                    continue;
                }

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

            var groupSites = await _context.RosterGroupSites
                .Where(x => x.RosterGroupId == groupId)
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

            var rosterData = groupSites.Select(gs => new
            {
                siteId = gs.ClientSiteId,
                siteName = gs.ClientSite.Name,
                clientTypeName = gs.ClientSite.ClientType?.Name ?? "N/A",
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
                            guardName = s.GuardId.HasValue ? s.Guard.Name : s.ProviderName,
                            guardLicense = s.GuardId.HasValue ? (s.Guard.SecurityNo ?? "N/A") : "External",
                            guardState = s.GuardId.HasValue ? (s.Guard.State ?? "N/A") : "N/A",
                            guardProvider = !string.IsNullOrEmpty(s.ProviderName) ? s.ProviderName : (s.GuardId.HasValue ? (s.Guard.Provider ?? "N/A") : "N/A"),
                            providerName = s.ProviderName,
                            payRateId = s.PayRateId,
                            shiftStart = s.ShiftStart.ToString("HH:mm"),
                            shiftEnd = s.ShiftEnd.ToString("HH:mm"),
                            callsignId = s.CallsignId,
                            callsignName = s.Callsign?.Name ?? "",
                            status = (int)s.Status,
                            durationHours = DateTimeHelper.CalculateDisplayDuration(s.ShiftStart, s.ShiftEnd),
                            payRate = s.PayRate != null ? s.PayRate.GuardPayRate : 0,
                            reliefGuardId = s.ReliefGuardId,
                            reliefGuardName = s.ReliefGuard?.Name ?? "",
                            reliefGuardLicense = s.ReliefGuardId.HasValue ? (s.ReliefGuard.SecurityNo ?? "N/A") : "",
                            reliefProviderName = s.ReliefProviderName ?? "",
                            reliefReason = s.ReliefReason ?? "",
                            shiftType = s.ShiftType ?? "Regular"
                        })
                        .ToList();
                }).ToList()
            }).ToList();

            return new JsonResult(new { results = rosterData });
        }

        public JsonResult OnGetSearchProviders(string search)
        {
            var providers = _viewDataService.ProviderList
                .Where(x => !string.IsNullOrEmpty(x.Text) && x.Text != "Select" && (string.IsNullOrEmpty(search) || x.Text.Contains(search, StringComparison.OrdinalIgnoreCase)))
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

        public async Task<IActionResult> OnPostAddShift(int groupId, int siteId, DateTime start, DateTime end, int? guardId, string providerName, int? payRateId, int? shiftId, int? callsignId, int? reliefGuardId, string reliefProviderName, string reliefReason, string shiftType)
        {
            // Lock Check
            var today = DateTime.Today;
            var firstDayOfCurrentMonth = new DateTime(today.Year, today.Month, 1);
            if (start < firstDayOfCurrentMonth)
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

            // Validation 2: Guard OR Provider must be selected
            if (!guardId.HasValue && string.IsNullOrEmpty(providerName))
            {
                return new JsonResult(new { success = false, message = "Please select a Guard or a Subcontractor Provider." });
            }

            // Validation 3: Conflict Detection (If Guard is selected)
            if (guardId.HasValue)
            {
                var conflict = await _context.RosterSchedules
                    .Where(x => x.GuardId == guardId && !x.IsDeleted && x.Id != (shiftId ?? 0) &&
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
                
                if (unavailGuard != null)
                {
                    var guard = await _context.Guards.FindAsync(guardId);
                    return new JsonResult(new { success = false, message = $"{guard.Name} cannot be rostered on as they are marked unavailable during this period (reasons {unavailGuard.Reason}, {unavailGuard.FromDate:dd MMMM yyyy} – {unavailGuard.ToDate:dd MMMM yyyy}). Please select another guard or adjust their HR records." });
                }
            }

            if (reliefGuardId.HasValue)
            {
                var unavailRelief = await _context.GuardUnavailabilities
                    .Where(u => u.GuardId == reliefGuardId && start.Date <= u.ToDate.Date && end.Date >= u.FromDate.Date)
                    .FirstOrDefaultAsync();

                if (unavailRelief != null)
                {
                    var guard = await _context.Guards.FindAsync(reliefGuardId);
                    return new JsonResult(new { success = false, message = $"Relief Guard {guard.Name} cannot be rostered on as they are marked unavailable during this period (reasons {unavailRelief.Reason}, {unavailRelief.FromDate:dd MMMM yyyy} – {unavailRelief.ToDate:dd MMMM yyyy}). Please select another guard or adjust their HR records." });
                }
            }

            if (shiftId.HasValue && shiftId.Value > 0)
            {
                var existing = await _context.RosterSchedules.FindAsync(shiftId.Value);
                if (existing == null) return new JsonResult(new { success = false, message = "Shift not found." });

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
                existing.ShiftType = shiftType;

                if (shiftType == "AdhocAccepted") existing.Status = RosterShiftStatus.Accepted;
                else if (shiftType == "AdhocNotAccepted") existing.Status = RosterShiftStatus.Pushed;

                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true, id = existing.Id });
            }
            else
            {
                var status = RosterShiftStatus.Pushed;
                if (shiftType == "AdhocAccepted") status = RosterShiftStatus.Accepted;

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
                    Status = status,
                    PayRateId = payRateId,
                    CallsignId = callsignId,
                    ShiftType = shiftType
                };
                _context.RosterSchedules.Add(schedule);
                await _context.SaveChangesAsync();
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

        public JsonResult OnGetSearchPayRateGroups(string search)
        {
            var groups = _context.PayRateGroups
                .Where(x => !x.IsDeleted && (string.IsNullOrEmpty(search) || x.Name.Contains(search)))
                .OrderBy(x => x.Name)
                .Select(x => new
                {
                    id = x.Id,
                    text = x.Name
                })
                .ToList();

            return new JsonResult(new { results = groups });
        }

        public async Task<IActionResult> OnPostUpdateStatus(int id, int status)
        {
            var schedule = await _context.RosterSchedules.FindAsync(id);
            if (schedule != null)
            {
                var today = DateTime.Today;
                var firstDayOfCurrentMonth = new DateTime(today.Year, today.Month, 1);
                if (schedule.ShiftStart < firstDayOfCurrentMonth)
                {
                    return new JsonResult(new { success = false, message = "Changes to previous months are locked." });
                }

                schedule.Status = (RosterShiftStatus)status;
                await _context.SaveChangesAsync();
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
                if (schedule.ShiftStart < firstDayOfCurrentMonth)
                {
                    return new JsonResult(new { success = false, message = "Changes to previous months are locked." });
                }

                schedule.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
            return new JsonResult(new { success = true });
        }

        public JsonResult OnGetSearchGuards(string search)
        {
            var providerList = _viewDataService.ProviderList;
            var guards = _context.Guards
                .Where(x => x.IsActive && (string.IsNullOrEmpty(search) || x.Name.Contains(search) || (x.SecurityNo != null && x.SecurityNo.Contains(search))))
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

        public async Task<IActionResult> OnGetDownloadPdf(int? groupId, int? binderId, DateTime startDate, int weeks = 1, bool includeFinancials = false, bool includeSuppliers = false)
        {
            byte[] pdfBytes = null;
            string fileName = "";

            if (binderId.HasValue && binderId.Value > 0)
            {
                pdfBytes = await _rosterReportGenerator.GenerateBinderRosterPdfAsync(binderId.Value, startDate, weeks, includeFinancials, includeSuppliers);
                var binderName = await _context.RosterBinders.Where(x => x.Id == binderId.Value).Select(x => x.Name).FirstOrDefaultAsync();
                fileName = $"Roster_Group_{binderName}_{startDate:yyyyMMdd}.pdf";
            }
            else if (groupId.HasValue && groupId.Value > 0)
            {
                pdfBytes = await _rosterReportGenerator.GenerateRosterPdfAsync(groupId.Value, startDate, weeks, includeFinancials, includeSuppliers);
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

                var projectSites = groupSites.Select(gs => new
                {
                    siteId = gs.ClientSiteId,
                    siteName = gs.ClientSite.Name,
                    clientTypeName = gs.ClientSite.ClientType?.Name ?? "N/A",
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
                                guardName = s.GuardId.HasValue ? s.Guard.Name : s.ProviderName,
                                guardLicense = s.GuardId.HasValue ? (s.Guard.SecurityNo ?? "N/A") : "External",
                                guardState = s.GuardId.HasValue ? (s.Guard.State ?? "N/A") : "N/A",
                                guardProvider = !string.IsNullOrEmpty(s.ProviderName) ? s.ProviderName : (s.GuardId.HasValue ? (s.Guard.Provider ?? "N/A") : "N/A"),
                                providerName = s.ProviderName,
                                payRateId = s.PayRateId,
                                shiftStart = s.ShiftStart.ToString("HH:mm"),
                                shiftEnd = s.ShiftEnd.ToString("HH:mm"),
                                callsignId = s.CallsignId,
                                callsignName = s.Callsign?.Name ?? "",
                                status = (int)s.Status,
                                durationHours = DateTimeHelper.CalculateDisplayDuration(s.ShiftStart, s.ShiftEnd),
                                payRate = s.PayRate != null ? s.PayRate.GuardPayRate : 0,
                                reliefGuardId = s.ReliefGuardId,
                                reliefGuardName = s.ReliefGuard?.Name ?? "",
                                reliefGuardLicense = s.ReliefGuardId.HasValue ? (s.ReliefGuard.SecurityNo ?? "N/A") : "",
                                reliefProviderName = s.ReliefProviderName ?? "",
                                reliefReason = s.ReliefReason ?? "",
                                shiftType = s.ShiftType ?? "Regular"
                            })
                            .ToList();
                    }).ToList()
                }).ToList();

                rosterData.Add(new { projectName = bp.RosterGroup.Name, projectId = bp.RosterGroupId, sites = projectSites });
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

        public async Task<IActionResult> OnPostRolloverRoster(int groupId, DateTime startDate, string option, bool eraseFuture = false)
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
                    var shiftsAllowedToDelete = shiftsToDelete.Where(x => x.ShiftStart >= firstDayOfCurrentMonth).ToList();
                    
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
                    if (targetWeekStart < firstDayOfCurrentMonth) continue; // Safety check

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

                        // Check for duplicate
                        var exists = await _context.RosterSchedules.AnyAsync(x =>
                            x.RosterGroupId == groupId &&
                            x.ClientSiteId == source.ClientSiteId &&
                            x.ShiftStart == newStart &&
                            x.GuardId == source.GuardId &&
                            x.ProviderName == source.ProviderName &&
                            x.CallsignId == source.CallsignId &&
                            !x.IsDeleted);

                        if (!exists)
                        {
                            _context.RosterSchedules.Add(new RosterSchedule
                            {
                                RosterGroupId = groupId,
                                ClientSiteId = source.ClientSiteId,
                                GuardId = source.GuardId,
                                ProviderName = source.ProviderName,
                                ShiftStart = newStart,
                                ShiftEnd = newEnd,
                                Status = RosterShiftStatus.Pushed, // Reset status to Pushed for new shifts
                                PayRateId = source.PayRateId,
                                CallsignId = source.CallsignId
                            });
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
            if (schedule.ShiftStart < firstDayOfCurrentMonth)
            {
                return new JsonResult(new { success = false, message = "Changes to previous months are locked." });
            }

            // Cycle: Regular -> AdhocAccepted -> AdhocNotAccepted -> Regular
            var currentType = schedule.ShiftType ?? "Regular";
            var nextType = "Regular";
            var nextStatus = RosterShiftStatus.Pushed;

            if (currentType == "Regular")
            {
                nextType = "AdhocAccepted";
                nextStatus = RosterShiftStatus.Accepted;
            }
            else if (currentType == "AdhocAccepted")
            {
                nextType = "AdhocNotAccepted";
                nextStatus = RosterShiftStatus.Pushed;
            }
            else
            {
                nextType = "Regular";
                nextStatus = RosterShiftStatus.Pushed;
            }

            schedule.ShiftType = nextType;
            schedule.Status = nextStatus;

            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, shiftType = nextType, status = (int)nextStatus });
        }
    }
}
