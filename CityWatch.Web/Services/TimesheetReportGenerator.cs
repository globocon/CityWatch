using System;
using CityWatch.Common.Helpers;
using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using CityWatch.Web.Helpers;
using CityWatch.Web.Models;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Svg.Renderers.Impl;
using Jering.Javascript.NodeJS;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using static Dropbox.Api.TeamLog.SpaceCapsType;
using IO = System.IO;
using Microsoft.AspNetCore.Http;
using DocumentFormat.OpenXml.Office2010.PowerPoint;
using System.Globalization;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Geom;
using iText.Layout.Properties;
using static Dropbox.Api.TeamLog.TimeUnit;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using iText.Kernel.Pdf.Action;
using iText.Layout.Renderer;

namespace CityWatch.Web.Services
{
    public interface ITimesheetReportGenerator
    {

        public string GeneratePdfTimesheetReport(string startdate, string endDate, int guradid);
        public string GeneratePdfTimesheetReportCustom(string startdate, string endDate, int guradid);
        Task<string> GenerateTimesheetZipFile(int[] clientSiteIds, string startdate, string endDate);
        Task<string> GenerateTimesheetZipFileFrequency(int[] clientSiteIds, string startdate, string endDate);
        public string GeneratePdfTimesheetReportBulk(string startdate, string endDate, int guradid, string fileNamePart, int? siteId = null);

    }
    public class TimesheetReportGenerator : ITimesheetReportGenerator
    {
        private const float CELL_FONT_SIZE = 7.5f;
        private const float PDF_DOC_MARGIN = 15f;
        private const string REPORT_DIR = "Output";

        private const string CELL_BG_GREEN = "#96e3ac";
        private const string CELL_BG_RED = "#ffcccc";
        private const string CELL_BG_YELLOW = "#fcf8d1";
        private const string CELL_BG_BLUE_HEADER = "#bdd7ee";
        private const string CELL_BG_YELLOW_IR_COUNT = "#feff9a";
        private const string CELL_BG_ORANGE_IR_ALARM = "#ffdab3";
        private const string CELL_FONT_GREEN = "#008000";
        private const string CELL_FONT_RED = "#FF0000";
        private const string CELL_FONT_YELLOW = "#FFFF00";
        private const string COLOR_WHITE = "#ffffff";
        private const string COLOR_GREY = "#666362";

        private readonly float[] ACTUAL_COLUMNS = { 1.5f, 1.8f, 1.2f, 1.2f, 1.2f, 5.0f };
        private readonly float[] BOOKING_COLUMNS = { 1.5f, 1.8f, 1.2f, 1.2f, 1.2f, 4.0f, 1.2f, 1.5f };
        private const float ROW_HEIGHT = 16f;

        private readonly string _reportRootDir;
        private readonly string _imageRootDir;
        private readonly string _siteImageRootDir;
        private readonly string _graphImageRootDir;

        private readonly IViewDataService _viewDataService;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly ILogger<TimesheetReportGenerator> _logger;
        private readonly Settings _settings;
        private readonly IPatrolDataReportService _patrolDataReportService;
        private readonly string _SiteimageRootDir;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly string _downloadsFolderPath;
        public TimesheetReportGenerator(IOptions<Settings> settings,
            IWebHostEnvironment webHostEnvironment,
            IViewDataService viewDataService,
            IClientDataProvider clientDataProvider,
            ILogger<TimesheetReportGenerator> logger, IPatrolDataReportService patrolDataReportService)
        {
            _viewDataService = viewDataService;
            _clientDataProvider = clientDataProvider;
            _logger = logger;
            _settings = settings.Value;
            _webHostEnvironment = webHostEnvironment;
            _reportRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "Pdf");
            _imageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "images");
            _siteImageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "SiteImage");
            _graphImageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "GraphImage");
            _SiteimageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "SiteImage");
            _downloadsFolderPath = IO.Path.Combine(_webHostEnvironment.WebRootPath, "Pdf", "FromDropbox");
            //nEWLY ADDAED-START

           _patrolDataReportService = patrolDataReportService;
            //nEWLY ADDAED-END

            if (!IO.Directory.Exists(IO.Path.Combine(_reportRootDir, REPORT_DIR)))
                IO.Directory.CreateDirectory(IO.Path.Combine(_reportRootDir, REPORT_DIR));

            if (!IO.Directory.Exists(_downloadsFolderPath))
                IO.Directory.CreateDirectory(_downloadsFolderPath);

            if (!IO.Directory.Exists(_graphImageRootDir))
                IO.Directory.CreateDirectory(_graphImageRootDir);
        }

        public async Task<string> GenerateTimesheetZipFileFrequency(int[] clientSiteIds, string startdate, string endDate)
        {
            try
            {
                
                if (clientSiteIds.Length <= 0)
                {
                    return string.Empty;
                }

               
                var zipFolderPath = GetZipFolderPath();
                var fileNamePart = string.Empty;

                
                var clientSiteKpiSettings = _clientDataProvider.GetClientSiteKpiSetting(clientSiteIds)
                    .Where(z => !string.IsNullOrEmpty(z.DropboxImagesDir)).ToList();

                
                var clientSiteDetails = _clientDataProvider.GetGuardDetailsAll(clientSiteIds, startdate, endDate);

               
                foreach (var clientSiteDetail in clientSiteDetails)
                {
                    var guardId = clientSiteDetail.GuardId;
                    fileNamePart = clientSiteDetail.ClientSite.Name;

                   
                    CreateLogBookReportsFusion(guardId, zipFolderPath, startdate, endDate, fileNamePart, clientSiteDetail.ClientSiteId);
                }


                //DateTime dateTimeStart = DateTime.ParseExact(startdate, "dd-MM-yyyy hh:mm:ss", null, System.Globalization.DateTimeStyles.None);
                //DateTime dateTimeEnd = DateTime.ParseExact(endDate, "dd-MM-yyyy hh:mm:ss", null, System.Globalization.DateTimeStyles.None);
                // DateTime dateTimeStart = DateTime.ParseExact(startdate, "dd/MM/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture);
                //DateTime dateTimeEnd = DateTime.ParseExact(endDate, "dd/MM/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture);

                return GetZipFileName(zipFolderPath, startdate, endDate, fileNamePart);

            }
            catch (FormatException ex)
            {
                Console.WriteLine($"Date format is invalid: {ex.Message}");
                return null;  
            }
            catch (Exception ex)
            {
               
                Console.WriteLine($"An error occurred: {ex.Message}");
                return null;  
            }
        }
        public async Task<string> GenerateTimesheetZipFile(int[] clientSiteIds, string startdate, string endDate)
        {
            try
            {
                
                if (clientSiteIds.Length <= 0)
                {
                    return string.Empty;
                }

                
                var zipFolderPath = GetZipFolderPath();
                var fileNamePart = string.Empty;

              
                var clientSiteKpiSettings = _clientDataProvider.GetClientSiteKpiSetting(clientSiteIds)
                    .Where(z => !string.IsNullOrEmpty(z.DropboxImagesDir)).ToList();

                
                var clientSiteDetails = _clientDataProvider.GetGuardDetailsAll(clientSiteIds, startdate, endDate);

               
                foreach (var clientSiteDetail in clientSiteDetails)
                {
                    var guardId = clientSiteDetail.GuardId;
                    fileNamePart = clientSiteDetail.ClientSite.Name;

                    
                    CreateLogBookReportsFusion(guardId, zipFolderPath, startdate, endDate, fileNamePart, clientSiteDetail.ClientSiteId);
                }

                
               // DateTime dateTimeStart = DateTime.ParseExact(startdate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
               // DateTime dateTimeEnd = DateTime.ParseExact(endDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                
                return GetZipFileName(zipFolderPath, startdate, endDate, fileNamePart);
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"Date format is invalid: {ex.Message}");
                return null;  
            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"An error occurred: {ex.Message}");
                return null; 
            }
        }

        private string GetZipFolderPath()
        {
            var zipFolderPath = IO.Path.Combine(_downloadsFolderPath, Guid.NewGuid().ToString());
            if (!Directory.Exists(zipFolderPath))
                Directory.CreateDirectory(zipFolderPath);
            return zipFolderPath;
        }
        private string GetZipFileName(string zipFolderPath, DateTime logFromDate, DateTime logToDate, string fileNamePart)
        {
            var zipFileName = $"{FileNameHelper.GetSanitizedFileNamePart(fileNamePart)}_{logFromDate:yyyyMMdd}_{logToDate:yyyyMMdd}_{new Random().Next(100, 999)}.zip";
            if (!Directory.Exists(zipFolderPath))
                Directory.Delete(zipFolderPath);
            ZipFile.CreateFromDirectory(zipFolderPath, IO.Path.Combine(_downloadsFolderPath, zipFileName), CompressionLevel.Optimal, false);

            

            return zipFileName;
        }
        private string GetZipFileName(string zipFolderPath, string logFromDate, string logToDate, string fileNamePart)
        {
            var sanitizedLogFromDate = logFromDate.Replace("/", "_").Replace(":", "_").Replace(" ", "_");
            var sanitizedLogToDate = logToDate.Replace("/", "_").Replace(":", "_").Replace(" ", "_");

            // Create the sanitized file name
            var zipFileName = $"{FileNameHelper.GetSanitizedFileNamePart(fileNamePart)}_{sanitizedLogFromDate}_{sanitizedLogToDate}_{new Random().Next(100, 999)}.zip";

            ZipFile.CreateFromDirectory(zipFolderPath, IO.Path.Combine(_downloadsFolderPath, zipFileName), CompressionLevel.Optimal, false);

            if (!Directory.Exists(zipFolderPath))
                Directory.Delete(zipFolderPath);

            return zipFileName;
        }
        private void CreateLogBookReportsFusion(int GuardId, string zipFolderPath, string startdate, string endDate, string fileNamePart, int? siteId = null)
        {
           
                var fileName = GetFusionLogFileName(GuardId, startdate, endDate, fileNamePart, siteId);
                if (!string.IsNullOrEmpty(fileName))
                {
                    var reportFilePath = IO.Path.Combine(_webHostEnvironment.WebRootPath, "Pdf", "Output", fileName);
                    File.Copy(reportFilePath, IO.Path.Combine(zipFolderPath, fileName));
                    File.Delete(reportFilePath);
                }
            
        }
        private string GetFusionLogFileName(int GuardId, string startdate, string endDate, string fileNamePart, int? siteId = null)
        {

            try
            {
                return GeneratePdfTimesheetReportBulk(startdate, endDate, GuardId, fileNamePart, siteId);
            }
            catch (Exception ex)
            {
                // Log the exception or handle it according to your needs
                Console.WriteLine($"An error occurred: {ex.Message}");
                return null; // Or return a custom error message or default value
            }


        }
        public string GeneratePdfTimesheetReportBulk(string startdate, string endDate, int guradid, string fileNamePart, int? siteId = null)
        {
            DateTime startdateTime;
            DateTime dateTime;

            // Robust multi-format parsing
            string[] formats = { "dd/MM/yyyy", "yyyy-MM-dd", "dd-MM-yyyy", "MM/dd/yyyy", "dd/MM/yyyy HH:mm:ss", "yyyy-MM-dd HH:mm:ss" };
            if (!DateTime.TryParseExact(startdate, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out startdateTime))
            {
                if (!DateTime.TryParse(startdate, CultureInfo.InvariantCulture, DateTimeStyles.None, out startdateTime))
                {
                    startdateTime = DateTime.Parse(startdate); // Fallback to system locale
                }
            }

            if (!DateTime.TryParseExact(endDate, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
            {
                if (!DateTime.TryParse(endDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
                {
                    dateTime = DateTime.Parse(endDate); 
                }
            }

            try
            {
                var LoginDetailsFull = _clientDataProvider.GetLoginDetailsGuard(guradid, startdateTime, dateTime) ?? new List<GuardLogin>();
                var LoginDetails = LoginDetailsFull.Where(x => (!siteId.HasValue || x.ClientSiteId == siteId.Value) && (x.OffDuty.HasValue && x.OffDuty.Value > x.OnDuty)).ToList();
                var Name = _clientDataProvider.GetGuardlogName(guradid, dateTime) ?? "Unknown";
                var LicenseNo = _clientDataProvider.GetGuardLicenseNo(guradid, dateTime) ?? "";
                var SiteName = _clientDataProvider.GetGuardlogSite(guradid, dateTime) ?? "";
                
                var sanitizedName = FileNameHelper.GetSanitizedFileNamePart(Name);
                var sanitizedSitePart = FileNameHelper.GetSanitizedFileNamePart(fileNamePart);
                
                var reportFileName = $"{DateTime.Now.ToString("yyyyMMdd")} - {sanitizedName} - Time Sheet - {sanitizedSitePart} -_{new Random().Next()}.pdf";
                var reportPdf = IO.Path.Combine(_reportRootDir, REPORT_DIR, reportFileName);
                
                var TimesheetDetails = _clientDataProvider.GetTimesheetDetails();
                var weekName = TimesheetDetails?.weekName ?? "Week";
                
                var Enrollment = _clientDataProvider.GetGuardEnrollment(guradid) ?? "";
                var State = _clientDataProvider.GetGuardLicenseState(guradid) ?? "";
                var Supplier = _clientDataProvider.GetGuardCRMSupplier(guradid) ?? "";

                var rosterDetailsFull = _clientDataProvider.GetGuardRosterDetails(guradid, startdateTime, dateTime) ?? new List<RosterSchedule>();
                var rosterDetails = rosterDetailsFull.Where(r => !siteId.HasValue || r.ClientSiteId == siteId.Value).ToList();

                // Bulk Fetch Logs for Performance
                int[] loginIds = LoginDetails.Select(x => x.Id).ToArray();
                var logs = _clientDataProvider.GetGuardLogsByLoginIds(loginIds);
                var logsLookup = logs.Where(x => x.GuardLoginId.HasValue).GroupBy(x => x.GuardLoginId.Value).ToDictionary(g => g.Key, g => g.ToList());

                var pdfDoc = new PdfDocument(new PdfWriter(reportPdf));
                pdfDoc.SetDefaultPageSize(PageSize.A4.Rotate());
                var doc = new Document(pdfDoc);
                var renderer = new HelperDocumentRenderer(doc);
                doc.SetRenderer(renderer);
                doc.SetMargins(PDF_DOC_MARGIN, PDF_DOC_MARGIN, PDF_DOC_MARGIN, PDF_DOC_MARGIN);

                var headerTable = CreateReportHeader();
                doc.Add(headerTable);

                doc.Add(CreateNameTable(Name, Enrollment));
                doc.Add(CreateLicenseTable(LicenseNo, State));
                doc.Add(CreateDateTable(dateTime, Supplier));
                doc.Add(new Paragraph("\n"));

                Table masterTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 })).UseAllAvailableWidth().SetBorder(Border.NO_BORDER);

                Cell bookingCell = new Cell().SetBorder(Border.NO_BORDER).SetPaddingRight(5);
                bookingCell.Add(new Paragraph("BOOKING").SetBold());
                var (bookingTables, totalBookingHours, totalBookingPay) = CreateBookingDetails(startdateTime, dateTime, LoginDetails, rosterDetails, weekName);
                foreach (var table in bookingTables)
                {
                    bookingCell.Add(table);
                    bookingCell.Add(new Paragraph("\n"));
                }
                masterTable.AddCell(bookingCell);

                Cell actualCell = new Cell().SetBorder(Border.NO_BORDER).SetPaddingLeft(5);
                actualCell.Add(new Paragraph("ACTUAL").SetBold());
                var (GuardLoginTables, totalHours) = CreateGuardLoginDetails(startdateTime, dateTime, LoginDetails, weekName, logsLookup);

                foreach (var table in GuardLoginTables)
                {
                    actualCell.Add(table);
                    actualCell.Add(new Paragraph("\n"));
                }
                masterTable.AddCell(actualCell);

                doc.Add(masterTable);

                if (doc.GetRenderer() is HelperDocumentRenderer helperRenderer)
                {
                    float currentY = helperRenderer.GetCurrentY();
                    if (currentY < 115)
                    {
                        doc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                    }
                }

                var commentTable = GetCommentTable();
                float pageWidth = pdfDoc.GetDefaultPageSize().GetWidth();
                commentTable.SetFixedPosition(PDF_DOC_MARGIN, PDF_DOC_MARGIN, pageWidth - (2 * PDF_DOC_MARGIN));
                doc.Add(commentTable);
                doc.Close();
                pdfDoc.Close();

                return reportFileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating individual PDF for guard {GuardId}", guradid);
                return null;
            }
        }
        public string GeneratePdfTimesheetReport(string startdate, string endDate, int guradid)
        {
            DateTime startdateTime = DateTime.Parse(startdate, System.Globalization.CultureInfo.InvariantCulture);
            DateTime dateTime = DateTime.Parse(endDate, System.Globalization.CultureInfo.InvariantCulture);
            var LoginDetails = _clientDataProvider.GetLoginDetailsGuard(guradid, startdateTime, dateTime);
            var Name = _clientDataProvider.GetGuardlogName(guradid, dateTime);
            var LicenseNo = _clientDataProvider.GetGuardLicenseNo(guradid, dateTime);
            var SiteName = _clientDataProvider.GetGuardlogSite(guradid, dateTime);
            var reportFileName = $"{DateTime.Now.ToString("yyyyMMdd")} - {FileNameHelper.GetSanitizedFileNamePart(Name)} - Time Sheet -_{new Random().Next()}.pdf";
            var reportPdf = IO.Path.Combine(_reportRootDir, REPORT_DIR, reportFileName);
            var TimesheetDetails = _clientDataProvider.GetTimesheetDetails();
            var Enrollment = _clientDataProvider.GetGuardEnrollment(guradid);
            var State = _clientDataProvider.GetGuardLicenseState(guradid);
            var Supplier = _clientDataProvider.GetGuardCRMSupplier(guradid);
            
            // New Data Retrieval for Booking
            var rosterDetails = _clientDataProvider.GetGuardRosterDetails(guradid, startdateTime, dateTime);

            var pdfDoc = new PdfDocument(new PdfWriter(reportPdf));
            pdfDoc.SetDefaultPageSize(PageSize.A4.Rotate());
            var doc = new Document(pdfDoc);
            var renderer = new HelperDocumentRenderer(doc);
            doc.SetRenderer(renderer);
            doc.SetMargins(PDF_DOC_MARGIN, PDF_DOC_MARGIN, PDF_DOC_MARGIN, PDF_DOC_MARGIN);
           

            var headerTable = CreateReportHeader();
            doc.Add(headerTable);

            doc.Add(CreateNameTable(Name, Enrollment));
            doc.Add(CreateLicenseTable(LicenseNo, State));
            doc.Add(CreateDateTable(dateTime, Supplier));
            // doc.Add(CreateSiteTable(SiteName));
            doc.Add(new Paragraph("\n"));
            
            // 2-Column Layout Implementation
            // Create Master Table to hold Booking (Left) and Actual (Right)
            Table masterTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 })).UseAllAvailableWidth().SetBorder(Border.NO_BORDER);
            
            // Left Column: BOOKING
            Cell bookingCell = new Cell().SetBorder(Border.NO_BORDER).SetPaddingRight(5);
            bookingCell.Add(new Paragraph("BOOKING").SetBold());
            var (bookingTables, totalBookingHours, totalBookingPay) = CreateBookingDetails(startdateTime, dateTime, LoginDetails, rosterDetails, TimesheetDetails.weekName);
            foreach(var table in bookingTables)
            {
                bookingCell.Add(table);
                bookingCell.Add(new Paragraph("\n"));
            }
            masterTable.AddCell(bookingCell);

            // Right Column: ACTUAL
            Cell actualCell = new Cell().SetBorder(Border.NO_BORDER).SetPaddingLeft(5);
            actualCell.Add(new Paragraph("ACTUAL").SetBold()); // Removed Red
            
            // Bulk Fetch Logs for Performance
            int[] loginIds = LoginDetails.Select(x => x.Id).ToArray();
            var logs = _clientDataProvider.GetGuardLogsByLoginIds(loginIds);
            var logsLookup = logs.Where(x => x.GuardLoginId.HasValue).GroupBy(x => x.GuardLoginId.Value).ToDictionary(g => g.Key, g => g.ToList());

            var (GuardLoginTables, totalHours) = CreateGuardLoginDetails(startdateTime, dateTime, LoginDetails, TimesheetDetails.weekName, logsLookup);
            
            // Combine all Actual tables into one container or add them sequentially to the cell
            foreach(var table in GuardLoginTables)
            {
                actualCell.Add(table);
                actualCell.Add(new Paragraph("\n"));
            }
            masterTable.AddCell(actualCell);

            // Add Master Table to Document
            doc.Add(masterTable);
            
            bool hasContentOnCurrentPage = true; // Assumed true as we added content
            
            if (hasContentOnCurrentPage)
            {
                // Check available space using custom renderer
                if (doc.GetRenderer() is HelperDocumentRenderer helperRenderer)
                {
                    float currentY = helperRenderer.GetCurrentY();
                    // Estimated table height for totals + Margin (15) + Buffer (20) = 115
                    if (currentY < 115) 
                    {
                        doc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                    }
                }
            }

            var commentTable = GetCommentTable();
            // Position at bottom of current page
            float pageWidth = pdfDoc.GetDefaultPageSize().GetWidth();
            commentTable.SetFixedPosition(PDF_DOC_MARGIN, PDF_DOC_MARGIN, pageWidth - (2 * PDF_DOC_MARGIN));
            doc.Add(commentTable);
            doc.Close();
            pdfDoc.Close();

            return reportFileName;
        }

        public string GeneratePdfTimesheetReportCustom(string startdate, string endDate, int guradid)
        {
            DateTime startdateTime = DateTime.Parse(startdate, System.Globalization.CultureInfo.InvariantCulture);
            DateTime dateTime = DateTime.Parse(endDate, System.Globalization.CultureInfo.InvariantCulture);
            var LoginDetails = _clientDataProvider.GetLoginDetailsGuard(guradid, startdateTime, dateTime);
            var Name = _clientDataProvider.GetGuardlogName(guradid, dateTime);
            var LicenseNo = _clientDataProvider.GetGuardLicenseNo(guradid, dateTime);
            var SiteName = _clientDataProvider.GetGuardlogSite(guradid, dateTime);
            var reportFileName = $"{DateTime.Now.ToString("yyyyMMdd")} - {FileNameHelper.GetSanitizedFileNamePart(Name)} - Time Sheet -_{new Random().Next()}.pdf";
            var reportPdf = IO.Path.Combine(_reportRootDir, REPORT_DIR, reportFileName);
            var TimesheetDetails = _clientDataProvider.GetTimesheetDetails();
            var Enrollment = _clientDataProvider.GetGuardEnrollment(guradid);
            var State = _clientDataProvider.GetGuardLicenseState(guradid);
            var Supplier = _clientDataProvider.GetGuardCRMSupplier(guradid);

            // New Data Retrieval for Booking
            var rosterDetails = _clientDataProvider.GetGuardRosterDetails(guradid, startdateTime, dateTime);

            var pdfDoc = new PdfDocument(new PdfWriter(reportPdf));
            pdfDoc.SetDefaultPageSize(PageSize.A4.Rotate());
            var doc = new Document(pdfDoc);
            var renderer = new HelperDocumentRenderer(doc);
            doc.SetRenderer(renderer);
            doc.SetMargins(PDF_DOC_MARGIN, PDF_DOC_MARGIN, PDF_DOC_MARGIN, PDF_DOC_MARGIN);


            var headerTable = CreateReportHeader();
            doc.Add(headerTable);

            doc.Add(CreateNameTable(Name, Enrollment));
            doc.Add(CreateLicenseTable(LicenseNo, State));
            doc.Add(CreateDateTable(dateTime, Supplier));
            // doc.Add(CreateSiteTable(SiteName));
            doc.Add(new Paragraph("\n"));

            // 2-Column Layout Implementation
            Table masterTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 })).UseAllAvailableWidth().SetBorder(Border.NO_BORDER);

            // Left Column: BOOKING
            Cell bookingCell = new Cell().SetBorder(Border.NO_BORDER).SetPaddingRight(5);
            bookingCell.Add(new Paragraph("BOOKING").SetBold());
            var (bookingTables, totalBookingHours, totalBookingPay) = CreateBookingDetails(startdateTime, dateTime, LoginDetails, rosterDetails, TimesheetDetails.weekName);
            foreach(var table in bookingTables)
            {
                bookingCell.Add(table);
                bookingCell.Add(new Paragraph("\n"));
            }
            masterTable.AddCell(bookingCell);

            // Right Column: ACTUAL
            Cell actualCell = new Cell().SetBorder(Border.NO_BORDER).SetPaddingLeft(5);
            actualCell.Add(new Paragraph("ACTUAL").SetBold()); // Removed Red
            
            // Bulk Fetch Logs for Performance
            int[] loginIdsCustom = LoginDetails.Select(x => x.Id).ToArray();
            var logsCustom = _clientDataProvider.GetGuardLogsByLoginIds(loginIdsCustom);
            var logsLookupCustom = logsCustom.Where(x => x.GuardLoginId.HasValue).GroupBy(x => x.GuardLoginId.Value).ToDictionary(g => g.Key, g => g.ToList());

            var (GuardLoginTables, totalHours) = CreateGuardLoginDetails(startdateTime, dateTime, LoginDetails, TimesheetDetails.weekName, logsLookupCustom);
            
            foreach(var table in GuardLoginTables)
            {
                actualCell.Add(table);
                actualCell.Add(new Paragraph("\n"));
            }
            masterTable.AddCell(actualCell);

            doc.Add(masterTable);

            bool hasContentOnCurrentPage = true;
            if (hasContentOnCurrentPage)
            {
                // Check available space using custom renderer
                if (doc.GetRenderer() is HelperDocumentRenderer helperRenderer)
                {
                    float currentY = helperRenderer.GetCurrentY();
                    // Estimated table height (80) + Margin (15) + Buffer (20) = 115
                    if (currentY < 115) 
                    {
                        doc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                    }
                }
            }

            var commentTable = GetCommentTable();
            // Position at bottom of current page
            float pageWidth = pdfDoc.GetDefaultPageSize().GetWidth();
            commentTable.SetFixedPosition(PDF_DOC_MARGIN, PDF_DOC_MARGIN, pageWidth - (2 * PDF_DOC_MARGIN));
            doc.Add(commentTable);
            doc.Close();
            pdfDoc.Close();

            return reportFileName;
        }
        private Table CreateNameTable(string Name,string Enrollment)
        {
            var siteDataTable = new Table(UnitValue.CreatePercentArray(new float[] { 15, 35, 15, 35 })).UseAllAvailableWidth().SetMarginTop(10);


            siteDataTable.AddCell(GetSiteValueCellHeader("Name"));

            siteDataTable.AddCell(GetSiteValueCell(Name));
            siteDataTable.AddCell(GetSiteValueCellHeader("Enrolled"));

            siteDataTable.AddCell(GetSiteValueCell(Enrollment));


            return siteDataTable;
        }
        private Table CreateLicenseTable(string LicensoNo,string State)
        {
            var siteDataTable = new Table(UnitValue.CreatePercentArray(new float[] { 15, 35, 15, 35 })).UseAllAvailableWidth().SetMarginTop(10);


            siteDataTable.AddCell(GetSiteValueCellHeader("Licence"));

            siteDataTable.AddCell(GetSiteValueCell(LicensoNo));
            siteDataTable.AddCell(GetSiteValueCellHeader("Licence State"));

            siteDataTable.AddCell(GetSiteValueCell(State));


            return siteDataTable;
        }
        private Table CreateDateTable(DateTime dateTime,string Supplier)
        {
            var siteDataTable = new Table(UnitValue.CreatePercentArray(new float[] { 15, 35, 15, 35 })).UseAllAvailableWidth().SetMarginTop(10);

            string formattedDate = dateTime.ToString("dd/MM/yyyy");
            siteDataTable.AddCell(GetSiteValueCellHeader("Week Ending"));

            siteDataTable.AddCell(GetSiteValueCell(formattedDate));

            siteDataTable.AddCell(GetSiteValueCellHeader("CRM (Supplier)"));

            siteDataTable.AddCell(GetSiteValueCell(Supplier));
            return siteDataTable;
        }
        private static Table CreateSiteTable(string sitename)
        {
            var siteTable = new Table(UnitValue.CreatePercentArray(new float[] { 5, 11 })).UseAllAvailableWidth().SetMarginTop(10);


            siteTable.AddCell(GetSiteValueCell("SITE"));

            siteTable.AddCell(GetSiteValueCell(sitename));


            return siteTable;
        }
        private static Color GetColorFromHex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return ColorConstants.WHITE;
            if (hex.StartsWith("#")) hex = hex.Substring(1);
            if (hex.Length != 6) return ColorConstants.WHITE;

            int r = int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            int g = int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            int b = int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

            return new DeviceRgb(r, g, b);
        }

        private static string TruncateSiteName(string siteName)
        {
            if (string.IsNullOrEmpty(siteName)) return "";
            if (siteName.Length > 45) return siteName.Substring(0, 42) + "...";
            return siteName;
        }

        private static Cell GetUnifiedValueCell(string text, bool isBold = false, string fontColorHex = null, string bgHex = null)
        {
            var cell = new Cell()
               .Add(new Paragraph().Add(new Text(text ?? "")))
               .SetFont(PdfHelper.GetPdfFont())
               .SetFontSize(CELL_FONT_SIZE)
               .SetTextAlignment(TextAlignment.LEFT)
               .SetHorizontalAlignment(HorizontalAlignment.CENTER)
               .SetVerticalAlignment(VerticalAlignment.MIDDLE)
               .SetHeight(ROW_HEIGHT)
               .SetPadding(1f);

            if (isBold) cell.SetBold();
            if (!string.IsNullOrEmpty(fontColorHex))
            {
                cell.SetFontColor(GetColorFromHex(fontColorHex));
            }
            if (!string.IsNullOrEmpty(bgHex))
            {
                cell.SetBackgroundColor(GetColorFromHex(bgHex));
            }
            return cell;
        }

        private static Cell GetUnifiedHeaderCell(string text)
        {
            Color CELL_BG_GREY_HEADER = new DeviceRgb(211, 211, 211);
            return new Cell()
               .Add(new Paragraph().Add(new Text(text ?? "")))
               .SetFont(PdfHelper.GetPdfFont())
               .SetFontSize(CELL_FONT_SIZE)
               .SetTextAlignment(TextAlignment.LEFT)
               .SetHorizontalAlignment(HorizontalAlignment.CENTER)
               .SetVerticalAlignment(VerticalAlignment.MIDDLE)
               .SetBackgroundColor(CELL_BG_GREY_HEADER)
               .SetHeight(ROW_HEIGHT)
               .SetPadding(1f);
        }

        private void CreateUnifiedHeader(Table table, bool isActual)
        {
            table.AddCell(GetUnifiedHeaderCell(""));
            table.AddCell(GetUnifiedHeaderCell("Date"));
            table.AddCell(GetUnifiedHeaderCell("Start"));

            if (isActual)
            {
                // Actual: 6 columns total
                table.AddCell(GetUnifiedHeaderCell("Map"));
                table.AddCell(GetUnifiedHeaderCell("Finish"));
                table.AddCell(GetUnifiedHeaderCell("Site Name"));
            }
            else
            {
                // Booking: 8 columns total
                table.AddCell(GetUnifiedHeaderCell("Finish"));
                table.AddCell(GetUnifiedHeaderCell("Hrs"));
                table.AddCell(GetUnifiedHeaderCell("Site Name"));
                table.AddCell(GetUnifiedHeaderCell("Rate"));
                table.AddCell(GetUnifiedHeaderCell("Pay"));
            }
        }

        private Table CreateReportHeader()
        {
            var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 20, 50, 30 })).UseAllAvailableWidth();
            
            var cwLogo = new Image(ImageDataFactory.Create(IO.Path.Combine(_imageRootDir, "CWSLogoPdf.png")))
                .SetHeight(50); // Slightly smaller to save space
            headerTable.AddCell(new Cell().Add(cwLogo).SetBorder(Border.NO_BORDER));
            
            var reportTitle = new Cell()
                .Add(new Paragraph().Add(new Text("TIME SHEET")))
                .SetFont(PdfHelper.GetPdfFont())
                .SetFontSize(CELL_FONT_SIZE * 3.5f) // Reduced slightly to save space
                .SetTextAlignment(TextAlignment.CENTER)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                .SetBorder(Border.NO_BORDER);
            headerTable.AddCell(reportTitle);
            
            var cellSiteImage = new Cell().SetBorder(Border.NO_BORDER);
            var logoName = GetSiteImage();
            var logoPath = IO.Path.Combine(_imageRootDir, logoName);
            if (IO.File.Exists(logoPath))
            {
                var siteImage = new Image(ImageDataFactory.Create(logoPath))
                    .SetHeight(25)
                    .SetHorizontalAlignment(HorizontalAlignment.RIGHT);
                cellSiteImage.Add(siteImage);
            }
            headerTable.AddCell(cellSiteImage).SetBorder(Border.NO_BORDER);

            return headerTable;
        }


        private string GetSiteImage()
        {
            return "CWSLogoPdf.png";
        }
        private static Cell GetSiteValueCell(string text)
        {
            return new Cell()
               .Add(new Paragraph().Add(new Text(text ?? "")))
               .SetFont(PdfHelper.GetPdfFont())
               .SetFontSize(CELL_FONT_SIZE)
               .SetTextAlignment(TextAlignment.LEFT)
               .SetHorizontalAlignment(HorizontalAlignment.CENTER)
               .SetVerticalAlignment(VerticalAlignment.MIDDLE);

        }
        private static Cell GetSiteValueCellHeader(string text)
        {
            Color CELL_BG_GREY_HEADER = new DeviceRgb(211, 211, 211);
            return new Cell()
               .Add(new Paragraph().Add(new Text(text ?? "")))
               .SetFont(PdfHelper.GetPdfFont())
               .SetFontSize(CELL_FONT_SIZE)
               .SetTextAlignment(TextAlignment.LEFT)
               .SetHorizontalAlignment(HorizontalAlignment.CENTER)
               .SetVerticalAlignment(VerticalAlignment.MIDDLE)
               .SetBackgroundColor(CELL_BG_GREY_HEADER);

        }
        private static Cell GetNoBorderValueCell(string text)
        {
            return new Cell()
               .Add(new Paragraph(text ?? ""))
               .SetFont(PdfHelper.GetPdfFont())
               .SetFontSize(CELL_FONT_SIZE)
               .SetBorderTop(Border.NO_BORDER)
               .SetBorderBottom(Border.NO_BORDER)
               .SetBorderLeft(Border.NO_BORDER);
        }
        private static Cell GetNoBorderTotalHrsCell(string text)
        {
            return new Cell()
               .Add(new Paragraph(text ?? ""))
               .SetFont(PdfHelper.GetPdfFont())
               .SetFontSize(CELL_FONT_SIZE)
               .SetBorderTop(Border.NO_BORDER)
               .SetBorderBottom(Border.NO_BORDER)
               .SetBorderLeft(Border.NO_BORDER)
               .SetBorderRight(Border.NO_BORDER);
        }
        private static Cell GetNoBorderCommentCell(string text)
        {
            return new Cell()


                 .SetBorderLeft(Border.NO_BORDER)
                 .SetBorderRight(Border.NO_BORDER);

        }
        private static Cell GetNoBorderComment1Cell(string text)
        {
            return new Cell()


                 .SetBorderLeft(Border.NO_BORDER);
                

        }
        private (List<Table> weeklyTables, int totalHours) CreateGuardLoginDetails(
    DateTime startDate,
    DateTime endDate,
    List<GuardLogin> LoginDetails,
    string weekname,
    Dictionary<int, List<GuardLog>> logsLookup = null)
        {
            Table CreateNewGuardTable()
            {
                var GuardTable = new Table(UnitValue.CreatePercentArray(ACTUAL_COLUMNS)).UseAllAvailableWidth();
                CreateUnifiedHeader(GuardTable, true);
                return GuardTable;
            }

            DateTime currentDate = startDate;
            int totalDays = (endDate - startDate).Days + 1;
            List<Table> weeklyTables = new List<Table>();
            int TotalWeeklyHrs = 0;
            int daysProcessed = 0;

            while (daysProcessed < totalDays)
            {
                var GuardTable = CreateNewGuardTable();
                int weeklyTotalHours = 0;

                for (int j = 0; j < 7 && daysProcessed < totalDays; j++)
                {
                    string dayName = currentDate.ToString("ddd");
                    string dateStr = currentDate.ToString("dd/MM/yyyy");

                    var dayLogins = LoginDetails.Where(x => x.LoginDate.Date == currentDate.Date).OrderBy(x => x.OnDuty).ToList();

                    if (dayLogins.Count > 0)
                    {
                        var earliest = dayLogins.First();
                        var latest = dayLogins.OrderByDescending(x => x.OffDuty.GetValueOrDefault(x.OnDuty)).First();
                        var allIds = dayLogins.Select(x => x.Id).ToList();

                        GuardTable.AddCell(GetUnifiedValueCell(dayName, false, null, CELL_BG_BLUE_HEADER));
                        GuardTable.AddCell(GetUnifiedValueCell(dateStr));

                        GuardTable.AddCell(GetUnifiedValueCell(earliest.OnDuty.ToString("HH:mm")));
                        GuardTable.AddCell(GetGpsIconCell(allIds, logsLookup)); // Updated helper below to handle list

                        if (latest.OffDuty.HasValue)
                        {
                            GuardTable.AddCell(GetUnifiedValueCell(latest.OffDuty.Value.ToString("HH:mm")));
                        }
                        else
                        {
                            GuardTable.AddCell(GetUnifiedValueCell(""));
                        }

                        int dailyTotalMins = dayLogins.Sum(x => x.OffDuty.HasValue ? (int)(x.OffDuty.Value - x.OnDuty).TotalMinutes : 0);
                        weeklyTotalHours += dailyTotalMins;

                        GuardTable.AddCell(GetUnifiedValueCell(TruncateSiteName(earliest.ClientSite?.Name)));
                    }
                    else
                    {
                        GuardTable.AddCell(GetUnifiedValueCell(dayName, false, null, CELL_BG_BLUE_HEADER));
                        GuardTable.AddCell(GetUnifiedValueCell(dateStr));
                        for (int i = 0; i < 4; i++) GuardTable.AddCell(GetUnifiedValueCell(""));
                    }

                    currentDate = currentDate.AddDays(1);
                    daysProcessed++;
                }

                // Add totals row - must have 6 columns
                for (int i = 0; i < 4; i++) GuardTable.AddCell(GetNoBorderTotalHrsCell(""));
                
                int hours1 = weeklyTotalHours / 60;
                int minutes1 = weeklyTotalHours % 60;
                GuardTable.AddCell(GetUnifiedValueCell($"{hours1:D2}:{minutes1:D2}"));
                GuardTable.AddCell(GetNoBorderTotalHrsCell(""));

                TotalWeeklyHrs += weeklyTotalHours;
                weeklyTables.Add(GuardTable);
            }

            return (weeklyTables, TotalWeeklyHrs);
        }

        private Cell GetGpsIconCell(List<int> loginIds, Dictionary<int, List<GuardLog>> logsLookup = null)
        {
            GuardLog logEntry = null;

            if (logsLookup != null)
            {
                foreach (var id in loginIds)
                {
                    if (logsLookup.ContainsKey(id))
                    {
                        var list = logsLookup[id];
                        var found = list?.FirstOrDefault(x => !string.IsNullOrEmpty(x.GpsCoordinates));
                        if (found != null)
                        {
                            logEntry = found;
                            break;
                        }
                    }
                }
            }
            if (logEntry == null && loginIds != null && loginIds.Any())
            {
                // Fallback attempt
                foreach (var id in loginIds)
                {
                    logEntry = _clientDataProvider.GetGuardLogs(id);
                    if (logEntry != null && !string.IsNullOrEmpty(logEntry.GpsCoordinates)) break;
                }
            }

            var cell = new Cell()
                .SetFont(PdfHelper.GetPdfFont())
                .SetFontSize(CELL_FONT_SIZE)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetHeight(ROW_HEIGHT)
                .SetPadding(1f);

            if (logEntry != null)
            {
                var gpsImagePath = IO.Path.Combine(_imageRootDir, "GPSImage.png");
                if (IO.File.Exists(gpsImagePath))
                {
                    var siteImage = new Image(ImageDataFactory.Create(gpsImagePath)).SetWidth(10).SetHeight(10);
                    var url = $"https://www.google.com/maps?q={logEntry.GpsCoordinates}";
                    siteImage.SetAction(PdfAction.CreateURI(url));
                    cell.Add(new Paragraph().Add(siteImage).SetPadding(0));
                }
            }
            return cell;
        }


        private (List<Table> weeklyTables, int totalHours) CreateGuardLoginDetails1(
  DateTime startDate,
  DateTime endDate,
  List<GuardLogin> LoginDetails,
  string weekname)
        {
            return CreateGuardLoginDetails(startDate, endDate, LoginDetails, weekname);
        }

        private void CreateGuardDetailsHeader(Table table)
        {
        }

        private int WeeksBetweenDates(DateTime startDate, DateTime endDate)
        {
            TimeSpan dateDifference = endDate - startDate;
            int weeksBetween = (int)(dateDifference.TotalDays / 7);
            return weeksBetween;
        }

        private void CreateBookingDetailsHeader(Table table)
        {
        }

        private (List<Table> weeklyTables, double totalHours, decimal totalPay) CreateBookingDetails(
            DateTime startDate,
            DateTime endDate,
            List<GuardLogin> LoginDetails,
            List<RosterSchedule> rosterDetails,
            string weekname)
        {
            Table CreateNewBookingTable()
            {
                var BookingTable = new Table(UnitValue.CreatePercentArray(BOOKING_COLUMNS)).UseAllAvailableWidth();
                CreateUnifiedHeader(BookingTable, false);
                return BookingTable;
            }

            double totalHoursAll = 0;
            decimal totalPayAll = 0;

            DateTime currentDate = startDate;
            int totalDays = (endDate - startDate).Days + 1;
            List<Table> weeklyTables = new List<Table>();
            int daysProcessed = 0;

            while (daysProcessed < totalDays)
            {
                var BookingTable = CreateNewBookingTable();
                double weeklyTotalHours = 0;
                decimal weeklyTotalPay = 0;

                for (int j = 0; j < 7 && daysProcessed < totalDays; j++)
                {
                    string dayName = currentDate.ToString("ddd");
                    string dateStr = currentDate.ToString("dd/MM/yyyy");

                    var dayRosters = rosterDetails.Where(r => r.ShiftStart.Date == currentDate.Date).OrderBy(r => r.ShiftStart).ToList();

                    if (dayRosters.Count > 0)
                    {
                        foreach (var roster in dayRosters)
                        {
                            BookingTable.AddCell(GetUnifiedValueCell(dayName, false, null, CELL_BG_BLUE_HEADER));
                            BookingTable.AddCell(GetUnifiedValueCell(dateStr));

                            BookingTable.AddCell(GetUnifiedValueCell(roster.ShiftStart.ToString("HH:mm")));
                            BookingTable.AddCell(GetUnifiedValueCell(roster.ShiftEnd.ToString("HH:mm")));

                            TimeSpan duration = (roster.ShiftEnd - roster.ShiftStart).Duration();
                            double hrs = duration.TotalHours;
                            weeklyTotalHours += hrs;

                            BookingTable.AddCell(GetUnifiedValueCell(hrs.ToString("F2") + "h"));
                            BookingTable.AddCell(GetUnifiedValueCell(TruncateSiteName(roster.ClientSite?.Name)));

                            decimal rate = roster.PayRate?.GuardPayRate ?? 0;
                            decimal pay = (decimal)hrs * rate;
                            weeklyTotalPay += pay;

                            BookingTable.AddCell(GetUnifiedValueCell(rate.ToString("F2")));
                            BookingTable.AddCell(GetUnifiedValueCell(pay.ToString("F2")));
                        }
                    }
                    else
                    {
                        BookingTable.AddCell(GetUnifiedValueCell(dayName, false, null, CELL_BG_BLUE_HEADER));
                        BookingTable.AddCell(GetUnifiedValueCell(dateStr));
                        for (int i = 0; i < 6; i++) BookingTable.AddCell(GetUnifiedValueCell(""));
                    }

                    currentDate = currentDate.AddDays(1);
                    daysProcessed++;
                }

                // Add totals row - 8 columns
                for (int i = 0; i < 4; i++) BookingTable.AddCell(GetNoBorderTotalHrsCell(""));

                BookingTable.AddCell(GetUnifiedValueCell(weeklyTotalHours.ToString("F2") + "h"));
                BookingTable.AddCell(GetNoBorderTotalHrsCell("")); // Site Name spacer
                BookingTable.AddCell(GetNoBorderTotalHrsCell("")); // Rate spacer
                BookingTable.AddCell(GetUnifiedValueCell(weeklyTotalPay.ToString("F2")));

                totalHoursAll += weeklyTotalHours;
                totalPayAll += weeklyTotalPay;
                weeklyTables.Add(BookingTable);
            }

            return (weeklyTables, totalHoursAll, totalPayAll);
        }

        private static Table GetCommentTable()
        {
            float[] columnPercentages = { 20, 80 };
            var CommentTable = new Table(UnitValue.CreatePercentArray(columnPercentages)).UseAllAvailableWidth().SetMarginTop(10);
            
            // Layout: "Further Comments:" label box | Empty box for writing
            var labelCell = new Cell()
                .Add(new Paragraph("Further Comments :").SetFont(PdfHelper.GetPdfFont()).SetFontSize(CELL_FONT_SIZE))
                .SetBorder(new SolidBorder(ColorConstants.BLACK, 1))
                .SetPadding(5)
                .SetVerticalAlignment(VerticalAlignment.TOP)
                .SetHeight(50); // Match desired height

            var inputCell = new Cell()
                .Add(new Paragraph(""))
                .SetBorder(new SolidBorder(ColorConstants.BLACK, 1))
                .SetHeight(50);

            CommentTable.AddCell(labelCell);
            CommentTable.AddCell(inputCell);

            return CommentTable;
        }

        private static void CreateGuardDetailsHeader1(Table table, double totalHours, decimal totalPay)
        {
             // Deprecated / Unused in new layout
        }


        private class HelperDocumentRenderer : DocumentRenderer
        {
            public HelperDocumentRenderer(Document document) : base(document) { }

            public float GetCurrentY()
            {
                return currentArea?.GetBBox().GetTop() ?? 0;
            }
        }
    }
}
