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
    /// This service specifically excludes financial ($$$) and supplier information.
    /// </summary>
    public interface IGuardRosterReportGenerator
    {
        Task<byte[]> GenerateSiteRosterPdfAsync(int siteId, DateTime startDate, int weeks = 1);
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

        public async Task<byte[]> GenerateSiteRosterPdfAsync(int siteId, DateTime startDate, int weeks = 1)
        {
            var site = await _context.ClientSites.Include(x => x.ClientType).FirstOrDefaultAsync(x => x.Id == siteId);
            if (site == null) return null;

            var totalEndDate = startDate.AddDays(weeks * 7).AddSeconds(-1);

            var schedules = await _context.RosterSchedules
                .Where(x => x.ClientSiteId == siteId && !x.IsDeleted && x.ShiftStart >= startDate && x.ShiftStart <= totalEndDate)
                .Include(x => x.Guard)
                .Include(x => x.ReliefGuard)
                .Include(x => x.Callsign)
                .ToListAsync();

            using (var ms = new MemoryStream())
            {
                using (var writer = new PdfWriter(ms))
                using (var pdf = new PdfDocument(writer))
                {
                    pdf.SetDefaultPageSize(PageSize.A4.Rotate());
                    var document = new Document(pdf);
                    document.SetMargins(MARGIN, MARGIN, MARGIN, MARGIN);

                    for (int w = 0; w < weeks; w++)
                    {
                        var weekStart = startDate.AddDays(w * 7);
                        var weekEnd = weekStart.AddDays(6);

                        // Fetch Holidays
                        var holidays = await _context.BroadcastBannerCalendarEvents
                            .Where(x => x.IsPublicHoliday && x.ExpiryDate >= weekStart && x.StartDate <= weekEnd)
                            .ToListAsync();
                        var holidayIds = holidays.Select(x => x.id).ToList();
                        var holidayStates = await _context.PublicHolidayStates
                            .Where(x => holidayIds.Contains(x.CalendarEventId) && !x.IsDeleted)
                            .ToListAsync();

                        if (w > 0) document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));

                        // Header
                        var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 20, 60, 20 })).UseAllAvailableWidth();
                        
                        string logoPath = string.Empty;
                        var subDomain = _configDataProvider.GetSubDomainID(site.TypeId);
                        if (subDomain != null && !string.IsNullOrEmpty(subDomain.Logo))
                        {
                            logoPath = Path.Combine(_subDomainImageRootDir, subDomain.Logo);
                        }
                        if (string.IsNullOrEmpty(logoPath) || !File.Exists(logoPath)) logoPath = Path.Combine(_imageRootDir, "CWSLogoPdf.png");

                        if (File.Exists(logoPath))
                        {
                            try {
                                var logo = new Image(ImageDataFactory.Create(logoPath)).SetHeight(50);
                                headerTable.AddCell(new Cell().Add(logo).SetBorder(Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.MIDDLE));
                            } catch { headerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER)); }
                        }
                        else { headerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER)); }

                        headerTable.AddCell(new Cell()
                            .Add(new Paragraph($"Site Roster: {site.Name}").SetFont(PdfHelper.GetPdfFont()).SetFontSize(16))
                            .Add(new Paragraph($"Week: {weekStart:dd MMM yyyy} - {weekEnd:dd MMM yyyy}").SetFontSize(12))
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetBorder(Border.NO_BORDER));
                        headerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));
                        document.Add(headerTable);
                        document.Add(new Paragraph("\n"));

                        // Table
                        float[] columnWidths = { 20f, 11.4f, 11.4f, 11.4f, 11.4f, 11.4f, 11.4f, 11.4f };
                        var table = new Table(UnitValue.CreatePercentArray(columnWidths)).UseAllAvailableWidth();

                        table.AddHeaderCell(CreateHeaderCell("Guard Name"));
                        for (int i = 0; i < 7; i++) table.AddHeaderCell(CreateHeaderCell(weekStart.AddDays(i).ToString("ddd dd/MM")));

                        // Guard List for sorting
                        var siteGuards = schedules
                            .Where(s => s.ShiftStart >= weekStart && s.ShiftStart <= weekEnd.AddDays(1).AddSeconds(-1))
                            .Select(s => s.ReliefGuardId.HasValue ? s.ReliefGuard : s.Guard)
                            .Where(g => g != null)
                            .Select(g => new { g.Id, g.Name })
                            .Distinct()
                            .OrderBy(g => g.Name)
                            .ToList();

                        foreach (var guard in siteGuards)
                        {
                            table.AddCell(new Cell().Add(new Paragraph(guard.Name).SetFontSize(FONT_SIZE_CELL).SetFont(PdfHelper.GetPdfFont())));

                            for (int i = 0; i < 7; i++)
                            {
                                var loopDate = weekStart.AddDays(i).Date;
                                var dayShifts = schedules
                                    .Where(s => s.ShiftStart.Date == loopDate && (s.GuardId == guard.Id || s.ReliefGuardId == guard.Id))
                                    .OrderBy(s => s.ShiftStart)
                                    .ToList();

                                var dayCell = new Cell().SetPadding(2);
                                
                                // Color logic
                                var columnBgColor = ColorConstants.WHITE;
                                if (loopDate.DayOfWeek == DayOfWeek.Saturday) columnBgColor = new DeviceRgb(240, 240, 240);
                                else if (loopDate.DayOfWeek == DayOfWeek.Sunday) columnBgColor = new DeviceRgb(230, 230, 230);

                                dayCell.SetBackgroundColor(columnBgColor);

                                foreach (var shift in dayShifts)
                                {
                                    var duration = DateTimeHelper.CalculateDisplayDuration(shift.ShiftStart, shift.ShiftEnd);
                                    var isRelief = shift.ReliefGuardId.HasValue;
                                    var bgColor = (isRelief) ? new DeviceRgb(111, 66, 193) : new DeviceRgb(255, 224, 178);
                                    var fontColor = (isRelief) ? ColorConstants.WHITE : ColorConstants.BLACK;

                                    var shiftBlock = new Div()
                                        .SetBackgroundColor(bgColor)
                                        .SetMarginBottom(2)
                                        .SetPadding(2)
                                        .SetBorder(new SolidBorder(isRelief ? ColorConstants.WHITE : ColorConstants.BLACK, 0.5f));

                                    shiftBlock.Add(new Paragraph($"{shift.ShiftStart:HH:mm} - {shift.ShiftEnd:HH:mm}").SetFontSize(7).SetFontColor(fontColor));
                                    shiftBlock.Add(new Paragraph($"({duration:F2}h)").SetFontSize(6).SetFontColor(fontColor));
                                    
                                    if (shift.Callsign != null)
                                    {
                                        shiftBlock.Add(new Paragraph(shift.Callsign.Name).SetFontSize(6).SetFontColor(fontColor));
                                    }

                                    dayCell.Add(shiftBlock);
                                }
                                table.AddCell(dayCell);
                            }
                        }

                        document.Add(table);
                        AddBrandedFooter(document, pdf, weekStart);
                    }
                    document.Close();
                }
                return ms.ToArray();
            }
        }

        private void AddBrandedFooter(Document document, PdfDocument pdf, DateTime startDate)
        {
            float footerY = 20;
            float margin = 20;
            float width = pdf.GetDefaultPageSize().GetWidth() - (margin * 2);
            Table footerTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 })).SetWidth(width);
            
            var timestamp = DateTime.Now;
            footerTable.AddCell(new Cell().Add(new Paragraph("CityWatch Roster Module").SetFontSize(9)).SetBorder(Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.BOTTOM));
            footerTable.AddCell(new Cell().Add(new Paragraph($"Generated: {timestamp:dd/MM/yyyy HH:mm}").SetFontSize(9).SetTextAlignment(TextAlignment.RIGHT)).SetBorder(Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.BOTTOM));
            
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
    }
}
