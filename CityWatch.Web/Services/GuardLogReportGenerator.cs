using CityWatch.Common.Helpers;
using CityWatch.Data.Enums;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using CityWatch.Web.Helpers;
using DocumentFormat.OpenXml.ExtendedProperties;
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
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using static Dropbox.Api.FileProperties.PropertiesSearchMode;
using static Dropbox.Api.Files.WriteMode;
using static System.Net.WebRequestMethods;
using IO = System.IO;


namespace CityWatch.Web.Services
{

    public interface IGuardLogReportGenerator
    {
        string GeneratePdfReport(int clientSiteLogBookId, string keywordDownSelect);
        public string GeneratePdfReportForFusion(List<ClientSiteRadioChecksActivityStatus_History> funsionLog);
        public Table CreateReportDataForFusionWithoutSiteName(List<ClientSiteRadioChecksActivityStatus_History> guardLog);

        public string GeneratePdfReportFusion(int clientSiteLogBookId);
        public string GeneratePdfReportSmartWand(int clientSiteLogBookId);
    }

    public class GuardLogReportGenerator : IGuardLogReportGenerator
    {
        private const string COLOR_WHITE = "#ffffff";
        private const string COLOR_NAVY_BLUE = "#002060";
        private const string COLOR_LIGHT_BLUE = "#d9e2f3";
        private const string COLOR_GREY_DARK = "#bfbfbf";
        private const string COLOR_GREY_LIGHT = "#a6a6a6";
        private const string COLOR_PALE_YELLOW = "#fcf8d1";
        private const string COLOR_PALE_RED = "#ffcccc";
        private const string FONT_COLOR_BLACK = "#000000";
        private const string REPORT_DIR = "Output";
        private const float CELL_FONT_SIZE = 7f;

        private readonly IClientDataProvider _clientDataProvider;
        private readonly IClientSiteWandDataProvider _clientSiteWandDataProvider;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        private readonly IGuardLoginDetailService _guardLoginDetailService;
        private readonly IConfigDataProvider _configDataProvider;
        private readonly Settings _settings;
        private readonly string _reportRootDir;
        private readonly string _imageRootDir;
        private readonly string _subDomainImageRootDir;

        //p6-102 Add photo -start
        private const float MAX_IMAGE_WIDTH = 600;
        private const float MAX_IMAGE_HEIGHT = 800;
        private const float SCALE_FACTOR = 0.92f;
        private const int ROTATION_ANGLE_DEG = 270;
        //p6-102 Add photo -end

        public GuardLogReportGenerator(IWebHostEnvironment webHostEnvironment,
            IClientDataProvider clientDataProvider,
            IClientSiteWandDataProvider clientSiteWandDataProvider,
            IGuardLogDataProvider guardLogDataProvider,
            IGuardLoginDetailService guardLoginDetailService,
            IConfigDataProvider configDataProvider,
            IOptions<Settings> settings)

        {
            _clientDataProvider = clientDataProvider;
            _clientSiteWandDataProvider = clientSiteWandDataProvider;
            _guardLogDataProvider = guardLogDataProvider;
            _guardLoginDetailService = guardLoginDetailService;
            _configDataProvider = configDataProvider;
            _settings = settings.Value;
            _reportRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "Pdf");
            _imageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "images");
            _subDomainImageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "SubdomainLogo");
        }

        public string GeneratePdfReport(int clientSiteLogBookId, string keywordDownSelect)
        {
            var clientsiteLogBook = _clientDataProvider.GetClientSiteLogBooks().SingleOrDefault(z => z.Id == clientSiteLogBookId);

            if (clientsiteLogBook == null)
                return string.Empty;

            var version = "v" + Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var reportPdf = GetReportPdfFilePath(clientsiteLogBook, version);
            var _guardLogs = _guardLogDataProvider.GetGuardLogs(clientSiteLogBookId, clientsiteLogBook.Date)
     .Where(x =>
         (string.IsNullOrEmpty(keywordDownSelect) ||
          (!string.IsNullOrEmpty(x.Notes) && x.Notes.Contains(keywordDownSelect)))
         && x.WAND_TAG_ENTRY_TYPE == ScanningType.Normal
     )
     .ToList();
            if (_guardLogs.Count() > 0)
            {
                var pdfDoc = new PdfDocument(new PdfWriter(reportPdf));
                pdfDoc.SetDefaultPageSize(PageSize.A4);
                var doc = new Document(pdfDoc);
                doc.SetMargins(15f, 30f, 40f, 30f);

                var headerTable = CreateReportHeader(clientsiteLogBook.ClientSite, version);
                doc.Add(headerTable);

                doc.Add(new Paragraph("On-Duty Guard Details")
                    .SetFontColor(WebColors.GetRGBColor(COLOR_NAVY_BLUE))
                    .SetFontSize(CELL_FONT_SIZE * 1.5f)
                    .SetMarginTop(5));

                var guardDetails = CreateGuardDetails(clientsiteLogBook);
                doc.Add(guardDetails);

                doc.Add(new Paragraph("Log Book")
                    .SetFontColor(WebColors.GetRGBColor(COLOR_NAVY_BLUE))
                    .SetFontSize(CELL_FONT_SIZE * 1.5f)
                    .SetMarginTop(5));

                var customFieldLogs = _guardLogDataProvider.GetCustomFieldLogs(clientSiteLogBookId).ToList();
                var patrolCarLogs = _guardLogDataProvider.GetPatrolCarLogs(clientSiteLogBookId).ToList();
                var crowdControlLogs = _guardLogDataProvider.GetMobileCrowdControlLogs(clientsiteLogBook.ClientSite.Id, clientSiteLogBookId, clientsiteLogBook.Date, clientsiteLogBook.Date).ToList();
                if (customFieldLogs.Any() || patrolCarLogs.Any() || crowdControlLogs.Any())
                {
                    //var addlFieldLogs = CreateCustomFieldAndPatrolCarLogsTable(customFieldLogs, patrolCarLogs, crowdControlLogs);
                    var addlFieldLogs = CreateCustomFieldCrowdControlLogsAndPatrolCarLogsTable(customFieldLogs, patrolCarLogs, crowdControlLogs);
                    doc.Add(addlFieldLogs);
                }

                var tableData = CreateReportData(_guardLogs);
                doc.Add(tableData);

                var logNotes = CreateNotes(clientsiteLogBook.ClientSite.Id);
                doc.Add(logNotes);

                int _clientTypeId = clientsiteLogBook.ClientSite.ClientType.Id;
                var footer = CreateFooter(_clientTypeId);
                pdfDoc.AddEventHandler(PdfDocumentEvent.END_PAGE, new TableFooterEventHandler(footer));

                //p6-102 Add photo -start Commented 19092024 Dileep Start
                //var index = 1;
                //foreach (var entry in _guardLogs)
                //{


                //    var guardlogImages = _guardLogDataProvider.GetGuardLogDocumentImaes(entry.Id);
                //    Paragraph notesParagraphnew = new Paragraph("See attached file  ").SetFontSize(CELL_FONT_SIZE);

                //    foreach (var guardLogImage in guardlogImages)
                //    {

                //        if (guardLogImage.IsRearfile == true)
                //        {
                //            var docImage = new Document(pdfDoc);
                //            var image = AttachImageToPdf(pdfDoc, ++index, guardLogImage.ImagePath);
                //            doc.Add(image);



                //            var paraName = new Paragraph($"File Name: {IO.Path.GetFileName(guardLogImage.ImagePath)}").SetFontColor(WebColors.GetRGBColor(FONT_COLOR_BLACK));
                //            doc.Add(paraName);
                //            docImage.Close();
                //        }
                //    }
                //}
                //p6-102 Add photo -end end 
                //New Code fix the image bug start Dileep 

                int lastPageIndex = pdfDoc.GetNumberOfPages();
                var index = lastPageIndex + 1;
                foreach (var entry in _guardLogs)
                {
                    var guardlogImages = _guardLogDataProvider.GetGuardLogDocumentImaes(entry.Id);
                    foreach (var guardLogImage in guardlogImages)
                    {

                        if (guardLogImage.IsRearfile == true)
                        {
                            try
                            {

                                AttachImageToPdf(pdfDoc, doc, index, guardLogImage.ImagePath);
                                index++;
                                // Add the image to the document
                                //doc.Add(image);
                                //var image = AttachImageToPdf(pdfDoc, index, guardLogImage.ImagePath);
                                //doc.Add(image);

                                //var paraName = new Paragraph($"File Name: {System.IO.Path.GetFileName(guardLogImage.ImagePath)}")
                                //    .SetFontColor(WebColors.GetRGBColor(FONT_COLOR_BLACK));
                                //doc.Add(paraName);

                            }
                            catch (Exception ex)
                            {
                                // Log exception or handle it as needed
                                Console.WriteLine($"Error attaching image: {ex.Message}");
                            }
                        }
                    }
                }

                //New Code fix the image bug end 
                doc.Close();
                pdfDoc.Close();
            }
            else
            {
                reportPdf = null;
            }

            return IO.Path.GetFileName(reportPdf);
        }
        //p6-102 Add photo -start Commented Dileep19092024 fix the image bug issue 
        //private Image AttachImageToPdf(PdfDocument pdfDocument, int index, string imagePath)
        //{
        //    var pageSize = new PageSize(pdfDocument.GetFirstPage().GetPageSize());
        //    pdfDocument.AddNewPage(index, pageSize);
        //    var imageData = ImageDataFactory.Create(imagePath);
        //    var image = new Image(imageData);
        //    bool rotateImage = image.GetImageWidth() > image.GetImageHeight();
        //    bool scaleImage = image.GetImageWidth() > MAX_IMAGE_WIDTH || image.GetImageHeight() > MAX_IMAGE_HEIGHT;

        //    if (rotateImage)
        //    {
        //        image.SetRotationAngle(ROTATION_ANGLE_DEG * (Math.PI / 180));
        //        if (scaleImage)
        //            image.ScaleToFit(PageSize.A4.GetHeight() * SCALE_FACTOR, PageSize.A4.GetWidth() * SCALE_FACTOR);
        //    }
        //    else
        //    {
        //        if (scaleImage)
        //            image.ScaleToFit(PageSize.A4.GetWidth() * SCALE_FACTOR, PageSize.A4.GetHeight() * SCALE_FACTOR);
        //    }

        //    var bottom = rotateImage ? pageSize.GetTop() : pageSize.GetTop() - image.GetImageScaledHeight();
        //    image.SetFixedPosition(index, 0, bottom);
        //    return image;
        //}
        //p6-102 Add photo -end

        /* New code for Image in Pdf Dileep 19092024 Start*/
        private void AttachImageToPdf(PdfDocument pdfDocument, Document doc, int index, string imagePath)
        {
            if (pdfDocument == null)
                throw new ArgumentNullException(nameof(pdfDocument), "PdfDocument cannot be null");

            if (pdfDocument.GetNumberOfPages() == 0)
                pdfDocument.AddNewPage(PageSize.A4); // Ensure there is at least one page

            var pageSize = PageSize.A4;

            // Create a new page at the specified index with A4 size
            pdfDocument.AddNewPage(index, PageSize.A4);

            var imageData = ImageDataFactory.Create(imagePath);
            var image = new Image(imageData);

            // Constants for maximum size and rotation
            const float MAX_IMAGE_WIDTH = 500f;
            const float MAX_IMAGE_HEIGHT = 600f;
            const float ROTATION_ANGLE_DEG = 90;
            const float SCALE_FACTOR = 0.9f;

            bool rotateImage = image.GetImageWidth() > image.GetImageHeight();
            bool scaleImage = image.GetImageWidth() > MAX_IMAGE_WIDTH || image.GetImageHeight() > MAX_IMAGE_HEIGHT;

            // Adjust image scaling and rotation based on its dimensions
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

            // Calculate the X and Y position for the image based on its size
            float imageX, imageY;

            if (rotateImage)
            {
                // For rotated image, position is calculated using height as width and vice versa
                imageX = (pageSize.GetWidth() - image.GetImageScaledHeight()) / 2; // Center horizontally
                imageY = (pageSize.GetHeight() - image.GetImageScaledWidth()) / 2; // Center vertically
            }
            else
            {
                // For non-rotated image
                imageX = (pageSize.GetWidth() - image.GetImageScaledWidth()) / 2; // Center horizontally
                imageY = (pageSize.GetHeight() - image.GetImageScaledHeight()) / 2; // Center vertically
            }

            // Set the image position on the PDF page
            image.SetFixedPosition(index, imageX, imageY);

            // Create a Paragraph to show the file name at the top of the page
            var fileName = System.IO.Path.GetFileName(imagePath);
            var fileNameParagraph = new Paragraph($"File Name: {fileName}")
                .SetFontColor(WebColors.GetRGBColor("#000000")) // Set font color (black)
                .SetFontSize(12) // Set font size
                .SetTextAlignment(TextAlignment.CENTER) // Center the text
                .SetFixedPosition(index, 0, pageSize.GetTop() - 30, pageSize.GetWidth()); // Position at the top of the page

            // Add the file name paragraph above the image
            doc.Add(fileNameParagraph);

            // Add the image to the document
            doc.Add(image);
        }
        /* New code for Image in Pdf Dileep 19092024 end*/
        private string GetReportPdfFilePath(ClientSiteLogBook clientsiteLogBook, string version)
        {
            var reportPdfPath = IO.Path.Combine(_reportRootDir, REPORT_DIR, $"{clientsiteLogBook.Date:yyyyMMdd} - Daily Guard Log - {FileNameHelper.GetSanitizedFileNamePart(clientsiteLogBook.ClientSite.Name)} - {version}.pdf");

            if (IO.File.Exists(reportPdfPath))
                IO.File.Delete(reportPdfPath);

            return reportPdfPath;
        }

        private Table CreateCustomFieldAndPatrolCarLogsTable(List<CustomFieldLog> customFieldLogs, List<PatrolCarLog> patrolCarLogs, List<MobileCrowdControlReportData> crowdControlLogs)
        {
            // Old Method not used now 06-01-2026
            var addlLogsTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 })).UseAllAvailableWidth().SetMarginBottom(5);

            var tableLeft = CreateCustomFieldLogsTable(customFieldLogs);
            var cellLeft = new Cell()
                   .SetBorder(Border.NO_BORDER)
                   .SetPaddingLeft(0)
                   .SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE))
                   .Add(tableLeft);
            addlLogsTable.AddCell(cellLeft);

            var tableRight = CreatePatrolCarLogsTable(patrolCarLogs);
            var cellRight = new Cell()
                   .SetBorder(Border.NO_BORDER)
                   .SetPaddingRight(0)
                   .SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE))
                   .Add(tableRight);
            addlLogsTable.AddCell(cellRight);

            return addlLogsTable;

        }

        private Table CreateCustomFieldCrowdControlLogsAndPatrolCarLogsTable(List<CustomFieldLog> customFieldLogs, List<PatrolCarLog> patrolCarLogs, List<MobileCrowdControlReportData> crowdControlLogs)
        {
            // Parent table: 2 columns (Left | Right)
            var addlLogsTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 })).UseAllAvailableWidth().SetMarginBottom(5);

            /* ---------------- LEFT CELL (STACKED ROWS) ---------------- */

            // Nested table for left cell (1 column, multiple rows)
            var leftInnerTable = new Table(1).UseAllAvailableWidth();

            // Row 1: Custom Field Logs (only if not empty)
            if (customFieldLogs != null && customFieldLogs.Any())
            {
                var customFieldTable = CreateCustomFieldLogsTable(customFieldLogs);

                leftInnerTable.AddCell(new Cell()
                        .SetBorder(Border.NO_BORDER).SetPadding(0)
                        .Add(customFieldTable)
                );
            }

            // Crowd Control Logs (row depends on CustomFieldLogs existence)
            if (crowdControlLogs != null && crowdControlLogs.Any())
            {
                var crowdControlTable = CreateCrowdControlLogsTable(crowdControlLogs);

                leftInnerTable.AddCell(new Cell()
                        .SetBorder(Border.NO_BORDER).SetPaddingTop(5).SetPaddingBottom(0)
                        .Add(crowdControlTable)
                );
            }

            //To add dummy table when both custom field and crowd control logs are null
            if (customFieldLogs == null && crowdControlLogs == null)
            {
                var customFieldTable = CreateCustomFieldLogsTable(customFieldLogs);

                leftInnerTable.AddCell(new Cell()
                        .SetBorder(Border.NO_BORDER).SetPadding(0)
                        .Add(customFieldTable)
                );
            }

            // Wrap nested left table into parent cell
            addlLogsTable.AddCell(new Cell()
                    .SetBorder(Border.NO_BORDER).SetPaddingLeft(0).SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE))
                    .Add(leftInnerTable)
            );

            /* ---------------- RIGHT CELL ---------------- */

            var patrolCarTable = CreatePatrolCarLogsTable(patrolCarLogs);

            addlLogsTable.AddCell(new Cell()
                    .SetBorder(Border.NO_BORDER).SetPaddingRight(0).SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE))
                    .Add(patrolCarTable)
            );

            return addlLogsTable;
        }



        private Table CreateCustomFieldLogsTable(List<CustomFieldLog> customFieldLogs)
        {
            if (!customFieldLogs.Any())
            {
                return new Table(1);
            }

            var timeSlotGroups = customFieldLogs.GroupBy(z => z.ClientSiteCustomField.TimeSlot);
            var fieldNames = customFieldLogs.Select(x => x.ClientSiteCustomField.Name).Distinct();
            var rows = new List<Dictionary<string, string>>();
            foreach (var group in timeSlotGroups)
            {
                var columns = new Dictionary<string, string>();
                if (!columns.ContainsKey(group.Key))
                {
                    columns.Add("timeSlot", group.Key);
                }

                foreach (var fieldName in fieldNames)
                {
                    var fieldValue = group.SingleOrDefault(z => z.ClientSiteCustomField.Name == fieldName)?.DayValue;
                    columns.Add(fieldName, fieldValue);
                }
                rows.Add(columns);
            }

            // count (total column count) = no of fields + time slot field
            var customFieldLogsTable = new Table(fieldNames.Count() + 1).UseAllAvailableWidth();

            var cellForColumnHeadingForTimeSlot = new Cell()
                      .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                      .SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY_DARK))
                      .Add(new Paragraph("Time Slot")
                      .SetFontSize(CELL_FONT_SIZE));
            customFieldLogsTable.AddCell(cellForColumnHeadingForTimeSlot);
            foreach (var coloumnHeadingForCustomFieldName in customFieldLogs.Select(x => x.ClientSiteCustomField.Name).Distinct())
            {
                var cellForColumnHeadingCustomFieldName = new Cell()
                   .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                   .SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY_DARK))
                   .Add(new Paragraph(coloumnHeadingForCustomFieldName)
                   .SetFontSize(CELL_FONT_SIZE));
                customFieldLogsTable.AddCell(cellForColumnHeadingCustomFieldName);
            }

            foreach (var row in rows)
            {
                var cellForTimeSlotField = new Cell()
                   .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                   .SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE))
                   .Add(new Paragraph(row["timeSlot"])
                   .SetFontSize(CELL_FONT_SIZE));
                customFieldLogsTable.AddCell(cellForTimeSlotField);
                foreach (var field in row)
                {
                    if (field.Key != "timeSlot")
                    {
                        var cellForFields = new Cell()
                        .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                        .SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE))
                        .SetFontSize(CELL_FONT_SIZE)
                        .Add(new Paragraph(field.Value ?? string.Empty).SetTextAlignment(TextAlignment.RIGHT));
                        customFieldLogsTable.AddCell(cellForFields);
                    }
                }
            }

            return customFieldLogsTable;
        }

        private Table CreateCrowdControlLogsTable(List<MobileCrowdControlReportData> crowdControlLogs)
        {
            if (!crowdControlLogs.Any())
            {
                return new Table(1);
            }

            var fieldNames = crowdControlLogs.Where(x => x.ColHeaderName != "Head Count").Select(x => x.ColHeaderName).Distinct().ToList();
            var rows = new List<Dictionary<string, string>>();

            var columns = new Dictionary<string, string>();
            columns.Add("timeSlot", "23:59");

            foreach (var fieldName in fieldNames)
            {
                var fieldValue = crowdControlLogs.SingleOrDefault(z => z.ColHeaderName == fieldName)?.CellValue;
                columns.Add(fieldName, fieldValue);
            }
            //rows.Add(columns);

            fieldNames.Add("Head Count");
            var totfieldValue = crowdControlLogs.SingleOrDefault(z => z.ColHeaderName == "Head Count")?.CellValue;
            columns.Add("Head Count", totfieldValue);
            rows.Add(columns);

            // count (total column count) = no of fields + time slot field
            var crowdControlLogsTable = new Table(fieldNames.Count() + 1).UseAllAvailableWidth();

            var cellForColumnHeadingForTimeSlot = new Cell()
                      .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                      .SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY_DARK))
                      .Add(new Paragraph("Time Slot")
                      .SetFontSize(CELL_FONT_SIZE));
            crowdControlLogsTable.AddCell(cellForColumnHeadingForTimeSlot);
            foreach (var coloumnHeadingForCustomFieldName in crowdControlLogs.Select(x => x.ColHeaderName).Distinct())
            {
                var cellForColumnHeadingCustomFieldName = new Cell()
                   .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                   .SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY_DARK))
                   .Add(new Paragraph(coloumnHeadingForCustomFieldName)
                   .SetFontSize(CELL_FONT_SIZE));
                crowdControlLogsTable.AddCell(cellForColumnHeadingCustomFieldName);
            }

            foreach (var row in rows)
            {
                var cellForTimeSlotField = new Cell()
                   .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                   .SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE))
                   .Add(new Paragraph(row["timeSlot"])
                   .SetFontSize(CELL_FONT_SIZE));
                crowdControlLogsTable.AddCell(cellForTimeSlotField);
                foreach (var field in row)
                {
                    if (field.Key != "timeSlot")
                    {
                        var cellForFields = new Cell()
                        .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                        .SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE))
                        .SetFontSize(CELL_FONT_SIZE)
                        .Add(new Paragraph(field.Value ?? string.Empty).SetTextAlignment(TextAlignment.CENTER));
                        crowdControlLogsTable.AddCell(cellForFields);
                    }
                }
            }

            return crowdControlLogsTable;
        }

        private Table CreatePatrolCarLogsTable(List<PatrolCarLog> patrolCarLogs)
        {
            if (!patrolCarLogs.Any())
            {
                return new Table(1);
            }

            var patrolCarLogTable = new Table(UnitValue.CreatePercentArray(new float[] { 80, 20 })).UseAllAvailableWidth();

            foreach (var patrolCarLog in patrolCarLogs.OrderBy(x=> x.ClientSitePatrolCar.Model))
            {
                var cellForPatrolCar = new Cell()
                    .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                    .SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY_DARK))
                    .Add(new Paragraph(patrolCarLog.ClientSitePatrolCar.Model + " - " + patrolCarLog.ClientSitePatrolCar.Rego + " - KM @ 00:01 HRS")
                    .SetFontSize(CELL_FONT_SIZE));
                patrolCarLogTable.AddCell(cellForPatrolCar);

                var cellForMileage = new Cell()
                   .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                   .SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE))
                   .Add(new Paragraph(patrolCarLog.MileageText)
                   .SetFontSize(CELL_FONT_SIZE)
                   .SetTextAlignment(TextAlignment.RIGHT));
                patrolCarLogTable.AddCell(cellForMileage);
            }

            return patrolCarLogTable;
        }

        private Table CreateReportHeader(ClientSite clientSite, string version)
        {
            var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 25, 45, 30 })).UseAllAvailableWidth();

            headerTable.AddCell(new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetBackgroundColor(WebColors.GetRGBColor(COLOR_NAVY_BLUE))
                .SetFontColor(WebColors.GetRGBColor(COLOR_WHITE))
                .SetFontSize(CELL_FONT_SIZE)
                .Add(new Paragraph(clientSite.SiteEmail ?? string.Empty)));

            headerTable.AddCell(new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetBackgroundColor(WebColors.GetRGBColor(COLOR_NAVY_BLUE))
                .SetFontColor(WebColors.GetRGBColor(COLOR_WHITE))
                .SetFontSize(CELL_FONT_SIZE * 1.5f)
                .Add(new Paragraph("Daily Shift Log Book"))
                .Add(new Paragraph(version).SetFontSize(CELL_FONT_SIZE)));

            var clientSiteWandNos = GetClientSiteWandNumbers(clientSite);
            headerTable.AddCell(new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetBackgroundColor(WebColors.GetRGBColor(COLOR_NAVY_BLUE))
                .SetFontColor(WebColors.GetRGBColor(COLOR_WHITE))
                .SetFontSize(CELL_FONT_SIZE)
                .Add(new Paragraph(clientSiteWandNos)));

            return headerTable;
        }

        private Table CreateReportHeaderForSmartWand(ClientSite clientSite, string version)
        {
            var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 25, 45, 30 })).UseAllAvailableWidth();

            headerTable.AddCell(new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetBackgroundColor(WebColors.GetRGBColor(COLOR_NAVY_BLUE))
                .SetFontColor(WebColors.GetRGBColor(COLOR_WHITE))
                .SetFontSize(CELL_FONT_SIZE)
                .Add(new Paragraph(clientSite.SiteEmail ?? string.Empty)));

            headerTable.AddCell(new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetBackgroundColor(WebColors.GetRGBColor(COLOR_NAVY_BLUE))
                .SetFontColor(WebColors.GetRGBColor(COLOR_WHITE))
                .SetFontSize(CELL_FONT_SIZE * 1.5f)
                .Add(new Paragraph("Smart Wand Strikes"))
                .Add(new Paragraph(version).SetFontSize(CELL_FONT_SIZE)));

            var clientSiteWandNos = GetClientSiteWandNumbers(clientSite);
            headerTable.AddCell(new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetBackgroundColor(WebColors.GetRGBColor(COLOR_NAVY_BLUE))
                .SetFontColor(WebColors.GetRGBColor(COLOR_WHITE))
                .SetFontSize(CELL_FONT_SIZE)
                .Add(new Paragraph(clientSiteWandNos)));

            return headerTable;
        }

        private string GetClientSiteWandNumbers(ClientSite clientSite)
        {
            var wandNumbers = new StringBuilder();
            if (!string.IsNullOrEmpty(clientSite.LandLine))
                wandNumbers.AppendLine($"Landline: {clientSite.LandLine}");

            var clientSiteWands = _clientSiteWandDataProvider.GetClientSiteSmartWands().Where(z => z.ClientSiteId == clientSite.Id);
            foreach (var wandInfo in clientSiteWands)
                wandNumbers.AppendLine($"{wandInfo.SmartWandId}: {wandInfo.PhoneNumber}");

            return wandNumbers.ToString();
        }

        private Table CreateGuardDetails(ClientSiteLogBook clientSiteLogBook)
        {
            var clientSite = clientSiteLogBook.ClientSite;
            var logDate = clientSiteLogBook.Date;
            var guardDetailGroup = _guardLoginDetailService.GetGuardDetailsByLogBookId(clientSiteLogBook.Id);

            var guardDetailsTable = new Table(UnitValue.CreatePercentArray(new float[] { 12, 9, 13, 9, 13, 9, 13, 9, 13 })).UseAllAvailableWidth();

            // first row
            guardDetailsTable.AddCell(new Cell().SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY_DARK)).SetFontSize(CELL_FONT_SIZE).Add(new Paragraph("Site:")).Add(new Paragraph("(Location)")));
            guardDetailsTable.AddCell(new Cell(1, 4).SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetFontSize(CELL_FONT_SIZE).Add(new Paragraph(clientSite.Name)).Add(new Paragraph(clientSite.Address ?? string.Empty)));
            guardDetailsTable.AddCell(new Cell(1, 2).SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY_DARK)).SetFontSize(CELL_FONT_SIZE).Add(new Paragraph("Date of Log:")));
            guardDetailsTable.AddCell(new Cell(1, 3).SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetFontSize(CELL_FONT_SIZE).SetTextAlignment(TextAlignment.CENTER).Add(new Paragraph($"{logDate.ToString("yyyy-MMM-dd-ddd").ToUpper()}")));

            var guardIndex = 0;
            foreach (var groupItem in guardDetailGroup)
            {
                var details = groupItem.OrderBy(z => z.OnDuty);
                var detailIndex = 0;
                guardDetailsTable.AddCell(new Cell().SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY_DARK)).SetFontSize(CELL_FONT_SIZE).Add(new Paragraph($"Guard {++guardIndex}\n({groupItem.First().SmartWandOrPosition})")));
                foreach (var item in details)
                {
                    detailIndex++;
                    if (detailIndex == 5)
                    {
                        // Break to next row, when a limit of 4 guards are reached
                        guardDetailsTable.AddCell(new Cell().SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY_DARK)).SetFontSize(CELL_FONT_SIZE).Add(new Paragraph("")));
                        detailIndex = 1;
                    }
                    guardDetailsTable.AddCell(new Cell().SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE)).SetFontSize(CELL_FONT_SIZE).Add(new Paragraph($"{item.OnDuty:HHmm}-{item.OffDuty:HHmm}")));
                    guardDetailsTable.AddCell(new Cell().SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetFontSize(CELL_FONT_SIZE).Add(new Paragraph(item.GuardName)));
                }

                while (detailIndex < 4)
                {
                    detailIndex++;
                    guardDetailsTable.AddCell(new Cell().SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE)).SetFontSize(CELL_FONT_SIZE).Add(new Paragraph("ADHOC-TOA")));
                    guardDetailsTable.AddCell(new Cell().SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetFontSize(CELL_FONT_SIZE).Add(new Paragraph("n/a")));
                }
            }

            return guardDetailsTable;
        }

        private Table CreateReportData(List<GuardLog> guardLog)
        {
            var reportDataTable = new Table(UnitValue.CreatePercentArray(new float[] { 10, 90, 4 })).UseAllAvailableWidth();

            reportDataTable.AddHeaderCell(new Cell().SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY_DARK)).Add(new Paragraph("Time").SetFontSize(CELL_FONT_SIZE)));
            reportDataTable.AddHeaderCell(new Cell().SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY_DARK)).Add(new Paragraph("Event / Notes with Guard Initials").SetFontSize(CELL_FONT_SIZE)));
            reportDataTable.AddHeaderCell(new Cell().SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY_DARK)).Add(new Paragraph("GPS").SetFontSize(CELL_FONT_SIZE)));

            foreach (var entry in guardLog)
            {
                //p6-102 Add photo -start
                var reportDataTable2 = new Table(UnitValue.CreatePercentArray(new float[] { 10, 90, 4 })).UseAllAvailableWidth();
                var guardlogImages = _guardLogDataProvider.GetGuardLogDocumentImaes(entry.Id);
                //Paragraph notesParagraphnew = new Paragraph("See attached file  ").SetFontSize(CELL_FONT_SIZE);
                Paragraph notesParagraphnew = new Paragraph().SetFontSize(CELL_FONT_SIZE);
                Paragraph notesParagraphImage = new Paragraph().SetFontSize(CELL_FONT_SIZE);
                foreach (var guardLogImage in guardlogImages)
                {
                    var reportDataTablenew = new Table(UnitValue.CreatePercentArray(new float[] { 10, 90, 4 })).UseAllAvailableWidth();
                    Paragraph notesParagraphnew1 = new Paragraph("See ").SetFontSize(CELL_FONT_SIZE);
                    if (guardLogImage.IsRearfile == true)
                    {

                        string baseUrl = guardLogImage.ImagePath;
                        string url = $"{baseUrl}";
                        string linkText = IO.Path.GetFileName(guardLogImage.ImagePath);


                        //var link = new Link(linkText, PdfAction.CreateURI(url))
                        //.SetFontColor(DeviceGray.BLACK)
                        //.SetFontColor(ColorConstants.BLUE);

                        //notesParagraphnew1.Add(link);
                        notesParagraphnew1.Add(linkText + " attached to this document");

                        notesParagraphnew.Add(notesParagraphnew1);



                    }
                    if (guardLogImage.IsTwentyfivePercentfile == true)
                    {
                        var logimage = new Image(ImageDataFactory.Create(guardLogImage.ImagePath))
                       .SetWidth(UnitValue.CreatePercentValue(27));
                        logimage.SetTextAlignment(TextAlignment.RIGHT);
                        logimage.SetMarginTop(10);
                        logimage.SetMarginLeft(10);
                        notesParagraphImage.Add(logimage);

                    }
                }
                //p6 - 102 Add photo -end
                //Commented the following line and for fixing the time issue 29/01/2024 dileep//
                //reportDataTable.AddCell(new Cell().SetKeepTogether(true).SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE)).Add(new Paragraph($"{entry.EventDateTime:HH:mm} hrs").SetFontSize(CELL_FONT_SIZE)));


                reportDataTable.AddCell(new Cell().SetKeepTogether(true).SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE)).Add(new Paragraph(getEventDateTimeUTCformat(entry)).SetFontSize(CELL_FONT_SIZE)));
                //Commented the following line and for fixing the time issue 29/01/2024 dileep end//
                //Commneted following line No Guard on duty shows error because entry.GuardLogin is null
                //var notes = entry.IrEntryType.HasValue ?
                //                    entry.Notes :
                //                    (string.IsNullOrEmpty(entry.GuardLogin.Guard.Initial) ? $"{entry.Notes} ;" : $"{entry.Notes} ;{entry.GuardLogin.Guard.Initial}");

                var notes = entry.IrEntryType.HasValue ? entry.Notes : $"{entry.Notes} ;{entry.GuardLogin?.Guard.Initial ?? string.Empty}";

                var bgColor = entry.IrEntryType.HasValue ?
                                ((entry.IrEntryType == IrEntryType.Normal) || (entry.IrEntryType == IrEntryType.Notification) ? COLOR_PALE_YELLOW : COLOR_PALE_RED) :
                                COLOR_WHITE;
                //Added To display GPS start
                var imagePath = "wwwroot/images/GPSImage.png";
                var siteImage = new Image(ImageDataFactory.Create(imagePath))
                .SetWidth(UnitValue.CreatePercentValue(27));
                siteImage.SetTextAlignment(TextAlignment.RIGHT);

                var paragraph = new Paragraph()
            .SetBorder(Border.NO_BORDER);
                if (entry.GpsCoordinates != null && entry.GpsCoordinates != "")
                {
                    paragraph.Add(siteImage);
                }

                var urlWithTargetBlank = $"https://www.google.com/maps?q={entry.GpsCoordinates}&target=_blank";
                var linkAction = PdfAction.CreateURI(urlWithTargetBlank);
                siteImage.SetAction(linkAction);

                Paragraph notesParagraph = new Paragraph(notes).SetFontSize(CELL_FONT_SIZE);
                if (entry.IsIRReportTypeEntry == true)
                {
                    var IncidentReport = entry.Notes + ".pdf";
                    string baseUrl = "https://c4istorage1.blob.core.windows.net/irfiles/";
                    string url = $"{baseUrl}{IncidentReport.Substring(0, 8)}/{IncidentReport}";
                    string linkText = "          click here";


                    var link = new Link(linkText, PdfAction.CreateURI(url))
                        .SetFontColor(DeviceGray.BLACK)
                        .SetFontColor(ColorConstants.BLUE);
                    notesParagraph.Add(link);
                }
                if (guardlogImages.Count > 0)
                {

                    reportDataTable.AddCell(new Cell()
                     .SetKeepTogether(true)
                     .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                     .SetBackgroundColor(WebColors.GetRGBColor(bgColor))
                     .Add(notesParagraph)
                     .Add(notesParagraphImage)
                     .Add(notesParagraphnew));
                }
                else
                {
                    reportDataTable.AddCell(new Cell()
                     .SetKeepTogether(true)
                     .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                     .SetBackgroundColor(WebColors.GetRGBColor(bgColor))
                     .Add(notesParagraph));
                }
                var cell = new Cell()
                .SetKeepTogether(true)
                .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                .SetBackgroundColor(WebColors.GetRGBColor(bgColor));

                cell.Add(paragraph);
                //Added To display GPS stop

                reportDataTable.AddCell(cell);
            }

            return reportDataTable;
        }

        /* new Function for add New Dateformat*/
        public string getEventDateTimeUTCformat(GuardLog entry)
        {
            if (entry.EventDateTimeLocal != null)
            {
                DateTime localTime = (DateTime)entry.EventDateTimeLocal;
                var dt = localTime.ToString("HH:mm") + " Hrs " + entry.EventDateTimeZoneShort;
                return dt;
            }
            else
            {
                CultureInfo cultureInfo = new CultureInfo("en-AU");
                DateTime eventDateTime = (DateTime)entry.EventDateTime;
                string formattedDateTime = eventDateTime.ToString("HH:mm", cultureInfo);

                return formattedDateTime + " Hrs";
            }


        }

        private Table CreateNotes(int clientSiteId)
        {
            var notesTable = new Table(UnitValue.CreatePercentArray(new float[] { 20, 80 })).UseAllAvailableWidth().SetMarginTop(15);
            var cellSiteImage = new Cell().SetBorder(Border.NO_BORDER);
            var imagePath = GetSiteImage(clientSiteId);

            if (!string.IsNullOrEmpty(imagePath))
            {
                try
                {
                    var imageData = ImageDataFactory.Create(imagePath);
                    if (imageData != null)
                    {
                        var siteImage = new Image(imageData)
                            .SetWidth(UnitValue.CreatePercentValue(90));

                        // Add the image to the cell
                        cellSiteImage.Add(siteImage);
                    }
                }
                catch (Exception ex)
                {

                }
            }
            notesTable.AddCell(cellSiteImage);

            var cellNotes = new Cell().SetBorder(Border.NO_BORDER);
            foreach (var line in GetNoteLines())
            {
                cellNotes.Add(new Paragraph(line).SetFontSize(CELL_FONT_SIZE * 0.8f));
            }
            notesTable.AddCell(cellNotes);
            notesTable.SetKeepTogether(true);
            return notesTable;
        }

        private string[] GetNoteLines()
        {
            return new string[]
            {
                "NOTE:\n\n This log book covers a 24 hour period only; if your shift spans overnight, then you use a separate report for the new day; " +
                "Please register meal breaks / rest (this is OH&S related and NOT " +
                "tied to renumeration so it does not need to be accurate).\n\n",

                "Entries are to assist guards with notes during their shift, and for " +
                "handover of the next guard. Do NOT write down anything related to sign-in or patrols times / frequency because they are automated and " +
                "recorded separately (and more accurate) and it is a waste of an entry.\n\n" +

                "Never leave more than 2 hours BLANK"
                //"Never leave more than 2 hours BLANK \n\n" +

                //"All incident reports are to be completed via  www.cws-ir.com ; of course mention them in here BUT only briefly describe them; All IR's" +
                //" need to be registered on the Smart WAND as an event (button 3A) unless generated from the Smart WAND (button 3B) as it will auto-register" +
                //" event.\n\n",

                //"Smart WAND is to be used on patrol for photos; such as critical infrastructure, alarm panel LED status, high risk areas, etc. – personal phone " +
                //"can be used as backup to reach KPI \n\n"+

                //"Use USB cable to dump all photos within the \"Daily Photo's\" Folder ; where needed you can mention the photo in" +
                //"the log (ie: Store X accessed, photo of log taken) \n\n" +

                //"24/7 sites should dump images (cut and paste) after midnight to dropbox – for each issued" +
                //" Smart WAND - even if images were created by another crew; Smart WAND should be EMPTY and CLEAR of images at midnight – ready for the next \"Day\")"
            };
        }

        private Table CreateFooter(int ClientTypeId)
        {

            Table footerTable;
            string clientLogo = IO.Path.Combine(_imageRootDir, "CWSLogoPdf.png"); // Default cws logo path

            var domain = _configDataProvider.GetSubDomainID(ClientTypeId);
            if (domain != null)
            {
                clientLogo = IO.Path.Combine(_subDomainImageRootDir, domain.Logo);
            }


            footerTable = new Table(UnitValue.CreatePercentArray(new float[] { 5, 20, 60, 15 })).UseAllAvailableWidth();

            var cwLogo = new Image(ImageDataFactory.Create(clientLogo)).SetHeight(20).SetHorizontalAlignment(HorizontalAlignment.CENTER);
            footerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER).Add(cwLogo));

            //var isoImage = new Image(ImageDataFactory.Create(IO.Path.Combine(_imageRootDir, "ISOv3.jpg"))).SetHeight(20).SetHorizontalAlignment(HorizontalAlignment.CENTER);
            //footerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER).Add(isoImage));

            // Add ISO image only if domain is not null, else add empty space
            if (domain == null)
            {
                var isoImage = new Image(ImageDataFactory.Create(IO.Path.Combine(_imageRootDir, "ISOv3.jpg"))).SetHeight(20).SetHorizontalAlignment(HorizontalAlignment.CENTER);
                footerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER).Add(isoImage));
            }
            else
            {
                // Add an empty cell for layout consistency
                footerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER).Add(new Paragraph("")));
            }

            footerTable.AddCell(new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontColor(WebColors.GetRGBColor(COLOR_GREY_DARK))
                .SetFontSize(CELL_FONT_SIZE * 0.8f)
                .Add(new Paragraph($"© {DateTime.Today:yyyy} - C4i System | Commercial-In-Confidence | [SEC=OFFICAL]")));

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

        private string GetSiteImage(int clientSiteId)
        {
            var clientSiteSetting = _clientDataProvider.GetClientSiteKpiSetting(clientSiteId);
            if (clientSiteSetting != null && !string.IsNullOrEmpty(clientSiteSetting.SiteImage))
                return $"{new Uri(_settings.KpiWebUrl)}{clientSiteSetting.SiteImage}";
            return string.Empty;
        }


        public string GeneratePdfReportForFusion(List<ClientSiteRadioChecksActivityStatus_History> funsionLog)
        {
            if (funsionLog == null || funsionLog.Count == 0)
            {
                return string.Empty;
            }

            string reportPdfPath = string.Empty;
            PdfDocument pdfDoc = null;

            try
            {
                var version = "v" + Assembly.GetExecutingAssembly().GetName().Version.ToString();
                var firstLog = funsionLog.FirstOrDefault();

                if (firstLog == null)
                {
                    return string.Empty;
                }

                reportPdfPath = IO.Path.Combine(_reportRootDir, REPORT_DIR, $"{firstLog.EventDateTime:yyyyMMdd} - Fusion Guard Log - {FileNameHelper.GetSanitizedFileNamePart(firstLog.SiteName)} - {version}.pdf");

                if (IO.File.Exists(reportPdfPath))
                {
                    IO.File.Delete(reportPdfPath);
                }

                int? clientSiteId = firstLog.ClientSiteId;
                /* WithClientType: this method reads ClientSite.ClientType.Id below, and the
                   plain overload does not Include either navigation. */
                var clientSiteLogBooks = _clientDataProvider.GetClientSiteLogBooksWithClientType(clientSiteId.Value, LogBookType.DailyGuardLog, firstLog.EventDateTime.Date, firstLog.EventDateTime.Date);

                if (clientSiteLogBooks == null || !clientSiteLogBooks.Any())
                {
                    return string.Empty;
                }

                pdfDoc = new PdfDocument(new PdfWriter(reportPdfPath));
                pdfDoc.SetDefaultPageSize(PageSize.A4);
                var doc = new Document(pdfDoc);
                doc.SetMargins(15f, 30f, 40f, 30f);

                var headerTable = CreateReportHeader(clientSiteLogBooks.FirstOrDefault().ClientSite, version);
                doc.Add(headerTable);

                doc.Add(new Paragraph("On-Duty Guard Details")
                    .SetFontColor(WebColors.GetRGBColor(COLOR_NAVY_BLUE))
                    .SetFontSize(CELL_FONT_SIZE * 1.5f)
                    .SetMarginTop(5));

                var guardDetails = CreateGuardDetails(clientSiteLogBooks.FirstOrDefault());
                doc.Add(guardDetails);

                doc.Add(new Paragraph("Log Book")
                    .SetFontColor(WebColors.GetRGBColor(COLOR_NAVY_BLUE))
                    .SetFontSize(CELL_FONT_SIZE * 1.5f)
                    .SetMarginTop(5));

                var customFieldLogs = _guardLogDataProvider.GetCustomFieldLogs(clientSiteLogBooks.FirstOrDefault().Id).ToList();
                var patrolCarLogs = _guardLogDataProvider.GetPatrolCarLogs(clientSiteLogBooks.FirstOrDefault().Id).ToList();
                var crowdControlLogs = _guardLogDataProvider.GetMobileCrowdControlLogs(clientSiteLogBooks.FirstOrDefault().ClientSite.Id, clientSiteLogBooks.FirstOrDefault().Id, clientSiteLogBooks.FirstOrDefault().Date, clientSiteLogBooks.FirstOrDefault().Date).ToList();
                if (customFieldLogs.Any() || patrolCarLogs.Any() || crowdControlLogs.Any())
                {
                    //var addlFieldLogs = CreateCustomFieldAndPatrolCarLogsTable(customFieldLogs, patrolCarLogs, crowdControlLogs);
                    var addlFieldLogs = CreateCustomFieldCrowdControlLogsAndPatrolCarLogsTable(customFieldLogs, patrolCarLogs, crowdControlLogs);
                    doc.Add(addlFieldLogs);
                }

                var tableData = CreateReportDataForFusion(funsionLog);
                if (tableData != null)
                {
                    doc.Add(tableData);
                }

                var logNotes = CreateNotes(clientSiteLogBooks.FirstOrDefault().ClientSite.Id);
                if (logNotes != null)
                {
                    doc.Add(logNotes);
                }

                int _clientTypeId = clientSiteLogBooks.FirstOrDefault().ClientSite.ClientType.Id;
                var footer = CreateFooter(_clientTypeId);
                pdfDoc.AddEventHandler(PdfDocumentEvent.END_PAGE, new TableFooterEventHandler(footer));

                doc.Close();
                pdfDoc.Close();

                return IO.Path.GetFileName(reportPdfPath);
            }
            catch (Exception)
            {
                if (pdfDoc != null && !pdfDoc.IsClosed())
                {
                    pdfDoc.Close();
                }

                /* The half-written PDF is already on disk. The caller only deletes files it
                   was handed a name for, so without this it stays in Pdf/Output forever. */
                if (!string.IsNullOrEmpty(reportPdfPath) && IO.File.Exists(reportPdfPath))
                {
                    try { IO.File.Delete(reportPdfPath); } catch { /* best effort - never mask the real error */ }
                }

                /* Rethrow instead of returning string.Empty. Swallowing here is what turned a
                   NullReferenceException into a 22 byte zip reported to the user as success.
                   CreateLogBookReportsFusion catches per day, so one bad day still cannot
                   cost the rest of the range. */
                throw;
            }
        }

        private Table CreateReportDataForFusion(List<ClientSiteRadioChecksActivityStatus_History> guardLog)
        {
            var reportDataTable = new Table(UnitValue.CreatePercentArray(new float[] { 6, 75, 4, 15, 15, 2 })).UseAllAvailableWidth();

            reportDataTable.AddHeaderCell(new Cell().SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY_DARK)).Add(new Paragraph("Time").SetFontSize(CELL_FONT_SIZE)));
            reportDataTable.AddHeaderCell(new Cell().SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY_DARK)).Add(new Paragraph("Event / Notes").SetFontSize(CELL_FONT_SIZE)));
            reportDataTable.AddHeaderCell(new Cell().SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY_DARK)).Add(new Paragraph("Source").SetFontSize(CELL_FONT_SIZE)));
            reportDataTable.AddHeaderCell(new Cell().SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY_DARK)).Add(new Paragraph("Client Site").SetFontSize(CELL_FONT_SIZE)));
            reportDataTable.AddHeaderCell(new Cell().SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY_DARK)).Add(new Paragraph("Guard").SetFontSize(CELL_FONT_SIZE)));
            reportDataTable.AddHeaderCell(new Cell().SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY_DARK)).Add(new Paragraph("GPS").SetFontSize(CELL_FONT_SIZE)));

            foreach (var entry in guardLog)
            {

                try
                {
                    //Commented the following line and for fixing the time issue 29/01/2024 dileep//
                    //reportDataTable.AddCell(new Cell().SetKeepTogether(true).SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE)).Add(new Paragraph($"{entry.EventDateTime:HH:mm} hrs").SetFontSize(CELL_FONT_SIZE)));


                    reportDataTable.AddCell(new Cell().SetKeepTogether(true)
                        .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                        .SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE))
                        .Add(new Paragraph(getEventDateTimeUTCformat(entry)).SetFontSize(CELL_FONT_SIZE)));
                    //Commented the following line and for fixing the time issue 29/01/2024 dileep end//
                    //var notes = entry.IrEntryType.HasValue ?
                    //                    entry.Notes :
                    //                    (string.IsNullOrEmpty(entry.GuardLogin.Guard.Initial) ? $"{entry.Notes} ;" : $"{entry.Notes} ;{entry.GuardLogin.Guard.Initial}");
                    var bgColor = entry.IrEntryType.HasValue ?
                                    ((entry.IrEntryType == IrEntryType.Normal) || (entry.IrEntryType == IrEntryType.Notification) ? COLOR_PALE_YELLOW : COLOR_PALE_RED) :
                                    COLOR_WHITE;
                    //Added To display GPS start
                    var notes = string.IsNullOrEmpty(entry.Notes) ? string.Empty : entry.Notes;
                    // Determine the row background color based on conditions
                    //var bgColor = COLOR_WHITE;
                    //if (string.IsNullOrEmpty(entry.GuardName))
                    //{
                    //    bgColor = COLOR_PALE_YELLOW;
                    //}
                    //if (entry.Notes.Contains("Duress Alarm Activated"))
                    //{
                    //    bgColor = COLOR_PALE_RED;
                    //}
                    //    var imagePath = "wwwroot/images/GPSImage.png";
                    //    var siteImage = new Image(ImageDataFactory.Create(imagePath))
                    //    .SetWidth(UnitValue.CreatePercentValue(27));
                    //    siteImage.SetTextAlignment(TextAlignment.RIGHT);

                    var paragraph = new Paragraph().SetFontSize(CELL_FONT_SIZE);
                    paragraph.Add(entry.ActivityType);
                    //if (entry.GpsCoordinates != null && entry.GpsCoordinates != "")
                    //{
                    //    paragraph.Add(siteImage);
                    //}

                    //var urlWithTargetBlank = $"https://www.google.com/maps?q={entry.GpsCoordinates}&target=_blank";
                    //var linkAction = PdfAction.CreateURI(urlWithTargetBlank);
                    //siteImage.SetAction(linkAction);

                    Paragraph notesParagraph = new Paragraph(notes).SetFontSize(CELL_FONT_SIZE);
                    if (entry.ActivityType == "IR")
                    {
                        var IncidentReport = entry.Notes + ".pdf";
                        string baseUrl = "https://c4istorage1.blob.core.windows.net/irfiles/";
                        string url = $"{baseUrl}{IncidentReport.Substring(0, 8)}/{IncidentReport}";
                        string linkText = "          click here";


                        var link = new Link(linkText, PdfAction.CreateURI(url))
                            .SetFontColor(DeviceGray.BLACK)
                            .SetFontColor(ColorConstants.BLUE);
                        notesParagraph.Add(link);
                    }
                    Paragraph notesParagraphnew = new Paragraph().SetFontSize(CELL_FONT_SIZE);
                    Paragraph notesParagraphImage = new Paragraph().SetFontSize(CELL_FONT_SIZE);
                    if (entry.LBId != null)
                    {
                        var guardlogImages = _guardLogDataProvider.GetGuardLogDocumentImaes((int)entry.LBId);

                        foreach (var guardLogImage in guardlogImages)
                        {
                            var reportDataTablenew = new Table(UnitValue.CreatePercentArray(new float[] { 10, 90, 4 })).UseAllAvailableWidth();
                            Paragraph notesParagraphnew1 = new Paragraph("See ").SetFontSize(CELL_FONT_SIZE);
                            if (guardLogImage.IsRearfile == true)
                            {

                                string baseUrl = guardLogImage.ImagePath;
                                string url = $"{baseUrl}";
                                string linkText = IO.Path.GetFileName(guardLogImage.ImagePath);


                                //var link = new Link(linkText, PdfAction.CreateURI(url))
                                //.SetFontColor(DeviceGray.BLACK)
                                //.SetFontColor(ColorConstants.BLUE);

                                //notesParagraphnew1.Add(link);
                                notesParagraphnew1.Add(linkText + " attached to this document");

                                notesParagraphnew.Add(notesParagraphnew1);



                            }
                            if (guardLogImage.IsTwentyfivePercentfile == true)
                            {
                                var logimage = new Image(ImageDataFactory.Create(guardLogImage.ImagePath))
                               .SetWidth(UnitValue.CreatePercentValue(27));
                                logimage.SetTextAlignment(TextAlignment.RIGHT);
                                logimage.SetMarginTop(10);
                                logimage.SetMarginLeft(10);
                                notesParagraphImage.Add(logimage);

                            }
                        }
                    }
                    reportDataTable.AddCell(new Cell()
                     .SetKeepTogether(true)
                     .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                     .SetBackgroundColor(WebColors.GetRGBColor(bgColor))
                     .Add(notesParagraph)
                     .Add(notesParagraphnew)
                     .Add(notesParagraphImage)
                     );




                    var cell = new Cell()
                    .SetKeepTogether(true)
                    .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                    .SetBackgroundColor(WebColors.GetRGBColor(bgColor));

                    cell.Add(paragraph);
                    //Added To display GPS stop

                    reportDataTable.AddCell(cell);

                    // Add "Site Name" cell
                    var siteName = entry.SiteName ?? "N/A"; // Replace with the actual property for site name
                    reportDataTable.AddCell(new Cell()
                        .SetKeepTogether(true)
                        .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                        .SetBackgroundColor(WebColors.GetRGBColor(bgColor))
                        .Add(new Paragraph(siteName).SetFontSize(CELL_FONT_SIZE)));

                    reportDataTable.AddCell(new Cell()
                    .SetKeepTogether(true)
                    .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                    .SetBackgroundColor(WebColors.GetRGBColor(bgColor))
                    .Add(new Paragraph(string.IsNullOrEmpty(entry.GuardName) ? "Admin" : entry.GuardName).SetFontSize(CELL_FONT_SIZE)));

                    if (!string.IsNullOrEmpty(entry.gpsCoordinates))
                    {
                        //var imagePath = "wwwroot/images/GPSImage.png";
                        //var siteImage = new Image(ImageDataFactory.Create(imagePath))
                        //    .SetWidth(UnitValue.CreatePercentValue(25)); // Adjust percentage width for the image
                        var imagePath = "wwwroot/images/GPSImage.png";
                        var siteImage = new Image(ImageDataFactory.Create(imagePath))
                            .SetWidth(UnitValue.CreatePercentValue(50)) // Adjusted width to 40% for enlargement
                            .SetHeight(UnitValue.CreatePercentValue(50)) // Adjusted height to 40% for proportional scaling
                            .SetTextAlignment(TextAlignment.RIGHT);

                        var urlWithTargetBlank = $"https://www.google.com/maps?q={entry.gpsCoordinates}";
                        siteImage.SetAction(PdfAction.CreateURI(urlWithTargetBlank));

                        var paragraphGPS = new Paragraph()
                            .Add(siteImage)
                            .SetTextAlignment(TextAlignment.RIGHT); // Align content properly

                        reportDataTable.AddCell(new Cell()
                            .SetKeepTogether(true)
                            .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                            .SetBackgroundColor(WebColors.GetRGBColor(bgColor))
                            .Add(paragraphGPS));
                    }
                    else
                    {
                        reportDataTable.AddCell(new Cell()
                            .SetKeepTogether(true)
                            .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                            .SetBackgroundColor(WebColors.GetRGBColor(bgColor))
                            .Add(new Paragraph(" ").SetFontSize(CELL_FONT_SIZE)));
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error adding row: {ex.Message}");
                }
            }

            return reportDataTable;
        }



        public Table CreateReportDataForFusionWithoutSiteName(List<ClientSiteRadioChecksActivityStatus_History> guardLog)
        {
            var reportDataTable = new Table(UnitValue.CreatePercentArray(new float[] { 6, 75, 4, 15, 2 })).UseAllAvailableWidth();

            reportDataTable.AddHeaderCell(new Cell().SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY_DARK)).Add(new Paragraph("Time").SetFontSize(CELL_FONT_SIZE)));
            reportDataTable.AddHeaderCell(new Cell().SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY_DARK)).Add(new Paragraph("Event / Notes").SetFontSize(CELL_FONT_SIZE)));
            reportDataTable.AddHeaderCell(new Cell().SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY_DARK)).Add(new Paragraph("Source").SetFontSize(CELL_FONT_SIZE)));
            //reportDataTable.AddHeaderCell(new Cell().SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY_DARK)).Add(new Paragraph("Client Site").SetFontSize(CELL_FONT_SIZE)));
            reportDataTable.AddHeaderCell(new Cell().SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY_DARK)).Add(new Paragraph("Guard").SetFontSize(CELL_FONT_SIZE)));
            reportDataTable.AddHeaderCell(new Cell().SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY_DARK)).Add(new Paragraph("GPS").SetFontSize(CELL_FONT_SIZE)));

            foreach (var entry in guardLog)
            {

                try
                {
                    //Commented the following line and for fixing the time issue 29/01/2024 dileep//
                    //reportDataTable.AddCell(new Cell().SetKeepTogether(true).SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE)).Add(new Paragraph($"{entry.EventDateTime:HH:mm} hrs").SetFontSize(CELL_FONT_SIZE)));


                    reportDataTable.AddCell(new Cell().SetKeepTogether(true).SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE)).Add(new Paragraph(getEventDateTimeUTCformat(entry)).SetFontSize(CELL_FONT_SIZE)));
                    //Commented the following line and for fixing the time issue 29/01/2024 dileep end//
                    //var notes = entry.IrEntryType.HasValue ?
                    //                    entry.Notes :
                    //                    (string.IsNullOrEmpty(entry.GuardLogin.Guard.Initial) ? $"{entry.Notes} ;" : $"{entry.Notes} ;{entry.GuardLogin.Guard.Initial}");
                    var bgColor = entry.IrEntryType.HasValue ?
                                    ((entry.IrEntryType == IrEntryType.Normal) || (entry.IrEntryType == IrEntryType.Notification) ? COLOR_PALE_YELLOW : COLOR_PALE_RED) :
                                    COLOR_WHITE;
                    //Added To display GPS start
                    var notes = string.IsNullOrEmpty(entry.Notes) ? string.Empty : entry.Notes;
                    // Determine the row background color based on conditions
                    //var bgColor = COLOR_WHITE;
                    //if (string.IsNullOrEmpty(entry.GuardName))
                    //{
                    //    bgColor = COLOR_PALE_YELLOW;
                    //}
                    //if (entry.Notes.Contains("Duress Alarm Activated"))
                    //{
                    //    bgColor = COLOR_PALE_RED;
                    //}
                    //    var imagePath = "wwwroot/images/GPSImage.png";
                    //    var siteImage = new Image(ImageDataFactory.Create(imagePath))
                    //    .SetWidth(UnitValue.CreatePercentValue(27));
                    //    siteImage.SetTextAlignment(TextAlignment.RIGHT);

                    var paragraph = new Paragraph().SetFontSize(CELL_FONT_SIZE);
                    paragraph.Add(entry.ActivityType);
                    //if (entry.GpsCoordinates != null && entry.GpsCoordinates != "")
                    //{
                    //    paragraph.Add(siteImage);
                    //}

                    //var urlWithTargetBlank = $"https://www.google.com/maps?q={entry.GpsCoordinates}&target=_blank";
                    //var linkAction = PdfAction.CreateURI(urlWithTargetBlank);
                    //siteImage.SetAction(linkAction);

                    Paragraph notesParagraph = new Paragraph(notes).SetFontSize(CELL_FONT_SIZE);
                    if (entry.ActivityType == "IR")
                    {
                        var IncidentReport = entry.Notes + ".pdf";
                        string baseUrl = "https://c4istorage1.blob.core.windows.net/irfiles/";
                        string url = $"{baseUrl}{IncidentReport.Substring(0, 8)}/{IncidentReport}";
                        string linkText = "          click here";


                        var link = new Link(linkText, PdfAction.CreateURI(url))
                            .SetFontColor(DeviceGray.BLACK)
                            .SetFontColor(ColorConstants.BLUE);
                        notesParagraph.Add(link);
                    }
                    Paragraph notesParagraphnew = new Paragraph().SetFontSize(CELL_FONT_SIZE);
                    Paragraph notesParagraphImage = new Paragraph().SetFontSize(CELL_FONT_SIZE);
                    if (entry.LBId != null)
                    {
                        var guardlogImages = _guardLogDataProvider.GetGuardLogDocumentImaes((int)entry.LBId);

                        foreach (var guardLogImage in guardlogImages)
                        {
                            var reportDataTablenew = new Table(UnitValue.CreatePercentArray(new float[] { 10, 90, 4 })).UseAllAvailableWidth();
                            Paragraph notesParagraphnew1 = new Paragraph("See ").SetFontSize(CELL_FONT_SIZE);
                            if (guardLogImage.IsRearfile == true)
                            {
                                string baseUrl = guardLogImage.ImagePath;
                                string url = $"{baseUrl}";
                                string linkText = IO.Path.GetFileName(guardLogImage.ImagePath);                                
                                notesParagraphnew1.Add(linkText + " attached to this document");
                                notesParagraphnew.Add(notesParagraphnew1);
                            }
                            if (guardLogImage.IsTwentyfivePercentfile == true)
                            {
                                var logimage = new Image(ImageDataFactory.Create(guardLogImage.ImagePath))
                               .SetWidth(UnitValue.CreatePercentValue(27));
                                logimage.SetTextAlignment(TextAlignment.RIGHT);
                                logimage.SetMarginTop(10);
                                logimage.SetMarginLeft(10);
                                notesParagraphImage.Add(logimage);
                            }
                        }
                    }

                    reportDataTable.AddCell(new Cell()
                     .SetKeepTogether(true)
                     .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                     .SetBackgroundColor(WebColors.GetRGBColor(bgColor))
                     .Add(notesParagraph)
                     .Add(notesParagraphnew)
                     .Add(notesParagraphImage)
                     );




                    var cell = new Cell()
                    .SetKeepTogether(true)
                    .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                    .SetBackgroundColor(WebColors.GetRGBColor(bgColor));

                    cell.Add(paragraph);
                    //Added To display GPS stop

                    reportDataTable.AddCell(cell);

                    // Add "Site Name" cell
                    //var siteName = entry.SiteName ?? "N/A"; // Replace with the actual property for site name
                    //reportDataTable.AddCell(new Cell()
                    //    .SetKeepTogether(true)
                    //    .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                    //    .SetBackgroundColor(WebColors.GetRGBColor(bgColor))
                    //    .Add(new Paragraph(siteName).SetFontSize(CELL_FONT_SIZE)));

                    reportDataTable.AddCell(new Cell()
                    .SetKeepTogether(true)
                    .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                    .SetBackgroundColor(WebColors.GetRGBColor(bgColor))
                    .Add(new Paragraph(string.IsNullOrEmpty(entry.GuardName) ? "Admin" : entry.GuardName).SetFontSize(CELL_FONT_SIZE)));

                    if (!string.IsNullOrEmpty(entry.gpsCoordinates))
                    {
                        //var imagePath = "wwwroot/images/GPSImage.png";
                        //var siteImage = new Image(ImageDataFactory.Create(imagePath))
                        //    .SetWidth(UnitValue.CreatePercentValue(25)); // Adjust percentage width for the image
                        var imagePath = "wwwroot/images/GPSImage.png";
                        var siteImage = new Image(ImageDataFactory.Create(imagePath))
                            .SetWidth(UnitValue.CreatePercentValue(50)) // Adjusted width to 40% for enlargement
                            .SetHeight(UnitValue.CreatePercentValue(50)) // Adjusted height to 40% for proportional scaling
                            .SetTextAlignment(TextAlignment.RIGHT);

                        var urlWithTargetBlank = $"https://www.google.com/maps?q={entry.gpsCoordinates}";
                        siteImage.SetAction(PdfAction.CreateURI(urlWithTargetBlank));

                        var paragraphGPS = new Paragraph()
                            .Add(siteImage)
                            .SetTextAlignment(TextAlignment.RIGHT); // Align content properly

                        reportDataTable.AddCell(new Cell()
                            .SetKeepTogether(true)
                            .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                            .SetBackgroundColor(WebColors.GetRGBColor(bgColor))
                            .Add(paragraphGPS));
                    }
                    else
                    {
                        reportDataTable.AddCell(new Cell()
                            .SetKeepTogether(true)
                            .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                            .SetBackgroundColor(WebColors.GetRGBColor(bgColor))
                            .Add(new Paragraph(" ").SetFontSize(CELL_FONT_SIZE)));
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error adding row: {ex.Message}");
                }
            }

            return reportDataTable;
        }

        public string getEventDateTimeUTCformat(ClientSiteRadioChecksActivityStatus_History entry)
        {
            try
            {

                //if (entry.EventDateTimeLocal != null)
                //{
                //    DateTime localTime = (DateTime)entry.EventDateTimeLocal;
                //    var dt = localTime.ToString("HH:mm") + " Hrs " + entry.EventDateTimeZoneShort;
                //    return dt;
                //}
                //else
                //{
                //    CultureInfo cultureInfo = new CultureInfo("en-AU");
                //    DateTime eventDateTime = (DateTime)entry.EventDateTime;
                //    string formattedDateTime = eventDateTime.ToString("HH:mm", cultureInfo);

                //    return formattedDateTime + " Hrs";
                //}

                return entry.EventDateTime.ToString("HH:mm") + " Hrs " + entry.EventDateTimeZoneShort;

            }
            catch (Exception ex)
            {
                return string.Empty;

            }


        }




        public string GeneratePdfReportFusion(int clientSiteLogBookId)
        {
            var clientsiteLogBook = _clientDataProvider.GetClientSiteLogBooks().SingleOrDefault(z => z.Id == clientSiteLogBookId);

            if (clientsiteLogBook == null)
                return string.Empty;

            var version = "v" + Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var reportPdf = GetReportPdfFilePathFusion(clientsiteLogBook, version);

            /* Fusion report only: a site can belong to a linked duress group (RCLinkedDuressMaster +
               RCLinkedDuressClientSites), and a duress raised on one member is actioned across the whole
               group, so the group's activity has to be readable in one place. Its logs are therefore
               merged into this site's Fusion PDF instead of being spread over one PDF per member site.
               getallClientSitesLinkedDuress() is the existing helper used by the duress flows in
               GuardLogDataProvider - it resolves the site's group and returns every member of it - so the
               grouping rule is defined in exactly one place. It returns an empty list when the site is in
               no group, in which case the array below stays exactly as before and the PDF is unchanged.
               Called once per PDF, outside the fusion loop in SiteLogUploadService, so it adds a single
               small lookup and no N+1. */
            var linkedDuressSiteIds = _guardLogDataProvider.getallClientSitesLinkedDuress(clientsiteLogBook.ClientSite.Id)
                                        ?.Select(z => z.ClientSiteId) ?? Enumerable.Empty<int>();

            /* Distinct() guards against the same site being listed more than once in the relationship
               table, and against the primary site coming back as a member of its own group. The primary
               id is prepended so it is always present even if the group lookup returns nothing. Duplicate
               ids could not duplicate log rows anyway (the provider filters with a single Contains(), so
               each history row matches once), but a clean list keeps the generated IN clause minimal. */
            int[] clientSiteId = new[] { clientsiteLogBook.ClientSite.Id }.Concat(linkedDuressSiteIds).Distinct().ToArray();

            /* Same-date restriction is unchanged and now covers the linked sites too: the existing
               clientsiteLogBook.Date is passed as both from and to, and the provider filters the history
               rows on EventDateTime.Date between them, so linked-site logs from any other date can never
               be pulled in. The provider returns one combined list ordered by EventDateTime, so the
               primary site's logs keep their existing chronological order with the linked entries
               interleaved by time. */
            var _guardLogs = _guardLogDataProvider.GetGuardFusionLogs(clientSiteId, clientsiteLogBook.Date, clientsiteLogBook.Date, false);
            //var _guardLogs = _guardLogDataProvider.ClientSiteRadioChecksActivityStatus_History(clientsiteLogBook.ClientSite.Id, clientsiteLogBook.Date);

            var pdfDoc = new PdfDocument(new PdfWriter(reportPdf));
            pdfDoc.SetDefaultPageSize(PageSize.A4);
            var doc = new Document(pdfDoc);
            doc.SetMargins(15f, 30f, 40f, 30f);

            var headerTable = CreateReportHeader(clientsiteLogBook.ClientSite, version);
            doc.Add(headerTable);

            doc.Add(new Paragraph("On-Duty Guard Details")
                .SetFontColor(WebColors.GetRGBColor(COLOR_NAVY_BLUE))
                .SetFontSize(CELL_FONT_SIZE * 1.5f)
                .SetMarginTop(5));

            var guardDetails = CreateGuardDetails(clientsiteLogBook);
            doc.Add(guardDetails);

            doc.Add(new Paragraph("Log Book")
                .SetFontColor(WebColors.GetRGBColor(COLOR_NAVY_BLUE))
                .SetFontSize(CELL_FONT_SIZE * 1.5f)
                .SetMarginTop(5));

            var customFieldLogs = _guardLogDataProvider.GetCustomFieldLogs(clientSiteLogBookId).ToList();
            var patrolCarLogs = _guardLogDataProvider.GetPatrolCarLogs(clientSiteLogBookId).ToList();
            var crowdControlLogs = _guardLogDataProvider.GetMobileCrowdControlLogs(clientsiteLogBook.ClientSite.Id, clientSiteLogBookId, clientsiteLogBook.Date, clientsiteLogBook.Date).ToList();
            if (customFieldLogs.Any() || patrolCarLogs.Any() || crowdControlLogs.Any())
            {
                //var addlFieldLogs = CreateCustomFieldAndPatrolCarLogsTable(customFieldLogs, patrolCarLogs, crowdControlLogs);
                var addlFieldLogs = CreateCustomFieldCrowdControlLogsAndPatrolCarLogsTable(customFieldLogs, patrolCarLogs, crowdControlLogs);
                doc.Add(addlFieldLogs);
            }

            var tableData = CreateReportDataForFusionWithoutSiteName(_guardLogs);
            doc.Add(tableData);

            var logNotes = CreateNotes(clientsiteLogBook.ClientSite.Id);
            doc.Add(logNotes);

            int _clientTypeId = clientsiteLogBook.ClientSite.ClientType.Id;
            var footer = CreateFooter(_clientTypeId);
            pdfDoc.AddEventHandler(PdfDocumentEvent.END_PAGE, new TableFooterEventHandler(footer));

            //p6-102 Add photo -start Commented 19092024 Dileep Start
            //var index = 1;
            //foreach (var entry in _guardLogs)
            //{


            //    var guardlogImages = _guardLogDataProvider.GetGuardLogDocumentImaes(entry.Id);
            //    Paragraph notesParagraphnew = new Paragraph("See attached file  ").SetFontSize(CELL_FONT_SIZE);

            //    foreach (var guardLogImage in guardlogImages)
            //    {

            //        if (guardLogImage.IsRearfile == true)
            //        {
            //            var docImage = new Document(pdfDoc);
            //            var image = AttachImageToPdf(pdfDoc, ++index, guardLogImage.ImagePath);
            //            doc.Add(image);



            //            var paraName = new Paragraph($"File Name: {IO.Path.GetFileName(guardLogImage.ImagePath)}").SetFontColor(WebColors.GetRGBColor(FONT_COLOR_BLACK));
            //            doc.Add(paraName);
            //            docImage.Close();
            //        }
            //    }
            //}
            //p6-102 Add photo -end end 
            //New Code fix the image bug start Dileep 

            int lastPageIndex = pdfDoc.GetNumberOfPages();
            var index = lastPageIndex + 1;
            foreach (var entry in _guardLogs)
            {
                if (entry.LBId == null)
                    continue;

                int imageDocid = entry.LBId ?? 0;
                var guardlogImages = _guardLogDataProvider.GetGuardLogDocumentImaes(imageDocid);
                foreach (var guardLogImage in guardlogImages)
                {

                    if (guardLogImage.IsRearfile == true)
                    {
                        try
                        {

                            AttachImageToPdf(pdfDoc, doc, index, guardLogImage.ImagePath);
                            index++;
                            // Add the image to the document
                            //doc.Add(image);
                            //var image = AttachImageToPdf(pdfDoc, index, guardLogImage.ImagePath);
                            //doc.Add(image);

                            //var paraName = new Paragraph($"File Name: {System.IO.Path.GetFileName(guardLogImage.ImagePath)}")
                            //    .SetFontColor(WebColors.GetRGBColor(FONT_COLOR_BLACK));
                            //doc.Add(paraName);

                        }
                        catch (Exception ex)
                        {
                            // Log exception or handle it as needed
                            Console.WriteLine($"Error attaching image: {ex.Message}");
                        }
                    }

                }
            }

            //New Code fix the image bug end 
            doc.Close();
            pdfDoc.Close();

            return IO.Path.GetFileName(reportPdf);
        }


        private string GetReportPdfFilePathFusion(ClientSiteLogBook clientsiteLogBook, string version)
        {
            var reportPdfPath = IO.Path.Combine(_reportRootDir, REPORT_DIR, $"{clientsiteLogBook.Date:yyyyMMdd} - Daily Guard Fusion Log - {FileNameHelper.GetSanitizedFileNamePart(clientsiteLogBook.ClientSite.Name)} - {version}.pdf");

            if (IO.File.Exists(reportPdfPath))
                IO.File.Delete(reportPdfPath);

            return reportPdfPath;
        }

        public string GeneratePdfReportSmartWand(int clientSiteLogBookId)
        {
            var clientsiteLogBook = _clientDataProvider.GetClientSiteLogBooks().SingleOrDefault(z => z.Id == clientSiteLogBookId);

            if (clientsiteLogBook == null)
                return string.Empty;

            var version = "v" + Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var reportPdfsw = GetReportPdfFilePathSmartWand(clientsiteLogBook, version);
            int[] clientSiteId = { clientsiteLogBook.ClientSite.Id };
            var _swGuardLogs = _guardLogDataProvider.GetGuardFusionLogs(clientsiteLogBook.ClientSite.Id, clientsiteLogBook.Date, clientsiteLogBook.Date, false);
            var _guardLogs = _swGuardLogs.Where(x => x.ActivityType.Trim().ToUpper().Equals("SW")).ToList(); // Filter for Smart Wand logs only

            var pdfDoc = new PdfDocument(new PdfWriter(reportPdfsw));
            pdfDoc.SetDefaultPageSize(PageSize.A4);
            var doc = new Document(pdfDoc);
            doc.SetMargins(15f, 30f, 40f, 30f);

            var headerTable = CreateReportHeaderForSmartWand(clientsiteLogBook.ClientSite, version);
            doc.Add(headerTable);

            doc.Add(new Paragraph("On-Duty Guard Details")
                .SetFontColor(WebColors.GetRGBColor(COLOR_NAVY_BLUE))
                .SetFontSize(CELL_FONT_SIZE * 1.5f)
                .SetMarginTop(5));

            var guardDetails = CreateGuardDetails(clientsiteLogBook);
            doc.Add(guardDetails);

            doc.Add(new Paragraph("Smart Wand Strike Logs")
                .SetFontColor(WebColors.GetRGBColor(COLOR_NAVY_BLUE))
                .SetFontSize(CELL_FONT_SIZE * 1.5f)
                .SetMarginTop(5));

            //var customFieldLogs = _guardLogDataProvider.GetCustomFieldLogs(clientSiteLogBookId).ToList();
            //var patrolCarLogs = _guardLogDataProvider.GetPatrolCarLogs(clientSiteLogBookId).ToList();
            //if (customFieldLogs.Any() || patrolCarLogs.Any())
            //{
            //    var addlFieldLogs = CreateCustomFieldAndPatrolCarLogsTable(customFieldLogs, patrolCarLogs);
            //    doc.Add(addlFieldLogs);
            //}

            var tableData = CreateReportDataForFusionWithoutSiteName(_guardLogs);
            doc.Add(tableData);

            var logNotes = CreateNotes(clientsiteLogBook.ClientSite.Id);
            doc.Add(logNotes);

            int _clientTypeId = clientsiteLogBook.ClientSite.ClientType.Id;
            var footer = CreateFooter(_clientTypeId);
            pdfDoc.AddEventHandler(PdfDocumentEvent.END_PAGE, new TableFooterEventHandler(footer));

            //int lastPageIndex = pdfDoc.GetNumberOfPages();
            //var index = lastPageIndex + 1;
            //foreach (var entry in _guardLogs)
            //{
            //    var guardlogImages = _guardLogDataProvider.GetGuardLogDocumentImaes(entry.Id);
            //    foreach (var guardLogImage in guardlogImages)
            //    {

            //        if (guardLogImage.IsRearfile == true)
            //        {
            //            try
            //            {

            //                AttachImageToPdf(pdfDoc, doc, index, guardLogImage.ImagePath);
            //                index++;                            

            //            }
            //            catch (Exception ex)
            //            {
            //                // Log exception or handle it as needed
            //                Console.WriteLine($"Error attaching image: {ex.Message}");
            //            }
            //        }
            //    }
            //}
            doc.Close();
            pdfDoc.Close();

            return IO.Path.GetFileName(reportPdfsw);
        }


        private string GetReportPdfFilePathSmartWand(ClientSiteLogBook clientsiteLogBook, string version)
        {
            var reportPdfPath = IO.Path.Combine(_reportRootDir, REPORT_DIR, $"{clientsiteLogBook.Date:yyyyMMdd} - Smart Wand Log - {FileNameHelper.GetSanitizedFileNamePart(clientsiteLogBook.ClientSite.Name)} - {version}.pdf");

            if (IO.File.Exists(reportPdfPath))
                IO.File.Delete(reportPdfPath);

            return reportPdfPath;
        }

    }


}
