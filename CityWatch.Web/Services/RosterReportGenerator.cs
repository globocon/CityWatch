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

using CityWatch.Data.Helpers;
using iText.IO.Image;
using iText.IO.Font.Constants;
using iText.Kernel.Font;

namespace CityWatch.Web.Services
{
    public interface IRosterReportGenerator
    {
        Task<byte[]> GenerateRosterPdfAsync(int groupId, DateTime startDate, int weeks = 1);
    }



    public class RosterReportGenerator : IRosterReportGenerator
    {
        private readonly CityWatchDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly string _imageRootDir;

        private const float MARGIN = 15f; // Match TimesheetReportGenerator
        private const float FONT_SIZE_HEADER = 12f;
        private const float FONT_SIZE_CELL = 7.5f; // Match TimesheetReportGenerator

        public RosterReportGenerator(CityWatchDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _imageRootDir = System.IO.Path.Combine(webHostEnvironment.WebRootPath, "images");
        }

        public async Task<byte[]> GenerateRosterPdfAsync(int groupId, DateTime startDate, int weeks = 1)
        {
            var group = await _context.RosterGroups.FindAsync(groupId);
            if (group == null) return null;

            var endDate = startDate.AddDays(7).AddSeconds(-1);
            var totalEndDate = startDate.AddDays(weeks * 7).AddSeconds(-1);

            // Fetch Data for the entire range
            var groupSites = await _context.RosterGroupSites
                .Where(x => x.RosterGroupId == groupId)
                .Include(x => x.ClientSite)
                .ThenInclude(x => x.ClientType)
                .ToListAsync();

            var schedules = await _context.RosterSchedules
                .Where(x => x.RosterGroupId == groupId && !x.IsDeleted && x.ShiftStart >= startDate && x.ShiftStart <= totalEndDate)
                .Include(x => x.Guard)
                .ToListAsync();

            using (var stream = new MemoryStream())
            {
                var writer = new PdfWriter(stream);
                var pdf = new PdfDocument(writer);
                pdf.SetDefaultPageSize(PageSize.A4.Rotate());
                var document = new Document(pdf);
                document.SetMargins(MARGIN, MARGIN, MARGIN, MARGIN);

                var groupName = group.Name ?? "Unknown Project";

                // Logo
                var logoPath = System.IO.Path.Combine(_imageRootDir, "CWSLogoPdf.png");
                if (File.Exists(logoPath))
                {
                    var cwLogo = new Image(ImageDataFactory.Create(logoPath)).SetHeight(50);
                    headerTable.AddCell(new Cell().Add(cwLogo).SetBorder(Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.MIDDLE));
                }
                else { headerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER)); }

                // Title Section
                var groupName = group.Name ?? "Unknown Project";
                var titleCell = new Cell()
                    .Add(new Paragraph($"Roster: {groupName}").SetFont(PdfHelper.GetPdfFont()).SetFontSize(16))
                    .Add(new Paragraph($"Week: {startDate:dd MMM yyyy} - {startDate.AddDays(6):dd MMM yyyy}").SetFontSize(12))
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .SetBorder(Border.NO_BORDER);

                headerTable.AddCell(titleCell);
                headerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));

                document.Add(headerTable);
                document.Add(new Paragraph("\n"));

                // Grid Table
                float[] columnWidths = { 20f, 11.4f, 11.4f, 11.4f, 11.4f, 11.4f, 11.4f, 11.4f };
                var table = new Table(UnitValue.CreatePercentArray(columnWidths)).UseAllAvailableWidth();

                // Table Header
                table.AddHeaderCell(CreateHeaderCell("Site"));
                for (int i = 0; i < 7; i++)
                {
                    table.AddHeaderCell(CreateHeaderCell(startDate.AddDays(i).ToString("ddd dd/MM")));
                }

                double[] dailyTotals = new double[7];
                double weeklyTotal = 0;

                // Table Rows
                foreach (var site in groupSites)
                {
                    var siteCell = new Cell().Add(new Paragraph(site.ClientSite.Name).SetFontSize(FONT_SIZE_CELL).SetFont(PdfHelper.GetPdfFont()));
                    siteCell.Add(new Paragraph(site.ClientSite.ClientType?.Name ?? "").SetFontSize(6f).SetFontColor(ColorConstants.GRAY));
                    table.AddCell(siteCell);

                    for (int i = 0; i < 7; i++)
                    {
                        var loopDate = startDate.AddDays(i).Date;
                        var dayShifts = schedules
                            .Where(s => s.ClientSiteId == site.ClientSiteId && s.ShiftStart.Date == loopDate)
                            .OrderBy(s => s.ShiftStart)
                            .ToList();

                        var dayCell = new Cell().SetPadding(2);
                        foreach (var shift in dayShifts)
                        {
                            var statusColor = GetStatusColor(shift.Status);
                            var guardName = shift.GuardId.HasValue ? shift.Guard.Name : shift.ProviderName;
                            var duration = (shift.ShiftEnd - shift.ShiftStart).TotalHours;
                            var timeRangeStr = $"{shift.ShiftStart:HH:mm} - {shift.ShiftEnd:HH:mm}";

                            dailyTotals[i] += duration;
                            weeklyTotal += duration;

                            var shiftBlock = new Div()
                                .SetBackgroundColor(statusColor)
                                .SetMarginBottom(2)
                                .SetPadding(2)
                                .SetBorder(new SolidBorder(ColorConstants.BLACK, 0.5f));

                            shiftBlock.Add(new Paragraph(guardName ?? "Unknown").SetFontSize(7).SetFont(PdfHelper.GetPdfFont()));
                            shiftBlock.Add(new Paragraph($"{timeRangeStr} ({Math.Round(duration, 2)}h)").SetFontSize(5.5f));

                            dayCell.Add(shiftBlock);
                        }
                        table.AddCell(dayCell);
                    }
                }

                // Total Row (Matching guard name font and size as requested)
                var totalLabelCell = new Cell().SetBackgroundColor(ColorConstants.WHITE).SetPadding(2);
                totalLabelCell.Add(new Paragraph($"Total HRS: {Math.Round(weeklyTotal, 2)}h")
                    .SetFont(PdfHelper.GetPdfFont())
                    .SetFontSize(7f)
                    .SetFontColor(ColorConstants.BLACK));
                table.AddCell(totalLabelCell);

                for (int i = 0; i < 7; i++)
                {
                    var dayTotalCell = new Cell().SetBackgroundColor(ColorConstants.WHITE).SetPadding(2).SetTextAlignment(TextAlignment.CENTER);
                    dayTotalCell.Add(new Paragraph($"{Math.Round(dailyTotals[i], 2)}h")
                        .SetFont(PdfHelper.GetPdfFont())
                        .SetFontSize(7f)
                        .SetFontColor(ColorConstants.BLACK));
                    table.AddCell(dayTotalCell);
                }

                document.Add(table);

                // Branded Footer
                AddBrandedFooter(document, pdf, startDate);

                for (int w = 0; w < weeks; w++)
                {
                    var weekStart = startDate.AddDays(w * 7);
                    var weekEnd = weekStart.AddDays(6);

                    // Header Table
                    var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 20, 60, 20 })).UseAllAvailableWidth();

                    // Logo
                    var logoPath = System.IO.Path.Combine(_imageRootDir, "CWSLogoPdf.png");
                    if (File.Exists(logoPath))
                    {
                        var cwLogo = new Image(ImageDataFactory.Create(logoPath)).SetHeight(50);
                        headerTable.AddCell(new Cell().Add(cwLogo).SetBorder(Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.MIDDLE));
                    }
                    else
                    {
                        headerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));
                    }

                    // Title Section
                    var titleCell = new Cell()
                        .Add(new Paragraph($"Roster: {groupName}").SetFont(PdfHelper.GetPdfFont()).SetFontSize(16))
                        .Add(new Paragraph($"Week: {weekStart:dd MMM yyyy} - {weekEnd:dd MMM yyyy}").SetFontSize(12))
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                        .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                        .SetBorder(Border.NO_BORDER);

                    headerTable.AddCell(titleCell);
                    headerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));

                    document.Add(headerTable);
                    document.Add(new Paragraph("\n")); // Spacer

                    // Grid Table
                    float[] columnWidths = { 20f, 11.4f, 11.4f, 11.4f, 11.4f, 11.4f, 11.4f, 11.4f };
                    var table = new Table(UnitValue.CreatePercentArray(columnWidths)).UseAllAvailableWidth();

                    // Table Header
                    table.AddHeaderCell(CreateHeaderCell("Site"));
                    for (int i = 0; i < 7; i++)
                    {
                        table.AddHeaderCell(CreateHeaderCell(weekStart.AddDays(i).ToString("ddd dd/MM")));
                    }

                    // Table Rows
                    foreach (var site in groupSites)
                    {
                        var siteCell = new Cell().Add(new Paragraph(site.ClientSite.Name).SetFontSize(FONT_SIZE_CELL).SetFont(PdfHelper.GetPdfFont()));
                        siteCell.Add(new Paragraph(site.ClientSite.ClientType?.Name ?? "").SetFontSize(6f).SetFontColor(ColorConstants.GRAY));
                        table.AddCell(siteCell);

                        for (int i = 0; i < 7; i++)
                        {
                            var loopDate = weekStart.AddDays(i).Date;
                            var dayShifts = schedules
                                .Where(s => s.ClientSiteId == site.ClientSiteId && s.ShiftStart.Date == loopDate)
                                .OrderBy(s => s.ShiftStart)
                                .ToList();

                            var dayCell = new Cell().SetPadding(2);

                            foreach (var shift in dayShifts)
                            {
                                var statusColor = GetStatusColor(shift.Status);
                                var guardName = shift.GuardId.HasValue ? shift.Guard.Name : shift.ProviderName;
                                var timeRange = $"{shift.ShiftStart:HH:mm}  -  {shift.ShiftEnd:HH:mm}";

                                var shiftBlock = new Div()
                                    .SetBackgroundColor(statusColor)
                                    .SetMarginBottom(2)
                                    .SetPadding(2)
                                    .SetBorder(new SolidBorder(ColorConstants.BLACK, 0.5f));

                                shiftBlock.Add(new Paragraph(guardName ?? "Unknown").SetFontSize(7).SetFont(PdfHelper.GetPdfFont()));
                                shiftBlock.Add(new Paragraph(timeRange).SetFontSize(6));

                                dayCell.Add(shiftBlock);
                            }

                            table.AddCell(dayCell);
                        }
                    }

                    document.Add(table);

                    // Add Footer Table
                    var footerTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 })).UseAllAvailableWidth().SetMarginTop(15);

                    // Left: Logo
                    var footerLogoPath = System.IO.Path.Combine(_imageRootDir, "c4ilogo.jpg");
                    if (File.Exists(footerLogoPath))
                    {
                        var footerLogo = new Image(ImageDataFactory.Create(footerLogoPath)).SetHeight(30);
                        footerTable.AddCell(new Cell().Add(footerLogo).SetBorder(Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.MIDDLE));
                    }
                    else
                    {
                        footerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));
                    }

                    // Right: Timestamp
                    var now = DateTime.Now;
                    var footerText = new Paragraph()
                        .SetFontSize(11)
                        .Add(new Text("Current as of: ").SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA)))
                        .Add(new Text($"{now:dd MMM yyyy} @ {now:HH:mm} hrs").SetFont(PdfHelper.GetPdfFont()));

                    footerTable.AddCell(new Cell().Add(footerText).SetTextAlignment(TextAlignment.RIGHT).SetVerticalAlignment(VerticalAlignment.MIDDLE).SetBorder(Border.NO_BORDER));

                    document.Add(footerTable);

                    if (w < weeks - 1)
                    {
                        document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                    }
                }

                document.Close();
                return stream.ToArray();
            }
        }

        private void AddBrandedFooter(Document document, PdfDocument pdf, DateTime startDate)
        {
            float footerY = 20;
            float margin = 20;
            float width = pdf.GetDefaultPageSize().GetWidth() - (margin * 2);

            Table footerTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 })).SetWidth(width);

            // Left: Logo
            var logoPath = System.IO.Path.Combine(_imageRootDir, "c4ilogo.jpg");
            if (File.Exists(logoPath))
            {
                Image logo = new Image(ImageDataFactory.Create(logoPath)).SetHeight(25);
                footerTable.AddCell(new Cell().Add(logo).SetBorder(Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.BOTTOM));
            }
            else { footerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER)); }

            // Right: Text
            var timestamp = DateTime.Now;
            Paragraph footerText = new Paragraph()
                .Add(new Text("Current as of: ").SetFontSize(11))
                .Add(new Text($"{timestamp:dd/MM/yyyy}").SetBold().SetFontSize(11))
                .Add(new Text(" @@ ").SetFontSize(11))
                .Add(new Text($"{timestamp:HH:mm}").SetBold().SetFontSize(11))
                .Add(new Text(" hrs").SetFontSize(11))
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetVerticalAlignment(VerticalAlignment.BOTTOM)
                .SetFontColor(ColorConstants.BLACK);

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
                case CityWatch.Data.Enums.RosterShiftStatus.Accepted: return new DeviceRgb(212, 237, 218); // Green-ish
                case CityWatch.Data.Enums.RosterShiftStatus.Declined: return new DeviceRgb(50, 50, 50); // Dark (Text will need handling, but stick to bg for now)
                default: return new DeviceRgb(255, 224, 178); // Orange-ish (Pushed)
            }
        }
    }
}
