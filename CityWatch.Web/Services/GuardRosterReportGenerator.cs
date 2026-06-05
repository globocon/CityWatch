using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CityWatch.Data;
using CityWatch.Data.Models;
using CityWatch.Web.Helpers;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using CityWatch.Data.Providers;
using CityWatch.Data.Helpers;
using iText.IO.Image;
using Path = System.IO.Path;
using iText.Layout.Renderer;
using iText.Kernel.Pdf.Canvas;

namespace CityWatch.Web.Services
{
    /// <summary>
    /// ISOLATED service for generating Guard-facing Roster PDF reports.
    /// This service replicates the exact style and code of the main RosterReportGenerator
    /// but specifically excludes financial ($$$) and supplier information.
    /// </summary>
    public interface IGuardRosterReportGenerator
    {
        Task<byte[]> GenerateSiteRosterPdfAsync(int siteId, DateTime startDate, int weeks = 1, bool includeFinancials = false, string rateType = "guard", string status = "", bool includeSuppliers = false);
    }

    public class GuardRosterReportGenerator : IGuardRosterReportGenerator
    {
        private readonly CityWatchDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly string _imageRootDir;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IConfigDataProvider _configDataProvider;
        private readonly Settings _settings;
        private readonly string _subDomainImageRootDir;
        private readonly string _imageStampDir;


        private const float MARGIN = 15f;
        private const float FONT_SIZE_CELL = 7.5f;

        public GuardRosterReportGenerator(CityWatchDbContext context, IWebHostEnvironment webHostEnvironment, IClientDataProvider clientDataProvider, IConfigDataProvider configDataProvider, IOptions<Settings> options)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _clientDataProvider = clientDataProvider;
            _configDataProvider = configDataProvider;
            _settings = options.Value;
            _imageRootDir = Path.Combine(webHostEnvironment.WebRootPath, "images");
            _subDomainImageRootDir = Path.Combine(webHostEnvironment.WebRootPath, "SubdomainLogo");
            _imageStampDir = Path.Combine(webHostEnvironment.WebRootPath, "images", "stamps");
        }


        private class PublicHolidayInfo
        {
            public DateTime Date { get; set; }
            public List<string> States { get; set; }
            public bool IsPublicHoliday { get; set; }
        }

        public async Task<byte[]> GenerateSiteRosterPdfAsync(int siteId, DateTime startDate, int weeks = 1, bool includeFinancials = false, string rateType = "guard", string status = "", bool includeSuppliers = false)
        {
            var site = await _context.ClientSites.Include(s => s.ClientType).FirstOrDefaultAsync(x => x.Id == siteId);
            if (site == null) return null;

            var totalEndDate = startDate.AddDays(weeks * 7).AddSeconds(-1);

            // Fetch schedules for the specific site only
            var schedules = await _context.RosterSchedules
                .Where(x => x.ClientSiteId == siteId && !x.IsDeleted && x.ShiftStart >= startDate && x.ShiftStart <= totalEndDate)
                .Include(x => x.Guard)
                .Include(x => x.ReliefGuard)
                .Include(x => x.Callsign)
                .Include(x => x.PayRate)
                .OrderBy(x => x.ShiftStart)
                .ToListAsync();

            // Fetch Holidays for the range (including recurring match patterns)
            var holidayEvents = await _context.BroadcastBannerCalendarEvents
                .Where(x => x.IsPublicHoliday && (x.RepeatYearly || (x.ExpiryDate >= startDate && x.StartDate <= totalEndDate)))
                .ToListAsync();

            var holidayStates = await _context.PublicHolidayStates
                .Where(s => !s.IsDeleted && holidayEvents.Select(h => h.id).Contains(s.CalendarEventId))
                .ToListAsync();

            using (var ms = new MemoryStream())
            {
                using (var writer = new PdfWriter(ms))
                using (var pdf = new PdfDocument(writer))
                {
                    pdf.SetDefaultPageSize(PageSize.A4.Rotate());
                    using (var document = new Document(pdf))
                    {
                        document.SetMargins(20f, MARGIN, 60f, MARGIN);
                        int weeksOnCurrentPage = 0;

                        for (int w = 0; w < weeks; w++)
                        {
                            var weekStart = startDate.AddDays(w * 7);
                            weeksOnCurrentPage++;
                            var weekEnd = weekStart.AddDays(6);

                            var weeklyHolidays = new List<PublicHolidayInfo>();
                            for (int i = 0; i < 7; i++)
                            {
                                var dDate = weekStart.AddDays(i).Date;
                                
                                // Match by absolute date or recurring Month/Day
                                var dayHolidays = holidayEvents.Where(h => 
                                    (dDate >= h.StartDate.Date && dDate <= h.ExpiryDate.Date) ||
                                    (h.RepeatYearly && h.StartDate.Month == dDate.Month && h.StartDate.Day == dDate.Day)
                                ).ToList();

                                var states = new List<string>();
                                bool isPh = dayHolidays.Any();

                                foreach (var h in dayHolidays)
                                {
                                    var hStates = holidayStates.Where(s => s.CalendarEventId == h.id).Select(s => s.State).ToList();
                                    if (hStates.Count == 0) states.Add("ALL");
                                    else states.AddRange(hStates);
                                }
                                weeklyHolidays.Add(new PublicHolidayInfo { Date = dDate, States = states.Distinct().ToList(), IsPublicHoliday = isPh });
                            }

                            // --- PART B: Intelligent Stacking Logic (Refined) ---
                            // We look at the "Max Lines Tall" (max shifts in any single day) to see if it's a "Short Week".
                            int maxDailyShifts = 0;
                            for (int i = 0; i < 7; i++)
                            {
                                var loopDate = weekStart.AddDays(i).Date;
                                var dayCount = schedules.Count(s => s.ShiftStart.Date == loopDate);
                                if (dayCount > maxDailyShifts) maxDailyShifts = dayCount;
                            }

                            bool isCurrentWeekSmall = maxDailyShifts <= 3; // Threshold: 1-3 lines tall as requested
                            int prevMaxDailyShifts = 0;
                            if (w > 0)
                            {
                                var prevWeekStart = startDate.AddDays((w - 1) * 7);
                                for (int i = 0; i < 7; i++)
                                {
                                    var loopDate = prevWeekStart.AddDays(i).Date;
                                    var dayCount = schedules.Count(s => s.ShiftStart.Date == loopDate);
                                    if (dayCount > prevMaxDailyShifts) prevMaxDailyShifts = dayCount;
                                }
                            }

                            bool forcePageBreak = (w % 2 == 0);
                            bool pageBreakHappened = false;

                            if (w > 0)
                            {
                                // Break if we reached 2 weeks, or if current week is tall, or previous week was tall
                                if (forcePageBreak || !isCurrentWeekSmall || prevMaxDailyShifts > 3)
                                {
                                    document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                                    pageBreakHappened = true;
                                }
                                else
                                {
                                    // Stack small weeks with a minimal spacer
                                    document.Add(new Paragraph("\n").SetFontSize(2));
                                }
                            }

                            bool showFullHeader = (w == 0) || pageBreakHappened;

                            if (showFullHeader)
                            {
                                var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 20, 60, 20 })).UseAllAvailableWidth();

                                // Branding Logic (Same as Admin)
                                string logoPath = string.Empty;
                                var subDomain = _configDataProvider.GetSubDomainID(site.TypeId);
                                if (subDomain != null && !string.IsNullOrEmpty(subDomain.Logo))
                                {
                                    logoPath = Path.Combine(_subDomainImageRootDir, subDomain.Logo);
                                }
                                if (string.IsNullOrEmpty(logoPath)) logoPath = Path.Combine(_imageRootDir, "CWSLogoPdf.png");

                                if (File.Exists(logoPath))
                                {
                                    try
                                    {
                                        var logo = new Image(ImageDataFactory.Create(logoPath)).SetHeight(50);
                                        headerTable.AddCell(new Cell().Add(logo).SetBorder(Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.MIDDLE));
                                    }
                                    catch { headerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER)); }
                                }
                                else { headerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER)); }

                                var titleCell = new Cell()
                                .Add(new Paragraph($"Roster: {site.Name}").SetFont(PdfHelper.GetPdfFont()).SetFontSize(16).SetMarginBottom(0))
                                .SetTextAlignment(TextAlignment.CENTER)
                                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                                .SetBorder(Border.NO_BORDER);
                                headerTable.AddCell(titleCell);

                                var cellSiteImage = new Cell().SetBorder(Border.NO_BORDER);
                                var clientSiteSetting = _clientDataProvider.GetClientSiteKpiSetting(siteId);
                                if (clientSiteSetting != null && !string.IsNullOrEmpty(clientSiteSetting.SiteImage))
                                {
                                    try
                                    {
                                        var siteImageUrl = $"{new Uri(_settings.KpiWebUrl)}{clientSiteSetting.SiteImage}";
                                        var siteImage = new Image(ImageDataFactory.Create(siteImageUrl)).SetHeight(50).SetHorizontalAlignment(HorizontalAlignment.RIGHT);
                                        cellSiteImage.Add(siteImage);
                                    }
                                    catch { }
                                }
                                headerTable.AddCell(cellSiteImage);
                                headerTable.SetMarginBottom(10f);
                                document.Add(headerTable);

                                // Week text aligned LEFT (under the logo area)
                                document.Add(new Paragraph($"Week: {weekStart:dd MMM yyyy} - {weekEnd:dd MMM yyyy}")
                                    .SetFont(PdfHelper.GetPdfFont())
                                    .SetFontSize(12)
                                    .SetMarginTop(0)
                                    .SetMarginBottom(10)
                                    .SetTextAlignment(TextAlignment.LEFT));
                            }
                            else
                            {
                                // Compact Header for stacked weeks
                                document.Add(new Paragraph($"Week: {weekStart:dd MMM yyyy} - {weekEnd:dd MMM yyyy}")
                                    .SetFont(PdfHelper.GetPdfFont())
                                    .SetFontSize(12)
                                    .SetMarginTop(10)
                                    .SetMarginBottom(5));
                            }

                            float[] columnWidths = { 20f, 11.4f, 11.4f, 11.4f, 11.4f, 11.4f, 11.4f, 11.4f };
                            var table = new Table(UnitValue.CreatePercentArray(columnWidths)).UseAllAvailableWidth();

                            table.AddHeaderCell(CreateHeaderCell("Site"));
                            for (int i = 0; i < 7; i++) table.AddHeaderCell(CreateHeaderCell(weekStart.AddDays(i).ToString("ddd dd/MM")));

                            double[] dailyTotals = new double[7];
                            double projectWeeklyGrandTotal = 0;

                            // Row for the site (Same design as Admin)
                            var siteCell = new Cell().SetPadding(5).SetBorder(new SolidBorder(ColorConstants.BLACK, 0.5f));
                            
                            // Site Name and Type
                            siteCell.Add(new Paragraph(site.Name).SetFontSize(FONT_SIZE_CELL).SetFont(PdfHelper.GetPdfFont()).SetMarginBottom(0));
                            siteCell.Add(new Paragraph(site.ClientType?.Name ?? "Security Service").SetFontSize(6.5f).SetFont(PdfHelper.GetPdfFont()).SetFontColor(ColorConstants.GRAY).SetMarginBottom(2));

                            var statusObj = await _context.RosterSiteWeekStatuses
                                .FirstOrDefaultAsync(x => x.ClientSiteId == siteId && x.StartDate == weekStart);
                            var weekStatus = statusObj?.Status ?? status;
                            if (string.IsNullOrEmpty(weekStatus)) weekStatus = "Live";

                            AddStatusStampToCell(siteCell, weekStatus);

                            // Append Legend and Notes to siteCell
                            var absenceDict = new Dictionary<string, List<string>>();
                            foreach (var shift in schedules.Where(s => s.ShiftStart >= weekStart && s.ShiftStart < weekEnd.AddDays(1)))
                            {
                                var dateStr = shift.ShiftStart.ToString("dd/MM");
                                if (!string.IsNullOrEmpty(shift.ReliefReason))
                                {
                                    var gName = shift.Guard?.Name ?? shift.ProviderName ?? "Unassigned";
                                    var key = $"{gName} - {shift.ReliefReason}";
                                    if (!string.IsNullOrEmpty(shift.ReliefReasonOther)) key += $" ({shift.ReliefReasonOther})";
                                    
                                    if (!absenceDict.ContainsKey(key)) absenceDict[key] = new List<string>();
                                    if (!absenceDict[key].Contains(dateStr)) absenceDict[key].Add(dateStr);
                                }
                                if (shift.Status == CityWatch.Data.Enums.RosterShiftStatus.Declined)
                                {
                                    var gName = shift.Guard?.Name ?? shift.ProviderName ?? "Unassigned";
                                    var key = $"{gName} - Declined";
                                    
                                    if (!absenceDict.ContainsKey(key)) absenceDict[key] = new List<string>();
                                    if (!absenceDict[key].Contains(dateStr)) absenceDict[key].Add(dateStr);
                                }
                            }
                            var absences = absenceDict.Select(kv => $"{kv.Key} ({string.Join(", ", kv.Value)})").ToList();
                            
                            siteCell.Add(new Paragraph("\n").SetFontSize(4f).SetMarginBottom(2f));
                            var legendTable = new Table(new float[] { 8f, 150f });
                            void AddLegendRow(string text, Color color, bool isCancel = false) {
                                var colorCell = new Cell().SetPadding(0).SetBorder(Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.MIDDLE).SetHorizontalAlignment(HorizontalAlignment.LEFT);
                                var colorBox = new Div().SetWidth(4f).SetHeight(4f).SetBackgroundColor(color);
                                if (isCancel) colorBox.SetBorder(new SolidBorder(ColorConstants.RED, 0.5f)).SetBackgroundColor(ColorConstants.WHITE);
                                colorCell.Add(colorBox);
                                
                                var textCell = new Cell().Add(new Paragraph(text).SetFontSize(5f).SetFont(PdfHelper.GetPdfFont()).SetFontColor(ColorConstants.DARK_GRAY).SetMargin(0)).SetBorder(Border.NO_BORDER).SetPadding(0).SetVerticalAlignment(VerticalAlignment.MIDDLE).SetHorizontalAlignment(HorizontalAlignment.LEFT);
                                
                                legendTable.AddCell(colorCell);
                                legendTable.AddCell(textCell);
                            }
                            AddLegendRow("Unassigned - FIXED", new DeviceRgb(255, 183, 77));
                            AddLegendRow("Unassigned - ADHOC", new DeviceRgb(255, 143, 0));
                            AddLegendRow("Accepted - FIXED", new DeviceRgb(144, 238, 144));
                            AddLegendRow("Accepted - ADHOC", new DeviceRgb(50, 205, 50));
                            AddLegendRow("Relief Guard (Accepted)", new DeviceRgb(111, 66, 193));
                            AddLegendRow("DECLINED (OPEN)", ColorConstants.BLACK);
                            AddLegendRow("CANCELLED (CLOSED)", ColorConstants.RED, true);
                            siteCell.Add(legendTable);
                            
                            siteCell.Add(new Paragraph("Not Avalible (DNC):").SetFontSize(5.5f).SetFont(PdfHelper.GetPdfFont()).SetFontColor(ColorConstants.BLACK).SetMarginTop(4f).SetMarginBottom(1f));
                            if (absences.Any()) {
                                foreach(var note in absences) {
                                    siteCell.Add(new Paragraph("- " + note).SetFontSize(5f).SetFont(PdfHelper.GetPdfFont()).SetFontColor(ColorConstants.DARK_GRAY).SetMarginBottom(0.5f));
                                }
                            } else {
                                siteCell.Add(new Paragraph("None").SetFontSize(5f).SetFont(PdfHelper.GetPdfFont()).SetFontColor(ColorConstants.GRAY).SetItalic());
                            }

                            siteCell.SetMinHeight(60f);
                            table.AddCell(siteCell);

                            for (int i = 0; i < 7; i++)
                            {
                                var loopDate = weekStart.AddDays(i).Date;
                                var dayShifts = schedules.Where(s => s.ShiftStart.Date == loopDate).OrderBy(s => s.ShiftStart).ToList();
                                var dayCell = new Cell().SetPadding(2);

                                var columnBgColor = ColorConstants.WHITE;
                                if (loopDate.DayOfWeek == DayOfWeek.Saturday) columnBgColor = new DeviceRgb(215, 240, 215);
                                else if (loopDate.DayOfWeek == DayOfWeek.Sunday) columnBgColor = new DeviceRgb(252, 228, 236);

                                var phInfo = weeklyHolidays.FirstOrDefault(x => x.Date == loopDate);
                                if (phInfo != null && phInfo.IsPublicHoliday)
                                {
                                    var siteState = site.State?.Trim().ToUpper();
                                    if (phInfo.States.Contains("ALL") || (!string.IsNullOrEmpty(siteState) && phInfo.States.Any(s => s.Trim().ToUpper() == siteState)))
                                    {
                                        columnBgColor = new DeviceRgb(255, 249, 196);
                                    }
                                }
                                dayCell.SetBackgroundColor(columnBgColor);

                                 foreach (var shift in dayShifts)
                                {
                                    var duration = DateTimeHelper.CalculateDisplayDuration(shift.ShiftStart, shift.ShiftEnd);
                                    var rate = (rateType == "sell") ? (shift.PayRate?.SellRateToClient ?? 0) : (shift.PayRate?.GuardPayRate ?? 0);
                                    var value = includeFinancials ? (duration * (double)rate) : duration;

                                    dailyTotals[i] += value;
                                    projectWeeklyGrandTotal += value;

                                    var isRelief = shift.ReliefGuardId.HasValue || !string.IsNullOrEmpty(shift.ReliefProviderName);
                                    var bgColor = GetStatusColor(shift.Status);

                                    // ADHOC Color Overrides
                                    if (shift.ShiftType == "Adhoc")
                                    {
                                        if (shift.Status == CityWatch.Data.Enums.RosterShiftStatus.Accepted)
                                            bgColor = new DeviceRgb(50, 205, 50); // Dark Green (#32CD32)
                                        else if (shift.Status == CityWatch.Data.Enums.RosterShiftStatus.Pushed)
                                            bgColor = new DeviceRgb(255, 143, 0); // Dark Orange (#FF8F00)
                                    }

                                    var borderColor = ColorConstants.BLACK;
                                    var fontColor = ColorConstants.BLACK;

                                    if (shift.ShiftType == "Adhoc" || shift.Status == CityWatch.Data.Enums.RosterShiftStatus.Declined)
                                    {
                                        fontColor = ColorConstants.WHITE;
                                        borderColor = ColorConstants.WHITE;
                                    }

                                    if (isRelief)
                                    {
                                        bgColor = new DeviceRgb(111, 66, 193);
                                        borderColor = ColorConstants.WHITE;
                                        fontColor = ColorConstants.WHITE;
                                    }

                                    if (shift.Status == CityWatch.Data.Enums.RosterShiftStatus.Cancelled)
                                    {
                                        borderColor = ColorConstants.RED;
                                        fontColor = ColorConstants.RED;
                                        bgColor = ColorConstants.WHITE;
                                    }

                                    var shiftBlock = new Div().SetBackgroundColor(bgColor).SetMarginBottom(1).SetPadding(1.5f).SetBorder(new SolidBorder(borderColor, shift.Status == CityWatch.Data.Enums.RosterShiftStatus.Cancelled ? 1.0f : 0.5f));

                                    if (shift.Status == CityWatch.Data.Enums.RosterShiftStatus.Cancelled)
                                    {
                                        shiftBlock.SetNextRenderer(new CrossRenderer(shiftBlock));
                                    }

                                    var guardName = shift.ReliefGuard?.Name ?? shift.ReliefProviderName ?? shift.Guard?.Name ?? shift.ProviderName ?? "Unassigned";
                                    if (isRelief)
                                    {
                                        guardName = "{R} " + guardName;
                                        if (!string.IsNullOrEmpty(shift.ReliefReason))
                                        {
                                            var replacedName = shift.Guard?.Name ?? shift.ProviderName ?? "";
                                            if (!string.IsNullOrEmpty(replacedName)) guardName += " [" + shift.ReliefReason + "] " + Truncate(replacedName, 8);
                                            else guardName += " [" + shift.ReliefReason + "]";
                                        }
                                    }

                                    shiftBlock.Add(new Paragraph(guardName).SetFontSize(7.5f).SetFont(PdfHelper.GetPdfFont()).SetFontColor(fontColor).SetMarginBottom(0));
                                    var license = (shift.ReliefGuardId.HasValue ? shift.ReliefGuard?.SecurityNo : shift.Guard?.SecurityNo) ?? "N/A";
                                    shiftBlock.Add(new Paragraph(license).SetFontSize(6f).SetFont(PdfHelper.GetPdfFont()).SetFontColor(fontColor).SetMarginTop(0).SetMarginBottom(0));
                                    var pTime = new Paragraph().SetFontSize(6f).SetFont(PdfHelper.GetPdfFont()).SetFontColor(fontColor).SetMarginTop(0).SetMarginBottom(0);
                                    pTime.Add(new Text($"{shift.ShiftStart:HH:mm} - {shift.ShiftEnd:HH:mm} ({duration:F2}h)"));
                                    if (!includeSuppliers && shift.Callsign != null) 
                                    {
                                        pTime.Add(new Text($" [{shift.Callsign.Name}]")); 
                                    }
                                    shiftBlock.Add(pTime);

                                    if (includeSuppliers)
                                    {
                                        var providerInfo = shift.ReliefGuardId.HasValue ? shift.ReliefProviderName : shift.ProviderName;
                                        var supplierText = providerInfo ?? "N/A";
                                        if (shift.Callsign != null) supplierText += $" ({shift.Callsign.Name})";
                                        
                                        shiftBlock.Add(new Paragraph(supplierText).SetFontSize(6.5f).SetFont(PdfHelper.GetPdfFont()).SetFontColor(new DeviceRgb(200, 0, 0)).SetBold().SetMarginTop(0).SetMarginBottom(0));
                                    }
 
                                    if (includeFinancials)
                                    {
                                        decimal totalAmount = (decimal)duration * rate;
                                        shiftBlock.Add(new Paragraph($"$ {totalAmount:F2}").SetFontSize(7f).SetFont(PdfHelper.GetPdfFont()).SetFontColor(new DeviceRgb(200, 0, 0)).SetBold().SetMarginTop(0.5f).SetMarginBottom(0));
                                    }

                                    dayCell.Add(shiftBlock);
                                }
                                table.AddCell(dayCell);
                            }

                            // Footer Row for Totals (Identical Style)
                            Cell totalLabelCell = new Cell().SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetPadding(2);
                            var grandTotalText = includeFinancials ? $"Total Pay: $ {projectWeeklyGrandTotal:F2}" : $"Total Hours: {projectWeeklyGrandTotal:F2}";
                            var grandTotalPara = new Paragraph(grandTotalText).SetFontSize(FONT_SIZE_CELL).SetFont(PdfHelper.GetPdfFont());
                            totalLabelCell.Add(grandTotalPara);
                            table.AddCell(totalLabelCell);

                            for (int i = 0; i < 7; i++)
                            {
                                var dayTotalText = includeFinancials ? $"$ {dailyTotals[i]:F2}" : $"{dailyTotals[i]:F2}";
                                var dayTotalPara = new Paragraph(dayTotalText).SetFontSize(FONT_SIZE_CELL).SetFont(PdfHelper.GetPdfFont()).SetTextAlignment(TextAlignment.CENTER);
                                table.AddCell(new Cell().Add(dayTotalPara).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetPadding(2));
                            }

                            document.Add(table);

                            if (includeFinancials)
                            {
                                var weekSchedules = schedules.Where(s => s.ShiftStart >= weekStart && s.ShiftStart <= weekEnd.AddDays(1).AddSeconds(-1)).ToList();
                                
                                var summaries = await _context.RosterRemunerationSummaries
                                    .Where(x => x.WeekStartDate == weekStart.Date && x.ClientSiteId == siteId)
                                    .ToListAsync();

                                var entities = new Dictionary<string, (string Name, decimal Total)>();

                                foreach(var s in weekSchedules)
                                {
                                    var guardId = s.ReliefGuardId ?? s.GuardId;
                                    var guardName = s.ReliefGuard?.Name ?? s.Guard?.Name ?? s.ProviderName;
                                    if (string.IsNullOrEmpty(guardName)) continue;

                                    var key = guardId.HasValue ? ("G" + guardId.Value) : ("P" + guardName);
                                    
                                    decimal duration = (decimal)((s.ShiftEnd - s.ShiftStart).TotalHours);
                                    decimal payRate = (rateType == "sell") ? (s.PayRate?.SellRateToClient ?? 0m) : (s.PayRate?.GuardPayRate ?? 0m);
                                    decimal amount = duration * payRate;

                                    if (!entities.ContainsKey(key))
                                    {
                                        entities[key] = (guardName, 0m);
                                    }
                                    entities[key] = (guardName, entities[key].Total + amount);
                                }

                                if (entities.Any())
                                {
                                    // Optionally start on a new page if the table is too long, but iText7 handles breaks naturally.
                                    document.Add(new Paragraph("\n"));
                                    
                                    var headerPara = new Paragraph("Remuneration Summary")
                                        .SetFont(PdfHelper.GetPdfFont())
                                        .SetFontSize(9f)
                                        .SetFontColor(new DeviceRgb(25, 118, 210)); // Blue color like UI
                                    document.Add(headerPara);

                                    Table remTable = new Table(new float[] { 40f, 20f, 15f, 40f }).UseAllAvailableWidth().SetMarginTop(5f);

                                    // Header
                                    remTable.AddHeaderCell(new Cell().Add(new Paragraph("GUARD NAME").SetFont(PdfHelper.GetPdfFont()).SetFontSize(7f).SetFontColor(ColorConstants.DARK_GRAY)).SetBackgroundColor(new DeviceRgb(241, 245, 249)));
                                    remTable.AddHeaderCell(new Cell().Add(new Paragraph("TOTAL AMOUNT").SetFont(PdfHelper.GetPdfFont()).SetFontSize(7f).SetFontColor(ColorConstants.DARK_GRAY)).SetBackgroundColor(new DeviceRgb(241, 245, 249)).SetTextAlignment(TextAlignment.RIGHT));
                                    remTable.AddHeaderCell(new Cell().Add(new Paragraph("PAID").SetFont(PdfHelper.GetPdfFont()).SetFontSize(7f).SetFontColor(ColorConstants.DARK_GRAY)).SetBackgroundColor(new DeviceRgb(241, 245, 249)).SetTextAlignment(TextAlignment.CENTER));
                                    remTable.AddHeaderCell(new Cell().Add(new Paragraph("NOTES").SetFont(PdfHelper.GetPdfFont()).SetFontSize(7f).SetFontColor(ColorConstants.DARK_GRAY)).SetBackgroundColor(new DeviceRgb(241, 245, 249)));

                                    decimal grandTotal = 0m;

                                    var sortedKeys = entities.Keys.OrderBy(k => entities[k].Name).ToList();
                                    foreach (var key in sortedKeys)
                                    {
                                        var ent = entities[key];
                                        grandTotal += ent.Total;

                                        int? gId = null;
                                        string pName = null;
                                        if (key.StartsWith("G")) gId = int.Parse(key.Substring(1));
                                        else pName = ent.Name;

                                        var dbItem = summaries.FirstOrDefault(x => x.GuardId == gId && (gId.HasValue || x.ProviderName == pName));
                                        bool isPaid = dbItem?.IsPaid ?? false;
                                        string notes = dbItem?.Notes ?? "";

                                        remTable.AddCell(new Cell().Add(new Paragraph(ent.Name).SetFont(PdfHelper.GetPdfFont()).SetFontSize(7.5f)));
                                        remTable.AddCell(new Cell().Add(new Paragraph($"$ {ent.Total:F2}").SetFont(PdfHelper.GetPdfFont()).SetFontSize(7.5f)).SetTextAlignment(TextAlignment.RIGHT));
                                        
                                        string checkText = isPaid ? "[ X ]" : "[   ]";
                                        remTable.AddCell(new Cell().Add(new Paragraph(checkText).SetFont(PdfHelper.GetPdfFont()).SetFontSize(7.5f)).SetTextAlignment(TextAlignment.CENTER));
                                        
                                        remTable.AddCell(new Cell().Add(new Paragraph(notes).SetFont(PdfHelper.GetPdfFont()).SetFontSize(7.5f)));
                                    }

                                    // Grand Total row
                                    remTable.AddCell(new Cell().Add(new Paragraph("SUMMARY TOTAL").SetFont(PdfHelper.GetPdfFont()).SetFontSize(7.5f).SetFontColor(ColorConstants.GRAY)));
                                    remTable.AddCell(new Cell().Add(new Paragraph($"$ {grandTotal:F2}").SetFont(PdfHelper.GetPdfFont()).SetFontSize(8f).SetFontColor(ColorConstants.RED)).SetTextAlignment(TextAlignment.RIGHT));
                                    remTable.AddCell(new Cell(1, 2).Add(new Paragraph("")));

                                    document.Add(remTable);
                                }
                            }

                            AddBrandedFooter(document, pdf, weekStart);
                        }
                        document.Close();
                    }
                    return ms.ToArray();
                }
            }

        }

        private void AddStatusStampToCell(Cell cell, string status)
        {
            string fileName = status.ToUpper() switch
            {
                "LIVE" => "STAMP - LIVE.png",
                "PAID" => "STAMP - PAID.png",
                "CANCELLED" => "STAMP - CANCELD.png",
                "CANCEL" => "STAMP - CANCELD.png",
                "CANCELED" => "STAMP - CANCELD.png",
                "INVOICED" => "STAMP - INV.png",
                "INV" => "STAMP - INV.png",
                "DISPUTED" => "STAMP - DISPUTED.png",
                _ => ""
            };


            if (!string.IsNullOrEmpty(fileName))
            {
                string filePath = Path.Combine(_imageStampDir, fileName);
                if (File.Exists(filePath))
                {
                    try
                    {
                        Image stamp = new Image(ImageDataFactory.Create(filePath))
                            .SetWidth(60) 
                            .SetHorizontalAlignment(HorizontalAlignment.CENTER);
                        cell.Add(stamp);
                        return;
                    }
                    catch { }
                }
            }

            // Fallback to text stamp if image not found
            Color color = ColorConstants.RED;
            if (status == "Cancelled") color = new DeviceRgb(97, 97, 97); // Gray
            else if (status == "Paid") color = new DeviceRgb(27, 94, 32); // Green
            else if (status == "Invoiced") color = new DeviceRgb(13, 71, 161); // Blue

            Paragraph stampPara = new Paragraph(status.ToUpper())
                .SetFont(PdfHelper.GetPdfFont())
                .SetFontSize(11)
                .SetFontColor(color)
                .SetBold()
                .SetBorder(new SolidBorder(color, 1.2f))
                .SetPadding(2)
                .SetPaddingLeft(8)
                .SetPaddingRight(8)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                .SetWidth(UnitValue.CreatePercentValue(80))
                .SetMarginTop(5);

            cell.Add(stampPara);
        }


        private void AddBrandedFooter(Document document, PdfDocument pdf, DateTime startDate)
        {
            float footerY = 20;
            float margin = 20;
            float width = pdf.GetDefaultPageSize().GetWidth() - (margin * 2);

            Table footerTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 })).SetWidth(width);
            var logoPath = Path.Combine(_imageRootDir, "c4ilogo.jpg");
            if (File.Exists(logoPath))
            {
                try {
                    Image logo = new Image(ImageDataFactory.Create(logoPath)).SetHeight(25);
                    footerTable.AddCell(new Cell().Add(logo).SetBorder(Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.BOTTOM));
                } catch { footerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER)); }
            }
            else { footerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER)); }

            var timestamp = DateTime.Now;
            Paragraph footerText = new Paragraph()
                .SetFont(PdfHelper.GetPdfFont())
                .Add(new Text("Current as of: ").SetFontSize(11))
                .Add(new Text($"{timestamp:dd/MM/yyyy} @ {timestamp:HH:mm} hrs").SetFontSize(11))
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetVerticalAlignment(VerticalAlignment.BOTTOM);

            footerTable.AddCell(new Cell().Add(footerText).SetBorder(Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.BOTTOM));
            footerTable.SetFixedPosition(margin, footerY, width);
            document.Add(footerTable);
        }

        private Cell CreateHeaderCell(string text)
        {
            return new Cell()
                .Add(new Paragraph(text).SetFont(PdfHelper.GetPdfFont()).SetFontSize(FONT_SIZE_CELL))
                .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                .SetTextAlignment(TextAlignment.CENTER);
        }

        private Color GetStatusColor(CityWatch.Data.Enums.RosterShiftStatus status)
        {
            switch (status)
            {
                case CityWatch.Data.Enums.RosterShiftStatus.Accepted:
                    return new DeviceRgb(144, 238, 144); // Light Green
                case CityWatch.Data.Enums.RosterShiftStatus.Declined:
                    return new DeviceRgb(67, 67, 67); // Dark Gray
                case CityWatch.Data.Enums.RosterShiftStatus.Cancelled:
                    return ColorConstants.WHITE;
                case CityWatch.Data.Enums.RosterShiftStatus.Pushed:
                default: 
                    return new DeviceRgb(255, 183, 77); // Orange
            }
            
        }

        private string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Length <= maxLength ? value : value.Substring(0, maxLength - 2) + "..";
        }

        private class CrossRenderer : DivRenderer
        {
            public CrossRenderer(Div modelElement) : base(modelElement) { }

            public override void Draw(DrawContext drawContext)
            {
                base.Draw(drawContext);
                PdfCanvas canvas = drawContext.GetCanvas();
                Rectangle rect = GetOccupiedAreaBBox();
                canvas.SetStrokeColor(ColorConstants.RED);
                canvas.SetLineWidth(0.8f);
                canvas.MoveTo(rect.GetLeft(), rect.GetBottom());
                canvas.LineTo(rect.GetRight(), rect.GetTop());
                canvas.MoveTo(rect.GetLeft(), rect.GetTop());
                canvas.LineTo(rect.GetRight(), rect.GetBottom());
                canvas.Stroke();
            }

            public override IRenderer GetNextRenderer()
            {
                return new CrossRenderer((Div)modelElement);
            }
        }
    }
}

