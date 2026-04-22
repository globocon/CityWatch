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
using iText.Kernel.Utils;
using Path = System.IO.Path;
using CityWatch.Data.Helpers;
using iText.IO.Image;
using iText.IO.Font.Constants;
using iText.Kernel.Font;

namespace CityWatch.Web.Services
{
    public interface IRosterReportGenerator
    {
        Task<byte[]> GenerateRosterPdfAsync(int groupId, DateTime startDate, int weeks = 1, bool includeFinancials = false, bool includeSuppliers = false, string rateType = "guard");
        Task<byte[]> GenerateBinderRosterPdfAsync(int binderId, DateTime startDate, int weeks = 1, bool includeFinancials = false, bool includeSuppliers = false, string rateType = "guard");
        Task<byte[]> GeneratePreviewRosterPdfAsync(string type, int id);
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
        private readonly string _imageStampDir;


        private const float MARGIN = 15f;
        private const float FONT_SIZE_CELL = 7.5f;

        public RosterReportGenerator(CityWatchDbContext context, IWebHostEnvironment webHostEnvironment, IClientDataProvider clientDataProvider, IConfigDataProvider configDataProvider, IOptions<Settings> options)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _clientDataProvider = clientDataProvider;
            _configDataProvider = configDataProvider;
            _settings = options.Value;
            _imageRootDir = Path.Combine(webHostEnvironment.WebRootPath, "images");
            _subDomainImageRootDir = Path.Combine(webHostEnvironment.WebRootPath, "SubdomainLogo");
            _imageStampDir = Path.Combine(webHostEnvironment.WebRootPath, "images", "stamps");


            // Ensure RosterCovers directories exist
            string projectCoverDir = Path.Combine(_webHostEnvironment.WebRootPath, "Uploads", "RosterCovers", "Projects");
            string groupCoverDir = Path.Combine(_webHostEnvironment.WebRootPath, "Uploads", "RosterCovers", "Groups");
            if (!Directory.Exists(projectCoverDir)) Directory.CreateDirectory(projectCoverDir);
            if (!Directory.Exists(groupCoverDir)) Directory.CreateDirectory(groupCoverDir);
        }

        private class PublicHolidayInfo
        {
            public DateTime Date { get; set; }
            public List<string> States { get; set; }
        }

        public async Task<byte[]> GenerateRosterPdfAsync(int groupId, DateTime startDate, int weeks = 1, bool includeFinancials = false, bool includeSuppliers = false, string rateType = "guard")
        {
            var group = await _context.RosterGroups.FindAsync(groupId);
            if (group == null) return null;

            byte[] rosterBytes = await GenerateSingleProjectRosterPartAsync(groupId, startDate, weeks, includeFinancials, includeSuppliers, rateType);

            if (string.IsNullOrEmpty(group.CoverFileName))
            {
                return rosterBytes;
            }

            using (var ms = new MemoryStream())
            {
                using (var writer = new PdfWriter(ms))
                using (var pdf = new PdfDocument(writer))
                {
                    pdf.SetDefaultPageSize(PageSize.A4.Rotate());
                    var merger = new PdfMerger(pdf);
                    AddFileToMerger(merger, "Projects", group.CoverFileName);
                    AddBytesToMerger(merger, rosterBytes);
                    merger.Close();
                }
                return ms.ToArray();
            }
        }

        public async Task<byte[]> GeneratePreviewRosterPdfAsync(string type, int id)
        {
            string fileName = "";
            string subDir = "";

            if (type == "project")
            {
                var project = await _context.RosterGroups.FindAsync(id);
                if (project == null || string.IsNullOrEmpty(project.CoverFileName)) return null;
                fileName = project.CoverFileName;
                subDir = "Projects";
            }
            else
            {
                var binder = await _context.RosterBinders.FindAsync(id);
                if (binder == null || string.IsNullOrEmpty(binder.CoverFileName)) return null;
                fileName = binder.CoverFileName;
                subDir = "Groups";
            }

            // 1. Generate Mock Roster Page
            byte[] mockRosterBytes = await GenerateMockRosterPartAsync();

            // 2. Merge with Cover
            using (var ms = new MemoryStream())
            {
                using (var writer = new PdfWriter(ms))
                using (var pdf = new PdfDocument(writer))
                {
                    pdf.SetDefaultPageSize(PageSize.A4.Rotate());
                    var merger = new PdfMerger(pdf);
                    AddFileToMerger(merger, subDir, fileName);
                    AddBytesToMerger(merger, mockRosterBytes);
                    merger.Close();
                }
                return ms.ToArray();
            }
        }

        private async Task<byte[]> GenerateMockRosterPartAsync()
        {
            using (var ms = new MemoryStream())
            {
                using (var writer = new PdfWriter(ms))
                using (var pdf = new PdfDocument(writer))
                {
                    pdf.SetDefaultPageSize(PageSize.A4.Rotate());
                    using (var document = new Document(pdf))
                    {
                        document.SetMargins(MARGIN, MARGIN, MARGIN, MARGIN);

                        // Simple Mock Header
                        Table headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 20, 60, 20 })).UseAllAvailableWidth().SetMarginBottom(20);
                        
                        // Logo Placeholder
                        string logoPath = Path.Combine(_imageRootDir, "CWSLogoPdf.png");
                        if (File.Exists(logoPath))
                        {
                            try {
                                var logo = new Image(ImageDataFactory.Create(logoPath)).SetHeight(40);
                                headerTable.AddCell(new Cell().Add(logo).SetBorder(Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.MIDDLE));
                            } catch { headerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER)); }
                        } else {
                            headerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));
                        }

                        headerTable.AddCell(new Cell()
                            .Add(new Paragraph("LIVE PREVIEW DEMO").SetFont(PdfHelper.GetPdfFont()).SetFontSize(16))
                            .Add(new Paragraph("This is a sample layout for verification").SetFontSize(10))
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetBorder(Border.NO_BORDER));
                        headerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));
                        document.Add(headerTable);
                        
                        Table table = new Table(UnitValue.CreatePercentArray(8)).UseAllAvailableWidth().SetMarginTop(10);
                        table.AddHeaderCell(CreateHeaderCell("Site"));
                        table.AddHeaderCell(CreateHeaderCell("Mon 01/01"));
                        table.AddHeaderCell(CreateHeaderCell("Tue 02/01"));
                        table.AddHeaderCell(CreateHeaderCell("Wed 03/01"));
                        table.AddHeaderCell(CreateHeaderCell("Thu 04/01"));
                        table.AddHeaderCell(CreateHeaderCell("Fri 05/01"));
                        table.AddHeaderCell(CreateHeaderCell("Sat 06/01"));
                        table.AddHeaderCell(CreateHeaderCell("Sun 07/01"));

                        table.AddCell(new Cell().Add(new Paragraph("SAMPLE CLIENT SITE").SetFontSize(8)));
                        for (int i = 0; i < 7; i++)
                        {
                            table.AddCell(new Cell().Add(new Paragraph("Sample Shift Name\n00:00 - 00:00 (H)").SetFontSize(7)));
                        }

                        // Mock Total Row
                        table.AddCell(new Cell().Add(new Paragraph("Total Hours: 56.00").SetFontSize(8).SetFont(PdfHelper.GetPdfFont())).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                        for (int i = 0; i < 7; i++)
                        {
                            table.AddCell(new Cell().Add(new Paragraph("8.00").SetFontSize(8).SetFont(PdfHelper.GetPdfFont()).SetTextAlignment(TextAlignment.CENTER)).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                        }

                        document.Add(table);
                        AddBrandedFooter(document, pdf, DateTime.Today);
                    }
                }
                return ms.ToArray();
            }
        }

        public async Task<byte[]> GenerateBinderRosterPdfAsync(int binderId, DateTime startDate, int weeks = 1, bool includeFinancials = false, bool includeSuppliers = false, string rateType = "guard")
        {
            var binder = await _context.RosterBinders.FindAsync(binderId);
            if (binder == null) return null;

            var binderProjects = await _context.RosterBinderProjects
                .Where(x => x.RosterBinderId == binderId)
                .Include(x => x.RosterGroup)
                .ToListAsync();

            if (!binderProjects.Any()) return null;

            using (var ms = new MemoryStream())
            {
                using (var writer = new PdfWriter(ms))
                using (var pdf = new PdfDocument(writer))
                {
                    pdf.SetDefaultPageSize(PageSize.A4.Rotate());
                    var merger = new PdfMerger(pdf);

                    // 1. Group Cover
                    if (!string.IsNullOrEmpty(binder.CoverFileName))
                    {
                        AddFileToMerger(merger, "Groups", binder.CoverFileName);
                    }

                    // 2. Project Parts
                    foreach (var bp in binderProjects.OrderBy(x => x.SortOrder).ThenBy(x => x.Id))
                    {
                        // 2a. Project Cover
                        if (!string.IsNullOrEmpty(bp.RosterGroup.CoverFileName))
                        {
                            AddFileToMerger(merger, "Projects", bp.RosterGroup.CoverFileName);
                        }

                        // 2b. Project Roster
                        byte[] partBytes = await GenerateSingleProjectRosterPartAsync(bp.RosterGroupId, startDate, weeks, includeFinancials, includeSuppliers, rateType);
                        AddBytesToMerger(merger, partBytes);
                    }
                    merger.Close();
                }
                return ms.ToArray();
            }
        }

        private HashSet<int> _pagesWithHeader = new HashSet<int>();

        private async Task<byte[]> GenerateSingleProjectRosterPartAsync(int groupId, DateTime startDate, int weeks, bool includeFinancials, bool includeSuppliers, string rateType)
        {
            var group = await _context.RosterGroups.FindAsync(groupId);
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
                    document.SetMargins(20f, MARGIN, 60f, MARGIN);
                    _pagesWithHeader.Clear();

                    var groupName = group.Name ?? "Unknown Project";

                    for (int w = 0; w < weeks; w++)
                    {
                        var weekStart = startDate.AddDays(w * 7);
                        var weekEnd = weekStart.AddDays(6);

                        // Fetch Holidays for this week (inclusive of recurring holidays)
                        var holidays = await _context.BroadcastBannerCalendarEvents
                            .Where(x => x.IsPublicHoliday && (x.RepeatYearly || (x.ExpiryDate >= weekStart && x.StartDate <= weekEnd)))
                            .ToListAsync();
                        var holidayIds = holidays.Select(x => x.id).ToList();
                        var holidayStates = await _context.PublicHolidayStates
                            .Where(x => holidayIds.Contains(x.CalendarEventId) && !x.IsDeleted)
                            .ToListAsync();

                        var weeklyHolidays = new List<PublicHolidayInfo>();
                        for (int d = 0; d < 7; d++)
                        {
                            var dDate = weekStart.AddDays(d).Date;
                            // Match by absolute date or recurring Month/Day
                            var dayHolidays = holidays.Where(h => 
                                (dDate >= h.StartDate.Date && dDate <= h.ExpiryDate.Date) ||
                                (h.RepeatYearly && h.StartDate.Month == dDate.Month && h.StartDate.Day == dDate.Day)
                            ).ToList();

                            var states = new List<string>();
                            foreach (var h in dayHolidays)
                            {
                                var hStates = holidayStates.Where(s => s.CalendarEventId == h.id).Select(s => s.State).ToList();
                                if (hStates.Count == 0) states.Add("ALL");
                                else states.AddRange(hStates);
                            }
                            weeklyHolidays.Add(new PublicHolidayInfo { Date = dDate, States = states.Distinct().ToList() });
                        }

                        // --- PART B: Intelligent Stacking Logic (Refined) ---
                        // We look at the "Max Lines Tall" (max shifts in any single day) to see if it's a "Short Week".
                        int maxDailyShifts = 0;
                        for (int i = 0; i < 7; i++)
                        {
                            var loopDate = weekStart.AddDays(i).Date;
                            var dayCount = schedules.Count(s => s.ClientSiteId == groupSites.FirstOrDefault()?.ClientSiteId && s.ShiftStart.Date == loopDate);
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
                                var dayCount = schedules.Count(s => s.ClientSiteId == groupSites.FirstOrDefault()?.ClientSiteId && s.ShiftStart.Date == loopDate);
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
                        foreach (var site in groupSites)
                        {
                            if (site.ClientSite != null)
                            {
                                var subDomain = _configDataProvider.GetSubDomainID(site.ClientSite.TypeId);
                                if (subDomain != null && !string.IsNullOrEmpty(subDomain.Logo))
                                {
                                    logoPath = Path.Combine(_subDomainImageRootDir, subDomain.Logo);
                                    break;
                                }
                            }
                        }
                        if (string.IsNullOrEmpty(logoPath)) logoPath = Path.Combine(_imageRootDir, "CWSLogoPdf.png");

                        string siteImageUrl = string.Empty;
                        var primarySite = groupSites.FirstOrDefault();
                        if (primarySite != null)
                        {
                            var clientSiteSetting = _clientDataProvider.GetClientSiteKpiSetting(primarySite.ClientSiteId);
                            if (clientSiteSetting != null && !string.IsNullOrEmpty(clientSiteSetting.SiteImage))
                            {
                                try { siteImageUrl = $"{new Uri(_settings.KpiWebUrl)}{clientSiteSetting.SiteImage}"; } catch { }
                            }
                        }

                        // Add Smart Header (Draws only once per page)
                        AddSmartHeader(document, pdf, groupName, logoPath, siteImageUrl);

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

                        // Track totals for the week
                        double[] dailyTotals = new double[7];
                        double projectWeeklyGrandTotal = 0;

                        foreach (var site in groupSites.OrderBy(x => x.ClientSite.Name))
                        {
                            var siteCell = new Cell().SetPadding(5).SetBorder(new SolidBorder(ColorConstants.BLACK, 0.5f));
                            
                            // Site Name and Type
                            siteCell.Add(new Paragraph(site.ClientSite.Name).SetFontSize(FONT_SIZE_CELL).SetFont(PdfHelper.GetPdfFont()).SetMarginBottom(0));
                            siteCell.Add(new Paragraph(site.ClientSite.ClientType?.Name ?? "Security Service").SetFontSize(6.5f).SetFont(PdfHelper.GetPdfFont()).SetFontColor(ColorConstants.GRAY).SetMarginBottom(2));

                            // Status Section (Read-Only Status from Roster Admin)
                            // We fetch the status persisted in the Roster Admin module for the specific site and week start date.
                            // If a status exists (Paid, Invoiced, Cancelled), we render a colored stamp on the site information column.
                            var statusObj = await _context.RosterSiteWeekStatuses
                                .FirstOrDefaultAsync(x => x.ClientSiteId == site.ClientSiteId && x.StartDate == weekStart);
                            var status = statusObj?.Status ?? "Live";

                            siteCell.SetMinHeight(60f);
                            table.AddCell(siteCell);

                            for (int i = 0; i < 7; i++)
                            {
                                var loopDate = weekStart.AddDays(i).Date;
                                var dayShifts = schedules.Where(s => s.ClientSiteId == site.ClientSiteId && s.ShiftStart.Date == loopDate).OrderBy(s => s.ShiftStart).ToList();
                                var dayCell = new Cell().SetPadding(2);

                                // Background Highlighting Logic
                                var columnBgColor = ColorConstants.WHITE;
                                
                                // Weekends
                                if (loopDate.DayOfWeek == DayOfWeek.Saturday) columnBgColor = new DeviceRgb(215, 240, 215); // #d7f0d7
                                else if (loopDate.DayOfWeek == DayOfWeek.Sunday) columnBgColor = new DeviceRgb(252, 228, 236); // #fce4ec

                                // Public Holidays
                                var phInfo = weeklyHolidays.FirstOrDefault(x => x.Date == loopDate);
                                if (phInfo != null && phInfo.States.Any())
                                {
                                    var siteState = site.ClientSite?.State?.Trim().ToUpper();
                                    if (phInfo.States.Contains("ALL") || (!string.IsNullOrEmpty(siteState) && phInfo.States.Any(s => s.Trim().ToUpper() == siteState)))
                                    {
                                        columnBgColor = new DeviceRgb(255, 249, 196); // #FFF9C4
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
                                    if (shift.ShiftType == "AdhocAccepted")
                                    {
                                        bgColor = new DeviceRgb(27, 94, 32); // Dark Green
                                    }
                                    else if (shift.ShiftType == "AdhocNotAccepted")
                                    {
                                        bgColor = new DeviceRgb(230, 81, 0); // Dark Orange
                                    }

                                    var borderColor = ColorConstants.BLACK;
                                    var fontColor = ColorConstants.BLACK;

                                    if (shift.ShiftType == "AdhocAccepted" || shift.ShiftType == "AdhocNotAccepted")
                                    {
                                        fontColor = ColorConstants.WHITE;
                                        borderColor = ColorConstants.WHITE;
                                    }

                                    if (isRelief && (string.IsNullOrEmpty(shift.ShiftType) || shift.ShiftType == "Regular"))
                                    {
                                        bgColor = new DeviceRgb(111, 66, 193); // Dark purple bg (matches #6f42c1)
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
                                            if (!string.IsNullOrEmpty(replacedName))
                                            {
                                                guardName += " [" + shift.ReliefReason + "] " + Truncate(replacedName, 8);
                                            }
                                            else
                                            {
                                                guardName += " [" + shift.ReliefReason + "]";
                                            }
                                        }
                                    }

                                    shiftBlock.Add(new Paragraph(guardName).SetFontSize(7).SetFont(PdfHelper.GetPdfFont()).SetFontColor(fontColor).SetMarginBottom(2));
                                    
                                    // Add License Number
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
                                        var supplierName = shift.Guard?.Provider ?? shift.ProviderName ?? "N/A";
                                        var callsignSuffix = shift.Callsign != null ? $" ({shift.Callsign.Name})" : "";
                                        shiftBlock.Add(new Paragraph(supplierName + callsignSuffix).SetFontSize(6.5f).SetFontColor(new DeviceRgb(200, 0, 0)).SetBold().SetMarginTop(0).SetMarginBottom(2));
                                    }

                                    if (includeFinancials)
                                    {
                                        shiftBlock.Add(new Paragraph($"$ {value:F2}").SetFontSize(6.5f).SetFontColor(new DeviceRgb(200, 0, 0)).SetBold().SetMarginTop(1).SetMarginBottom(0));
                                    }
                                    dayCell.Add(shiftBlock);
                                }
                                table.AddCell(dayCell);
                            }
                        }

                        // Add Footer Row for Totals
                        Cell totalLabelCell = new Cell().SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetPadding(2);
                        string totalText = includeFinancials ? $"Total Pay: $ {projectWeeklyGrandTotal:F2}" : $"Total Hours: {projectWeeklyGrandTotal:F2}";
                        totalLabelCell.Add(new Paragraph(totalText).SetFontSize(FONT_SIZE_CELL).SetFont(PdfHelper.GetPdfFont()));
                        table.AddCell(totalLabelCell);

                        for (int i = 0; i < 7; i++)
                        {
                            string dailyTotalText = includeFinancials ? $"$ {dailyTotals[i]:F2}" : $"{dailyTotals[i]:F2}";
                            table.AddCell(new Cell().Add(new Paragraph(dailyTotalText).SetFontSize(FONT_SIZE_CELL).SetFont(PdfHelper.GetPdfFont()).SetTextAlignment(TextAlignment.CENTER)).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetPadding(2));
                        }

                        weekWrapperCell.Add(table);
                        weekWrapper.AddCell(weekWrapperCell);
                        document.Add(weekWrapper);

                        // Call again in case the wrapper moved to a new page!
                        AddSmartHeader(document, pdf, groupName, logoPath, siteImageUrl);

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

        private void AddSmartHeader(Document document, PdfDocument pdf, string groupName, string logoPath, string siteImageUrl)
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
                .Add(new Paragraph($"Roster: {groupName}").SetFont(PdfHelper.GetPdfFont()).SetFontSize(16))
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

        private void AddFileToMerger(PdfMerger merger, string subDir, string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;
            string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "Uploads", "RosterCovers", subDir, fileName);
            if (!File.Exists(filePath)) return;

            try
            {
                using (var reader = new PdfReader(filePath))
                using (var pdf = new PdfDocument(reader))
                {
                    merger.Merge(pdf, 1, pdf.GetNumberOfPages());
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error merging file: {ex.Message}"); }
        }

        private void AddBytesToMerger(PdfMerger merger, byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return;
            try
            {
                using (var ms = new MemoryStream(bytes))
                using (var reader = new PdfReader(ms))
                using (var pdf = new PdfDocument(reader))
                {
                    merger.Merge(pdf, 1, pdf.GetNumberOfPages());
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error merging bytes: {ex.Message}"); }
        }

        private Cell CreateHeaderCell(string text)
        {
            return new Cell()
                .Add(new Paragraph(text).SetFont(PdfHelper.GetPdfFont()).SetFontSize(FONT_SIZE_CELL))
                .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                .SetTextAlignment(TextAlignment.CENTER);
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
