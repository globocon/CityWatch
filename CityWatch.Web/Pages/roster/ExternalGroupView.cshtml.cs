using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CityWatch.Data;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Enums;
using CityWatch.Web.Services;
using CityWatch.Data.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace CityWatch.Web.Pages.roster
{
    [AllowAnonymous]
    public class ExternalGroupViewModel : PageModel
    {
        private readonly CityWatchDbContext _context;
        private readonly IClientDataProvider _clientDataProvider;

        public ExternalGroupViewModel(CityWatchDbContext context, IClientDataProvider clientDataProvider)
        {
            _context = context;
            _clientDataProvider = clientDataProvider;
        }

        public string GroupName { get; set; }
        public Guid AccessKey { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string WeekRange { get; set; }
        public int BinderId { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid accessKey, DateTime? startDate)
        {
            var binder = await _context.RosterBinders
                .FirstOrDefaultAsync(x => x.AccessKey == accessKey && !x.IsDeleted);

            if (binder == null)
            {
                return NotFound("Invalid access link.");
            }

            GroupName = binder.Name;
            AccessKey = accessKey;
            BinderId = binder.Id;

            var today = DateTime.Today;
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

            return Page();
        }

        public async Task<JsonResult> OnGetLoadRoster(Guid accessKey, DateTime startDate)
        {
            var binder = await _context.RosterBinders
                .FirstOrDefaultAsync(x => x.AccessKey == accessKey && !x.IsDeleted);

            if (binder == null) return new JsonResult(new { success = false, message = "Invalid access." });

            var endDate = startDate.AddDays(6).AddDays(1).AddSeconds(-1);

            var binderProjects = await _context.RosterBinderProjects
                .Where(x => x.RosterBinderId == binder.Id)
                .Include(x => x.RosterGroup)
                .ToListAsync();

            var projectIds = binderProjects.Select(bp => bp.RosterGroupId).ToList();

            var schedules = await _context.RosterSchedules
                .Where(x => projectIds.Contains(x.RosterGroupId) && !x.IsDeleted && x.ShiftStart >= startDate && x.ShiftStart <= endDate)
                .Include(x => x.Guard)
                .Include(x => x.ReliefGuard)
                .Include(x => x.Callsign)
                .ToListAsync();

            var rosterData = new List<object>();

            foreach (var bp in binderProjects.OrderBy(x => x.SortOrder).ThenBy(x => x.Id))
            {
                var groupSites = await _context.RosterGroupSites
                    .Where(x => x.RosterGroupId == bp.RosterGroupId)
                    .Include(x => x.ClientSite)
                    .ThenInclude(x => x.ClientType)
                    .ToListAsync();

                var groupSiteIds = groupSites.Select(gs => gs.ClientSiteId).ToList();
                var groupWeekStatuses = await _context.RosterSiteWeekStatuses
                    .Where(x => groupSiteIds.Contains(x.ClientSiteId) && x.StartDate == startDate)
                    .ToListAsync();

                var projectSites = groupSites.Select(gs => new
                {
                    siteId = gs.ClientSiteId,
                    siteName = gs.ClientSite.Name,
                    clientTypeName = gs.ClientSite.ClientType?.Name ?? "N/A",
                    status = groupWeekStatuses.FirstOrDefault(ws => ws.ClientSiteId == gs.ClientSiteId)?.Status ?? "Live",
                    projectId = bp.RosterGroupId,
                    projectName = bp.RosterGroup.Name,
                    days = Enumerable.Range(0, 7).Select(dayOffset =>
                    {
                        var targetDate = startDate.AddDays(dayOffset);
                        return schedules
                            .Where(s => s.RosterGroupId == bp.RosterGroupId && s.ClientSiteId == gs.ClientSiteId && s.ShiftStart.Date == targetDate.Date)
                            .OrderBy(s => s.ShiftStart)
                            .Select(s => new
                            {
                                id = s.Id,
                                providerName = s.ProviderName,
                                guardId = s.GuardId,
                                guardName = s.GuardId.HasValue ? s.Guard.Name : (s.ProviderName ?? "Unassigned"),
                                guardLicense = s.GuardId.HasValue ? s.Guard.SecurityNo : "",
                                shiftStart = s.ShiftStart.ToString("HH:mm"),
                                shiftEnd = s.ShiftEnd.ToString("HH:mm"),
                                callsignName = s.Callsign?.Name ?? "",
                                status = (int)s.Status,
                                durationHours = DateTimeHelper.CalculateDisplayDuration(s.ShiftStart, s.ShiftEnd),
                                reliefGuardName = s.ReliefGuard?.Name ?? s.ReliefProviderName ?? "",
                                reliefGuardLicense = s.ReliefGuardId.HasValue ? s.ReliefGuard.SecurityNo : "",
                                reliefReason = s.ReliefReason ?? "",
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
    }
}
