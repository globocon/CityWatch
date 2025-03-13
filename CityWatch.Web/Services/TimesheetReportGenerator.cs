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

namespace CityWatch.Web.Services
{
    public interface ITimesheetReportGenerator
    {

        public string GeneratePdfTimesheetReport(string startdate, string endDate, int guradid);
        public string GeneratePdfTimesheetReportCustom(string startdate, string endDate, int guradid);
        Task<string> GenerateTimesheetZipFile(int[] clientSiteIds, string startdate, string endDate);
        Task<string> GenerateTimesheetZipFileFrequency(int[] clientSiteIds, string startdate, string endDate);
        public string GeneratePdfTimesheetReportBulk(string startdate, string endDate, int guradid, string fileNamePart);

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

                   
                    CreateLogBookReportsFusion(guardId, zipFolderPath, startdate, endDate, fileNamePart);
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

                    
                    CreateLogBookReportsFusion(guardId, zipFolderPath, startdate, endDate, fileNamePart);
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
        private void CreateLogBookReportsFusion(int GuardId, string zipFolderPath,string startdate, string endDate,string fileNamePart)
        {
           
                var fileName = GetFusionLogFileName(GuardId, startdate, endDate, fileNamePart);
                if (!string.IsNullOrEmpty(fileName))
                {
                    var reportFilePath = IO.Path.Combine(_webHostEnvironment.WebRootPath, "Pdf", "Output", fileName);
                    File.Copy(reportFilePath, IO.Path.Combine(zipFolderPath, fileName));
                    File.Delete(reportFilePath);
                }
            
        }
        private string GetFusionLogFileName(int GuardId, string startdate,string endDate, string fileNamePart)
        {

            try
            {
                return GeneratePdfTimesheetReportBulk(startdate, endDate, GuardId, fileNamePart);
            }
            catch (Exception ex)
            {
                // Log the exception or handle it according to your needs
                Console.WriteLine($"An error occurred: {ex.Message}");
                return null; // Or return a custom error message or default value
            }


        }
        public string GeneratePdfTimesheetReportBulk(string startdate, string endDate, int guradid,string fileNamePart)
        {
            DateTime startdateTime = DateTime.Parse(startdate);
            DateTime dateTime = DateTime.Parse(endDate);
            var LoginDetails = _clientDataProvider.GetLoginDetailsGuard(guradid, startdateTime, dateTime);
            var Name = _clientDataProvider.GetGuardlogName(guradid, dateTime);
            var LicenseNo = _clientDataProvider.GetGuardLicenseNo(guradid, dateTime);
            var SiteName = _clientDataProvider.GetGuardlogSite(guradid, dateTime);
            var reportFileName = $"{DateTime.Now.ToString("yyyyMMdd")} - {FileNameHelper.GetSanitizedFileNamePart(Name)} - Time Sheet- {fileNamePart} -_{new Random().Next()}.pdf";
            var reportPdf = IO.Path.Combine(_reportRootDir, REPORT_DIR, reportFileName);
            var TimesheetDetails = _clientDataProvider.GetTimesheetDetails();
            var Enrollment = _clientDataProvider.GetGuardEnrollment(guradid);
            var State = _clientDataProvider.GetGuardLicenseState(guradid);
            var Supplier = _clientDataProvider.GetGuardCRMSupplier(guradid);

            var pdfDoc = new PdfDocument(new PdfWriter(reportPdf));
            pdfDoc.SetDefaultPageSize(PageSize.A4.Rotate());
            var doc = new Document(pdfDoc);
            doc.SetMargins(PDF_DOC_MARGIN, PDF_DOC_MARGIN, PDF_DOC_MARGIN, PDF_DOC_MARGIN);


            var headerTable = CreateReportHeader();
            doc.Add(headerTable);

            doc.Add(CreateNameTable(Name, Enrollment));
            doc.Add(CreateLicenseTable(LicenseNo, State));
            doc.Add(CreateDateTable(dateTime, Supplier));
            // doc.Add(CreateSiteTable(SiteName));
            doc.Add(new Paragraph("\n"));
            var (GuardLoginTables, totalHours) = CreateGuardLoginDetails(startdateTime, dateTime, LoginDetails, TimesheetDetails.weekName);
            bool hasContentOnCurrentPage = false;
            for (int i = 0; i < GuardLoginTables.Count; i++)
            {
                var GuardLoginTable = GuardLoginTables[i];
                if (GuardLoginTable.GetNumberOfRows() > 0)
                {
                    doc.Add(GuardLoginTable);
                    hasContentOnCurrentPage = true;
                    if (i < GuardLoginTables.Count - 1) // Only add space if it's not the last table
                    {
                        doc.Add(new Paragraph("\n"));
                        doc.Add(new Paragraph("\n")); // Add a space between tables
                    }
                }


            }
            if (hasContentOnCurrentPage)
            {
                doc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
            }
            var commentTable = GetCommentTable(totalHours);
            doc.Add(commentTable);
            doc.Close();
            pdfDoc.Close();

            return reportFileName;
        }
        public string GeneratePdfTimesheetReport(string startdate, string endDate, int guradid)
        {
            DateTime startdateTime = DateTime.Parse(startdate);
            DateTime dateTime = DateTime.Parse(endDate);
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

            var pdfDoc = new PdfDocument(new PdfWriter(reportPdf));
            pdfDoc.SetDefaultPageSize(PageSize.A4.Rotate());
            var doc = new Document(pdfDoc);
            doc.SetMargins(PDF_DOC_MARGIN, PDF_DOC_MARGIN, PDF_DOC_MARGIN, PDF_DOC_MARGIN);
           

            var headerTable = CreateReportHeader();
            doc.Add(headerTable);

            doc.Add(CreateNameTable(Name, Enrollment));
            doc.Add(CreateLicenseTable(LicenseNo, State));
            doc.Add(CreateDateTable(dateTime, Supplier));
            // doc.Add(CreateSiteTable(SiteName));
            doc.Add(new Paragraph("\n"));
            var (GuardLoginTables, totalHours) = CreateGuardLoginDetails(startdateTime, dateTime, LoginDetails, TimesheetDetails.weekName);
            bool hasContentOnCurrentPage = false;
            for (int i = 0; i < GuardLoginTables.Count; i++)
            {
                var GuardLoginTable = GuardLoginTables[i];
                if (GuardLoginTable.GetNumberOfRows() > 0)
                {
                    doc.Add(GuardLoginTable);
                    hasContentOnCurrentPage = true;
                    if (i < GuardLoginTables.Count - 1) // Only add space if it's not the last table
                    {
                        doc.Add(new Paragraph("\n"));
                        doc.Add(new Paragraph("\n")); // Add a space between tables
                    }
                }
               
               
            }
            if (hasContentOnCurrentPage)
            {
                doc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
            }
            var commentTable = GetCommentTable(totalHours);
            doc.Add(commentTable);
            doc.Close();
            pdfDoc.Close();

            return reportFileName;
        }

        public string GeneratePdfTimesheetReportCustom(string startdate, string endDate, int guradid)
        {
            DateTime startdateTime = DateTime.Parse(startdate);
            DateTime dateTime = DateTime.Parse(endDate);
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

            var pdfDoc = new PdfDocument(new PdfWriter(reportPdf));
            pdfDoc.SetDefaultPageSize(PageSize.A4.Rotate());
            var doc = new Document(pdfDoc);
            doc.SetMargins(PDF_DOC_MARGIN, PDF_DOC_MARGIN, PDF_DOC_MARGIN, PDF_DOC_MARGIN);


            var headerTable = CreateReportHeader();
            doc.Add(headerTable);

            doc.Add(CreateNameTable(Name, Enrollment));
            doc.Add(CreateLicenseTable(LicenseNo, State));
            doc.Add(CreateDateTable(dateTime, Supplier));
            // doc.Add(CreateSiteTable(SiteName));
            doc.Add(new Paragraph("\n"));
            var (GuardLoginTables, totalHours) = CreateGuardLoginDetails1(startdateTime, dateTime, LoginDetails, TimesheetDetails.weekName);
            bool hasContentOnCurrentPage = false;
            for (int i = 0; i < GuardLoginTables.Count; i++)
            {
                var GuardLoginTable = GuardLoginTables[i];
                if (GuardLoginTable.GetNumberOfRows() > 0)
                {
                    doc.Add(GuardLoginTable);
                    hasContentOnCurrentPage = true;
                    if (i < GuardLoginTables.Count - 1) // Only add space if it's not the last table
                    {
                        doc.Add(new Paragraph("\n"));
                        doc.Add(new Paragraph("\n")); // Add a space between tables
                    }
                }


            }
            if (hasContentOnCurrentPage)
            {
                doc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
            }
            var commentTable = GetCommentTable(totalHours);
            doc.Add(commentTable);
            doc.Close();
            pdfDoc.Close();

            return reportFileName;
        }
        private static Table CreateNameTable(string Name,string Enrollment)
        {
            var siteDataTable = new Table(UnitValue.CreatePercentArray(new float[] { 3, 11 , 3, 11 })).UseAllAvailableWidth().SetMarginTop(10);


            siteDataTable.AddCell(GetSiteValueCellHeader("Name"));

            siteDataTable.AddCell(GetSiteValueCell(Name));
            siteDataTable.AddCell(GetSiteValueCellHeader("Enrolled"));

            siteDataTable.AddCell(GetSiteValueCell(Enrollment));


            return siteDataTable;
        }
        private static Table CreateLicenseTable(string LicensoNo,string State)
        {
            var siteDataTable = new Table(UnitValue.CreatePercentArray(new float[] { 3, 11, 3, 11 })).UseAllAvailableWidth().SetMarginTop(10);


            siteDataTable.AddCell(GetSiteValueCellHeader("Licence"));

            siteDataTable.AddCell(GetSiteValueCell(LicensoNo));
            siteDataTable.AddCell(GetSiteValueCellHeader("Licence State"));

            siteDataTable.AddCell(GetSiteValueCell(State));


            return siteDataTable;
        }
        private static Table CreateDateTable(DateTime dateTime,string Supplier)
        {
            var siteDataTable = new Table(UnitValue.CreatePercentArray(new float[] { 3, 11, 3, 11 })).UseAllAvailableWidth().SetMarginTop(10);

            string formattedDate = dateTime.ToString("dd/MM/yyyy");
            siteDataTable.AddCell(GetSiteValueCellHeader("Week Ending"));

            siteDataTable.AddCell(GetSiteValueCell(formattedDate));

            siteDataTable.AddCell(GetSiteValueCellHeader("CRM(Supplier)"));

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
        private Table CreateReportHeader()
        {
            var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 20, 50, 30 })).UseAllAvailableWidth();

            var cwLogo = new Image(ImageDataFactory.Create(IO.Path.Combine(_imageRootDir, "CWSLogoPdf.png")))
                .SetHeight(60);
            headerTable.AddCell(new Cell().Add(cwLogo).SetBorder(Border.NO_BORDER));

            var reportTitle = new Cell()
                .Add(new Paragraph().Add(new Text("TIME SHEET")))
                .SetFont(PdfHelper.GetPdfFont())
                .SetFontSize(CELL_FONT_SIZE * 4f)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                .SetBorder(Border.NO_BORDER);
            headerTable.AddCell(reportTitle);

            var cellSiteImage = new Cell().SetBorder(Border.NO_BORDER);
            var imagePath = string.Empty;

            var cwLogopath = new Image(ImageDataFactory.Create(IO.Path.Combine(_imageRootDir, "CWSLogoPdf.png")));

            imagePath = GetSiteImage();
            var folderPath = IO.Path.Combine(_SiteimageRootDir, imagePath);
            if (IO.File.Exists(folderPath))
            {
                var siteImage = new Image(ImageDataFactory.Create(imagePath))
                    .SetHeight(30)
                    .SetHorizontalAlignment(HorizontalAlignment.RIGHT);
                cellSiteImage.Add(siteImage);
            }
            headerTable.AddCell(cellSiteImage).SetBorder(Border.NO_BORDER);



            return headerTable;
        }


        private string GetSiteImage()
        {

            return $"{new Uri(_settings.KpiWebUrl)}{"CWSLogoPdf.png"}";

        }
        private static Cell GetSiteValueCell(string text)
        {
            return new Cell()
               .Add(new Paragraph().Add(new Text(text)))
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
               .Add(new Paragraph().Add(new Text(text)))
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

               .SetBorderTop(Border.NO_BORDER)
               .SetBorderBottom(Border.NO_BORDER)
                 .SetBorderLeft(Border.NO_BORDER);

        }
        private static Cell GetNoBorderTotalHrsCell(string text)
        {
            return new Cell()

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
    string weekname)
        {
            // Method to create a new table with headers
            Table CreateNewGuardTable()
            {
                float[] columnPercentages = new float[8];
                var GuardTable = new Table(UnitValue.CreatePercentArray(columnPercentages)).UseAllAvailableWidth();
                CreateGuardDetailsHeader(GuardTable);
                return GuardTable;
            }

            var SiteName = LoginDetails.Select(x => x.ClientSite.Name).FirstOrDefault();

            // Handle single-day logic
            if (startDate.Date == endDate.Date)
            {
                var GuardTable = CreateNewGuardTable();
                int dailyTotalHours = 0;

                // Only create a table for the specific day
                string dayName = startDate.ToString("dddd");
                GuardTable.AddCell(GetSiteValueCell(dayName));
                GuardTable.AddCell(GetSiteValueCell(startDate.ToString("dd/MM/yyyy")));
                //GuardTable.AddCell(GetSiteValueCell(dayName));
                var start = LoginDetails.FirstOrDefault(x => x.LoginDate.Date == startDate.Date);
                if (start != null)
                {
                    GuardTable.AddCell(GetSiteValueCell(start.OnDuty.ToString("HH:mm")));
                    var _guardLogs = _clientDataProvider.GetGuardLogs(start.Id);
                    //Set GPS Image start
                    var imagePath = "wwwroot/images/GPSImage.png";
                    var siteImage = new Image(ImageDataFactory.Create(imagePath))
                        .SetWidth(12)
                        .SetHeight(12);

                    siteImage.SetTextAlignment(TextAlignment.RIGHT);

                    var paragraph = new Paragraph()
                        .SetBorder(Border.NO_BORDER);
                       if (_guardLogs != null && _guardLogs.GpsCoordinates != null && _guardLogs.GpsCoordinates != "")
                    {
                        paragraph.Add(siteImage);
                    }

                    if (_guardLogs != null && _guardLogs.GpsCoordinates != null && _guardLogs.GpsCoordinates != "")
                    {
                        var urlWithTargetBlank = $"https://www.google.com/maps?q={_guardLogs.GpsCoordinates}";
                        var linkAction = PdfAction.CreateURI(urlWithTargetBlank);
                        siteImage.SetAction(linkAction);
                    }
                    var cell = new Cell()
                        .SetFont(PdfHelper.GetPdfFont())
                        .SetFontSize(CELL_FONT_SIZE)
                        .SetTextAlignment(TextAlignment.LEFT)
                        .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                        .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                        .SetPadding(2)
                        .SetMargin(0)
                        .SetHeight(15);

                    cell.Add(paragraph);
                    GuardTable.AddCell(cell);
                    //Set GPS Image stop

                    TimeSpan? endDateDifference = start.OffDuty.HasValue ? start.OffDuty.Value - start.OnDuty : null;
                    if (endDateDifference.HasValue)
                    {
                        string enddate1 = string.Format("{0:D2}:{1:D2}",
                                                        (int)endDateDifference.Value.TotalHours,
                                                        endDateDifference.Value.Minutes);
                        GuardTable.AddCell(GetSiteValueCell(enddate1));

                        //Set GPS Image start
                        var imagePath1 = "wwwroot/images/GPSImage.png";
                        var siteImage1 = new Image(ImageDataFactory.Create(imagePath1))
                            .SetWidth(12)
                            .SetHeight(12);

                        siteImage1.SetTextAlignment(TextAlignment.RIGHT);

                        var paragraph1 = new Paragraph()
                            .SetBorder(Border.NO_BORDER)
                            .Add(siteImage1);

                        var cell1 = new Cell()
                            .SetFont(PdfHelper.GetPdfFont())
                            .SetFontSize(CELL_FONT_SIZE)
                            .SetTextAlignment(TextAlignment.LEFT)
                            .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                            .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                            .SetPadding(2)
                            .SetMargin(0)
                            .SetHeight(15);

                        cell1.Add(paragraph1);
                        GuardTable.AddCell(cell1);
                        //Set GPS Image stop
                        DateTime enddate = DateTime.ParseExact(enddate1, "HH:mm", CultureInfo.InvariantCulture);
                        DateTime startd = DateTime.ParseExact(start.OnDuty.ToString("HH:mm"), "HH:mm", CultureInfo.InvariantCulture);

                        TimeSpan TotalHrs = (enddate - startd).Duration();
                        int totalHrs = (int)TotalHrs.TotalMinutes;
                        dailyTotalHours += totalHrs;
                        int hoursDail = totalHrs / 60;
                        int minutesDail = totalHrs % 60;
                        GuardTable.AddCell(GetSiteValueCell($"{hoursDail}:{minutesDail}"));
                    }
                    GuardTable.AddCell(GetSiteValueCell(start.ClientSite.Name ?? ""));
                }
                else
                {
                    GuardTable.AddCell(GetSiteValueCell(""));
                    GuardTable.AddCell(GetSiteValueCell(""));
                    GuardTable.AddCell(GetSiteValueCell(""));
                    GuardTable.AddCell(GetSiteValueCell(""));
                    GuardTable.AddCell(GetSiteValueCell(""));
                    GuardTable.AddCell(GetSiteValueCell(start?.ClientSite.Name ?? ""));
                }

                return (new List<Table> { GuardTable }, dailyTotalHours);
            }

            // Weekly logic for a range of dates
            DateTime currentDate = new DateTime(startDate.Year, startDate.Month, 1); 
            int totalDays = (endDate - startDate).Days + 1;
            int startDayIndex = Array.IndexOf(CultureInfo.CurrentCulture.DateTimeFormat.DayNames, weekname);

            // Adjust the currentDate to start on the specified day of the week
            //while ((int)currentDate.DayOfWeek != startDayIndex && currentDate <= endDate)
            //{
            //    currentDate = currentDate.AddDays(1);
            //}
            DayOfWeek targetStartDay = DayOfWeek.Monday; // Set this to your desired week start day
            int daysToAdjust = ((int)startDate.DayOfWeek - (int)targetStartDay + 7) % 7;
            bool isMonthlyView = (endDate - startDate).Days >= 28;
            if (!isMonthlyView)
            {
                currentDate = startDate.AddDays(-daysToAdjust);
            }
            else
            {
                currentDate = currentDate;
            }
                

            // Ensure endDate is exactly one week after startDate for single-week mode
            //if ((endDate - startDate).Days >= 7)
            //{
            //    endDate = startDate.AddDays(6);
            //}

            List<Table> weeklyTables = new List<Table>();
            int TotalWeeklyHrs = 0;
            int daysProcessed = 0;

            while (daysProcessed < totalDays)
            {
                var GuardTable = CreateNewGuardTable();
                int weeklyTotalHours = 0;

                // Process each day in the week (up to 7 days or remaining days)
                for (int j = 0; j < 7 && daysProcessed < totalDays; j++)
                {
                    string dayName = currentDate.ToString("dddd");
                    GuardTable.AddCell(GetSiteValueCellHeader(dayName));

                    if (currentDate > endDate)
                    {
                        GuardTable.AddCell(GetSiteValueCell(""));
                    }
                    else
                    {
                        GuardTable.AddCell(GetSiteValueCell(currentDate.ToString("dd/MM/yyyy")));
                    }

                    var start = LoginDetails.FirstOrDefault(x => x.LoginDate.Date == currentDate.Date);
                    if (start != null)
                    {
                        GuardTable.AddCell(GetSiteValueCell(start.OnDuty.ToString("HH:mm")));
                        var _guardLogs = _clientDataProvider.GetGuardLogs(start.Id);
                        //Set GPS Image start
                        var imagePath = "wwwroot/images/GPSImage.png";
                        var siteImage = new Image(ImageDataFactory.Create(imagePath))
                            .SetWidth(12) 
                            .SetHeight(12); 

                        siteImage.SetTextAlignment(TextAlignment.RIGHT);

                        var paragraph = new Paragraph()
                            .SetBorder(Border.NO_BORDER);
                            if (_guardLogs!=null && _guardLogs.GpsCoordinates != null && _guardLogs.GpsCoordinates != "")
                        {
                            paragraph.Add(siteImage);
                        }

                       
                        if (_guardLogs != null && _guardLogs.GpsCoordinates != null && _guardLogs.GpsCoordinates != "")
                        {
                            var urlWithTargetBlank = $"https://www.google.com/maps?q={_guardLogs.GpsCoordinates}";
                            var linkAction = PdfAction.CreateURI(urlWithTargetBlank);
                            siteImage.SetAction(linkAction);
                        }
                        var cell = new Cell()
                            .SetFont(PdfHelper.GetPdfFont())
                            .SetFontSize(CELL_FONT_SIZE)
                            .SetTextAlignment(TextAlignment.LEFT)
                            .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                            .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                            .SetPadding(2) 
                            .SetMargin(0) 
                            .SetHeight(15); 

                        cell.Add(paragraph);
                        GuardTable.AddCell(cell);
                        //Set GPS Image stop
                        TimeSpan? endDateDifference = start.OffDuty?.TimeOfDay;
                        if (endDateDifference.HasValue)
                        {
                            string enddate1 = string.Format("{0:D2}:{1:D2}", (int)endDateDifference.Value.TotalHours, endDateDifference.Value.Minutes);
                            GuardTable.AddCell(GetSiteValueCell(enddate1));
                            //Set GPS Image start
                            var imagePath1 = "wwwroot/images/GPSImage.png";
                            var siteImage1 = new Image(ImageDataFactory.Create(imagePath1))
                                .SetWidth(12)
                                .SetHeight(12);

                            siteImage1.SetTextAlignment(TextAlignment.RIGHT);

                            var paragraph1 = new Paragraph()
                                .SetBorder(Border.NO_BORDER);
                                if (_guardLogs != null && _guardLogs.GpsCoordinates != null && _guardLogs.GpsCoordinates != "")
                            {
                                paragraph1.Add(siteImage1);
                            }
                            if (_guardLogs != null && _guardLogs.GpsCoordinates != null && _guardLogs.GpsCoordinates != "")
                            {
                                var urlWithTargetBlank = $"https://www.google.com/maps?q={_guardLogs.GpsCoordinates}";
                                var linkAction = PdfAction.CreateURI(urlWithTargetBlank);
                                siteImage1.SetAction(linkAction);
                            }
                            var cell1 = new Cell()
                                .SetFont(PdfHelper.GetPdfFont())
                                .SetFontSize(CELL_FONT_SIZE)
                                .SetTextAlignment(TextAlignment.LEFT)
                                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                                .SetPadding(2)
                                .SetMargin(0)
                                .SetHeight(15);

                            cell1.Add(paragraph1);
                            GuardTable.AddCell(cell1);
                            //Set GPS Image stop
                            TimeSpan enddate = TimeSpan.Parse(enddate1);
                            TimeSpan startd = TimeSpan.ParseExact(start.OnDuty.ToString("HH:mm"), "hh\\:mm", CultureInfo.InvariantCulture);

                            TimeSpan TotalHrs = (enddate - startd).Duration();
                            int totalHrs = (int)TotalHrs.TotalMinutes;
                            weeklyTotalHours += totalHrs;

                            string formattedTotalHrs = string.Format("{0:D2}:{1:D2}", TotalHrs.Hours, TotalHrs.Minutes);
                            GuardTable.AddCell(GetSiteValueCell(formattedTotalHrs));
                        }
                        else
                        {
                            GuardTable.AddCell(GetSiteValueCell(""));
                            GuardTable.AddCell(GetSiteValueCell(""));
                        }

                        GuardTable.AddCell(GetSiteValueCell(start.ClientSite?.Name ?? ""));
                    }
                    else
                    {
                        GuardTable.AddCell(GetSiteValueCell(""));
                        GuardTable.AddCell(GetSiteValueCell(""));
                        GuardTable.AddCell(GetSiteValueCell(""));
                        GuardTable.AddCell(GetSiteValueCell(""));
                        GuardTable.AddCell(GetSiteValueCell(""));
                        GuardTable.AddCell(GetSiteValueCell(""));
                    }

                    currentDate = currentDate.AddDays(1);
                    daysProcessed++;
                }

                GuardTable.AddCell(GetNoBorderTotalHrsCell(""));
                GuardTable.AddCell(GetNoBorderTotalHrsCell(""));
                GuardTable.AddCell(GetNoBorderTotalHrsCell(""));
                GuardTable.AddCell(GetNoBorderTotalHrsCell(""));
                GuardTable.AddCell(GetNoBorderTotalHrsCell(""));
                GuardTable.AddCell(GetNoBorderTotalHrsCell(""));

                int hours1 = weeklyTotalHours / 60;
                int minutes1 = weeklyTotalHours % 60;
                GuardTable.AddCell(GetSiteValueCell($"{hours1:D2}:{minutes1:D2}"));

                GuardTable.AddCell(GetNoBorderTotalHrsCell(SiteName));
                TotalWeeklyHrs += weeklyTotalHours;
                weeklyTables.Add(GuardTable);
            }

            return (weeklyTables, TotalWeeklyHrs);
        }


        private (List<Table> weeklyTables, int totalHours) CreateGuardLoginDetails1(
  DateTime startDate,
  DateTime endDate,
  List<GuardLogin> LoginDetails,
  string weekname)
        {
            // Method to create a new table with headers
            Table CreateNewGuardTable()
            {
                float[] columnPercentages = new float[8];
                var GuardTable = new Table(UnitValue.CreatePercentArray(columnPercentages)).UseAllAvailableWidth();
                CreateGuardDetailsHeader(GuardTable);
                return GuardTable;
            }

            var SiteName = LoginDetails.Select(x => x.ClientSite.Name).FirstOrDefault();

            // Handle single-day logic
            if (startDate.Date == endDate.Date)
            {
                var GuardTable = CreateNewGuardTable();
                int dailyTotalHours = 0;

                // Only create a table for the specific day
                string dayName = startDate.ToString("dddd");
                GuardTable.AddCell(GetSiteValueCell(dayName));
                GuardTable.AddCell(GetSiteValueCell(startDate.ToString("dd/MM/yyyy")));

                var start = LoginDetails.FirstOrDefault(x => x.LoginDate.Date == startDate.Date);
                if (start != null)
                {
                    GuardTable.AddCell(GetSiteValueCell(start.OnDuty.ToString("HH:mm")));
                    var _guardLogs = _clientDataProvider.GetGuardLogs(start.Id);
                    //Set GPS Image start
                    var imagePath = "wwwroot/images/GPSImage.png";
                    var siteImage = new Image(ImageDataFactory.Create(imagePath))
                        .SetWidth(12)
                        .SetHeight(12);

                    siteImage.SetTextAlignment(TextAlignment.RIGHT);

                    var paragraph = new Paragraph()
                        .SetBorder(Border.NO_BORDER);
                        if (_guardLogs != null && _guardLogs.GpsCoordinates != null && _guardLogs.GpsCoordinates != "")
                    {
                        paragraph.Add(siteImage);
                    }
                    if (_guardLogs != null && _guardLogs.GpsCoordinates != null && _guardLogs.GpsCoordinates != "")
                    {
                        var urlWithTargetBlank = $"https://www.google.com/maps?q={_guardLogs.GpsCoordinates}";
                        var linkAction = PdfAction.CreateURI(urlWithTargetBlank);
                        siteImage.SetAction(linkAction);
                    }

                    var cell = new Cell()
                        .SetFont(PdfHelper.GetPdfFont())
                        .SetFontSize(CELL_FONT_SIZE)
                        .SetTextAlignment(TextAlignment.LEFT)
                        .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                        .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                        .SetPadding(2)
                        .SetMargin(0)
                        .SetHeight(15);

                    cell.Add(paragraph);
                    GuardTable.AddCell(cell);
                    //Set GPS Image stop

                    TimeSpan? endDateDifference = start.OffDuty.HasValue ? start.OffDuty.Value - start.OnDuty : null;
                    if (endDateDifference.HasValue)
                    {
                        string enddate1 = string.Format("{0:D2}:{1:D2}",
                                                        (int)endDateDifference.Value.TotalHours,
                                                        endDateDifference.Value.Minutes);
                        GuardTable.AddCell(GetSiteValueCell(enddate1));
                        //Set GPS Image start
                        var imagePath1 = "wwwroot/images/GPSImage.png";
                        var siteImage1 = new Image(ImageDataFactory.Create(imagePath1))
                            .SetWidth(12)
                            .SetHeight(12);

                        siteImage1.SetTextAlignment(TextAlignment.RIGHT);

                        var paragraph1 = new Paragraph()
                            .SetBorder(Border.NO_BORDER);
                             if (_guardLogs != null && _guardLogs.GpsCoordinates != null && _guardLogs.GpsCoordinates != "")
                        {
                            paragraph1.Add(siteImage1);
                        }

                        if (_guardLogs != null && _guardLogs.GpsCoordinates != null && _guardLogs.GpsCoordinates != "")
                        {
                            var urlWithTargetBlank = $"https://www.google.com/maps?q={_guardLogs.GpsCoordinates}";
                            var linkAction = PdfAction.CreateURI(urlWithTargetBlank);
                            siteImage1.SetAction(linkAction);
                        }

                        var cell1 = new Cell()
                            .SetFont(PdfHelper.GetPdfFont())
                            .SetFontSize(CELL_FONT_SIZE)
                            .SetTextAlignment(TextAlignment.LEFT)
                            .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                            .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                            .SetPadding(2)
                            .SetMargin(0)
                            .SetHeight(15);

                        cell1.Add(paragraph1);
                        GuardTable.AddCell(cell1);
                        //Set GPS Image stop

                        DateTime enddate = DateTime.ParseExact(enddate1, "HH:mm", CultureInfo.InvariantCulture);
                        DateTime startd = DateTime.ParseExact(start.OnDuty.ToString("HH:mm"), "HH:mm", CultureInfo.InvariantCulture);

                        TimeSpan TotalHrs = (enddate - startd).Duration();
                        int totalHrs = (int)TotalHrs.TotalMinutes;
                        dailyTotalHours += totalHrs;
                        int hoursDail = totalHrs / 60;
                        int minutesDail = totalHrs % 60;
                        GuardTable.AddCell(GetSiteValueCell($"{hoursDail}:{minutesDail}"));
                    }
                    GuardTable.AddCell(GetSiteValueCell(start.ClientSite.Name ?? ""));
                }
                else
                {
                    GuardTable.AddCell(GetSiteValueCell(""));
                    GuardTable.AddCell(GetSiteValueCell(""));
                    GuardTable.AddCell(GetSiteValueCell(""));
                    GuardTable.AddCell(GetSiteValueCell(""));
                    GuardTable.AddCell(GetSiteValueCell(""));

                    GuardTable.AddCell(GetSiteValueCell(start?.ClientSite.Name ?? ""));
                }

                return (new List<Table> { GuardTable }, dailyTotalHours);
            }

            // Weekly logic for a range of dates
            DateTime currentDate = startDate;
            int totalDays = (endDate - startDate).Days + 1;
            int startDayIndex = Array.IndexOf(CultureInfo.CurrentCulture.DateTimeFormat.DayNames, weekname);

            // Adjust the currentDate to start on the specified day of the week
            //while ((int)currentDate.DayOfWeek != startDayIndex && currentDate <= endDate)
            //{
            //    currentDate = currentDate.AddDays(1);
            //}
           


            // Ensure endDate is exactly one week after startDate for single-week mode
            //if ((endDate - startDate).Days >= 7)
            //{
            //    endDate = startDate.AddDays(6);
            //}

            List<Table> weeklyTables = new List<Table>();
            int TotalWeeklyHrs = 0;
            int daysProcessed = 0;

            while (daysProcessed < totalDays)
            {
                var GuardTable = CreateNewGuardTable();
                int weeklyTotalHours = 0;

                // Process each day in the week (up to 7 days or remaining days)
                for (int j = 0; j < 7 && daysProcessed < totalDays; j++)
                {
                    string dayName = currentDate.ToString("dddd");
                    GuardTable.AddCell(GetSiteValueCellHeader(dayName));

                    if (currentDate > endDate)
                    {
                        GuardTable.AddCell(GetSiteValueCell(""));
                    }
                    else
                    {
                        GuardTable.AddCell(GetSiteValueCell(currentDate.ToString("dd/MM/yyyy")));
                    }

                    var start = LoginDetails.FirstOrDefault(x => x.LoginDate.Date == currentDate.Date);
                    if (start != null)
                    {
                        GuardTable.AddCell(GetSiteValueCell(start.OnDuty.ToString("HH:mm")));
                        var _guardLogs = _clientDataProvider.GetGuardLogs(start.Id);
                        //Set GPS Image start
                        var imagePath = "wwwroot/images/GPSImage.png";
                        var siteImage = new Image(ImageDataFactory.Create(imagePath))
                            .SetWidth(12)
                            .SetHeight(12);

                        siteImage.SetTextAlignment(TextAlignment.RIGHT);

                        var paragraph = new Paragraph()
                            .SetBorder(Border.NO_BORDER);
                             if (_guardLogs != null && _guardLogs.GpsCoordinates != null && _guardLogs.GpsCoordinates != "")
                        {
                            paragraph.Add(siteImage);
                        }
                        if (_guardLogs != null && _guardLogs.GpsCoordinates != null && _guardLogs.GpsCoordinates != "")
                        {
                            var urlWithTargetBlank = $"https://www.google.com/maps?q={_guardLogs.GpsCoordinates}";
                            var linkAction = PdfAction.CreateURI(urlWithTargetBlank);
                            siteImage.SetAction(linkAction);
                        }


                        var cell = new Cell()
                            .SetFont(PdfHelper.GetPdfFont())
                            .SetFontSize(CELL_FONT_SIZE)
                            .SetTextAlignment(TextAlignment.LEFT)
                            .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                            .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                            .SetPadding(2)
                            .SetMargin(0)
                            .SetHeight(15);

                        cell.Add(paragraph);
                        GuardTable.AddCell(cell);
                        //Set GPS Image stop

                        TimeSpan? endDateDifference = start.OffDuty?.TimeOfDay;
                        if (endDateDifference.HasValue)
                        {
                            string enddate1 = string.Format("{0:D2}:{1:D2}", (int)endDateDifference.Value.TotalHours, endDateDifference.Value.Minutes);
                            GuardTable.AddCell(GetSiteValueCell(enddate1));
                            //Set GPS Image start
                            var imagePath1 = "wwwroot/images/GPSImage.png";
                            var siteImage1 = new Image(ImageDataFactory.Create(imagePath1))
                                .SetWidth(12)
                                .SetHeight(12);

                            siteImage1.SetTextAlignment(TextAlignment.RIGHT);

                            var paragraph1 = new Paragraph()
                                .SetBorder(Border.NO_BORDER);
                                  if (_guardLogs != null && _guardLogs.GpsCoordinates != null && _guardLogs.GpsCoordinates != "")
                            {
                                paragraph1.Add(siteImage1);
                            }

                            if (_guardLogs != null && _guardLogs.GpsCoordinates != null && _guardLogs.GpsCoordinates != "")
                            {
                                var urlWithTargetBlank = $"https://www.google.com/maps?q={_guardLogs.GpsCoordinates}";
                                var linkAction = PdfAction.CreateURI(urlWithTargetBlank);
                                siteImage1.SetAction(linkAction);
                            }

                            var cell1 = new Cell()
                                .SetFont(PdfHelper.GetPdfFont())
                                .SetFontSize(CELL_FONT_SIZE)
                                .SetTextAlignment(TextAlignment.LEFT)
                                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                                .SetPadding(2)
                                .SetMargin(0)
                                .SetHeight(15);

                            cell1.Add(paragraph1);
                            GuardTable.AddCell(cell1);
                            //Set GPS Image stop

                            TimeSpan enddate = TimeSpan.Parse(enddate1);
                            TimeSpan startd = TimeSpan.ParseExact(start.OnDuty.ToString("HH:mm"), "hh\\:mm", CultureInfo.InvariantCulture);

                            TimeSpan TotalHrs = (enddate - startd).Duration();
                            int totalHrs = (int)TotalHrs.TotalMinutes;
                            weeklyTotalHours += totalHrs;

                            string formattedTotalHrs = string.Format("{0:D2}:{1:D2}", TotalHrs.Hours, TotalHrs.Minutes);
                            GuardTable.AddCell(GetSiteValueCell(formattedTotalHrs));
                        }
                        else
                        {
                            GuardTable.AddCell(GetSiteValueCell(""));
                            GuardTable.AddCell(GetSiteValueCell(""));
                        }

                        GuardTable.AddCell(GetSiteValueCell(start.ClientSite?.Name ?? ""));
                    }
                    else
                    {
                        GuardTable.AddCell(GetSiteValueCell(""));
                        GuardTable.AddCell(GetSiteValueCell(""));
                        GuardTable.AddCell(GetSiteValueCell(""));
                        GuardTable.AddCell(GetSiteValueCell(""));
                        GuardTable.AddCell(GetSiteValueCell(""));
                        GuardTable.AddCell(GetSiteValueCell(""));
                    }

                    currentDate = currentDate.AddDays(1);
                    daysProcessed++;
                }

                GuardTable.AddCell(GetNoBorderTotalHrsCell(""));
                GuardTable.AddCell(GetNoBorderTotalHrsCell(""));
                GuardTable.AddCell(GetNoBorderTotalHrsCell(""));
                GuardTable.AddCell(GetNoBorderTotalHrsCell(""));
                GuardTable.AddCell(GetNoBorderTotalHrsCell(""));
                GuardTable.AddCell(GetNoBorderTotalHrsCell(""));

                int hours1 = weeklyTotalHours / 60;
                int minutes1 = weeklyTotalHours % 60;
                GuardTable.AddCell(GetSiteValueCell($"{hours1:D2}:{minutes1:D2}"));

                GuardTable.AddCell(GetNoBorderTotalHrsCell(SiteName));
                TotalWeeklyHrs += weeklyTotalHours;
                weeklyTables.Add(GuardTable);
            }

            return (weeklyTables, TotalWeeklyHrs);
        }




        private int WeeksBetweenDates(DateTime startDate, DateTime endDate)
        {
            TimeSpan dateDifference = endDate - startDate;
            int weeksBetween = (int)(dateDifference.TotalDays / 7);
            return weeksBetween;
        }
        private void CreateGuardDetailsHeader(Table table)
        {
            try
            {
                float[] columnWidths = { 100f, 200f, 100f, 100f, 100f, 100f, 20f,30f }; // Adjust these values as needed
                                                                        // Total width of the table in points
                table.SetWidth(UnitValue.CreatePointValue(380)); // Example total width in points, adjust as needed
              

                Color CELL_BG_GREY_HEADER = new DeviceRgb(211, 211, 211);
                // const float CELL_WIDTH = 1f;
                table.AddCell(GetSiteValueCellHeader(""));
                table.AddCell(GetSiteValueCellHeader("Date"));
                table.AddCell(GetSiteValueCellHeader("Start"));
                table.AddCell(GetSiteValueCellHeader("GPS"));
                table.AddCell(GetSiteValueCellHeader("Finish"));
                table.AddCell(GetSiteValueCellHeader("GPS"));
                table.AddCell(GetSiteValueCellHeader("Total Hrs"));
                table.AddCell(GetSiteValueCellHeader("Site"));


            }
            catch (Exception ex)
            {
                // Handle the exception here, for example, log it or show an error message.
                Console.WriteLine($"An error occurred: {ex.Message}");
                // You can rethrow the exception if needed.
                throw;
            }
        }
        private static Table GetCommentTable(int weelTotalHrs)
        {
            float[] columnPercentages = new float[5];
            var CommentTable = new Table(UnitValue.CreatePercentArray(columnPercentages)).UseAllAvailableWidth();

            CreateGuardDetailsHeader1(CommentTable, weelTotalHrs);
            var cell1 = GetSiteValueCell("Further Comments :");
            var cell2 = GetNoBorderCommentCell("");
            var cell3 = GetNoBorderCommentCell("");
            var cell4 = GetNoBorderCommentCell("");
            var cell5 = GetNoBorderComment1Cell("");

            float desiredHeight = 50f;
            cell1.SetHeight(desiredHeight);
            cell2.SetHeight(desiredHeight);
            cell3.SetHeight(desiredHeight);
            cell4.SetHeight(desiredHeight);
            cell5.SetHeight(desiredHeight);

            CommentTable.AddCell(cell1);
            CommentTable.AddCell(cell2);
            CommentTable.AddCell(cell3);
            CommentTable.AddCell(cell4);
            CommentTable.AddCell(cell5);

            return CommentTable;
        }
        private static void CreateGuardDetailsHeader1(Table table, int weelTotalHrs)
        {
            try
            {
                float[] columnWidths = { 100f, 200f, 100f, 100f, 100f }; // Adjust these values as needed
                                                                         // Total width of the table in points


                Color CELL_BG_GREY_HEADER = new DeviceRgb(211, 211, 211);
                // const float CELL_WIDTH = 1f;
                table.AddCell(GetNoBorderTotalHrsCell(""));
                var TotalTitle = new Cell()
                  .Add(new Paragraph().Add(new Text("Total")))
                  .SetFont(PdfHelper.GetPdfFont())
                  .SetFontSize(CELL_FONT_SIZE * 2f)
                  .SetTextAlignment(TextAlignment.CENTER)
                  .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                  .SetBorder(Border.NO_BORDER);
                table.AddCell(TotalTitle);
                table.AddCell(GetNoBorderTotalHrsCell(""));
                table.AddCell(GetNoBorderValueCell(""));
                int hours1 = weelTotalHrs / 60;
                int minutes1 = weelTotalHrs % 60;
                table.AddCell(GetSiteValueCell($"{hours1}:{minutes1}"));
            }
            catch (Exception ex)
            {
                // Handle the exception here, for example, log it or show an error message.
                Console.WriteLine($"An error occurred: {ex.Message}");
                // You can rethrow the exception if needed.
                throw;
            }
        }
    }
}
