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

namespace CityWatch.Web.Services
{
    public interface ITimesheetReportGenerator
    {

        public string GeneratePdfTimesheetReport(string startdate, string endDate, int guradid);
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

            _reportRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "Pdf");
            _imageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "images");
            _siteImageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "SiteImage");
            _graphImageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "GraphImage");
            _SiteimageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "SiteImage");
            //nEWLY ADDAED-START

            _patrolDataReportService = patrolDataReportService;
            //nEWLY ADDAED-END

            if (!IO.Directory.Exists(IO.Path.Combine(_reportRootDir, REPORT_DIR)))
                IO.Directory.CreateDirectory(IO.Path.Combine(_reportRootDir, REPORT_DIR));

            if (!IO.Directory.Exists(_graphImageRootDir))
                IO.Directory.CreateDirectory(_graphImageRootDir);
        }




        public string GeneratePdfTimesheetReport(string startdate, string endDate, int guradid)
        {
            DateTime startdateTime = DateTime.Parse(startdate);
            DateTime dateTime = DateTime.Parse(endDate);
            var LoginDetails = _clientDataProvider.GetLoginDetailsGuard(guradid, startdateTime, dateTime);
            var Name = _clientDataProvider.GetGuardlogName(guradid, dateTime);
            var LicenseNo = _clientDataProvider.GetGuardLicenseNo(guradid, dateTime);
            var SiteName = _clientDataProvider.GetGuardlogSite(guradid, dateTime);
            var reportFileName = $"{DateTime.Now.ToString("yyyyMMdd")} - {FileNameHelper.GetSanitizedFileNamePart(Name)} - Daily KPI Reports -_{new Random().Next()}.pdf";
            var reportPdf = IO.Path.Combine(_reportRootDir, REPORT_DIR, reportFileName);
            var TimesheetDetails = _clientDataProvider.GetTimesheetDetails();

            var pdfDoc = new PdfDocument(new PdfWriter(reportPdf));
            pdfDoc.SetDefaultPageSize(PageSize.A4.Rotate());
            var doc = new Document(pdfDoc);
            doc.SetMargins(PDF_DOC_MARGIN, PDF_DOC_MARGIN, PDF_DOC_MARGIN, PDF_DOC_MARGIN);

            var headerTable = CreateReportHeader();
            doc.Add(headerTable);
            doc.Add(CreateNameTable(Name));
            doc.Add(CreateLicenseTable(LicenseNo));
            doc.Add(CreateDateTable(dateTime));
           // doc.Add(CreateSiteTable(SiteName));
            doc.Add(new Paragraph("\n"));
            var (GuardLoginTables, totalHours) = CreateGuardLoginDetails(startdateTime, dateTime, LoginDetails, TimesheetDetails.weekName);

            foreach (var GuardLoginTable in GuardLoginTables)
            {
                doc.Add(GuardLoginTable);
                doc.Add(new Paragraph("\n")); // Add a space between tables
            }

            var commentTable = GetCommentTable(totalHours);
            doc.Add(commentTable);
            doc.Close();
            pdfDoc.Close();

            return reportFileName;
        }

        private static Table CreateNameTable(string Name)
        {
            var siteDataTable = new Table(UnitValue.CreatePercentArray(new float[] { 5, 11 })).UseAllAvailableWidth().SetMarginTop(10);


            siteDataTable.AddCell(GetSiteValueCell("Name"));

            siteDataTable.AddCell(GetSiteValueCell(Name));


            return siteDataTable;
        }
        private static Table CreateLicenseTable(string LicensoNo)
        {
            var siteDataTable = new Table(UnitValue.CreatePercentArray(new float[] { 5, 11 })).UseAllAvailableWidth().SetMarginTop(10);


            siteDataTable.AddCell(GetSiteValueCell("Licence"));

            siteDataTable.AddCell(GetSiteValueCell(LicensoNo));


            return siteDataTable;
        }
        private static Table CreateDateTable(DateTime dateTime)
        {
            var siteDataTable = new Table(UnitValue.CreatePercentArray(new float[] { 5, 11 })).UseAllAvailableWidth().SetMarginTop(10);

            string formattedDate = dateTime.ToString("dd/MM/yyyy");
            siteDataTable.AddCell(GetSiteValueCell("WEEK ENDING"));

            siteDataTable.AddCell(GetSiteValueCell(formattedDate));


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
        private (List<Table> weeklyTables, int totalHours) CreateGuardLoginDetails(DateTime startDate, DateTime endDate, List<GuardLogin> LoginDetails, string weekname)
        {
            // Method to create a new table with headers
            Table CreateNewGuardTable()
            {
                float[] columnPercentages = new float[6];
                var GuardTable = new Table(UnitValue.CreatePercentArray(columnPercentages)).UseAllAvailableWidth();
                CreateGuardDetailsHeader(GuardTable);
                return GuardTable;
            }
            var SiteName = LoginDetails.Select(x => x.ClientSite.Name).FirstOrDefault(); 
            // Check if startDate and endDate are the same
            if (startDate.Date == endDate.Date)
            {
                var GuardTable = CreateNewGuardTable();
                int dailyTotalHours = 0;

                // Only create a table for the specific day
                string weekName = startDate.ToString("dddd");
                GuardTable.AddCell(GetSiteValueCell(weekName));
                GuardTable.AddCell(GetSiteValueCell(startDate.ToString("dd/MM/yyyy")));

                var start = LoginDetails.FirstOrDefault(x => x.LoginDate.Date == startDate.Date);
                if (start != null)
                {
                    GuardTable.AddCell(GetSiteValueCell(start.OnDuty.ToString("HH:mm")));
                    TimeSpan? endDateDifference = null;

                    if (start.OffDuty.HasValue)
                    {
                        endDateDifference = start.OffDuty.Value - start.OnDuty;
                    }

                    string enddate1 = string.Format("{0:D2}:{1:D2}", (int)endDateDifference.Value.TotalHours, endDateDifference.Value.Minutes);
                    GuardTable.AddCell(GetSiteValueCell(enddate1));

                    DateTime enddate = DateTime.ParseExact(enddate1, "HH:mm", CultureInfo.InvariantCulture);
                    DateTime startd = DateTime.ParseExact(start.OnDuty.ToString("HH:mm"), "HH:mm", CultureInfo.InvariantCulture);

                    TimeSpan TotalHrs = (enddate - startd).Duration();
                    int totalHrs = (int)TotalHrs.TotalMinutes;
                    dailyTotalHours += totalHrs;
                    int hoursDail = totalHrs / 60;
                    int minutesDail = totalHrs % 60;
                    GuardTable.AddCell(GetSiteValueCell($"{hoursDail}:{minutesDail}"));
                    if (SiteName != null)
                    {
                        GuardTable.AddCell(GetSiteValueCell(SiteName.ToString()));
                    }
                    else
                    {
                        GuardTable.AddCell(GetSiteValueCell(""));
                    }
                }
                else
                {
                    GuardTable.AddCell(GetSiteValueCell(""));
                    GuardTable.AddCell(GetSiteValueCell(""));
                    GuardTable.AddCell(GetSiteValueCell(""));
                    if (SiteName!=null)
                    {
                        GuardTable.AddCell(GetSiteValueCell(SiteName.ToString()));
                    }
                    else
                    {
                        GuardTable.AddCell(GetSiteValueCell(""));
                    }
                    
                }
               
                return (new List<Table> { GuardTable }, dailyTotalHours);
            }

            // If not the same date, proceed with weekly logic
            DateTime currentDate = startDate;
            int totalDays = (endDate - startDate).Days + 1; // Total days between the start and end date
            string[] daysOfWeek = CultureInfo.CurrentCulture.DateTimeFormat.DayNames;
            int startDayIndex = Array.IndexOf(daysOfWeek, weekname);

            // Adjust the currentDate to the correct starting day (e.g., Monday)
            while ((int)currentDate.DayOfWeek != startDayIndex)
            {
                currentDate = currentDate.AddDays(1);
            }

            List<Table> weeklyTables = new List<Table>();
            int TotalWeeklyHrs = 0;
            TimeSpan totalAccumulatedHrs = TimeSpan.Zero;

            int daysProcessed = 0;
            int weeklyTotalHours = 0;
            var SiteName1 = LoginDetails.Select(x => x.ClientSite.Name).ToList();
            // Loop through each week and break it into individual days
            while (daysProcessed < totalDays)
            {
                var GuardTable = CreateNewGuardTable();
                weeklyTotalHours = 0;

                // Process each day in the week (up to 7 days or remaining days)
                for (int j = 0; j < 7 && daysProcessed < totalDays; j++)
                {
                    string weekName = currentDate.ToString("dddd"); // e.g., "Monday"
                    GuardTable.AddCell(GetSiteValueCell(weekName));

                    if (currentDate > endDate)
                    {
                        GuardTable.AddCell("");
                    }
                    else
                    {
                        GuardTable.AddCell(GetSiteValueCell(currentDate.ToString("dd/MM/yyyy")));
                    }

                    string enddate1 = "";
                    var start = LoginDetails.Where(x => x.LoginDate.Date == currentDate.Date).FirstOrDefault();
                    if (start != null)
                    {
                        GuardTable.AddCell(GetSiteValueCell(start.OnDuty.ToString("HH:mm")));
                        TimeSpan? endDateDifference = null;

                        if (start.OffDuty.HasValue)
                        {
                            endDateDifference = start.OffDuty.Value - start.OnDuty;
                        }

                        // Instead of converting to DateTime, format the TimeSpan directly
                         enddate1 = string.Format("{0:D2}:{1:D2}", (int)endDateDifference.Value.TotalHours, endDateDifference.Value.Minutes);
                        GuardTable.AddCell(GetSiteValueCell(enddate1));
                        string[] timeParts = enddate1.Split(':');

                        // Parse hours and minutes manually
                        int hours = int.Parse(timeParts[0]);
                        int minutes = int.Parse(timeParts[1]);

                        // Create a TimeSpan from the parsed hours and minutes
                        TimeSpan enddate = TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes);
                        TimeSpan startd = TimeSpan.ParseExact(start.OnDuty.ToString("HH:mm"), "hh\\:mm", CultureInfo.InvariantCulture);

                        TimeSpan TotalHrs = (enddate - startd).Duration();
                        int totalHrs = (int)TotalHrs.TotalMinutes;
                        // No need to parse enddate1 as DateTime; use TimeSpan directly for total hours
                        totalAccumulatedHrs = totalAccumulatedHrs.Add(TotalHrs);
                        string formattedTotalHrs = string.Format("{0:D2}:{1:D2}", TotalHrs.Hours, TotalHrs.Minutes);

                        weeklyTotalHours += totalHrs;
                        GuardTable.AddCell(GetSiteValueCell(formattedTotalHrs.ToString()));
                        if (SiteName1 != null && j >= 0 && j < SiteName1.Count)
                        {
                            GuardTable.AddCell(GetSiteValueCell(SiteName1[j].ToString()));
                        }
                        else
                        {
                            GuardTable.AddCell(GetSiteValueCell(""));
                        }
                    }
                    else
                    {
                        GuardTable.AddCell(GetSiteValueCell(""));
                        GuardTable.AddCell(GetSiteValueCell(""));
                        GuardTable.AddCell(GetSiteValueCell(""));
                        if (SiteName1 != null && j >= 0 && j < SiteName1.Count)
                        {
                            GuardTable.AddCell(GetSiteValueCell(SiteName1[j].ToString()));
                        }
                        else
                        {
                          GuardTable.AddCell(GetSiteValueCell(""));
                        }
                    }

                    currentDate = currentDate.AddDays(1);
                    daysProcessed++;
                }

                // Add footer to table
                GuardTable.AddCell(GetNoBorderTotalHrsCell(""));
                GuardTable.AddCell(GetNoBorderTotalHrsCell(""));
                GuardTable.AddCell(GetNoBorderTotalHrsCell(""));
                GuardTable.AddCell(GetNoBorderTotalHrsCell(""));
                int hours1 = weeklyTotalHours / 60;
                int minutes1 = weeklyTotalHours % 60;
                GuardTable.AddCell(GetSiteValueCell($"{hours1}:{minutes1}"));

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
                float[] columnWidths = { 100f, 200f, 100f, 100f, 20f,30f }; // Adjust these values as needed
                                                                        // Total width of the table in points
                table.SetWidth(UnitValue.CreatePointValue(380)); // Example total width in points, adjust as needed
              

                Color CELL_BG_GREY_HEADER = new DeviceRgb(211, 211, 211);
                // const float CELL_WIDTH = 1f;
                table.AddCell(GetNoBorderValueCell(""));
                table.AddCell(GetSiteValueCell("Date"));
                table.AddCell(GetSiteValueCell("start"));
                table.AddCell(GetSiteValueCell("Finish"));
                table.AddCell(GetSiteValueCell("Total Hrs"));
                table.AddCell(GetSiteValueCell("Site"));


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
