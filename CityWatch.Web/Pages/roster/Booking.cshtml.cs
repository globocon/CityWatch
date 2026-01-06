using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

        public BookingModel(
            ILogger<BookingModel> logger, 
            IViewDataService viewDataService,
            CityWatchDbContext context,
            IClientDataProvider clientDataProvider)
        {
            _logger = logger;
            _viewDataService = viewDataService;
            _context = context;
            _clientDataProvider = clientDataProvider;
        }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string WeekRange { get; set; }
        public DateTime PreviousWeek { get; set; }
        public DateTime NextWeek { get; set; }

        public void OnGet(DateTime? startDate)
        {
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
                var today = DateTime.Today;
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
        }

        public JsonResult OnGetSearchProjects(string search)
        {
            var projects = _context.RosterGroups
                .Where(x => !x.IsDeleted && (string.IsNullOrEmpty(search) || x.Name.Contains(search)))
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
                .ToListAsync();

            var rosterData = groupSites.Select(gs => new
            {
                siteId = gs.ClientSiteId,
                siteName = gs.ClientSite.Name,
                clientTypeName = gs.ClientSite.ClientType?.Name ?? "N/A",
                days = Enumerable.Range(0, 7).Select(d =>
                {
                    var date = startDate.AddDays(d);
                    var daySchedules = schedules
                        .Where(s => s.ClientSiteId == gs.ClientSiteId && s.ShiftStart.Date == date.Date)
                        .Select(s => new
                        {
                            id = s.Id,
                            guardId = s.GuardId,
                            guardName = s.Guard?.Name ?? s.ProviderName ?? "Unknown",
                            shiftStart = s.ShiftStart.ToString("HH:mm"),
                            shiftEnd = s.ShiftEnd.ToString("HH:mm"),
                            status = (int)s.Status
                        }).ToList();
                    return daySchedules;
                }).ToList()
            }).ToList();

            return new JsonResult(new { results = rosterData });
        }

        public async Task<IActionResult> OnPostAddSiteToGroup(int groupId, int siteId)
        {
            var exists = await _context.RosterGroupSites.AnyAsync(x => x.RosterGroupId == groupId && x.ClientSiteId == siteId);
            if (!exists)
            {
                _context.RosterGroupSites.Add(new RosterGroupSite
                {
                    RosterGroupId = groupId,
                    ClientSiteId = siteId
                });
                await _context.SaveChangesAsync();
            }
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostCreateGroup(string name)
        {
            var group = new RosterGroup { Name = name };
            _context.RosterGroups.Add(group);
            await _context.SaveChangesAsync();
            return new JsonResult(new { success = true, id = group.Id });
        }

        public async Task<IActionResult> OnPostAddShift(int groupId, int siteId, DateTime start, DateTime end, int? guardId, string providerName)
        {
            var schedule = new RosterSchedule
            {
                RosterGroupId = groupId,
                ClientSiteId = siteId,
                ShiftStart = start,
                ShiftEnd = end,
                GuardId = guardId,
                ProviderName = providerName,
                Status = RosterShiftStatus.Pushed
            };
            _context.RosterSchedules.Add(schedule);
            await _context.SaveChangesAsync();
            return new JsonResult(new { success = true, id = schedule.Id });
        }

        public async Task<IActionResult> OnPostUpdateStatus(int id, int status)
        {
            var schedule = await _context.RosterSchedules.FindAsync(id);
            if (schedule != null)
            {
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
                schedule.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
            return new JsonResult(new { success = true });
        }

        public JsonResult OnGetSearchGuards(string search)
        {
            var guards = _context.Guards
                .Where(x => x.IsActive && (string.IsNullOrEmpty(search) || x.Name.Contains(search)))
                .Select(x => new { id = x.Id, text = x.Name })
                .ToList();

            return new JsonResult(new { results = guards });
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
