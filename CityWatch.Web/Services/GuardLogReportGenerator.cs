using CityWatch.Common.Helpers;
using CityWatch.Data.Enums;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using CityWatch.Web.Helpers;
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
using System.Text;
using static System.Net.WebRequestMethods;
using IO = System.IO;


namespace CityWatch.Web.Services
{

    public interface IGuardLogReportGenerator
    {
        string GeneratePdfReport(int clientSiteLogBookId);
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

        private const string REPORT_DIR = "Output";
        private const float CELL_FONT_SIZE = 7f;

        private readonly IClientDataProvider _clientDataProvider;
        private readonly IClientSiteWandDataProvider _clientSiteWandDataProvider;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        private readonly IGuardLoginDetailService _guardLoginDetailService;
        private readonly Settings _settings;
        private readonly string _reportRootDir;
        private readonly string _imageRootDir;

        public GuardLogReportGenerator(IWebHostEnvironment webHostEnvironment,
            IClientDataProvider clientDataProvider,
            IClientSiteWandDataProvider clientSiteWandDataProvider,
            IGuardLogDataProvider guardLogDataProvider,
            IGuardLoginDetailService guardLoginDetailService,
            IOptions<Settings> settings)

        {
            _clientDataProvider = clientDataProvider;
            _clientSiteWandDataProvider = clientSiteWandDataProvider;
            _guardLogDataProvider = guardLogDataProvider;
            _guardLoginDetailService = guardLoginDetailService;
            _settings = settings.Value;            
            _reportRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "Pdf");
            _imageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "images");            
        }

        public string GeneratePdfReport(int clientSiteLogBookId)
        {
            var clientsiteLogBook = _clientDataProvider.GetClientSiteLogBooks().SingleOrDefault(z => z.Id == clientSiteLogBookId);

            if (clientsiteLogBook == null)
                return string.Empty;

            var version = "v" + Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var reportPdf = GetReportPdfFilePath(clientsiteLogBook, version);
            var _guardLogs = _guardLogDataProvider.GetGuardLogs(clientSiteLogBookId, clientsiteLogBook.Date);
               
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
            if (customFieldLogs.Any() || patrolCarLogs.Any())   
            { 
                var addlFieldLogs = CreateCustomFieldAndPatrolCarLogsTable(customFieldLogs, patrolCarLogs);
                doc.Add(addlFieldLogs);
            }

            var tableData = CreateReportData(_guardLogs);
            doc.Add(tableData);

            var logNotes = CreateNotes(clientsiteLogBook.ClientSite.Id);
            doc.Add(logNotes);

            var footer = CreateFooter();
            pdfDoc.AddEventHandler(PdfDocumentEvent.END_PAGE, new TableFooterEventHandler(footer));

            doc.Close();
            pdfDoc.Close();

            return IO.Path.GetFileName(reportPdf);
        }

        private string GetReportPdfFilePath(ClientSiteLogBook clientsiteLogBook, string version)
        {
            var reportPdfPath = IO.Path.Combine(_reportRootDir, REPORT_DIR, $"{clientsiteLogBook.Date:yyyyMMdd} - Daily Guard Log - {FileNameHelper.GetSanitizedFileNamePart(clientsiteLogBook.ClientSite.Name)} - {version}.pdf");

            if (IO.File.Exists(reportPdfPath))
                IO.File.Delete(reportPdfPath);

            return reportPdfPath;
        }

        private Table CreateCustomFieldAndPatrolCarLogsTable(List<CustomFieldLog> customFieldLogs, List<PatrolCarLog> patrolCarLogs)
        {
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

        private Table CreatePatrolCarLogsTable(List<PatrolCarLog> patrolCarLogs)
        {
            if (!patrolCarLogs.Any())
            {
                return new Table(1);
            }                

            var patrolCarLogTable = new Table(UnitValue.CreatePercentArray(new float[] { 80, 20 })).UseAllAvailableWidth();

            foreach (var patrolCarLog in patrolCarLogs)
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
                .Add(new Paragraph("Gate House: Daily Shift Log Book"))
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
                    guardDetailsTable.AddCell(new Cell().SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE)).SetFontSize(CELL_FONT_SIZE).Add(new Paragraph($"{item.OnDuty :HHmm}-{item.OffDuty :HHmm}")));
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
            var reportDataTable = new Table(UnitValue.CreatePercentArray(new float[] { 10, 90,10 })).UseAllAvailableWidth();

            reportDataTable.AddHeaderCell(new Cell().SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY_DARK)).Add(new Paragraph("Time").SetFontSize(CELL_FONT_SIZE)));
            reportDataTable.AddHeaderCell(new Cell().SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY_DARK)).Add(new Paragraph("Event / Notes with Guard Initials").SetFontSize(CELL_FONT_SIZE)));
            reportDataTable.AddHeaderCell(new Cell().SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY_DARK)).Add(new Paragraph("GPS").SetFontSize(CELL_FONT_SIZE)));

            foreach (var entry in guardLog)
            {
                //Commented the following line and for fixing the time issue 29/01/2024 dileep//
                //reportDataTable.AddCell(new Cell().SetKeepTogether(true).SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE)).Add(new Paragraph($"{entry.EventDateTime:HH:mm} hrs").SetFontSize(CELL_FONT_SIZE)));
                

                reportDataTable.AddCell(new Cell().SetKeepTogether(true).SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f)).SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE)).Add(new Paragraph(getEventDateTimeUTCformat(entry)).SetFontSize(CELL_FONT_SIZE)));
                //Commented the following line and for fixing the time issue 29/01/2024 dileep end//
                var notes = entry.IrEntryType.HasValue ?
                                    entry.Notes :
                                    (string.IsNullOrEmpty(entry.GuardLogin.Guard.Initial) ? $"{entry.Notes} ;" : $"{entry.Notes} ;{entry.GuardLogin.Guard.Initial}");
                var bgColor = entry.IrEntryType.HasValue ?
                                (entry.IrEntryType == IrEntryType.Normal ? COLOR_PALE_YELLOW : COLOR_PALE_RED) :
                                COLOR_WHITE;
                //Added To display GPS start
                var imagePath = "wwwroot/images/GPSImage.png";
                var siteImage = new Image(ImageDataFactory.Create(imagePath))
                .SetWidth(UnitValue.CreatePercentValue(10));
                siteImage.SetTextAlignment(TextAlignment.RIGHT);

                var paragraph = new Paragraph()
            .SetBorder(Border.NO_BORDER);
                if (entry.GpsCoordinates!=null && entry.GpsCoordinates!="")
                {
                    paragraph.Add(siteImage);
                }
                
                var urlWithTargetBlank = $"https://www.google.com/maps?q={entry.GpsCoordinates}&target=_blank";
                var linkAction = PdfAction.CreateURI(urlWithTargetBlank);
                siteImage.SetAction(linkAction);

                Paragraph notesParagraph = new Paragraph(notes).SetFontSize(CELL_FONT_SIZE);
                if (entry.IrEntryType.HasValue && IrEntryType.Normal==entry.IrEntryType)
                {
                    var IncidentReport = entry.Notes + ".pdf";
                    string baseUrl = "https://c4istorage1.blob.core.windows.net/irfiles/";
                    string url = $"{baseUrl}{IncidentReport.Substring(0, 8)}/{IncidentReport}";
                    string linkText = "    click here";


                    var link = new Link(linkText, PdfAction.CreateURI(url))
                        .SetFontColor(DeviceGray.BLACK)
                        .SetFontColor(ColorConstants.BLUE);
                    notesParagraph.Add(link);
                }
                
                reportDataTable.AddCell(new Cell()
                 .SetKeepTogether(true)
                 .SetBorder(new SolidBorder(WebColors.GetRGBColor(COLOR_GREY_LIGHT), 0.25f))
                 .SetBackgroundColor(WebColors.GetRGBColor(bgColor))
                 .Add(notesParagraph));

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
            if (entry.EventDateTimeLocal != null )
            {
                DateTime localTime =(DateTime)entry.EventDateTimeLocal;
                var dt = localTime.ToString("HH:mm")+ " Hrs " + entry.EventDateTimeZoneShort;
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
                "Please use Smart WAND to register shift change and meal breaks / rest (this is OH&S related and NOT " +
                "tied to renumeration so it does not need to be accurate).\n\n",

                "Entries are to assist guards with notes during their shift, and for " +
                "handover of the next guard. Do NOT write down anything related to sign-in or patrols times / frequency because they are automated and " +
                "recorded separately (and more accurate) and it is a waste of an entry.\n\n" +

                "Never leave more than 2 hours BLANK \n\n" +

                "All incident reports are to be completed via  www.cws-ir.com ; of course mention them in here BUT only briefly describe them; All IR's" +
                " need to be registered on the Smart WAND as an event (button 3A) unless generated from the Smart WAND (button 3B) as it will auto-register" +
                " event.\n\n",

                "Smart WAND is to be used on patrol for photos; such as critical infrastructure, alarm panel LED status, high risk areas, etc. – personal phone " +
                "can be used as backup to reach KPI \n\n"+

                "Use USB cable to dump all photos within the \"Daily Photo's\" Folder ; where needed you can mention the photo in" +
                "the log (ie: Store X accessed, photo of log taken) \n\n" +

                "24/7 sites should dump images (cut and paste) after midnight to dropbox – for each issued" +
                " Smart WAND - even if images were created by another crew; Smart WAND should be EMPTY and CLEAR of images at midnight – ready for the next \"Day\")"
            };
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
        
        private string GetSiteImage(int clientSiteId)
        {
            var clientSiteSetting = _clientDataProvider.GetClientSiteKpiSetting(clientSiteId);
            if (clientSiteSetting != null && !string.IsNullOrEmpty(clientSiteSetting.SiteImage))
                return $"{new Uri(_settings.KpiWebUrl)}{clientSiteSetting.SiteImage}";
            return string.Empty;
        }

    }
}
