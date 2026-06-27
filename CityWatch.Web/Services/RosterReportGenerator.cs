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
using iText.Layout.Renderer;
using iText.Kernel.Pdf.Canvas;

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
                            bool isDuplicate = false;
                            if (!string.IsNullOrEmpty(binder.CoverFileName))
                            {
                                string groupCoverPath = Path.Combine(_webHostEnvironment.WebRootPath, "Uploads", "RosterCovers", "Groups", binder.CoverFileName);
                                string projectCoverPath = Path.Combine(_webHostEnvironment.WebRootPath, "Uploads", "RosterCovers", "Projects", bp.RosterGroup.CoverFileName);
                                
                                if (File.Exists(groupCoverPath) && File.Exists(projectCoverPath))
                                {
                                    var groupInfo = new FileInfo(groupCoverPath);
                                    var projectInfo = new FileInfo(projectCoverPath);
                                    
                                    // If the files are the exact same size, they are almost certainly the exact same uploaded file
                                    if (groupInfo.Length == projectInfo.Length)
                                    {
                                        isDuplicate = true;
                                    }
                                }
                            }

                            if (!isDuplicate)
                            {
                                AddFileToMerger(merger, "Projects", bp.RosterGroup.CoverFileName);
                            }
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

                    var groupName = group.Name ?? "Unknown Project";
                    int weeksOnCurrentPage = 0;

                    for (int w = 0; w < weeks; w++)
                    {
                        var weekStart = startDate.AddDays(w * 7);
                        weeksOnCurrentPage++;
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

                            // Branding Logic
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
                                .Add(new Paragraph($"Roster: {groupName}").SetFont(PdfHelper.GetPdfFont()).SetFontSize(16).SetMarginBottom(0))
                                .SetTextAlignment(TextAlignment.CENTER)
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
                                        var siteImage = new Image(ImageDataFactory.Create(siteImageUrl)).SetHeight(50).SetHorizontalAlignment(HorizontalAlignment.RIGHT);
                                        cellSiteImage.Add(siteImage);
                                    }
                                    catch { }
                                }
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

                            AddStatusStampToCell(siteCell, status);

                            // Append Legend and Notes to siteCell
                            var absenceDict = new Dictionary<string, List<string>>();
                            foreach (var shift in schedules.Where(s => s.ClientSiteId == site.ClientSiteId && s.ShiftStart >= weekStart && s.ShiftStart < weekEnd.AddDays(1)))
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
                                if (shift.Status == CityWatch.Data.Enums.RosterShiftStatus.Declined || shift.Status == CityWatch.Data.Enums.RosterShiftStatus.Missed)
                                {
                                    var gName = shift.Guard?.Name ?? shift.ProviderName ?? "Unassigned";
                                    var key = $"{gName} - {(shift.Status == CityWatch.Data.Enums.RosterShiftStatus.Missed ? "Missed" : "Declined")}";
                                    
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
                            AddLegendRow("Unassigned - UNKNOWN", new DeviceRgb(189, 189, 189));
                            AddLegendRow("Unassigned - FIXED", new DeviceRgb(255, 183, 77));
                            AddLegendRow("Unassigned - ADHOC", new DeviceRgb(255, 143, 0));
                            AddLegendRow("Accepted - FIXED", new DeviceRgb(144, 238, 144));
                            AddLegendRow("Accepted - ADHOC", new DeviceRgb(50, 205, 50));
                            AddLegendRow("Relief Guard (Accepted)", new DeviceRgb(111, 66, 193));
                            AddLegendRow("DECLINED (OPEN)", ColorConstants.BLACK);
                            AddLegendRow("MISSED (inc LATE)", new DeviceRgb(66, 66, 66));
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
                                // Match the on-screen Booking grid: keep a guard's continuous
                                // (back-to-back) shifts together instead of letting another guard be
                                // printed between them. See RosterShiftSorter.
                                var dayShifts = RosterShiftSorter.OrderByContinuousBlocks(
                                    schedules.Where(s => s.ClientSiteId == site.ClientSiteId && s.ShiftStart.Date == loopDate)).ToList();
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
                                    
                                    if (shift.Status != CityWatch.Data.Enums.RosterShiftStatus.Cancelled && shift.Status != CityWatch.Data.Enums.RosterShiftStatus.Missed)
                                    {
                                        dailyTotals[i] += value;
                                        projectWeeklyGrandTotal += value;
                                    }

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

                                    // Contractor / Provider Color Override
                                    if (!string.IsNullOrEmpty(shift.ProviderName) && (shift.GuardId == null || shift.Guard?.Name == "External"))
                                    {
                                        bgColor = new DeviceRgb(189, 189, 189); // Light Grey
                                    }

                                    var borderColor = ColorConstants.BLACK;
                                    var fontColor = ColorConstants.BLACK;

                                    if (shift.ShiftType == "Adhoc" || shift.Status == CityWatch.Data.Enums.RosterShiftStatus.Declined)
                                    {
                                        fontColor = ColorConstants.WHITE;
                                        borderColor = ColorConstants.WHITE;
                                    }
                                    
                                    if (shift.Status == CityWatch.Data.Enums.RosterShiftStatus.Missed)
                                    {
                                        fontColor = ColorConstants.WHITE;
                                        borderColor = ColorConstants.WHITE;
                                        bgColor = new DeviceRgb(66, 66, 66);
                                    }

                                    if (isRelief)
                                    {
                                        bgColor = new DeviceRgb(111, 66, 193); // Dark purple bg (matches #6f42c1)
                                        borderColor = ColorConstants.WHITE;
                                        fontColor = ColorConstants.WHITE;
                                    }

                                    if (shift.Status == CityWatch.Data.Enums.RosterShiftStatus.Cancelled)
                                    {
                                        borderColor = ColorConstants.RED;
                                        fontColor = ColorConstants.RED;
                                        bgColor = ColorConstants.WHITE;
                                    }

                                    var shiftBlock = new Div()
                                        .SetBackgroundColor(bgColor)
                                        .SetMarginBottom(1)
                                        .SetPadding(1.5f)
                                        .SetBorder(new SolidBorder(borderColor, shift.Status == CityWatch.Data.Enums.RosterShiftStatus.Cancelled ? 1.0f : 0.5f));

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

                                    shiftBlock.Add(new Paragraph(guardName).SetFontSize(7.5f).SetFont(PdfHelper.GetPdfFont()).SetFontColor(fontColor).SetMarginBottom(0));
                                    
                                    // Add License Number
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
                                        var supplierName = shift.Guard?.Provider ?? shift.ProviderName ?? "N/A";
                                        var callsignSuffix = shift.Callsign != null ? $" ({shift.Callsign.Name})" : "";
                                        shiftBlock.Add(new Paragraph(supplierName + callsignSuffix).SetFontSize(6.5f).SetFontColor(new DeviceRgb(200, 0, 0)).SetBold().SetMarginTop(0).SetMarginBottom(0));
                                    }

                                    if (includeFinancials)
                                    {
                                        shiftBlock.Add(new Paragraph($"$ {value:F2}").SetFontSize(7f).SetFontColor(new DeviceRgb(200, 0, 0)).SetBold().SetMarginTop(0.5f).SetMarginBottom(0));
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


        private Color GetStatusColor(CityWatch.Data.Enums.RosterShiftStatus status)
        {
            switch (status)
            {
                case CityWatch.Data.Enums.RosterShiftStatus.Accepted:
                    return new DeviceRgb(144, 238, 144); // Light Green
                case CityWatch.Data.Enums.RosterShiftStatus.Declined:
                    return new DeviceRgb(67, 67, 67); // Dark Gray
                case CityWatch.Data.Enums.RosterShiftStatus.Missed:
                    return new DeviceRgb(66, 66, 66); // Dark Gray
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
