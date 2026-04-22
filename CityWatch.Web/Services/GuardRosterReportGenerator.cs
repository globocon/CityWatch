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

        private HashSet<int> _pagesWithHeader = new HashSet<int>();


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
                        _pagesWithHeader.Clear();
                        if (pdf.GetNumberOfPages() == 0) pdf.AddNewPage();

                        for (int w = 0; w < weeks; w++)
                        {
                            var weekStart = startDate.AddDays(w * 7);
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

                            if (w > 0 && w % 2 == 0)
                            {
                                document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                            }

                            var weekContainer = new Div().SetKeepTogether(true);

                            // Fetch Branding info once
                            string logoPath = string.Empty;
                            var subDomain = _configDataProvider.GetSubDomainID(site.TypeId);
                            if (subDomain != null && !string.IsNullOrEmpty(subDomain.Logo))
                            {
                                logoPath = Path.Combine(_subDomainImageRootDir, subDomain.Logo);
                            }
                            if (string.IsNullOrEmpty(logoPath)) logoPath = Path.Combine(_imageRootDir, "CWSLogoPdf.png");

                            string siteImageUrl = string.Empty;
                            var clientSiteSetting = _clientDataProvider.GetClientSiteKpiSetting(siteId);
                            if (clientSiteSetting != null && !string.IsNullOrEmpty(clientSiteSetting.SiteImage))
                            {
                                try { siteImageUrl = $"{new Uri(_settings.KpiWebUrl)}{clientSiteSetting.SiteImage}"; } catch { }
                            }

                            // Add Smart Header (Draws only once per page)
                            AddSmartHeader(document, pdf, site.Name, logoPath, siteImageUrl);

                            // Create a wrapper table to ensure everything (Week text + Roster table) stays together
                            var weekWrapper = new Table(1).SetWidth(UnitValue.CreatePercentValue(100)).SetKeepTogether(true).SetBorder(Border.NO_BORDER);
                            var weekWrapperCell = new Cell().SetBorder(Border.NO_BORDER).SetPadding(0);

                            // Week text aligned LEFT (under the logo area and aligned with table)
                            weekWrapperCell.Add(new Paragraph($"Week: {weekStart:dd MMM yyyy} - {weekEnd:dd MMM yyyy}")
                                .SetFont(PdfHelper.GetPdfFont())
                                .SetFontSize(12)
                                .SetMarginTop(0)
                                .SetMarginBottom(10)
                                .SetTextAlignment(TextAlignment.LEFT));

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

                                    if (shift.ShiftType == "AdhocAccepted") bgColor = new DeviceRgb(27, 94, 32);
                                    else if (shift.ShiftType == "AdhocNotAccepted") bgColor = new DeviceRgb(230, 81, 0);

                                    var borderColor = ColorConstants.BLACK;
                                    var fontColor = ColorConstants.BLACK;

                                    if (shift.ShiftType == "AdhocAccepted" || shift.ShiftType == "AdhocNotAccepted")
                                    {
                                        fontColor = ColorConstants.WHITE;
                                        borderColor = ColorConstants.WHITE;
                                    }

                                    if (isRelief && (string.IsNullOrEmpty(shift.ShiftType) || shift.ShiftType == "Regular"))
                                    {
                                        bgColor = new DeviceRgb(111, 66, 193);
                                        borderColor = ColorConstants.WHITE;
                                        fontColor = ColorConstants.WHITE;
                                    }

                                    var shiftBlock = new Div()
                                        .SetBackgroundColor(bgColor)
                                        .SetMarginBottom(2)
                                        .SetPadding(3)
                                        .SetBorder(new SolidBorder(borderColor, 0.5f));

                                    var guardName = shift.ReliefGuard?.Name ?? shift.ReliefProviderName ?? shift.Guard?.Name ?? shift.ProviderName ?? "Unknown";
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

                                    shiftBlock.Add(new Paragraph(guardName).SetFontSize(7).SetFont(PdfHelper.GetPdfFont()).SetFontColor(fontColor).SetMarginBottom(2));
                                    var license = (shift.ReliefGuardId.HasValue ? shift.ReliefGuard?.SecurityNo : shift.Guard?.SecurityNo) ?? "N/A";
                                    shiftBlock.Add(new Paragraph(license).SetFontSize(5.5f).SetFont(PdfHelper.GetPdfFont()).SetFontColor(fontColor).SetMarginTop(-1).SetMarginBottom(2));
                                    var pTime = new Paragraph().SetFontSize(5.5f).SetFont(PdfHelper.GetPdfFont()).SetFontColor(fontColor).SetMarginTop(0).SetMarginBottom(2);
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
                                        
                                        shiftBlock.Add(new Paragraph(supplierText).SetFontSize(6.5f).SetFont(PdfHelper.GetPdfFont()).SetFontColor(new DeviceRgb(200, 0, 0)).SetBold().SetMarginTop(0).SetMarginBottom(2));
                                    }

                                    if (includeFinancials)
                                    {
                                        decimal totalAmount = (decimal)duration * rate;
                                        shiftBlock.Add(new Paragraph($"$ {totalAmount:F2}").SetFontSize(6.5f).SetFont(PdfHelper.GetPdfFont()).SetFontColor(new DeviceRgb(200, 0, 0)).SetBold().SetMarginTop(1).SetMarginBottom(0));
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

                            weekWrapperCell.Add(table);
                            weekWrapper.AddCell(weekWrapperCell);
                            document.Add(weekWrapper);

                            // Call again in case the wrapper moved to a new page!
                            AddSmartHeader(document, pdf, site.Name, logoPath, siteImageUrl);

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
                "CANCELLED" => "STAMP - CANCELD.jpg",
                "CANCEL" => "STAMP - CANCELD.jpg",
                "CANCELED" => "STAMP - CANCELD.jpg",
                "INVOICED" => "STAMP - INV.png",
                "INV" => "STAMP - INV.png",
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
            footerTable.SetFixedPosition(pdf.GetNumberOfPages(), margin, footerY, width);
            document.Add(footerTable);
        }

        private void AddSmartHeader(Document document, PdfDocument pdf, string siteName, string logoPath, string siteImageUrl)
        {
            int currentPage = pdf.GetNumberOfPages();
            if (_pagesWithHeader.Contains(currentPage)) return;
            _pagesWithHeader.Add(currentPage);

            float margin = 20;
            float headerY = pdf.GetDefaultPageSize().GetHeight() - 75;
            float width = pdf.GetDefaultPageSize().GetWidth() - (margin * 2);

            Table headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 20, 60, 20 })).SetWidth(width);
            
            // Logo
            if (File.Exists(logoPath))
            {
                try {
                    Image logo = new Image(ImageDataFactory.Create(logoPath)).SetHeight(50);
                    headerTable.AddCell(new Cell().Add(logo).SetBorder(Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.MIDDLE));
                } catch { headerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER)); }
            }
            else { headerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER)); }

            // Title
            headerTable.AddCell(new Cell()
                .Add(new Paragraph($"Roster: {siteName}").SetFont(PdfHelper.GetPdfFont()).SetFontSize(16))
                .SetTextAlignment(TextAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetBorder(Border.NO_BORDER));

            // Site Image
            Cell cellSiteImage = new Cell().SetBorder(Border.NO_BORDER);
            if (!string.IsNullOrEmpty(siteImageUrl))
            {
                try
                {
                    Image siteImage = new Image(ImageDataFactory.Create(siteImageUrl)).SetHeight(50).SetHorizontalAlignment(HorizontalAlignment.RIGHT);
                    cellSiteImage.Add(siteImage);
                } catch { }
            }
            headerTable.AddCell(cellSiteImage);

            headerTable.SetFixedPosition(currentPage, margin, headerY, width);
            document.Add(headerTable);
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
                default: return new DeviceRgb(255, 224, 178); // Orange
            }
        }

        private string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Length <= maxLength ? value : value.Substring(0, maxLength - 2) + "..";
        }
    }
}
