using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CityWatch.Data;
using CityWatch.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CityWatch.Web.Pages.roster
{
    /// <summary>
    /// ISOLATED controller/handler for Guard Portal Roster requests.
    /// This page does not have a View and is used purely for AJAX calls to keep them separate from the Admin Booking module.
    /// </summary>
    public class GuardRosterActionModel : PageModel
    {
        private readonly CityWatchDbContext _context;
        private readonly IGuardRosterReportGenerator _rosterReportGenerator;

        public GuardRosterActionModel(CityWatchDbContext context, IGuardRosterReportGenerator rosterReportGenerator)
        {
            _context = context;
            _rosterReportGenerator = rosterReportGenerator;
        }

        public void OnGet() { }

        public async Task<JsonResult> OnGetLoadRosterForSite(int siteId, DateTime startDate, int weeks = 1)
        {
            var totalEndDate = startDate.AddDays(weeks * 7).AddSeconds(-1);

            // Fetch schedules for the specific site only
            var schedules = await _context.RosterSchedules
                .Where(x => x.ClientSiteId == siteId && !x.IsDeleted && x.ShiftStart >= startDate && x.ShiftStart <= totalEndDate)
                .Include(x => x.Guard)
                .Include(x => x.ReliefGuard)
                .Include(x => x.Callsign)
                .OrderBy(x => x.ShiftStart)
                .ToListAsync();

            var site = await _context.ClientSites.Include(s => s.ClientType).FirstOrDefaultAsync(x => x.Id == siteId);

            // Group into the format expected by the "mdel styles" UI
            var results = new List<object>();

            if (site != null)
            {
                var days = new List<List<object>>();
                for (int i = 0; i < 7; i++)
                {
                    var loopDate = startDate.AddDays(i).Date;
                    var dayShifts = schedules
                        .Where(s => s.ShiftStart.Date == loopDate)
                        .OrderBy(s => s.ShiftStart)
                        .Select(s => new
                        {
                            s.Id,
                            shiftStart = s.ShiftStart.ToString("HH:mm"),
                            shiftEnd = s.ShiftEnd.ToString("HH:mm"),
                            guardName = s.Guard != null ? s.Guard.Name : s.ProviderName,
                            reliefGuardId = s.ReliefGuardId,
                            reliefGuardName = s.ReliefGuard != null ? s.ReliefGuard.Name : s.ReliefProviderName,
                            reliefProviderName = s.ReliefProviderName,
                            reliefReason = s.ReliefReason,
                            guardLicense = s.Guard != null ? s.Guard.SecurityNo : "",
                            reliefGuardLicense = s.ReliefGuard != null ? s.ReliefGuard.SecurityNo : "",
                            shiftType = s.ShiftType ?? "Regular",
                            status = (int)s.Status,
                            callsignName = s.Callsign != null ? s.Callsign.Name : "",
                            durationHours = Math.Round((s.ShiftEnd - s.ShiftStart).TotalHours, 2)
                        })
                        .ToList<object>();
                    days.Add(dayShifts);
                }

                results.Add(new
                {
                    siteName = site.Name,
                    clientTypeName = site.ClientType?.Name ?? "Security Service",
                    days = days
                });
            }

            // Fetch Holidays for the range
            var holidays = await _context.BroadcastBannerCalendarEvents
                .Where(x => x.IsPublicHoliday && x.ExpiryDate >= startDate && x.StartDate <= totalEndDate)
                .Select(x => new
                {
                    x.id,
                    x.StartDate,
                    x.ExpiryDate,
                    States = _context.PublicHolidayStates
                        .Where(s => s.CalendarEventId == x.id && !s.IsDeleted)
                        .Select(s => s.State)
                        .ToList()
                })
                .ToListAsync();

            return new JsonResult(new { results, holidays, siteState = site?.State });
        }

        public async Task<IActionResult> OnGetDownloadSiteRosterPdf(int siteId, DateTime startDate, int weeks = 1)
        {
            var pdfBytes = await _rosterReportGenerator.GenerateSiteRosterPdfAsync(siteId, startDate, weeks);
            if (pdfBytes == null) return NotFound();

            string fileName = $"Site_Roster_{siteId}_{startDate:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
    }
}
