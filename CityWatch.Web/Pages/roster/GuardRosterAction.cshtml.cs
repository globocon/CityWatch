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

            // Fetch schedules for the specific site only, excluding financials/suppliers
            var schedules = await _context.RosterSchedules
                .Where(x => x.ClientSiteId == siteId && !x.IsDeleted && x.ShiftStart >= startDate && x.ShiftStart <= totalEndDate)
                .Include(x => x.Guard)
                .Include(x => x.ReliefGuard)
                .Include(x => x.Callsign)
                .OrderBy(x => x.ShiftStart)
                .Select(x => new
                {
                    id = x.Id,
                    shiftStart = x.ShiftStart.ToString("HH:mm"),
                    shiftEnd = x.ShiftEnd.ToString("HH:mm"),
                    guardName = x.ReliefGuardId.HasValue || !string.IsNullOrEmpty(x.ReliefProviderName) 
                        ? "{R} " + (x.ReliefGuard != null ? x.ReliefGuard.Name : x.ReliefProviderName)
                        : (x.Guard != null ? x.Guard.Name : x.ProviderName),
                    shiftType = x.ShiftType ?? "Regular",
                    status = (int)x.Status,
                    callsignName = x.Callsign != null ? x.Callsign.Name : "",
                    licenseNo = x.ReliefGuardId.HasValue 
                        ? (x.ReliefGuard != null ? x.ReliefGuard.SecurityNo : "") 
                        : (x.Guard != null ? x.Guard.SecurityNo : ""),
                    startDate = x.ShiftStart.Date
                })
                .ToListAsync();

            // Fetch Holidays for the range
            var holidays = await _context.BroadcastBannerCalendarEvents
                .Where(x => x.IsPublicHoliday && x.ExpiryDate >= startDate && x.StartDate <= totalEndDate)
                .Select(x => new
                {
                    id = x.id,
                    startDate = x.StartDate,
                    expiryDate = x.ExpiryDate,
                    states = _context.PublicHolidayStates
                        .Where(s => s.CalendarEventId == x.id && !s.IsDeleted)
                        .Select(s => s.State)
                        .ToList()
                })
                .ToListAsync();

            var site = await _context.ClientSites.FirstOrDefaultAsync(x => x.Id == siteId);

            return new JsonResult(new { results = schedules, holidays, siteState = site?.State });
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
