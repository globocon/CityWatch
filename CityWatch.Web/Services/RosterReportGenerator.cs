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
using iText.IO.Font.Constants;
using iText.Kernel.Font;

namespace CityWatch.Web.Services
{
    public interface IRosterReportGenerator
    {
        Task<byte[]> GenerateRosterPdfAsync(int groupId, DateTime startDate, int weeks = 1, bool includeFinancials = false, bool includeSuppliers = false);
        Task<byte[]> GenerateBinderRosterPdfAsync(int binderId, DateTime startDate, int weeks = 1, bool includeFinancials = false, bool includeSuppliers = false);
    }



    public class RosterReportGenerator : IRosterReportGenerator
    {
        private readonly CityWatchDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly string _imageRootDir;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IConfigDataProvider _configDataProvider;
        private readonly Settings _settings;
        private readonly string _subDomainImageRootDir;

        private const float MARGIN = 15f; // Match TimesheetReportGenerator
        private const float FONT_SIZE_HEADER = 12f;
        private const float FONT_SIZE_CELL = 7.5f; // Match TimesheetReportGenerator

        public RosterReportGenerator(CityWatchDbContext context, IWebHostEnvironment webHostEnvironment, IClientDataProvider clientDataProvider, IConfigDataProvider configDataProvider, IOptions<Settings> options)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _clientDataProvider = clientDataProvider;
            _configDataProvider = configDataProvider;
            _settings = options.Value;
            _imageRootDir = System.IO.Path.Combine(webHostEnvironment.WebRootPath, "images");
            _subDomainImageRootDir = System.IO.Path.Combine(webHostEnvironment.WebRootPath, "SubdomainLogo");
        }

        public async Task<byte[]> GenerateBinderRosterPdfAsync(int binderId, DateTime startDate, int weeks = 1, bool includeFinancials = false, bool includeSuppliers = false)
        {
            var binder = await _context.RosterBinders.FindAsync(binderId);
            if (binder == null) return null;

            var binderProjects = await _context.RosterBinderProjects
                .Where(x => x.RosterBinderId == binderId)
                .Include(x => x.RosterGroup)
                .ToListAsync();

            if (!binderProjects.Any()) return null;

            var totalEndDate = startDate.AddDays(weeks * 7).AddSeconds(-1);

            using (var ms = new MemoryStream())
            {
                using (var writer = new PdfWriter(ms))
                using (var pdf = new PdfDocument(writer))
                {
                    pdf.SetDefaultPageSize(PageSize.A4.Rotate());
                    var document = new Document(pdf);
                    document.SetMargins(MARGIN, MARGIN, MARGIN, MARGIN);

                bool firstProject = true;

                foreach (var bp in binderProjects.OrderBy(x => x.RosterGroup.Name))
                {
                    if (!firstProject)
                    {
                        document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                    }
                    firstProject = false;

                    var groupId = bp.RosterGroupId;
                    var group = bp.RosterGroup;

                    var groupSites = await _context.RosterGroupSites
                        .Where(x => x.RosterGroupId == groupId)
                        .Include(x => x.ClientSite)
                        .ThenInclude(x => x.ClientType)
                        .ToListAsync();

                    var schedules = await _context.RosterSchedules
                        .Where(x => x.RosterGroupId == groupId && !x.IsDeleted && x.ShiftStart >= startDate && x.ShiftStart <= totalEndDate)
                        .Include(x => x.Guard)
                        .Include(x => x.ReliefGuard)
                        .Include(x => x.Callsign)
                        .Include(x => x.PayRate)
                        .ToListAsync();

                    var groupName = group.Name ?? "Unknown Project";

                    for (int w = 0; w < weeks; w++)
                    {
                        var weekStart = startDate.AddDays(w * 7);
                        var weekEnd = weekStart.AddDays(6);

                        if (w > 0)
                        {
                            document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                        }

                        var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 20, 60, 20 })).UseAllAvailableWidth();

                        // Resolve Logo
                        string logoPath = string.Empty;
                        foreach (var site in groupSites)
                        {
                            if (site.ClientSite != null)
                            {
                                var subDomain = _configDataProvider.GetSubDomainID(site.ClientSite.TypeId);
                                if (subDomain != null && !string.IsNullOrEmpty(subDomain.Logo))
                                {
                                    logoPath = System.IO.Path.Combine(_subDomainImageRootDir, subDomain.Logo);
                                    break;
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(logoPath))
                        {
                            logoPath = System.IO.Path.Combine(_imageRootDir, "CWSLogoPdf.png");
                        }

                        if (!string.IsNullOrEmpty(logoPath))
                        {
                            try {
                                var logo = new Image(ImageDataFactory.Create(logoPath)).SetHeight(50);
                                headerTable.AddCell(new Cell().Add(logo).SetBorder(Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.MIDDLE));
                            } catch { headerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER)); }
                        } else { headerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER)); }

                        var titleCell = new Cell()
                            .Add(new Paragraph($"Roster: {groupName}").SetFont(PdfHelper.GetPdfFont()).SetFontSize(16))
                            .Add(new Paragraph($"Week: {weekStart:dd MMM yyyy} - {weekEnd:dd MMM yyyy}").SetFontSize(12))
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                            .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                            .SetBorder(Border.NO_BORDER);

                        headerTable.AddCell(titleCell);
                        
                        var cellSiteImage = new Cell().SetBorder(Border.NO_BORDER);
                        var primarySite = groupSites.FirstOrDefault();
                        if (primarySite != null)
                        {
                            var clientSiteSetting = _clientDataProvider.GetClientSiteKpiSetting(primarySite.ClientSiteId);
                            if (clientSiteSetting != null && !string.IsNullOrEmpty(clientSiteSetting.SiteImage))
                            {
                                try {
                                    var siteImageUrl = $"{new Uri(_settings.KpiWebUrl)}{clientSiteSetting.SiteImage}";
                                    var siteImage = new Image(ImageDataFactory.Create(siteImageUrl)).SetHeight(50).SetHorizontalAlignment(HorizontalAlignment.RIGHT);
                                    cellSiteImage.Add(siteImage);
                                } catch (Exception) { }
                            }
                        }
                        headerTable.AddCell(cellSiteImage);

                        document.Add(headerTable);
                        document.Add(new Paragraph("\n"));

                        float[] columnWidths = { 20f, 11.4f, 11.4f, 11.4f, 11.4f, 11.4f, 11.4f, 11.4f };
                        var table = new Table(UnitValue.CreatePercentArray(columnWidths)).UseAllAvailableWidth();

                        table.AddHeaderCell(CreateHeaderCell("Site"));
                        for (int i = 0; i < 7; i++)
                        {
                            table.AddHeaderCell(CreateHeaderCell(weekStart.AddDays(i).ToString("ddd dd/MM")));
                        }
                        double[] dailyTotals = new double[7];
                        double weeklyTotal = 0;

                        foreach (var site in groupSites)
                        {
                            var siteCell = new Cell().Add(new Paragraph(site.ClientSite.Name).SetFontSize(FONT_SIZE_CELL).SetFont(PdfHelper.GetPdfFont()));
                            siteCell.Add(new Paragraph(site.ClientSite.ClientType?.Name ?? "").SetFontSize(6f).SetFontColor(ColorConstants.GRAY));
                            table.AddCell(siteCell);

                            for (int i = 0; i < 7; i++)
                            {
                                var loopDate = weekStart.AddDays(i).Date;
                                var dayShifts = schedules.Where(s => s.ClientSiteId == site.ClientSiteId && s.ShiftStart.Date == loopDate).OrderBy(s => s.ShiftStart).ToList();

                                var dayCell = new Cell().SetPadding(2);
                                foreach (var shift in dayShifts)
                                {
                                    bool isRelief = shift.ReliefGuardId.HasValue || !string.IsNullOrEmpty(shift.ReliefProviderName);
                                    var statusColor = isRelief ? new DeviceRgb(243, 229, 245) : GetStatusColor(shift.Status);
                                    
                                    var guardName = shift.GuardId.HasValue ? shift.Guard.Name : shift.ProviderName;
                                    if (isRelief) {
                                        guardName = "{R} " + (shift.ReliefGuardId.HasValue ? shift.ReliefGuard.Name : shift.ReliefProviderName);
                                    }

                                    var duration = DateTimeHelper.CalculateDisplayDuration(shift.ShiftStart, shift.ShiftEnd);
                                    var timeRangeStr = $"{shift.ShiftStart:HH:mm} - {shift.ShiftEnd:HH:mm}";

                                    dailyTotals[i] += includeFinancials ? (duration * (double)(shift.PayRate?.GuardPayRate ?? 0)) : duration;
                                    weeklyTotal += includeFinancials ? (duration * (double)(shift.PayRate?.GuardPayRate ?? 0)) : duration;

                                    var shiftBlock = new Div()
                                        .SetBackgroundColor(statusColor)
                                        .SetMarginBottom(2)
                                        .SetPadding(2)
                                        .SetBorder(new SolidBorder(isRelief ? new DeviceRgb(111, 66, 193) : ColorConstants.BLACK, 0.5f));

                                    shiftBlock.Add(new Paragraph(guardName ?? "Unknown").SetFontSize(7).SetFont(PdfHelper.GetPdfFont()).SetBold());
                                    
                                    if (isRelief && !string.IsNullOrEmpty(shift.ReliefReason)) {
                                        shiftBlock.Add(new Paragraph("[" + shift.ReliefReason + "]").SetFontSize(6).SetFontColor(new DeviceRgb(111, 66, 193)).SetItalic());
                                    }

                                    shiftBlock.Add(new Paragraph($"{timeRangeStr} ({Math.Round(duration, 2)}h)").SetFontSize(5.5f));
                                    
                                    if (includeSuppliers)
                                    {
                                        var supplierName = shift.GuardId.HasValue ? (shift.Guard.Provider ?? "N/A") : (shift.ProviderName ?? "N/A");
                                        var callsignSuffix = shift.Callsign != null ? $" ({shift.Callsign.Name})" : "";
                                        shiftBlock.Add(new Paragraph(supplierName + callsignSuffix).SetFontSize(6.5f).SetFontColor(new DeviceRgb(0, 86, 179)).SetBold());
                                    }
                                    else if (shift.Callsign != null)
                                    {
                                        shiftBlock.Add(new Paragraph($"Callsign: {shift.Callsign.Name}").SetFontSize(6).SetFont(PdfHelper.GetPdfFont()));
                                    }

                                    if (includeFinancials)
                                    {
                                        var pay = duration * (double)(shift.PayRate?.GuardPayRate ?? 0);
                                        shiftBlock.Add(new Paragraph($"$ {pay:F2}").SetFontSize(6.5f).SetFontColor(new DeviceRgb(200, 0, 0)).SetBold());
                                    }

                                    dayCell.Add(shiftBlock);
                                }
                                table.AddCell(dayCell);
                            }
                        }

                        var totalLabelCell = new Cell().SetBackgroundColor(ColorConstants.WHITE).SetPadding(2);
                        string weeklyTotalStr = includeFinancials ? $"Total Pay: ${weeklyTotal:F2}" : $"Total HRS: {Math.Round(weeklyTotal, 2)}h";
                        totalLabelCell.Add(new Paragraph(weeklyTotalStr).SetFont(PdfHelper.GetPdfFont()).SetFontSize(7f).SetFontColor(ColorConstants.BLACK));
                        table.AddCell(totalLabelCell);

                        for (int i = 0; i < 7; i++)
                        {
                            var dayTotalCell = new Cell().SetBackgroundColor(ColorConstants.WHITE).SetPadding(2).SetTextAlignment(TextAlignment.CENTER);
                            string dayTotalStr = includeFinancials ? $"${dailyTotals[i]:F2}" : $"{Math.Round(dailyTotals[i], 2)}h";
                            dayTotalCell.Add(new Paragraph(dayTotalStr).SetFont(PdfHelper.GetPdfFont()).SetFontSize(7f).SetFontColor(ColorConstants.BLACK));
                            table.AddCell(dayTotalCell);
                        }

                        document.Add(table);
                        AddBrandedFooter(document, pdf, weekStart);
                    }
                }

                document.Close();
                }
                return ms.ToArray();
            }
        }

        public async Task<byte[]> GenerateRosterPdfAsync(int groupId, DateTime startDate, int weeks = 1, bool includeFinancials = false, bool includeSuppliers = false)
        {
            var group = await _context.RosterGroups.FindAsync(groupId);
            if (group == null) return null;

            var totalEndDate = startDate.AddDays(weeks * 7).AddSeconds(-1);

            var groupSites = await _context.RosterGroupSites
                .Where(x => x.RosterGroupId == groupId)
                .Include(x => x.ClientSite)
                .ThenInclude(x => x.ClientType)
                .ToListAsync();

            var schedules = await _context.RosterSchedules
                .Where(x => x.RosterGroupId == groupId && !x.IsDeleted && x.ShiftStart >= startDate && x.ShiftStart <= totalEndDate)
                .Include(x => x.Guard)
                .Include(x => x.ReliefGuard)
                .Include(x => x.Callsign)
                .Include(x => x.PayRate)
                .ToListAsync();

            using (var ms = new MemoryStream())
            {
                using (var writer = new PdfWriter(ms))
                using (var pdf = new PdfDocument(writer))
                {
                    pdf.SetDefaultPageSize(PageSize.A4.Rotate());
                    var document = new Document(pdf);
                    document.SetMargins(MARGIN, MARGIN, MARGIN, MARGIN);

                var groupName = group.Name ?? "Unknown Project";

                for (int w = 0; w < weeks; w++)
                {
                    var weekStart = startDate.AddDays(w * 7);
                    var weekEnd = weekStart.AddDays(6);

                    var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 20, 60, 20 })).UseAllAvailableWidth();

                    // Resolve Logo (Check for 3rd Party Branding)
                    string logoPath = string.Empty;
                    foreach (var site in groupSites)
                    {
                        if (site.ClientSite != null)
                        {
                            var subDomain = _configDataProvider.GetSubDomainID(site.ClientSite.TypeId);
                            if (subDomain != null && !string.IsNullOrEmpty(subDomain.Logo))
                            {
                                logoPath = System.IO.Path.Combine(_subDomainImageRootDir, subDomain.Logo);
                                break;
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(logoPath))
                    {
                        logoPath = System.IO.Path.Combine(_imageRootDir, "CWSLogoPdf.png");
                    }

                    if (!string.IsNullOrEmpty(logoPath))
                    {
                        try
                        {
                            var logo = new Image(ImageDataFactory.Create(logoPath)).SetHeight(50);
                            headerTable.AddCell(new Cell().Add(logo).SetBorder(Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.MIDDLE));
                        }
                        catch
                        {
                            headerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));
                        }
                    }
                    else { headerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER)); }

                    var titleCell = new Cell()
                        .Add(new Paragraph($"Roster: {groupName}").SetFont(PdfHelper.GetPdfFont()).SetFontSize(16))
                        .Add(new Paragraph($"Week: {weekStart:dd MMM yyyy} - {weekEnd:dd MMM yyyy}").SetFontSize(12))
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                        .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                        .SetBorder(Border.NO_BORDER);

                    headerTable.AddCell(titleCell);
                    
                    var cellSiteImage = new Cell().SetBorder(Border.NO_BORDER);
                    var primarySite = groupSites.FirstOrDefault();
                    if (primarySite != null)
                    {
                        var clientSiteSetting = _clientDataProvider.GetClientSiteKpiSetting(primarySite.ClientSiteId);
                        if (clientSiteSetting != null && !string.IsNullOrEmpty(clientSiteSetting.SiteImage))
                        {
                            try
                            {
                                var siteImageUrl = $"{new Uri(_settings.KpiWebUrl)}{clientSiteSetting.SiteImage}";
                                var siteImage = new Image(ImageDataFactory.Create(siteImageUrl))
                                    .SetHeight(50)
                                    .SetHorizontalAlignment(HorizontalAlignment.RIGHT);
                                cellSiteImage.Add(siteImage);
                            }
                            catch (Exception) { /* Fallback if image fails to load */ }
                        }
                    }
                    headerTable.AddCell(cellSiteImage);

                    document.Add(headerTable);
                    document.Add(new Paragraph("\n"));

                    float[] columnWidths = { 20f, 11.4f, 11.4f, 11.4f, 11.4f, 11.4f, 11.4f, 11.4f };
                    var table = new Table(UnitValue.CreatePercentArray(columnWidths)).UseAllAvailableWidth();

                    table.AddHeaderCell(CreateHeaderCell("Site"));
                    for (int i = 0; i < 7; i++)
                    {
                        table.AddHeaderCell(CreateHeaderCell(weekStart.AddDays(i).ToString("ddd dd/MM")));
                    }
                    double[] dailyTotals = new double[7];
                    double weeklyTotal = 0;

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
                                bool isRelief = shift.ReliefGuardId.HasValue || !string.IsNullOrEmpty(shift.ReliefProviderName);
                                var statusColor = isRelief ? new DeviceRgb(243, 229, 245) : GetStatusColor(shift.Status);
                                
                                var guardName = shift.GuardId.HasValue ? shift.Guard.Name : shift.ProviderName;
                                if (isRelief) {
                                    guardName = "{R} " + (shift.ReliefGuardId.HasValue ? shift.ReliefGuard.Name : shift.ReliefProviderName);
                                }

                                var duration = DateTimeHelper.CalculateDisplayDuration(shift.ShiftStart, shift.ShiftEnd);
                                var timeRangeStr = $"{shift.ShiftStart:HH:mm} - {shift.ShiftEnd:HH:mm}";
                                
                                dailyTotals[i] += includeFinancials ? (duration * (double)(shift.PayRate?.GuardPayRate ?? 0)) : duration;
                                weeklyTotal += includeFinancials ? (duration * (double)(shift.PayRate?.GuardPayRate ?? 0)) : duration;

                                var shiftBlock = new Div()
                                    .SetBackgroundColor(statusColor)
                                    .SetMarginBottom(2)
                                    .SetPadding(2)
                                    .SetBorder(new SolidBorder(isRelief ? new DeviceRgb(111, 66, 193) : ColorConstants.BLACK, 0.5f));

                                shiftBlock.Add(new Paragraph(guardName ?? "Unknown").SetFontSize(7).SetFont(PdfHelper.GetPdfFont()).SetBold());
                                
                                if (isRelief && !string.IsNullOrEmpty(shift.ReliefReason)) {
                                    shiftBlock.Add(new Paragraph("[" + shift.ReliefReason + "]").SetFontSize(6).SetFontColor(new DeviceRgb(111, 66, 193)).SetItalic());
                                }
                                
                                // Row 2: Time Range
                                shiftBlock.Add(new Paragraph($"{timeRangeStr} ({Math.Round(duration, 2)}h)").SetFontSize(5.5f));
                                
                                // Row 3: Supplier (+ Callsign)
                                if (includeSuppliers)
                                {
                                    var supplierName = shift.GuardId.HasValue ? (shift.Guard.Provider ?? "N/A") : (shift.ProviderName ?? "N/A");
                                    var callsignSuffix = shift.Callsign != null ? $" ({shift.Callsign.Name})" : "";
                                    shiftBlock.Add(new Paragraph(supplierName + callsignSuffix)
                                        .SetFontSize(6.5f)
                                        .SetFontColor(new DeviceRgb(0, 86, 179)) // Blue
                                        .SetBold());
                                }
                                else if (shift.Callsign != null)
                                {
                                    shiftBlock.Add(new Paragraph($"Callsign: {shift.Callsign.Name}").SetFontSize(6).SetFont(PdfHelper.GetPdfFont()));
                                }

                                // Row 4: Money
                                if (includeFinancials)
                                {
                                    var pay = duration * (double)(shift.PayRate?.GuardPayRate ?? 0);
                                    shiftBlock.Add(new Paragraph($"$ {pay:F2}")
                                        .SetFontSize(6.5f)
                                        .SetFontColor(new DeviceRgb(200, 0, 0)) // Bold Red
                                        .SetBold());
                                }

                                dayCell.Add(shiftBlock);
                            }
                            table.AddCell(dayCell);
                        }
                    }

                    // Total Row
                    var totalLabelCell = new Cell().SetBackgroundColor(ColorConstants.WHITE).SetPadding(2);
                    string weeklyTotalStr = includeFinancials ? $"Total Pay: ${weeklyTotal:F2}" : $"Total HRS: {Math.Round(weeklyTotal, 2)}h";
                    totalLabelCell.Add(new Paragraph(weeklyTotalStr)
                        .SetFont(PdfHelper.GetPdfFont())
                        .SetFontSize(7f)
                        .SetFontColor(ColorConstants.BLACK));
                    table.AddCell(totalLabelCell);

                    for (int i = 0; i < 7; i++)
                    {
                        var dayTotalCell = new Cell().SetBackgroundColor(ColorConstants.WHITE).SetPadding(2).SetTextAlignment(TextAlignment.CENTER);
                        string dayTotalStr = includeFinancials ? $"${dailyTotals[i]:F2}" : $"{Math.Round(dailyTotals[i], 2)}h";
                        dayTotalCell.Add(new Paragraph(dayTotalStr)
                            .SetFont(PdfHelper.GetPdfFont())
                            .SetFontSize(7f)
                            .SetFontColor(ColorConstants.BLACK));
                        table.AddCell(dayTotalCell);
                    }

                    document.Add(table);
                    AddBrandedFooter(document, pdf, weekStart);

                    if (w < weeks - 1)
                    {
                        document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                    }
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
                .Add(new Text(" @ ").SetFontSize(11))
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
