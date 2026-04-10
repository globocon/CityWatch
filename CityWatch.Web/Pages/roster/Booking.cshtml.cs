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

namespace CityWatch.Web.Pages.roster
{
    public class BookingModel : PageModel
    {
        private readonly ILogger<BookingModel> _logger;
        private readonly IViewDataService _viewDataService;
        private readonly CityWatchDbContext _context;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IRosterReportGenerator _rosterReportGenerator;

        public BookingModel(
            ILogger<BookingModel> logger,
            IViewDataService viewDataService,
            CityWatchDbContext context,
            IClientDataProvider clientDataProvider,
            IRosterReportGenerator rosterReportGenerator)
        {
            _logger = logger;
            _viewDataService = viewDataService;
            _context = context;
            _clientDataProvider = clientDataProvider;
            _rosterReportGenerator = rosterReportGenerator;
        }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string WeekRange { get; set; }
        public DateTime PreviousWeek { get; set; }
        public DateTime NextWeek { get; set; }
        public int? SelectedGroupId { get; set; }
        public List<PayRate> PayRatesList { get; set; }
        public List<IncidentReportField> CallsignList { get; set; }
        public bool IsLocked { get; set; }

        public void OnGet(DateTime? startDate, int? groupId)
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

            var endDate = startDate.AddDays(6).AddDays(1).AddSeconds(-1);

            var groupSites = await _context.RosterGroupSites
                .Where(x => x.RosterGroupId == groupId)
                .Include(x => x.ClientSite)
                .ThenInclude(x => x.ClientType)
                .ToListAsync();

            var schedules = await _context.RosterSchedules
                .Where(x => x.RosterGroupId == groupId && !x.IsDeleted && x.ShiftStart >= startDate && x.ShiftStart <= endDate)
                .Include(x => x.Guard)
                .Include(x => x.Callsign)
                .ToListAsync();

            var rosterData = groupSites.Select(gs => new
            {
                siteId = gs.ClientSiteId,
                siteName = gs.ClientSite.Name,
                clientTypeName = gs.ClientSite.ClientType?.Name ?? "N/A",
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
                            guardProvider = s.GuardId.HasValue ? (s.Guard.Provider ?? "N/A") : s.ProviderName,
                            providerName = s.ProviderName,
                            payRateId = s.PayRateId,
                            shiftStart = s.ShiftStart.ToString("HH:mm"),
                            shiftEnd = s.ShiftEnd.ToString("HH:mm"),
                            callsignId = s.CallsignId,
                            callsignName = s.Callsign?.Name ?? "",
                            status = (int)s.Status
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

        public async Task<IActionResult> OnPostAddShift(int groupId, int siteId, DateTime start, DateTime end, int? guardId, string providerName, int? payRateId, int? shiftId, int? callsignId)
        {
            // Lock Check
            var today = DateTime.Today;
            var firstDayOfCurrentMonth = new DateTime(today.Year, today.Month, 1);
            if (start < firstDayOfCurrentMonth)
            {
                return new JsonResult(new { success = false, message = "Changes to previous months are locked." });
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
                existing.PayRateId = payRateId;
                existing.CallsignId = callsignId;

                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true, id = existing.Id });
            }
            else
            {
                var schedule = new RosterSchedule
                {
                    RosterGroupId = groupId,
                    ClientSiteId = siteId,
                    ShiftStart = start,
                    ShiftEnd = end,
                    GuardId = guardId,
                    ProviderName = providerName,
                    Status = RosterShiftStatus.Pushed,
                    PayRateId = payRateId,
                    CallsignId = callsignId
                };
                _context.RosterSchedules.Add(schedule);
                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true, id = schedule.Id });
            }
        }

        public JsonResult OnGetSearchPayRates(string search)
        {
            var rates = _context.PayRates
                .Where(x => !x.IsDeleted && (string.IsNullOrEmpty(search) || x.Description.Contains(search)))
                .Select(x => new
                {
                    id = x.Id,
                    text = x.Description,
                    rate = x.GuardPayRate
                })
                .ToList();

            return new JsonResult(new { results = rates });
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

        public async Task<IActionResult> OnGetDownloadPdf(int groupId, DateTime startDate)
        {
            var pdfBytes = await _rosterReportGenerator.GenerateRosterPdfAsync(groupId, startDate);
            if (pdfBytes != null)
            {
                var groupName = await _context.RosterGroups.Where(x => x.Id == groupId).Select(x => x.Name).FirstOrDefaultAsync();
                var fileName = $"Roster_{groupName}_{startDate:yyyyMMdd}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            return NotFound();
        }

        public async Task<IActionResult> OnPostCheckFutureData(int groupId, DateTime startDate, string option)
        {
            DateTime copyUntil;
            if (option == "NextWeek")
            {
                copyUntil = startDate.AddDays(14);
            }
            else if (option == "Month")
            {
                copyUntil = new DateTime(startDate.Year, startDate.Month, 1).AddMonths(1);
            }
            else if (option == "Year")
            {
                copyUntil = new DateTime(startDate.Year, 12, 31).AddDays(1);
            }
            else
            {
                return new JsonResult(new { success = false, message = "Invalid option." });
            }

            var targetStart = startDate.AddDays(7);
            var hasData = await _context.RosterSchedules.AnyAsync(x =>
                x.RosterGroupId == groupId && 
                !x.IsDeleted && 
                x.ShiftStart >= targetStart && 
                x.ShiftStart < copyUntil);

            var sourceEndDate = startDate.AddDays(7);
            var hasSourceData = await _context.RosterSchedules.AnyAsync(x =>
                x.RosterGroupId == groupId && 
                !x.IsDeleted && 
                x.ShiftStart >= startDate && 
                x.ShiftStart < sourceEndDate);

            return new JsonResult(new { success = true, hasData = hasData, hasSourceData = hasSourceData });
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
    }
}
