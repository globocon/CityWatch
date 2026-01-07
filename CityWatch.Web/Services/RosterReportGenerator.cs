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

namespace CityWatch.Web.Services
{
    public interface IRosterReportGenerator
    {
        Task<byte[]> GenerateRosterPdfAsync(int groupId, DateTime startDate);
    }

    public class RosterReportGenerator : IRosterReportGenerator
    {
        private readonly CityWatchDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        private const float MARGIN = 15f; // Match TimesheetReportGenerator
        private const float FONT_SIZE_HEADER = 12f;
        private const float FONT_SIZE_CELL = 7.5f; // Match TimesheetReportGenerator

        public RosterReportGenerator(CityWatchDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<byte[]> GenerateRosterPdfAsync(int groupId, DateTime startDate)
        {
            var group = await _context.RosterGroups.FindAsync(groupId);
            if (group == null) return null;

            var endDate = startDate.AddDays(6).AddDays(1).AddSeconds(-1);

            // Fetch Data
            var groupSites = await _context.RosterGroupSites
                .Where(x => x.RosterGroupId == groupId)
                .Include(x => x.ClientSite)
                .ThenInclude(x => x.ClientType)
                .ToListAsync();

            var schedules = await _context.RosterSchedules
                .Where(x => x.RosterGroupId == groupId && !x.IsDeleted && x.ShiftStart >= startDate && x.ShiftStart <= endDate)
                .Include(x => x.Guard)
                .ToListAsync();

            using (var stream = new MemoryStream())
            {
                var writer = new PdfWriter(stream);
                var pdf = new PdfDocument(writer);
                pdf.SetDefaultPageSize(PageSize.A4.Rotate());
                var document = new Document(pdf);
                document.SetMargins(MARGIN, MARGIN, MARGIN, MARGIN);

                // Header
                var groupName = group.Name ?? "Unknown Project";
                
                document.Add(new Paragraph($"Roster: {groupName}")
                    .SetFont(PdfHelper.GetPdfFont())
                    .SetFontSize(16)
                    .SetTextAlignment(TextAlignment.CENTER));

                document.Add(new Paragraph($"Week: {startDate:dd MMM yyyy} - {startDate.AddDays(6):dd MMM yyyy}")
                    .SetFontSize(12)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(10));

                // Grid Table
                // Columns: Site (20%), Mon(11.4%), Tue... Sun
                float[] columnWidths = { 20f, 11.4f, 11.4f, 11.4f, 11.4f, 11.4f, 11.4f, 11.4f };
                var table = new Table(UnitValue.CreatePercentArray(columnWidths)).UseAllAvailableWidth();

                // Table Header
                table.AddHeaderCell(CreateHeaderCell("Site"));
                for (int i = 0; i < 7; i++)
                {
                    table.AddHeaderCell(CreateHeaderCell(startDate.AddDays(i).ToString("ddd dd/MM")));
                }

                // Table Rows
                foreach (var site in groupSites)
                {
                    // Site Cell
                    var siteCell = new Cell().Add(new Paragraph(site.ClientSite.Name).SetFontSize(FONT_SIZE_CELL).SetFont(PdfHelper.GetPdfFont()));
                    siteCell.Add(new Paragraph(site.ClientSite.ClientType?.Name ?? "").SetFontSize(6f).SetFontColor(ColorConstants.GRAY));
                    table.AddCell(siteCell);

                    // Days
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
                            var timeRange = $"{shift.ShiftStart:HH:mm}-{shift.ShiftEnd:HH:mm}";

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
                document.Close();

                return stream.ToArray();
            }
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
