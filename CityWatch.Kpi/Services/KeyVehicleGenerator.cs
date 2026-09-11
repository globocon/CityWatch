using System;
using CityWatch.Common.Helpers;
using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
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
using CityWatch.Kpi.Helpers;
using CityWatch.Kpi.Services;
using CityWatch.Common.Models;
using CityWatch.Kpi.Models;
namespace CityWatch.Kpi.Services
{
    public enum KvlStatusFilter
    {
        All = 0,

        Open = 1,

        Closed = 2,

        Pending = 3
    }
    public interface IKeyVehicleGenerator
    {

        string GeneratePdfReport(List<ClientSiteKpiSetting> clientSiteKpiSettings, KpiSendKVSchedules schedule, DateTime reportStartDate, DateTime reportEndDate);
        string GeneratePdfKVReport(DateTime startdate, DateTime endDate, int clientsiteId);
        string GeneratePdfReportWithClientSiteIds(int ClientSiteId, KpiSendKVSchedules schedule, DateTime reportStartDate, DateTime reportEndDate);
    }
    public class KeyVehicleGenerator: IKeyVehicleGenerator
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

        private const string COLOR_LIGHT_BLUE = "#d9e2f3";
        private const string FONT_COLOR_BLACK = "#000000";
        private const float MAX_IMAGE_WIDTH = 600;
        private const float MAX_IMAGE_HEIGHT = 800;
        private const float SCALE_FACTOR = 0.92f;
        private const int ROTATION_ANGLE_DEG = 270;

        private readonly string _reportRootDir;
        private readonly string _imageRootDir;
        private readonly string _siteImageRootDir;
        private readonly string _graphImageRootDir;
        private readonly string _uploadsRootDir;

        private readonly IViewDataService _viewDataService;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly ILogger<KeyVehicleGenerator> _logger;
        private readonly Settings _settings;
        private readonly IPatrolDataReportService _patrolDataReportService;
        private readonly string _SiteimageRootDir;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly string _downloadsFolderPath;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        public KeyVehicleGenerator(IOptions<Settings> settings,
            IWebHostEnvironment webHostEnvironment,
            IViewDataService viewDataService,

            IClientDataProvider clientDataProvider,
            ILogger<KeyVehicleGenerator> logger, IPatrolDataReportService patrolDataReportService,
            IGuardLogDataProvider guardLogDataProvider)
        {
            _viewDataService = viewDataService;
            _clientDataProvider = clientDataProvider;
            _logger = logger;
            _settings = settings.Value;
            _webHostEnvironment = webHostEnvironment;
            _reportRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "Pdf");
            _imageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "images");
            _uploadsRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "KvlUploads");
            _siteImageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "SiteImage");
            _graphImageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "GraphImage");
            _SiteimageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "SiteImage");
            _downloadsFolderPath = IO.Path.Combine(_webHostEnvironment.WebRootPath, "Pdf", "FromDropbox");
            //nEWLY ADDAED-START

            _patrolDataReportService = patrolDataReportService;
            _guardLogDataProvider = guardLogDataProvider;
            //nEWLY ADDAED-END

            if (!IO.Directory.Exists(IO.Path.Combine(_reportRootDir, REPORT_DIR)))
                IO.Directory.CreateDirectory(IO.Path.Combine(_reportRootDir, REPORT_DIR));

            if (!IO.Directory.Exists(_graphImageRootDir))
                IO.Directory.CreateDirectory(_graphImageRootDir);
        }
        public string GeneratePdfReport(List<ClientSiteKpiSetting> clientSiteKpiSettings, KpiSendKVSchedules schedule, DateTime reportStartDate, DateTime reportEndDate)
        {
            var version = "v" + Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var reportFileName = $"{DateTime.Now.ToString("yyyyMMdd")} - " + schedule.ProjectName + ".pdf";
            var reportPdf = IO.Path.Combine(_reportRootDir, REPORT_DIR, reportFileName);
            var pdfDoc = new PdfDocument(new PdfWriter(reportPdf));

            pdfDoc.SetDefaultPageSize(PageSize.A4.Rotate());
            var doc = new Document(pdfDoc);
            doc.SetMargins(15f, 30f, 40f, 30f);
            //int index = 0 ;
            foreach (var clientSiteKpiSetting in clientSiteKpiSettings)
            {
                var clientSiteLogBooks = _clientDataProvider.GetClientSiteLogBooks(clientSiteKpiSetting.ClientSiteId, LogBookType.VehicleAndKeyLog, reportStartDate, reportEndDate);
                if (!clientSiteLogBooks.Any())
                    continue;
                var logBookIds = clientSiteLogBooks.Select(z => z.Id).ToList();
                foreach (var logBookId in logBookIds)
                {
                
                var clientsiteLogBook = _clientDataProvider.GetClientSiteLogBooks().SingleOrDefault(z => z.Id == logBookId);

                if (clientsiteLogBook == null)
                    continue;
                    
                    var kvlAuditLogRequest = new KeyVehicleLogAuditLogRequest()
                    {
                        VehicleRego = schedule.VehicleRego,
                        ClientSiteLocationId = schedule.ClientSiteLocationId,
                        ClientSiteId = clientSiteKpiSetting.ClientSiteId.ToString(),
                        CompanyName = schedule.CompanyName,
                        LogFromDate=reportStartDate,
                        LogToDate=reportEndDate
                    };
                    var keyVehicleLogs = GetKeyVehicleLogs(kvlAuditLogRequest).Where(z => z.Detail.GuardLogin.ClientSiteLogBookId == clientsiteLogBook.Id).ToList();

                    if (keyVehicleLogs.Count == 0)
                    {
                        continue;
                    }
                    pdfDoc.SetDefaultPageSize(PageSize.A4.Rotate());
                    //var doc = new Document(pdfDoc);
                    doc.SetMargins(15f, 30f, 40f, 30f);

                    var headerTable = CreateReportHeaderTable(clientsiteLogBook);
                    doc.Add(headerTable);

                    var reportSummaryTable = CreateReportDataTable(keyVehicleLogs);
                    doc.Add(reportSummaryTable);

                    var totalEventCountTable = CreateEventCountTable(keyVehicleLogs.Count());
                    doc.Add(totalEventCountTable);

                    //InsertAttachments(keyVehicleLogs, pdfDoc, doc);
                    //  pdfDoc.AddNewPage();
                }
                var pageSize = new PageSize(pdfDoc.GetFirstPage().GetPageSize());
                //index = index + 1;
                //pdfDoc.AddNewPage(index, pageSize);
            }
            doc.Close();
            pdfDoc.Close();

            return IO.Path.GetFileName(reportPdf);
        }
        public List<KeyVehicleLogViewModel> GetKeyVehicleLogs(KeyVehicleLogAuditLogRequest kvlRequest)
        {
            var kvlFields = _guardLogDataProvider. GetKeyVehicleLogFields();
            return _guardLogDataProvider.GetKeyVehicleLogs(kvlRequest.ClientSiteIds, kvlRequest.LogFromDate, kvlRequest.LogToDate)
                .Where(z =>
                    (string.IsNullOrEmpty(kvlRequest.VehicleRego) || string.Equals(z.VehicleRego, kvlRequest.VehicleRego, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrEmpty(kvlRequest.CompanyName) || string.Equals(z.CompanyName, kvlRequest.CompanyName, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrEmpty(kvlRequest.PersonName) || string.Equals(z.PersonName, kvlRequest.PersonName, StringComparison.OrdinalIgnoreCase)) &&
                    (!kvlRequest.PersonType.HasValue || z.PersonType == kvlRequest.PersonType) &&
                    (!kvlRequest.EntryReason.HasValue || z.EntryReason == kvlRequest.EntryReason) &&
                    (string.IsNullOrEmpty(kvlRequest.Product) || z.Product == kvlRequest.Product) &&
                    (!kvlRequest.TruckConfig.HasValue || z.TruckConfig == kvlRequest.TruckConfig) &&
                    (!kvlRequest.TrailerType.HasValue || z.TrailerType == kvlRequest.TrailerType) &&
                    (!kvlRequest.ClientSitePocId.HasValue || z.ClientSitePocId == kvlRequest.ClientSitePocId) &&
                    (!kvlRequest.ClientSiteLocationId.HasValue || z.ClientSiteLocationId == kvlRequest.ClientSiteLocationId) &&
                    (string.IsNullOrEmpty(kvlRequest.KeyNo) || (!string.IsNullOrEmpty(z.KeyNo) && z.KeyNo.Contains(kvlRequest.KeyNo)))
                     && (string.IsNullOrEmpty(kvlRequest.KeyVehicleDownselect) || (!string.IsNullOrEmpty(z.Notes) && z.Notes.Contains(kvlRequest.KeyVehicleDownselect, StringComparison.OrdinalIgnoreCase))))
                .Select(z => new KeyVehicleLogViewModel(z, kvlFields))
                .ToList();
        }
        private Table CreateReportHeaderTable(ClientSiteLogBook clientsiteLogBook)
        {
            var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 5, 10, 10, 25, 10, 25, 10, 5 })).UseAllAvailableWidth();

            var cellSiteImage = new Cell().SetBorder(Border.NO_BORDER);
            var imagePath = GetSiteImage(clientsiteLogBook.ClientSiteId);
            var folderPath = IO.Path.Combine(_SiteimageRootDir, imagePath);
            if (IO.File.Exists(folderPath))
            {
                var siteImage = new Image(ImageDataFactory.Create(imagePath))
                    .SetHeight(30)
                    .SetHorizontalAlignment(HorizontalAlignment.CENTER);
                cellSiteImage.Add(siteImage);
            }
            headerTable.AddCell(cellSiteImage);

            headerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));

            var columnSite = new Cell()
                .Add(new Paragraph().Add(new Text("Site:")))
                .SetFont(PdfHelper.GetPdfFont())
                .SetFontSize(CELL_FONT_SIZE)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE));
            headerTable.AddCell(columnSite);

            var siteName = new Cell()
                .Add(new Paragraph().Add(new Text(clientsiteLogBook.ClientSite.Name.ToString())
                .SetFont(PdfHelper.GetPdfFont())))
                .Add(new Paragraph().Add(new Text(clientsiteLogBook.ClientSite.Address ?? string.Empty)))
                .SetFontSize(CELL_FONT_SIZE)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER);
            headerTable.AddCell(siteName);

            var columnDateOfLog = new Cell()
                .Add(new Paragraph().Add(new Text("Date of Log:")))
                .SetFont(PdfHelper.GetPdfFont())
                .SetFontSize(CELL_FONT_SIZE)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE));
            headerTable.AddCell(columnDateOfLog);

            var dateOfLog = new Cell()
                .Add(new Paragraph().Add(new Text(clientsiteLogBook.Date.ToString("yyyy-MMM-dd-dddd"))))
                .SetFont(PdfHelper.GetPdfFont())
                .SetFontSize(CELL_FONT_SIZE)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE);
            headerTable.AddCell(dateOfLog);

            headerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));

            // TODO: Client logo
            var cwLogo = new Image(ImageDataFactory.Create(IO.Path.Combine(_imageRootDir, "CWSLogoPdf.png")))
                .SetHeight(20).
                SetHorizontalAlignment(HorizontalAlignment.CENTER);

            headerTable.AddCell(new Cell()
                .Add(cwLogo)
                .SetBorder(Border.NO_BORDER));

            return headerTable;
        }
        private Table CreateReportDataTable(List<KeyVehicleLogViewModel> keyVehicleLogs)
        {
            var colDimensions = new float[] { 3, 3, 3, 3, 4, 5 /* = Car/Truck */, 3, 5, 5, 3, 3, 3, 3, 5 /* = Key/Card Scan*/, 6, 3, 7, 3, 3, 3, 7 /* = Purpose of Entry*/, 3, 3, 3, 8 };
            var reportDataTable = new Table(UnitValue.CreatePercentArray(colDimensions))
                .UseAllAvailableWidth();

            CreateReportHeader(reportDataTable);

            foreach (var keyVehicleLog in keyVehicleLogs)
            {
                reportDataTable.AddCell(CreateDataCell(keyVehicleLog.Detail.InitialCallTime?.ToString("H:mm")));
                reportDataTable.AddCell(CreateDataCell(keyVehicleLog.Detail.EntryTime?.ToString("H:mm")));
                reportDataTable.AddCell(CreateDataCell(keyVehicleLog.Detail.SentInTime?.ToString("H:mm")));
                reportDataTable.AddCell(CreateDataCell(keyVehicleLog.Detail.ExitTime?.ToString("H:mm")));
                reportDataTable.AddCell(CreateDataCell(keyVehicleLog.Detail.TimeSlotNo));
                reportDataTable.AddCell(CreateDataCell(keyVehicleLog.Detail.VehicleRego));
                reportDataTable.AddCell(CreateDataCell(keyVehicleLog.Plate));
                reportDataTable.AddCell(CreateDataCell(keyVehicleLog.TruckConfigText));
                reportDataTable.AddCell(CreateDataCell(keyVehicleLog.TrailerTypeText));
                reportDataTable.AddCell(CreateDataCell(keyVehicleLog.Detail.Trailer1Rego));
                reportDataTable.AddCell(CreateDataCell(keyVehicleLog.Detail.Trailer2Rego));
                reportDataTable.AddCell(CreateDataCell(keyVehicleLog.Detail.Trailer3Rego));
                reportDataTable.AddCell(CreateDataCell(keyVehicleLog.Detail.Trailer4Rego));
                reportDataTable.AddCell(CreateDataCell(keyVehicleLog.Detail.KeyNo));
                reportDataTable.AddCell(CreateDataCell(keyVehicleLog.Detail.CompanyName));
                reportDataTable.AddCell(CreateDataCell(keyVehicleLog.Detail.PersonName));
                reportDataTable.AddCell(CreateDataCell(keyVehicleLog.Detail.MobileNumber));
                reportDataTable.AddCell(CreateDataCell(keyVehicleLog.PersonTypeText));
                reportDataTable.AddCell(CreateDataCell(keyVehicleLog.ClientSitePocName));
                reportDataTable.AddCell(CreateDataCell(keyVehicleLog.ClientSiteLocationName));
                reportDataTable.AddCell(CreateDataCell(keyVehicleLog.PurposeOfEntry));
                reportDataTable.AddCell(CreateDataCell(keyVehicleLog.Detail.InWeight?.ToString()));
                reportDataTable.AddCell(CreateDataCell(keyVehicleLog.Detail.OutWeight?.ToString()));
                reportDataTable.AddCell(CreateDataCell(keyVehicleLog.Detail.TareWeight?.ToString()));
                reportDataTable.AddCell(CreateDataCell(keyVehicleLog.Detail.Notes));
            }
            return reportDataTable;
        }
        private Table CreateReportHeader(Table reportHeaderTable)
        {
            reportHeaderTable.SetMarginTop(15);

            // first row
            reportHeaderTable.AddHeaderCell(new Cell(1, 5)
                .SetFont(PdfHelper.GetPdfFont())
                .SetFontSize(CELL_FONT_SIZE)
                .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE))
                .SetBorderBottom(new SolidBorder(WebColors.GetRGBColor(COLOR_WHITE), 0.25f))
                .SetTextAlignment(TextAlignment.CENTER)
                .Add(new Paragraph("Clocks")));

            reportHeaderTable.AddHeaderCell(new Cell(2, 1)
                .SetFont(PdfHelper.GetPdfFont())
                .SetFontSize(CELL_FONT_SIZE)
                .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE))
                .SetTextAlignment(TextAlignment.CENTER)
                .Add(new Paragraph("ID No / Vehicle Rego")));

            reportHeaderTable.AddHeaderCell(new Cell(2, 1)
                .SetFont(PdfHelper.GetPdfFont())
                .SetFontSize(CELL_FONT_SIZE)
                .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE))
                .SetTextAlignment(TextAlignment.CENTER)
                .Add(new Paragraph("ID / Plate")));

            reportHeaderTable.AddHeaderCell(new Cell(1, 2)
                .SetFont(PdfHelper.GetPdfFont())
                .SetFontSize(CELL_FONT_SIZE)
                .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE))
                .SetTextAlignment(TextAlignment.CENTER)
                .Add(new Paragraph("Vehicle Description")));

            reportHeaderTable.AddHeaderCell(new Cell(1, 4)
                .SetFont(PdfHelper.GetPdfFont())
                .SetFontSize(CELL_FONT_SIZE)
                .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE))
                .SetTextAlignment(TextAlignment.CENTER)
                .Add(new Paragraph("Trailers Rego or ISO")));

            reportHeaderTable.AddHeaderCell(new Cell(2, 1)
                .SetFont(PdfHelper.GetPdfFont())
                .SetFontSize(CELL_FONT_SIZE)
                .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE))
                .SetTextAlignment(TextAlignment.CENTER)
                .Add(new Paragraph("Key / Card Scan")));

            reportHeaderTable.AddHeaderCell(new Cell(2, 1)
                .SetFont(PdfHelper.GetPdfFont())
                .SetFontSize(CELL_FONT_SIZE)
                .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE))
                .SetTextAlignment(TextAlignment.CENTER)
                .Add(new Paragraph("Company Name")));

            reportHeaderTable.AddHeaderCell(new Cell(1, 3)
                .SetFont(PdfHelper.GetPdfFont())
                .SetFontSize(CELL_FONT_SIZE)
                .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE))
                .SetTextAlignment(TextAlignment.CENTER)
                .Add(new Paragraph("Individual")));

            reportHeaderTable.AddHeaderCell(new Cell(2, 1)
               .SetFont(PdfHelper.GetPdfFont())
               .SetFontSize(CELL_FONT_SIZE)
               .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE))
               .SetTextAlignment(TextAlignment.CENTER)
               .Add(new Paragraph("Site POC")));

            reportHeaderTable.AddHeaderCell(new Cell(2, 1)
               .SetFont(PdfHelper.GetPdfFont())
               .SetFontSize(CELL_FONT_SIZE)
               .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE))
               .SetTextAlignment(TextAlignment.CENTER)
               .Add(new Paragraph("Site Location")));

            reportHeaderTable.AddHeaderCell(new Cell(2, 1)
                .SetFont(PdfHelper.GetPdfFont())
                .SetFontSize(CELL_FONT_SIZE)
                .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE))
                .SetTextAlignment(TextAlignment.CENTER)
                .Add(new Paragraph("Purpose of Entry")));

            reportHeaderTable.AddHeaderCell(new Cell(1, 3)
                .SetFont(PdfHelper.GetPdfFont())
                .SetFontSize(CELL_FONT_SIZE)
                .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE))
                .SetBorderBottom(new SolidBorder(WebColors.GetRGBColor(COLOR_WHITE), 0.25f))
                .SetTextAlignment(TextAlignment.CENTER)
                .Add(new Paragraph("Weight")));

            reportHeaderTable.AddHeaderCell(new Cell(2, 1)
                .SetFont(PdfHelper.GetPdfFont())
                .SetFontSize(CELL_FONT_SIZE)
                .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE))
                .SetTextAlignment(TextAlignment.CENTER)
                .Add(new Paragraph("Notes")));

            //second row
            var initialCall = new Cell()
                           .Add(new Paragraph().Add(new Text("Initial Call")))
                           .SetFont(PdfHelper.GetPdfFont())
                           .SetFontSize(CELL_FONT_SIZE)
                           .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE))
                           .SetTextAlignment(TextAlignment.CENTER)
                           .SetHorizontalAlignment(HorizontalAlignment.CENTER);
            reportHeaderTable.AddHeaderCell(initialCall);

            var entryTime = new Cell()
                          .Add(new Paragraph().Add(new Text("Entry Time")))
                          .SetFont(PdfHelper.GetPdfFont())
                          .SetFontSize(CELL_FONT_SIZE)
                          .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE))
                          .SetTextAlignment(TextAlignment.CENTER)
                          .SetHorizontalAlignment(HorizontalAlignment.CENTER);
            reportHeaderTable.AddHeaderCell(entryTime);

            var sentInTime = new Cell()
                        .Add(new Paragraph().Add(new Text("SentIn Time")))
                        .SetFont(PdfHelper.GetPdfFont())
                        .SetFontSize(CELL_FONT_SIZE)
                        .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE))
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetHorizontalAlignment(HorizontalAlignment.CENTER);
            reportHeaderTable.AddHeaderCell(sentInTime);

            var exitTime = new Cell()
                         .Add(new Paragraph().Add(new Text("Exit Time")))
                         .SetFont(PdfHelper.GetPdfFont())
                         .SetFontSize(CELL_FONT_SIZE)
                         .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE))
                         .SetTextAlignment(TextAlignment.CENTER)
                         .SetHorizontalAlignment(HorizontalAlignment.CENTER);
            reportHeaderTable.AddHeaderCell(exitTime);

            var timeSlotNo = new Cell()
                        .Add(new Paragraph().Add(new Text("Time Slot No")))
                        .SetFont(PdfHelper.GetPdfFont())
                        .SetFontSize(CELL_FONT_SIZE)
                        .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE))
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetHorizontalAlignment(HorizontalAlignment.CENTER);
            reportHeaderTable.AddHeaderCell(timeSlotNo);

            var truckConfig = new Cell()
                         .Add(new Paragraph().Add(new Text("Truck Config")))
                         .SetFont(PdfHelper.GetPdfFont())
                         .SetFontSize(CELL_FONT_SIZE)
                         .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE))
                         .SetTextAlignment(TextAlignment.CENTER)
                         .SetHorizontalAlignment(HorizontalAlignment.CENTER);
            reportHeaderTable.AddHeaderCell(truckConfig);

            var trailerType = new Cell()
                         .Add(new Paragraph().Add(new Text("Trailer Type")))
                         .SetFont(PdfHelper.GetPdfFont())
                         .SetFontSize(CELL_FONT_SIZE)
                         .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE))
                         .SetTextAlignment(TextAlignment.CENTER)
                         .SetHorizontalAlignment(HorizontalAlignment.CENTER);
            reportHeaderTable.AddHeaderCell(trailerType);

            var tailer1 = new Cell()
                         .Add(new Paragraph().Add(new Text("1")))
                         .SetFont(PdfHelper.GetPdfFont())
                         .SetFontSize(CELL_FONT_SIZE)
                         .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE))
                         .SetTextAlignment(TextAlignment.CENTER)
                         .SetHorizontalAlignment(HorizontalAlignment.CENTER);
            reportHeaderTable.AddHeaderCell(tailer1);

            var tailer2 = new Cell()
                        .Add(new Paragraph().Add(new Text("2")))
                        .SetFont(PdfHelper.GetPdfFont())
                        .SetFontSize(CELL_FONT_SIZE)
                        .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE))
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetHorizontalAlignment(HorizontalAlignment.CENTER);
            reportHeaderTable.AddHeaderCell(tailer2);

            var tailer3 = new Cell()
                        .Add(new Paragraph().Add(new Text("3")))
                        .SetFont(PdfHelper.GetPdfFont())
                        .SetFontSize(CELL_FONT_SIZE)
                        .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE))
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetHorizontalAlignment(HorizontalAlignment.CENTER);
            reportHeaderTable.AddHeaderCell(tailer3);

            var tailer4 = new Cell()
                        .Add(new Paragraph().Add(new Text("4")))
                        .SetFont(PdfHelper.GetPdfFont())
                        .SetFontSize(CELL_FONT_SIZE)
                        .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE))
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetHorizontalAlignment(HorizontalAlignment.CENTER);
            reportHeaderTable.AddHeaderCell(tailer4);

            var individualName = new Cell()
                      .Add(new Paragraph().Add(new Text("Name")))
                      .SetFont(PdfHelper.GetPdfFont())
                      .SetFontSize(CELL_FONT_SIZE)
                      .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE))
                      .SetTextAlignment(TextAlignment.CENTER)
                      .SetHorizontalAlignment(HorizontalAlignment.CENTER);
            reportHeaderTable.AddHeaderCell(individualName);

            var mobileNumber = new Cell()
                       .Add(new Paragraph().Add(new Text("Mobile No:")))
                       .SetFont(PdfHelper.GetPdfFont())
                       .SetFontSize(CELL_FONT_SIZE)
                       .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE))
                       .SetTextAlignment(TextAlignment.CENTER)
                       .SetHorizontalAlignment(HorizontalAlignment.CENTER);
            reportHeaderTable.AddHeaderCell(mobileNumber);

            var personType = new Cell()
                       .Add(new Paragraph().Add(new Text("Type")))
                       .SetFont(PdfHelper.GetPdfFont())
                       .SetFontSize(CELL_FONT_SIZE)
                       .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE))
                       .SetTextAlignment(TextAlignment.CENTER)
                       .SetHorizontalAlignment(HorizontalAlignment.CENTER);
            reportHeaderTable.AddHeaderCell(personType);

            var weightInGross = new Cell()
                       .Add(new Paragraph().Add(new Text("In Gross (t)")))
                       .SetFont(PdfHelper.GetPdfFont())
                       .SetFontSize(CELL_FONT_SIZE)
                       .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE))
                       .SetTextAlignment(TextAlignment.CENTER)
                       .SetHorizontalAlignment(HorizontalAlignment.CENTER);
            reportHeaderTable.AddHeaderCell(weightInGross);

            var weightOutNet = new Cell()
                       .Add(new Paragraph().Add(new Text("Out Net (t)")))
                       .SetFont(PdfHelper.GetPdfFont())
                       .SetFontSize(CELL_FONT_SIZE)
                       .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE))
                       .SetTextAlignment(TextAlignment.CENTER)
                       .SetHorizontalAlignment(HorizontalAlignment.CENTER);
            reportHeaderTable.AddHeaderCell(weightOutNet);

            var weightTare = new Cell()
                     .Add(new Paragraph().Add(new Text("Tare (t)")))
                     .SetFont(PdfHelper.GetPdfFont())
                     .SetFontSize(CELL_FONT_SIZE)
                     .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE))
                     .SetTextAlignment(TextAlignment.CENTER)
                     .SetHorizontalAlignment(HorizontalAlignment.CENTER);
            reportHeaderTable.AddHeaderCell(weightTare);

            return reportHeaderTable;
        }

        private Cell CreateDataCell(string text)
        {
            var cell = new Cell().SetKeepTogether(true);
            cell.SetFontSize(CELL_FONT_SIZE)
                  .SetTextAlignment(TextAlignment.CENTER)
                  .SetHorizontalAlignment(HorizontalAlignment.CENTER);
            cell.Add(new Paragraph(text ?? string.Empty));

            return cell;
        }

        private Table CreateEventCountTable(int eventCount)
        {
            var eventCountTable = new Table(1).UseAllAvailableWidth().SetMarginTop(3);
            eventCountTable.AddCell(new Cell().SetBorder(Border.NO_BORDER).Add(new Paragraph().Add(new Text($"Total Events: {eventCount}")).SetFontSize(CELL_FONT_SIZE)));
            return eventCountTable;
        }

        private void InsertAttachments(List<KeyVehicleLogViewModel> keyVehicleLogs, PdfDocument pdfDoc, Document doc)
        {
            var index = 1;
            foreach (var keyVehicleLog in keyVehicleLogs)
            {
                var reportReference = keyVehicleLog.Detail.ReportReference?.ToString();
                var fileNames = _viewDataService.GetKeyVehicleLogAttachments(_uploadsRootDir, reportReference);
                foreach (var fileName in fileNames)
                {
                    var image = AttachImageToPdf(pdfDoc, ++index, IO.Path.Combine(_uploadsRootDir, reportReference, fileName));
                    var paraName = new Paragraph($"ID No / Vehicle Rego: {keyVehicleLog.Detail.VehicleRego}. File Name: {fileName}")
                        .SetPaddingLeft(5f)
                        .SetFontSize(8f)
                        .SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE))
                        .SetFontColor(WebColors.GetRGBColor(FONT_COLOR_BLACK));
                    paraName.SetFixedPosition(index, 0, 0, 600);
                    doc.Add(image).Add(paraName);
                }
            }
        }

        private Image AttachImageToPdf(PdfDocument pdfDocument, int index, string imagePath)
        {
            var pageSize = new PageSize(pdfDocument.GetFirstPage().GetPageSize());
            pdfDocument.AddNewPage(index, pageSize);
            var imageData = ImageDataFactory.Create(imagePath);
            var image = new Image(imageData);
            bool rotateImage = image.GetImageWidth() > image.GetImageHeight();
            bool scaleImage = image.GetImageWidth() > MAX_IMAGE_WIDTH || image.GetImageHeight() > MAX_IMAGE_HEIGHT;
            if (rotateImage)
            {
                image.SetRotationAngle(ROTATION_ANGLE_DEG * (Math.PI / 180));
                if (scaleImage)
                    image.ScaleToFit(PageSize.A4.GetHeight() * SCALE_FACTOR, PageSize.A4.GetWidth() * SCALE_FACTOR);
            }
            else
            {
                if (scaleImage)
                    image.ScaleToFit(PageSize.A4.GetWidth() * SCALE_FACTOR, PageSize.A4.GetHeight() * SCALE_FACTOR);
            }
            var bottom = rotateImage ? pageSize.GetTop() : pageSize.GetTop() - image.GetImageScaledHeight();
            image.SetFixedPosition(index, 0, bottom);
            return image;
        }

        private string GetSiteImage(int clientSiteId)
        {
            var clientSiteSetting = _clientDataProvider.GetClientSiteKpiSetting(clientSiteId);
            if (clientSiteSetting != null && !string.IsNullOrEmpty(clientSiteSetting.SiteImage))
                return $"{new Uri(_settings.IrApiUrl)}{clientSiteSetting.SiteImage}";
            return string.Empty;
        }
        //private string GeneratePdf(ClientSiteLogBook clientsiteLogBook, List<KeyVehicleLogViewModel> keyVehicleLogs)
        //{
        //    var version = "v" + Assembly.GetExecutingAssembly().GetName().Version.ToString();
        //    var reportPdf = GetReportPdfFilePath(clientsiteLogBook, version);
        //    var pdfDoc = new PdfDocument(new PdfWriter(reportPdf));

        //    pdfDoc.SetDefaultPageSize(PageSize.A4.Rotate());
        //    var doc = new Document(pdfDoc);
        //    doc.SetMargins(15f, 30f, 40f, 30f);

        //    var headerTable = CreateReportHeaderTable(clientsiteLogBook);
        //    doc.Add(headerTable);

        //    var reportSummaryTable = CreateReportDataTable(keyVehicleLogs);
        //    doc.Add(reportSummaryTable);

        //    var totalEventCountTable = CreateEventCountTable(keyVehicleLogs.Count());
        //    doc.Add(totalEventCountTable);

        //    InsertAttachments(keyVehicleLogs, pdfDoc, doc);

        //    doc.Close();
        //    pdfDoc.Close();

        //    return IO.Path.GetFileName(reportPdf);
        //}


        public string GeneratePdfKVReport(DateTime startdate, DateTime endDate, int clientsiteId)
        {
            //DateTime startdateTime = DateTime.Parse(startdate);
            var clientsiteName = _clientDataProvider.GetClientSiteDetailsWithId(clientsiteId).FirstOrDefault().Name;
            var version = "v" + Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var reportFileName = $"{DateTime.Now.ToString("yyyyMMdd")} - " + clientsiteName + ".pdf";
            var reportPdf = IO.Path.Combine(_reportRootDir, REPORT_DIR, reportFileName);
            var pdfDoc = new PdfDocument(new PdfWriter(reportPdf));

            pdfDoc.SetDefaultPageSize(PageSize.A4.Rotate());
            var doc = new Document(pdfDoc);
            doc.SetMargins(15f, 30f, 40f, 30f);
            //int index = 0 ;
            
                var clientSiteLogBooks = _clientDataProvider.GetClientSiteLogBooks(clientsiteId, LogBookType.VehicleAndKeyLog, startdate, endDate);
                if (!clientSiteLogBooks.Any())
                    return null;
                var logBookIds = clientSiteLogBooks.Select(z => z.Id).ToList();
                foreach (var logBookId in logBookIds)
                {

                    var clientsiteLogBook = _clientDataProvider.GetClientSiteLogBooks().SingleOrDefault(z => z.Id == logBookId);

                    if (clientsiteLogBook == null)
                        continue;

                    var kvlAuditLogRequest = new KeyVehicleLogAuditLogRequest()
                    {
                      
                        ClientSiteId = clientsiteId.ToString(),
                        LogFromDate = startdate,
                        LogToDate = endDate
                    };
                    var keyVehicleLogs = GetKeyVehicleLogs(kvlAuditLogRequest).Where(z => z.Detail.GuardLogin.ClientSiteLogBookId == clientsiteLogBook.Id).ToList();

                    if (keyVehicleLogs.Count == 0)
                    {
                        continue;
                    }
                    pdfDoc.SetDefaultPageSize(PageSize.A4.Rotate());
                    //var doc = new Document(pdfDoc);
                    doc.SetMargins(15f, 30f, 40f, 30f);

                    var headerTable = CreateReportHeaderTable(clientsiteLogBook);
                    doc.Add(headerTable);

                    var reportSummaryTable = CreateReportDataTable(keyVehicleLogs);
                    doc.Add(reportSummaryTable);

                    var totalEventCountTable = CreateEventCountTable(keyVehicleLogs.Count());
                    doc.Add(totalEventCountTable);

                    //InsertAttachments(keyVehicleLogs, pdfDoc, doc);
                    //  pdfDoc.AddNewPage();
                }
                var pageSize = new PageSize(pdfDoc.GetFirstPage().GetPageSize());
                //index = index + 1;
                //pdfDoc.AddNewPage(index, pageSize);
            
            doc.Close();
            pdfDoc.Close();

            return reportFileName;
        }

        public string GeneratePdfReportWithClientSiteIds(int ClientSiteId, KpiSendKVSchedules schedule, DateTime reportStartDate, DateTime reportEndDate)
        {
            var version = "v" + Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var reportFileName = $"{DateTime.Now.ToString("yyyyMMdd")} - " + schedule.ProjectName + ".pdf";
            var reportPdf = IO.Path.Combine(_reportRootDir, REPORT_DIR, reportFileName);
            var pdfDoc = new PdfDocument(new PdfWriter(reportPdf));

            pdfDoc.SetDefaultPageSize(PageSize.A4.Rotate());
            var doc = new Document(pdfDoc);
            doc.SetMargins(15f, 30f, 40f, 30f);
            //int index = 0 ;
            //foreach (var clientSiteKpiSetting in clientSiteKpiSettings)
            //{
            var clientSiteLogBooks = _clientDataProvider.GetClientSiteLogBooks(ClientSiteId, LogBookType.VehicleAndKeyLog, reportStartDate, reportEndDate);
            if (!clientSiteLogBooks.Any())
                return null;
            var logBookIds = clientSiteLogBooks.Select(z => z.Id).ToList();
            foreach (var logBookId in logBookIds)
            {

                var clientsiteLogBook = _clientDataProvider.GetClientSiteLogBooks().SingleOrDefault(z => z.Id == logBookId);

                if (clientsiteLogBook == null)
                    continue;

                var kvlAuditLogRequest = new KeyVehicleLogAuditLogRequest()
                {
                    VehicleRego = schedule.VehicleRego,
                    ClientSiteLocationId = schedule.ClientSiteLocationId,
                    ClientSiteId = ClientSiteId.ToString(),
                    CompanyName = schedule.CompanyName,
                    LogFromDate = reportStartDate,
                    LogToDate = reportEndDate
                };
                var keyVehicleLogs = GetKeyVehicleLogs(kvlAuditLogRequest).Where(z => z.Detail.GuardLogin.ClientSiteLogBookId == clientsiteLogBook.Id).ToList();

                if (keyVehicleLogs.Count == 0)
                {
                    continue;
                }
                pdfDoc.SetDefaultPageSize(PageSize.A4.Rotate());
                //var doc = new Document(pdfDoc);
                doc.SetMargins(15f, 30f, 40f, 30f);

                var headerTable = CreateReportHeaderTable(clientsiteLogBook);
                doc.Add(headerTable);

                var reportSummaryTable = CreateReportDataTable(keyVehicleLogs);
                doc.Add(reportSummaryTable);

                var totalEventCountTable = CreateEventCountTable(keyVehicleLogs.Count());
                doc.Add(totalEventCountTable);

                //InsertAttachments(keyVehicleLogs, pdfDoc, doc);
                //  pdfDoc.AddNewPage();
            }
            var pageSize = new PageSize(pdfDoc.GetFirstPage().GetPageSize());
            //index = index + 1;
            //pdfDoc.AddNewPage(index, pageSize);
            //}
            doc.Close();
            pdfDoc.Close();

            return IO.Path.GetFileName(reportPdf);
        }


    }
}
