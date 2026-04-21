using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CityWatch.Data;
using CityWatch.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CityWatch.Data.Models;
using CityWatch.Web.Helpers;
using CityWatch.Data.Helpers;
using CityWatch.Data.Providers;


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
        private readonly IClientDataProvider _clientDataProvider;

        public GuardRosterActionModel(CityWatchDbContext context, IGuardRosterReportGenerator rosterReportGenerator, IClientDataProvider clientDataProvider)
        {
            _context = context;
            _rosterReportGenerator = rosterReportGenerator;
            _clientDataProvider = clientDataProvider;
        }

        public void OnGet() { }

        public JsonResult OnGetGetStartOfWeek(DateTime? date)
        {
            var today = date ?? DateTime.Today;
            var timesheet = _clientDataProvider.GetTimesheetDetails();
            DayOfWeek firstDayOfWeek = DayOfWeek.Monday;

            if (timesheet != null && !string.IsNullOrEmpty(timesheet.weekName))
            {
                if (Enum.TryParse<DayOfWeek>(timesheet.weekName, true, out var parsedDay))
                {
                    firstDayOfWeek = parsedDay;
                }
            }

            int diff = (7 + (today.DayOfWeek - firstDayOfWeek)) % 7;
            var startOfWeek = today.AddDays(-1 * diff).Date;
            
            return new JsonResult(new { startDate = startOfWeek.ToString("yyyy-MM-dd") });
        }

        public async Task<JsonResult> OnGetLoadRosterForSite(int siteId, DateTime startDate, int weeks = 1)
        {
            var totalEndDate = startDate.AddDays(weeks * 7).AddSeconds(-1);

            // Fetch schedules for the specific site only
            var schedules = await _context.RosterSchedules
                .Where(x => x.ClientSiteId == siteId && !x.IsDeleted && x.ShiftStart >= startDate && x.ShiftStart <= totalEndDate)
                .Include(x => x.Guard)
                .Include(x => x.Guard)
                .Include(x => x.ReliefGuard)
                .Include(x => x.Callsign)
                .Include(x => x.PayRate)
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
                            guardId = s.GuardId,
                            reliefGuardId = s.ReliefGuardId,
                            reliefGuardName = s.ReliefGuard != null ? s.ReliefGuard.Name : s.ReliefProviderName,
                            reliefProviderName = s.ReliefProviderName,
                            reliefReason = s.ReliefReason,
                            guardLicense = s.Guard != null ? s.Guard.SecurityNo : "",
                            reliefGuardLicense = s.ReliefGuard != null ? s.ReliefGuard.SecurityNo : "",
                            guardProvider = !string.IsNullOrEmpty(s.ProviderName) ? s.ProviderName : (s.Guard != null ? (s.Guard.Provider ?? "N/A") : "N/A"),
                            shiftType = s.ShiftType ?? "Regular",
                            status = (int)s.Status,
                            callsignName = s.Callsign != null ? s.Callsign.Name : "",
                            durationHours = DateTimeHelper.CalculateDisplayDuration(s.ShiftStart, s.ShiftEnd),
                            sellRate = s.PayRate != null ? s.PayRate.SellRateToClient : 0,
                            buyRate = s.PayRate != null ? s.PayRate.GuardPayRate : 0
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
                .Where(x => x.IsPublicHoliday && (x.RepeatYearly || (x.ExpiryDate >= startDate && x.StartDate <= totalEndDate)))
                .Select(x => new
                {
                    x.id,
                    x.StartDate,
                    x.ExpiryDate,
                    x.RepeatYearly,
                    Reason = x.TextMessage,
                    States = _context.PublicHolidayStates
                        .Where(s => s.CalendarEventId == x.id && !s.IsDeleted)
                        .Select(s => s.State)
                        .ToList()
                })
                .ToListAsync();

            var statusObj = await _context.RosterSiteWeekStatuses
                .FirstOrDefaultAsync(x => x.ClientSiteId == siteId && x.StartDate == startDate);
            var status = statusObj?.Status ?? (schedules.Any(s => s.ClientSiteId == siteId) ? "Live" : "");


            return new JsonResult(new { results, holidays, siteState = site?.State, status });
        }

        public async Task<IActionResult> OnGetDownloadSiteRosterPdf(int siteId, DateTime startDate, int weeks = 1, bool includeFinancials = false, string rateType = "guard", string status = "", bool includeSuppliers = false)
        {
            if (string.IsNullOrEmpty(status))
            {
                var statusObj = await _context.RosterSiteWeekStatuses.FirstOrDefaultAsync(x => x.ClientSiteId == siteId && x.StartDate.Date == startDate.Date);
                status = statusObj?.Status ?? "Live";
            }

            var pdfBytes = await _rosterReportGenerator.GenerateSiteRosterPdfAsync(siteId, startDate, weeks, includeFinancials, rateType, status, includeSuppliers);
            if (pdfBytes == null) return NotFound();

            string fileName = $"Site_Roster_{siteId}_{startDate:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        public async Task<JsonResult> OnGetLoadRosterStatus(int siteId, DateTime startDate)
        {
            var statusObj = await _context.RosterSiteWeekStatuses
                .FirstOrDefaultAsync(x => x.ClientSiteId == siteId && x.StartDate.Date == startDate.Date);

            return new JsonResult(new { status = statusObj?.Status ?? "Live" });
        }

        public async Task<JsonResult> OnPostSaveRosterStatus(int siteId, DateTime startDate, string status)
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
                    UpdatedBy = AuthUserHelper.LoggedInUserId?.ToString() ?? "System"
                };
                _context.RosterSiteWeekStatuses.Add(statusObj);
            }
            else
            {
                statusObj.Status = status;
                statusObj.UpdatedDate = DateTime.Now;
                statusObj.UpdatedBy = AuthUserHelper.LoggedInUserId?.ToString() ?? "System";
            }

            await _context.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public async Task<JsonResult> OnGetLoadRemunerationSummary(DateTime startDate, string guardIds)
        {
            if (string.IsNullOrEmpty(guardIds)) return new JsonResult(new { results = new List<object>() });

            try
            {
                var validGuardIds = guardIds.Split(',')
                    .Where(x => !string.IsNullOrEmpty(x) && int.TryParse(x, out _))
                    .Select(x => int.Parse(x))
                    .ToList();

                var summaries = await _context.RosterRemunerationSummaries
                    .Where(x => x.WeekStartDate == startDate.Date)
                    .Select(x => new {
                        x.GuardId,
                        x.ProviderName,
                        x.IsPaid,
                        x.Notes,
                        x.TotalAmount
                    })
                    .ToListAsync();

                // Only return summaries that match either the guard IDs or have a provider name
                // In practice, we can just return all since we filter in JS, but let's be slightly more efficient
                var filteredSummaries = summaries
                    .Where(x => (x.GuardId.HasValue && validGuardIds.Contains(x.GuardId.Value)) || !string.IsNullOrEmpty(x.ProviderName))
                    .ToList();

                return new JsonResult(new { success = true, results = filteredSummaries });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = "Error loading summary" });
            }
        }

        public async Task<IActionResult> OnPostSaveRemunerationSummary(DateTime startDate, int? guardId, string providerName, bool isPaid, string notes, decimal totalAmount)
        {
            try
            {
                RosterRemunerationSummary summary;
                if (guardId.HasValue)
                {
                    summary = await _context.RosterRemunerationSummaries
                        .FirstOrDefaultAsync(x => x.WeekStartDate == startDate.Date && x.GuardId == guardId.Value);
                }
                else
                {
                    summary = await _context.RosterRemunerationSummaries
                        .FirstOrDefaultAsync(x => x.WeekStartDate == startDate.Date && x.ProviderName == providerName);
                }

                if (summary == null)
                {
                    summary = new RosterRemunerationSummary
                    {
                        WeekStartDate = startDate.Date,
                        GuardId = guardId,
                        ProviderName = providerName,
                        IsPaid = isPaid,
                        Notes = notes,
                        TotalAmount = totalAmount
                    };
                    _context.RosterRemunerationSummaries.Add(summary);
                }
                else
                {
                    summary.IsPaid = isPaid;
                    summary.Notes = notes;
                    summary.TotalAmount = totalAmount;
                }

                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = "Error saving summary" });
            }
        }
    }
}
