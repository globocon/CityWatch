using CityWatch.Common.Helpers;
using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using CityWatch.Web.Helpers;
using CityWatch.Web.Pages.Radio;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Events;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Action;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;
using IO = System.IO;
namespace CityWatch.Web.Services
{
    public interface ISmartWandReportZipGenarator
    {
        Task<string> GenerateZipFile(int[] clientSiteIds, DateTime logFromDate, DateTime logToDate);


    }
    public class SmartWandReportZipGenarator : ISmartWandReportZipGenarator
    {
        private const string COLOR_WHITE = "#ffffff";
        private const string COLOR_NAVY_BLUE = "#002060";
        private const string COLOR_LIGHT_BLUE = "#d9e2f3";
        private const string COLOR_GREY_DARK = "#bfbfbf";
        private const string COLOR_GREY_LIGHT = "#a6a6a6";
        private const string COLOR_PALE_YELLOW = "#fcf8d1";
        private const string COLOR_PALE_RED = "#ffcccc";

        private const string REPORT_DIR = "Output";
        private const float CELL_FONT_SIZE = 7f;

        private readonly IClientDataProvider _clientDataProvider;
        private readonly IClientSiteWandDataProvider _clientSiteWandDataProvider;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        private readonly IGuardLoginDetailService _guardLoginDetailService;
        private readonly Settings _settings;
        private readonly string _reportRootDir;
        private readonly string _imageRootDir;
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly string _downloadsFolderPath;
        public SmartWandReportZipGenarator(IWebHostEnvironment webHostEnvironment,
            IClientDataProvider clientDataProvider,
            IClientSiteWandDataProvider clientSiteWandDataProvider,
            IGuardLogDataProvider guardLogDataProvider,
            IGuardLoginDetailService guardLoginDetailService,
            IOptions<Settings> settings,
            IGuardDataProvider guardDataProvider)

        {
            _clientDataProvider = clientDataProvider;
            _clientSiteWandDataProvider = clientSiteWandDataProvider;
            _guardLogDataProvider = guardLogDataProvider;
            _guardLoginDetailService = guardLoginDetailService;
            _settings = settings.Value;
            _reportRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "Pdf");
            _imageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "images");
            _guardDataProvider = guardDataProvider;
            _downloadsFolderPath = IO.Path.Combine(webHostEnvironment.WebRootPath, "Pdf", "FromDropbox");
        }
        public async Task<string> GenerateZipFile(int[] clientSiteIds, DateTime logFromDate, DateTime logToDate)
        {
            if (clientSiteIds.Length <= 0)
            {
                return string.Empty;
            }
            var zipFolderPath = GetZipFolderPath();
            var fileNamePart = string.Empty;
            var clientSite = _clientDataProvider.GetClientSiteDetails(clientSiteIds[0]);
            fileNamePart = clientSite.Name;
            foreach ( var clientSiteId in clientSiteIds)
            {
                CreateFQReports(clientSiteId, zipFolderPath,  logFromDate,  logToDate);
            }

            return GetZipFileName(zipFolderPath, logFromDate, logToDate, fileNamePart);
        }
        private string GetZipFileName(string zipFolderPath, DateTime logFromDate, DateTime logToDate, string fileNamePart)
        {
            var zipFileName = $"{FileNameHelper.GetSanitizedFileNamePart(fileNamePart)}_{logFromDate:yyyyMMdd}_{logToDate:yyyyMMdd}_{new Random().Next(100, 999)}.zip";
            ZipFile.CreateFromDirectory(zipFolderPath, IO.Path.Combine(_downloadsFolderPath, zipFileName), CompressionLevel.Optimal, false);

            if (!Directory.Exists(zipFolderPath))
                Directory.Delete(zipFolderPath);

            return zipFileName;
        }

        private string GetZipFolderPath()
        {
            var zipFolderPath =IO.Path.Combine(_downloadsFolderPath, Guid.NewGuid().ToString());
            if (!Directory.Exists(zipFolderPath))
                Directory.CreateDirectory(zipFolderPath);
            return zipFolderPath;
        }
        private void CreateFQReports(int clientSiteId, string zipFolderPath, DateTime logFromDate, DateTime logToDate)
        {
            
                var fileName = GenerateSartWandPdfReports(clientSiteId,  logFromDate,  logToDate);
                if (!string.IsNullOrEmpty(fileName))
                {
                    var reportFilePath = IO.Path.Combine(_reportRootDir, "Output", fileName);
                    IO.File.Copy(reportFilePath, IO.Path.Combine(zipFolderPath, fileName));
                    IO.File.Delete(reportFilePath);
                }
            
        }
        public string GenerateSartWandPdfReports(int ClientSiteId, DateTime logFromDate, DateTime logToDate)
        {

            var clientsiteLogBook = _guardLogDataProvider.GetTagStatusPendingForSpecificClientSite(ClientSiteId, logFromDate, logToDate); 

            if (clientsiteLogBook == null)
                return string.Empty;
            var clientSite = _clientDataProvider.GetClientSiteDetails(ClientSiteId);
          
            var reportPdf = GetReportPdfFilePath(clientSite);

            var pdfDoc = new PdfDocument(new PdfWriter(reportPdf));
            pdfDoc.SetDefaultPageSize(PageSize.A4);
            var doc = new Document(pdfDoc);
            doc.SetMargins(15f, 30f, 40f, 30f);

            doc.Add(new Paragraph("Daily Wand Scan Details")
        .SetFontColor(WebColors.GetRGBColor(COLOR_NAVY_BLUE))
        .SetFontSize(CELL_FONT_SIZE * 1.5f)
        .SetTextAlignment(TextAlignment.CENTER)   // center the heading
        .SetMarginTop(5));

         doc.Add(new Paragraph("Site: " + clientSite.Name)
        .SetFontColor(WebColors.GetRGBColor(COLOR_NAVY_BLUE))
        .SetFontSize(CELL_FONT_SIZE * 1.5f)
        .SetTextAlignment(TextAlignment.CENTER)   // center the heading
        .SetMarginTop(5));



            var guardWandScanDetails = CreateGuardWandScanDetails(clientsiteLogBook);
            doc.Add(guardWandScanDetails);



            var footer = CreateFooter();
            pdfDoc.AddEventHandler(PdfDocumentEvent.END_PAGE, new TableFooterEventHandler(footer));

            doc.Close();
            pdfDoc.Close();

            return IO.Path.GetFileName(reportPdf);
        }
        private string GetReportPdfFilePath(ClientSite clientsiteLogBook)
        {
            var reportPdfPath = IO.Path.Combine(_reportRootDir, REPORT_DIR, $"{DateTime.Now:yyyyMMdd} - Guard SmartWand  Log - {FileNameHelper.GetSanitizedFileNamePart(clientsiteLogBook.Name)} .pdf");

            if (IO.File.Exists(reportPdfPath))
                IO.File.Delete(reportPdfPath);

            return reportPdfPath;
        }
        private Table CreateGuardWandScanDetails(List<SiteTagStatusPendingNew> clientSiteLogBook)
        {
            var guardWandScanDetailsTable =
                new Table(UnitValue.CreatePercentArray(new float[] { 2, 9, 5, 2, 2 }))
                .UseAllAvailableWidth()
                .SetMarginBottom(15);

            // HEADER ROW
            guardWandScanDetailsTable.AddCell(
                new Cell()
                .SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY_DARK))
                .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                .SetFontSize(CELL_FONT_SIZE)
                .Add(new Paragraph("Type")));

            guardWandScanDetailsTable.AddCell(
                new Cell()
                .SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY_DARK))
                .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                .SetFontSize(CELL_FONT_SIZE)
                .Add(new Paragraph("Label")));

            guardWandScanDetailsTable.AddCell(
                new Cell()
                .SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY_DARK))
                .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                .SetFontSize(CELL_FONT_SIZE)
                .Add(new Paragraph("Pending FQ")));

            guardWandScanDetailsTable.AddCell(
                new Cell()
                .SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY_DARK))
                .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                .SetFontSize(CELL_FONT_SIZE)
                .Add(new Paragraph("Scans")));

            guardWandScanDetailsTable.AddCell(
                new Cell()
                .SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY_DARK))
                .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                .SetFontSize(CELL_FONT_SIZE)
                .Add(new Paragraph("[HN] Scans")));

            // DATA ROWS
            foreach (var groupItem in clientSiteLogBook)
            {
                guardWandScanDetailsTable.AddCell(
                    new Cell()
                    .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                    .SetFontSize(CELL_FONT_SIZE)
                    .Add(new Paragraph(groupItem.TagType)));

                guardWandScanDetailsTable.AddCell(
                    new Cell()
                    .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                    .SetFontSize(CELL_FONT_SIZE)
                    .Add(new Paragraph(groupItem.LabelDescription)));

                guardWandScanDetailsTable.AddCell(
                    new Cell()
                    .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                    .SetFontSize(CELL_FONT_SIZE)
                    .Add(new Paragraph(groupItem.RoundNumber.ToString())));

                guardWandScanDetailsTable.AddCell(
                    new Cell()
                    .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                    .SetFontSize(CELL_FONT_SIZE)
                    .Add(new Paragraph(groupItem.TodayScanCount.ToString())));

                guardWandScanDetailsTable.AddCell(
                    new Cell()
                    .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                    .SetFontSize(CELL_FONT_SIZE)
                    .Add(new Paragraph(groupItem.MyScans.ToString())));
            }

            return guardWandScanDetailsTable;
        }
        private Table CreateFooter()
        {
            var footerTable = new Table(UnitValue.CreatePercentArray(new float[] { 5, 20, 60, 15 })).UseAllAvailableWidth();

            var cwLogo = new Image(ImageDataFactory.Create(IO.Path.Combine(_imageRootDir, "CWSLogoPdf.png"))).SetHeight(20).SetHorizontalAlignment(HorizontalAlignment.CENTER);
            footerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER).Add(cwLogo));

            var isoImage = new Image(ImageDataFactory.Create(IO.Path.Combine(_imageRootDir, "ISOv3.jpg"))).SetHeight(20).SetHorizontalAlignment(HorizontalAlignment.CENTER);
            footerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER).Add(isoImage));

            footerTable.AddCell(new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontColor(WebColors.GetRGBColor(COLOR_GREY_DARK))
                .SetFontSize(CELL_FONT_SIZE * 0.8f)
                .Add(new Paragraph($"© {DateTime.Today:yyyy} - CityWatch Security (AUST) Pty. Ltd | ABN: 46 094 745 758 | Commercial-In-Confidence | [SEC=OFFICAL]")));

            footerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER).Add(new Paragraph("")));


            return footerTable;
        }
        private class TableFooterEventHandler : IEventHandler
        {
            private readonly Table _footerTable;

            public TableFooterEventHandler(Table footerTable)
            {
                _footerTable = footerTable;
            }

            public void HandleEvent(Event currentEvent)
            {
                PdfDocumentEvent docEvent = (PdfDocumentEvent)currentEvent;
                PdfDocument pdfDoc = docEvent.GetDocument();
                PdfPage page = docEvent.GetPage();
                PdfCanvas canvas = new PdfCanvas(page.NewContentStreamBefore(), page.GetResources(), pdfDoc);

                new Canvas(canvas, new Rectangle(30, 0, page.GetPageSize().GetWidth() - 60, 40)).Add(_footerTable).Close();
            }
        }
    }
}
