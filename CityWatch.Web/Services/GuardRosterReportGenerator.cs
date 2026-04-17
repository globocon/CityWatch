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
        Task<byte[]> GenerateSiteRosterPdfAsync(int siteId, DateTime startDate, int weeks = 1, bool includeFinancials = false, string rateType = "guard", string status = "");
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
        }

        private class PublicHolidayInfo
        {
            public DateTime Date { get; set; }
            public List<string> States { get; set; }
        }

        public async Task<byte[]> GenerateSiteRosterPdfAsync(int siteId, DateTime startDate, int weeks = 1, bool includeFinancials = false, string rateType = "guard", string status = "")
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
                .OrderBy(x => x.ShiftStart)
                .ToListAsync();

            // Fetch Holidays for the range
            var holidayEvents = await _context.BroadcastBannerCalendarEvents
                .Where(x => x.IsPublicHoliday && x.ExpiryDate >= startDate && x.StartDate <= totalEndDate)
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
                        document.SetMargins(MARGIN, MARGIN, MARGIN, MARGIN);

                        for (int w = 0; w < weeks; w++)
                        {
                            var weekStart = startDate.AddDays(w * 7);
                            var weekEnd = weekStart.AddDays(6);

                            var weeklyHolidays = new List<PublicHolidayInfo>();
                            for (int i = 0; i < 7; i++)
                            {
                                var dDate = weekStart.AddDays(i).Date;
                                var hDay = holidayEvents.FirstOrDefault(h => h.StartDate.Date <= dDate && h.ExpiryDate.Date >= dDate);
                                var states = new List<string>();
                                if (hDay != null)
                                {
                                    var hStates = holidayStates.Where(s => s.CalendarEventId == hDay.id).Select(s => s.State).ToList();
                                    if (hStates.Count == 0) states.Add("ALL");
                                    else states.AddRange(hStates);
                                }
                                weeklyHolidays.Add(new PublicHolidayInfo { Date = dDate, States = states.Distinct().ToList() });
                            }

                            if (w > 0) document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));

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
                                .Add(new Paragraph($"Site Roster: {site.Name}").SetFont(PdfHelper.GetPdfFont()).SetFontSize(16))
                                .Add(new Paragraph($"Week: {weekStart:dd MMM yyyy} - {weekEnd:dd MMM yyyy}").SetFontSize(12))
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
                            document.Add(headerTable);

                            document.Add(new Paragraph("\n"));

                            float[] columnWidths = { 20f, 11.4f, 11.4f, 11.4f, 11.4f, 11.4f, 11.4f, 11.4f };
                            var table = new Table(UnitValue.CreatePercentArray(columnWidths)).UseAllAvailableWidth();

                            table.AddHeaderCell(CreateHeaderCell("Site"));
                            for (int i = 0; i < 7; i++) table.AddHeaderCell(CreateHeaderCell(weekStart.AddDays(i).ToString("ddd dd/MM")));

                            double[] dailyTotals = new double[7];
                            double projectWeeklyGrandTotal = 0;

                            // Row for the site (Same design as Admin)
                            var siteCell = new Cell().SetPadding(0).SetBorder(new SolidBorder(ColorConstants.BLACK, 0.5f));
                            var siteInnerTable = new Table(1).UseAllAvailableWidth().SetHeight(120f);
                            
                            var siteInfoCell = new Cell().SetBorder(Border.NO_BORDER).SetPadding(5);
                            siteInfoCell.Add(new Paragraph(site.Name).SetFontSize(FONT_SIZE_CELL).SetFont(PdfHelper.GetPdfFont()));
                            siteInfoCell.Add(new Paragraph(site.ClientType?.Name ?? "Security Service").SetFontSize(6f).SetFontColor(ColorConstants.GRAY));
                            siteInnerTable.AddCell(siteInfoCell);

                            var statusCell = new Cell().SetBorder(Border.NO_BORDER).SetPadding(5).SetVerticalAlignment(VerticalAlignment.BOTTOM);
                            if (!string.IsNullOrEmpty(status) && status != "Live")
                            {
                                statusCell.Add(new Paragraph("Status:").SetFont(PdfHelper.GetPdfFont()).SetFontSize(9).SetFontColor(ColorConstants.RED).SetBold().SetMarginBottom(5));
                                statusCell.Add(GetStatusStampParagraph(status));
                            }
                            siteInnerTable.AddCell(statusCell);
                            
                            siteCell.Add(siteInnerTable);
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
                                if (phInfo != null && phInfo.States.Any())
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
                                    var duration = (shift.ShiftEnd - shift.ShiftStart).TotalHours;
                                    dailyTotals[i] += duration;
                                    projectWeeklyGrandTotal += duration;

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

                                    var shiftBlock = new Div().SetBackgroundColor(bgColor).SetMarginBottom(2).SetPadding(2).SetBorder(new SolidBorder(borderColor, 0.5f));

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

                                    shiftBlock.Add(new Paragraph(guardName).SetFontSize(7).SetFont(PdfHelper.GetPdfFont()).SetFontColor(fontColor));
                                    var license = (shift.ReliefGuardId.HasValue ? shift.ReliefGuard?.SecurityNo : shift.Guard?.SecurityNo) ?? "N/A";
                                    shiftBlock.Add(new Paragraph(license).SetFontSize(5.5f).SetFontColor(fontColor).SetMarginTop(-2));
                                    shiftBlock.Add(new Paragraph($"{shift.ShiftStart:HH:mm} - {shift.ShiftEnd:HH:mm} ({duration:F2}h)").SetFontSize(5.5f).SetFontColor(fontColor));

                                    if (shift.Callsign != null) shiftBlock.Add(new Paragraph($"Callsign: {shift.Callsign.Name}").SetFontSize(6));

                                    if (includeFinancials)
                                    {
                                        decimal rate = rateType == "sell" ? (shift.PayRate?.SellRateToClient ?? 0) : (shift.PayRate?.GuardPayRate ?? 0);
                                        decimal total = (decimal)duration * rate;
                                        shiftBlock.Add(new Paragraph($"Rate: ${rate:F2} | Total: ${total:F2}").SetFontSize(5.5f).SetFontColor(fontColor).SetItalic());
                                    }

                                    dayCell.Add(shiftBlock);
                                }
                                table.AddCell(dayCell);
                            }

                            // Footer Row for Totals (Identical Style)
                            Cell totalLabelCell = new Cell().SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetPadding(2);
                            totalLabelCell.Add(new Paragraph($"Total Hours: {projectWeeklyGrandTotal:F2}").SetFontSize(FONT_SIZE_CELL).SetFont(PdfHelper.GetPdfFont()));
                            table.AddCell(totalLabelCell);

                            for (int i = 0; i < 7; i++)
                            {
                                table.AddCell(new Cell().Add(new Paragraph($"{dailyTotals[i]:F2}").SetFontSize(FONT_SIZE_CELL).SetFont(PdfHelper.GetPdfFont()).SetTextAlignment(TextAlignment.CENTER)).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetPadding(2));
                            }

                            document.Add(table);
                            AddBrandedFooter(document, pdf, weekStart);
                        }
                        document.Close();
                    }
                    return ms.ToArray();
                }
            }

        }

        private Paragraph GetStatusStampParagraph(string status)
        {
            Color color = ColorConstants.RED;
            if (status == "Cancelled") color = ColorConstants.GRAY;
            else if (status == "Paid") color = new DeviceRgb(27, 94, 32); // Greenish

            Paragraph stampPara = new Paragraph(status.ToUpper())
                .SetFont(PdfHelper.GetPdfFont())
                .SetFontSize(14)
                .SetFontColor(color)
                .SetBold()
                .SetBorder(new SolidBorder(color, 1.5f))
                .SetPadding(3)
                .SetPaddingLeft(10)
                .SetPaddingRight(10)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetRotationAngle(Math.PI / 12) // Slight Tilt
                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                .SetMarginLeft(20);

            return stampPara;
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
                .Add(new Text("Current as of: ").SetFontSize(11))
                .Add(new Text($"{timestamp:dd/MM/yyyy} @ {timestamp:HH:mm} hrs").SetBold().SetFontSize(11))
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
