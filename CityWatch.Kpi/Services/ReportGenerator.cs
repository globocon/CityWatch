using CityWatch.Common.Helpers;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Kpi.Helpers;
using CityWatch.Kpi.Models;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Jering.Javascript.NodeJS;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IO = System.IO;

namespace CityWatch.Kpi.Services
{
    public interface IReportGenerator
    {
        string GeneratePdfReport(int clientSiteId, DateTime fromDate, DateTime toDate, bool isHrTimerPaused = false);
    }

    public class ReportGenerator : IReportGenerator
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

        private readonly string _reportRootDir;
        private readonly string _imageRootDir;
        private readonly string _siteImageRootDir;
        private readonly string _graphImageRootDir;

        private readonly IViewDataService _viewDataService;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly ILogger<ReportGenerator> _logger;

        public ReportGenerator(IOptions<Settings> settings,
            IWebHostEnvironment webHostEnvironment,
            IViewDataService viewDataService,
            IClientDataProvider clientDataProvider,
            ILogger<ReportGenerator> logger)
        {
            _viewDataService = viewDataService;
            _clientDataProvider = clientDataProvider;
            _logger = logger;

            _reportRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "Pdf");
            _imageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "images");
            _siteImageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "SiteImage");
            _graphImageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "GraphImage");

            if (!IO.Directory.Exists(IO.Path.Combine(_reportRootDir, REPORT_DIR)))
                IO.Directory.CreateDirectory(IO.Path.Combine(_reportRootDir, REPORT_DIR));

            if (!IO.Directory.Exists(_graphImageRootDir))
                IO.Directory.CreateDirectory(_graphImageRootDir);
        }

        public string GeneratePdfReport(int clientSiteId, DateTime fromDate, DateTime toDate, bool isHrTimerPaused)
        {
            var _clientSiteKpiSetting = _clientDataProvider.GetClientSiteKpiSetting(clientSiteId);
            if (_clientSiteKpiSetting == null)
                return string.Empty;

            var reportFileName = $"{DateTime.Now.ToString("yyyyMMdd")} - {FileNameHelper.GetSanitizedFileNamePart(_clientSiteKpiSetting.ClientSite.Name)} - Daily KPI Reports - {fromDate.ToString("MMM")} {fromDate.Year}_{new Random().Next()}.pdf";
            var reportPdf = IO.Path.Combine(_reportRootDir, REPORT_DIR, reportFileName);

            var pdfDoc = new PdfDocument(new PdfWriter(reportPdf));
            pdfDoc.SetDefaultPageSize(PageSize.A4.Rotate());
            var doc = new Document(pdfDoc);
            doc.SetMargins(PDF_DOC_MARGIN, PDF_DOC_MARGIN, PDF_DOC_MARGIN, PDF_DOC_MARGIN);

            var headerTable = CreateReportHeader(_clientSiteKpiSetting);
            doc.Add(headerTable);

            var monthlyData = _viewDataService.GetKpiReportData(_clientSiteKpiSetting.ClientSiteId, fromDate, toDate);
            var tableData = CreateReportData(_clientSiteKpiSetting, fromDate, monthlyData.DailyKpiResults, isHrTimerPaused);
            CreateReportDataSummary(tableData, monthlyData);
            var tableSiteStats = CreateSiteStatsData(_clientSiteKpiSetting, monthlyData, fromDate);
            
            var tableLayout = new Table(UnitValue.CreatePercentArray(new float[] { 75, 25 })).UseAllAvailableWidth();
            tableLayout.AddCell(new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER).Add(tableData));
            tableLayout.AddCell(new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER).Add(tableSiteStats));
            doc.Add(tableLayout);

            doc.Close();
            pdfDoc.Close();

            return reportFileName;
        }

        private Table CreateReportDataSummary(Table table, MonthlyKpiResult monthlyKpiResult)
        {
            table.AddCell(new Cell(1, 4)
               .SetFontSize(CELL_FONT_SIZE)
               .SetTextAlignment(TextAlignment.LEFT)
               .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
               .Add(new Paragraph("Site Results Average Against KPI")));
            table.AddCell(new Cell(1, 2)
               .SetFontSize(CELL_FONT_SIZE)
               .SetTextAlignment(TextAlignment.RIGHT)
               .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
               .Add(new Paragraph($"{monthlyKpiResult.ImageCountAverage:0.00}"))); ;
            table.AddCell(new Cell(1, 2)
              .SetFontSize(CELL_FONT_SIZE)
              .SetTextAlignment(TextAlignment.RIGHT)
              .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
              .Add(new Paragraph($"{monthlyKpiResult.WandScanAverage:0.00}")));
            table.AddCell(new Cell(1, 1)
              .SetFontSize(CELL_FONT_SIZE)
              .SetTextAlignment(TextAlignment.RIGHT)
              .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
              .Add(new Paragraph($"{monthlyKpiResult.WandPatrolsAverage:0.00}")));
            table.AddCell(new Cell(1, 1)
              .SetFontSize(CELL_FONT_SIZE)
              .SetTextAlignment(TextAlignment.LEFT)
              .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
              .Add(new Paragraph($"{monthlyKpiResult.NotInAcceptableLogFreqCount}")));
            table.AddCell(new Cell(1, 1)
              .SetFontSize(CELL_FONT_SIZE)
              .SetTextAlignment(TextAlignment.LEFT)
              .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
              .Add(new Paragraph($"{monthlyKpiResult.IrCountTotal}")));
            table.AddCell(new Cell(1, 1)
              .SetFontSize(CELL_FONT_SIZE)
              .SetTextAlignment(TextAlignment.LEFT)
              .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
              .Add(new Paragraph($"{monthlyKpiResult.AlarmCountTotal}")));

            // row 2
            table.AddCell(new Cell(1, 4)
               .SetFontSize(CELL_FONT_SIZE)
               .SetTextAlignment(TextAlignment.LEFT)
               .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
               .Add(new Paragraph("Site Results % Against KPI")));

            var colorImagePercentage = CELL_BG_RED;            
            if (monthlyKpiResult.ImageCountPercentage >= 100)
                colorImagePercentage = CELL_BG_GREEN;
            table.AddCell(new Cell(1, 2)
               .SetFontSize(CELL_FONT_SIZE)
               .SetTextAlignment(TextAlignment.RIGHT)
               .SetBackgroundColor(WebColors.GetRGBColor(colorImagePercentage))
               .Add(new Paragraph($"{monthlyKpiResult.ImageCountPercentage.GetValueOrDefault():0.00}%")));

            var colorWandPercentage = CELL_BG_RED;
            if (monthlyKpiResult.WandScanPercentage >= 100)
                colorWandPercentage = CELL_BG_GREEN;
            table.AddCell(new Cell(1, 2)
              .SetFontSize(CELL_FONT_SIZE)
              .SetTextAlignment(TextAlignment.RIGHT)
              .SetBackgroundColor(WebColors.GetRGBColor(colorWandPercentage))
              .Add(new Paragraph($"{monthlyKpiResult.WandScanPercentage.GetValueOrDefault():0.00}%")));
            
            table.AddCell(new Cell(1, 1)
             .SetFontSize(CELL_FONT_SIZE)
             .SetTextAlignment(TextAlignment.RIGHT)
             .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
             .Add(new Paragraph($"{monthlyKpiResult.WandPatrolsPercentage:0.00}%")));

            table.AddCell(new Cell(1, 1)
               .SetFontSize(CELL_FONT_SIZE)
               .SetTextAlignment(TextAlignment.LEFT)
               .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
               .Add(new Paragraph(string.Empty)));

            table.AddCell(new Cell(1, 1)
               .SetFontSize(CELL_FONT_SIZE)
               .SetTextAlignment(TextAlignment.LEFT)
               .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
               .Add(new Paragraph("Site Score")));

            var colorSiteScore = CELL_BG_RED;
            if (monthlyKpiResult.SiteScorePercentage >= 100)
                colorSiteScore = CELL_BG_GREEN;
            table.AddCell(new Cell(1, 1)
              .SetFontSize(CELL_FONT_SIZE)
              .SetTextAlignment(TextAlignment.RIGHT)
              .SetBackgroundColor(WebColors.GetRGBColor(colorSiteScore))
              .Add(new Paragraph($"{monthlyKpiResult.SiteScorePercentage.GetValueOrDefault():0.00}%")));

            return table;
        }

        private Table CreateReportHeader(ClientSiteKpiSetting clientSiteKpiSetting)
        {
            var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 20, 60, 20 })).UseAllAvailableWidth();

            var cellSiteImage = new Cell()
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER);
            if (!string.IsNullOrEmpty(clientSiteKpiSetting.SiteImage))
            {
                var siteImagePath = IO.Path.Combine(_siteImageRootDir, IO.Path.GetFileName(clientSiteKpiSetting.SiteImage));
                if (IO.File.Exists(siteImagePath))
                {
                    var siteImage = new Image(ImageDataFactory.Create(siteImagePath)).SetHeight(60);
                    cellSiteImage.Add(siteImage);
                }
            }
            var thermalCamSite = clientSiteKpiSetting.IsThermalCameraSite ? "Day Camera + Thermal (Ti) Channel" : "Day Camera Only";
            cellSiteImage.Add(new Paragraph(thermalCamSite).SetFontSize(CELL_FONT_SIZE * .8f));

            var weekendSite = clientSiteKpiSetting.IsWeekendOnlySite ? "Weekend Only Site" : "7-Day Site";
            cellSiteImage.Add(new Paragraph(weekendSite).SetFontSize(CELL_FONT_SIZE * .8f));
            headerTable.AddCell(cellSiteImage);

            var cellReportTitle = new Cell()
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.CENTER)
                .Add(new Paragraph("Site KPI Telematics Engine & Statistics\n").SetFont(PdfHelper.GetPdfFont()).SetFontSize(CELL_FONT_SIZE * 1.5f))
                .Add(new Paragraph("KPI v" + Assembly.GetExecutingAssembly().GetName().Version.ToString()).SetFontSize(CELL_FONT_SIZE * 1.25f))                
                .Add(new Paragraph(clientSiteKpiSetting.ClientSite.ClientType.Name).SetFontSize(CELL_FONT_SIZE * 1.25f))
                .Add(new Paragraph($"{clientSiteKpiSetting.ClientSite.Name} \n\n").SetFontSize(CELL_FONT_SIZE));
            headerTable.AddCell(cellReportTitle);

            var image = new Image(ImageDataFactory.Create(IO.Path.Combine(_imageRootDir, "CWSLogoPdf.png")))
                .SetHeight(50)
                .SetHorizontalAlignment(HorizontalAlignment.RIGHT);
            var cellLogoImage = new Cell()
                .Add(image)
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetHorizontalAlignment(HorizontalAlignment.RIGHT);
            headerTable.AddCell(cellLogoImage);

            headerTable.AddCell(new Cell(1, 3).SetPadding(3).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
            return headerTable;
        }

        private Table CreateReportData(ClientSiteKpiSetting clientSiteKpiSetting, DateTime fromDate, List<DailyKpiResult> dailyKpiResults, bool isHrTimerPaused)
        {
            var colWidth = new float[] { 2, 10, 8, 8, 9, 9, 9, 9, 12, 8, 8, 8 };
            var table = new Table(UnitValue.CreatePercentArray(colWidth)).UseAllAvailableWidth();

            CreateHeaderRow(table, fromDate, clientSiteKpiSetting);
                        
            foreach (var item in dailyKpiResults)
            {
                CreateDataRow(table, item, clientSiteKpiSetting, isHrTimerPaused);
            }

            return table;
        }

        private Table CreateSiteStatsData(ClientSiteKpiSetting clientSiteKpiSetting, MonthlyKpiResult monthlyKpiResult, DateTime fromDate)
        {
            var tableSiteStats = new Table(UnitValue.CreatePercentArray(1)).UseAllAvailableWidth();

            var siteStatsTitle = new Paragraph(new Text("SITE Statistics\n\n")).SetFont(PdfHelper.GetPdfFont()).SetTextAlignment(TextAlignment.CENTER);
            var siteStatsLine1 = new Paragraph($"Expected Patrol Duration {clientSiteKpiSetting.ExpPatrolDuration} min");
            var siteStatsLine2 = new Paragraph($"Min. Patrol Freq. {clientSiteKpiSetting.MinPatrolFreq} p/hr");
            var siteStatsLine3 = new Paragraph($"Min. Images per patrol {clientSiteKpiSetting.MinImagesPerPatrol}");

            var cellTop = new Cell().SetFontSize(CELL_FONT_SIZE);
            cellTop.Add(siteStatsTitle)
                .Add(siteStatsLine1)
                .Add(siteStatsLine2)
                .Add(siteStatsLine3)
                .SetHeight(75);
            tableSiteStats.AddCell(cellTop);
            
            var graphImage = GetGraphImage(monthlyKpiResult.EffortCounts);
            var cellGraphImage = new Cell();
            if (graphImage != null) 
            {
                cellGraphImage.Add(new Paragraph("Effort Counter: Week Vs. Week")
                    .SetFontSize(CELL_FONT_SIZE)
                    .SetTextAlignment(TextAlignment.CENTER));
                cellGraphImage.Add(graphImage);
            }   
            tableSiteStats.AddCell(cellGraphImage);

            var notesTitle = new Paragraph("NOTES:\n\n").SetFont(PdfHelper.GetPdfFont()).SetTextAlignment(TextAlignment.CENTER);
            var monthNote = clientSiteKpiSetting.Notes?.SingleOrDefault(z => z.ForMonth == new DateTime(fromDate.Year, fromDate.Month, 1))?.Notes ?? string.Empty;
            var notes = new Paragraph(monthNote);
            var cellBottom = new Cell().SetFontSize(CELL_FONT_SIZE);
            cellBottom.Add(notesTitle)
                .Add(notes)
                .SetHeight(242);
            tableSiteStats.AddCell(cellBottom);

            tableSiteStats.AddCell(new Cell()
                .SetFontSize(CELL_FONT_SIZE)
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .Add(new Paragraph("Data Generated: " + monthlyKpiResult.ReportTimeStamp)));

            return tableSiteStats;
        }

        private void CreateHeaderRow(Table table, DateTime fromDate, ClientSiteKpiSetting clientSiteKpiSetting)
        {
            var tuneBuffer = string.Empty;
            if (clientSiteKpiSetting.TuneDowngradeBuffer.HasValue)
                tuneBuffer = ((clientSiteKpiSetting.TuneDowngradeBuffer.Value - 1) * 100).ToString("0");

            table.AddCell(new Cell(1, 4)
                .SetFontSize(CELL_FONT_SIZE)
                .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_BLUE_HEADER))
                .Add(new Paragraph()
                    .Add(new Text($"MONTH/YEAR: {fromDate.ToString("MMM").ToUpper()} {fromDate.Year}\n"))
                    .Add(new Text($"KPI Tune buffer @ {tuneBuffer}%").SetFontSize(CELL_FONT_SIZE * .8f))));
            table.AddCell(new Cell(1, 2)
                .SetFontSize(CELL_FONT_SIZE)
                .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_BLUE_HEADER))
                .Add(new Paragraph()
                    .Add(new Text("DAILY IMAGES\n"))
                    .Add(new Text($"{clientSiteKpiSetting.ImageTargetText}").SetFontSize(CELL_FONT_SIZE * .8f))));
            table.AddCell(new Cell(1, 2)
                .SetFontSize(CELL_FONT_SIZE)
                .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_BLUE_HEADER))
                .Add(new Paragraph()
                    .Add(new Text("DAILY WAND SCANS\n"))
                    .Add(new Text($"{clientSiteKpiSetting.WandScanTargetText}").SetFontSize(CELL_FONT_SIZE * .8f))));
            table.AddCell(new Cell(1, 1)
                .SetFontSize(CELL_FONT_SIZE)
                .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_BLUE_HEADER))
                .Add(new Paragraph()
                    .Add(new Text("DAILY PATROLS\n"))
                    .Add(new Text($"{clientSiteKpiSetting.PatrolsTargetText}").SetFontSize(CELL_FONT_SIZE * .8f))));

            table.AddCell(new Cell(1, 3)
                .SetFontSize(CELL_FONT_SIZE)
                .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_BLUE_HEADER))
                .Add(new Paragraph("EXCEPTION ALARM EVENTS")));

            table.AddCell(CreateHeaderCell("DATE"));
            table.AddCell(CreateHeaderCell("DAY"));
            table.AddCell(CreateHeaderCell("EXPECTED\nHOURS"));
            table.AddCell(CreateHeaderCell("HOURS\nCHANGE"));

            var hourlyImageUnit = clientSiteKpiSetting.ClientSiteDayKpiSettings.All(z => z.PatrolFrequency == 1) ? "p/24 hr" : "p/hr";
            var hourlyImagesHeader = clientSiteKpiSetting.IsThermalCameraSite ? "Ti Only" : string.Empty;
            table.AddCell(CreateHeaderCell(clientSiteKpiSetting.IsThermalCameraSite ? "Day + Ti Total" : "Day + Total"));
            table.AddCell(CreateHeaderCell($"{hourlyImagesHeader} {hourlyImageUnit}"));
            table.AddCell(CreateHeaderCell("Total"));
            table.AddCell(CreateHeaderCell("p/hr"));
            table.AddCell(CreateHeaderCell("p/hr"));

            table.AddCell(CreateHeaderCell("DAILY LOG 2HR TIMER"));
            table.AddCell(CreateHeaderCell("IR REPORTS"));
            table.AddCell(CreateHeaderCell("FIRE or ALARMS"));
        }

        private void CreateDataRow(Table table, DailyKpiResult item, ClientSiteKpiSetting clientSiteKpiSetting, bool isHrTimerPaused)
        {
            // Date
            table.AddCell(CreateDataCell(item.DayOfDate.ToString()));

            // Name of Day
            table.AddCell(CreateDataCell(item.NameOfDay.ToString()));

            // Employee Hours
            table.AddCell(CreateDataCell(item.EmployeeHours.ToString()));

            //ActualEmployeeHours
            table.AddCell(CreateDataCell(item.ActualEmployeeHours.ToString()));

            // Image Count (from Dropbox)
            table.AddCell(CreateDataCell(item.ImageCount.ToString()));

            // Image Count Per Hour
            var cellValue = item.ImageCountPerHr.HasValue ? item.ImageCountPerHr.ToString() : "N/A";
            var cellHasBg = item.ImageCountPerHr.GetValueOrDefault() > 0;
            if (cellHasBg && item.Date > DateTime.Today) cellHasBg = false;
            var cellColor = CELL_BG_GREEN;
            if (item.ImageCountPerHr.GetValueOrDefault() > 0 &&
                item.ImagesTarget.GetValueOrDefault() > 0 &&
                item.ImageCountPerHr.GetValueOrDefault() < item.ImagesTarget.GetValueOrDefault())
            {
                cellColor = CELL_BG_RED;
            }
            table.AddCell(CreateDataCell(cellValue, cellHasBg, cellColor));

            // Wand Scan Count (from KOIOS API)
            table.AddCell(CreateDataCell(item.WandScanCount.ToString()));

            // Wand Scans Per Hour
            cellValue = item.WandScanCountPerHr.HasValue ? item.WandScanCountPerHr.ToString() : "N/A";
            cellHasBg = item.WandScanCountPerHr.GetValueOrDefault() > 0;
            if (cellHasBg && item.Date > DateTime.Today) cellHasBg = false;
            cellColor = CELL_BG_GREEN;
            if (item.WandScanCountPerHr.GetValueOrDefault() > 0 &&
                item.WandScansTarget.GetValueOrDefault() > 0 &&
                item.WandScanCountPerHr.GetValueOrDefault() < item.WandScansTarget.GetValueOrDefault())
            {
                cellColor = CELL_BG_RED;
            }
            table.AddCell(CreateDataCell(cellValue, cellHasBg, cellColor));

            // Daily Patrols
            cellValue = item.WandPatrolsRatio.HasValue ? item.WandPatrolsRatio.ToString() : "N/A";
            cellHasBg = item.WandPatrolsRatio.GetValueOrDefault() > 0;
            if (cellHasBg && item.Date > DateTime.Today) cellHasBg = false;
            cellColor = CELL_BG_GREEN;
            var patrolTarget = clientSiteKpiSetting.ClientSiteDayKpiSettings.SingleOrDefault(z => z.WeekDay.ToString() == item.NameOfDay)?.NoOfPatrols;
            if (item.WandPatrolsRatio.GetValueOrDefault() > 0 &&
                item.WandScansTarget.GetValueOrDefault() > 0 &&
                item.WandPatrolsRatio.GetValueOrDefault() < patrolTarget.GetValueOrDefault())
            {
                cellColor = CELL_BG_RED;
            }
            table.AddCell(CreateDataCell(cellValue, cellHasBg, cellColor));

            //DAILY LOG 2HR TIMER
            cellColor = string.Empty;
            cellHasBg = false;
            cellValue = "-";
            if (!isHrTimerPaused && item.IsAcceptableLogFreq.HasValue)
            {
                cellColor = item.IsAcceptableLogFreq.Value ? CELL_BG_GREEN : CELL_BG_RED;
                cellValue = item.IsAcceptableLogFreq.Value ? "< 2hr" : "> 2hr";
                cellHasBg = true;
            }
            table.AddCell(CreateDataCell(cellValue, cellHasBg, cellColor).SetTextAlignment(TextAlignment.LEFT));

            // IR count
            table.AddCell(CreateDataCell(item.IncidentCount.ToString(), item.IncidentCount.GetValueOrDefault() > 0, CELL_BG_YELLOW_IR_COUNT));

            // IR has fire alarm event
            table.AddCell(CreateDataCell(item.HasFireOrAlarm, !string.IsNullOrEmpty(item.HasFireOrAlarm), CELL_BG_ORANGE_IR_ALARM));
        }

        private Cell CreateHeaderCell(string text)
        {
            var cell = new Cell()
                .SetFontSize(CELL_FONT_SIZE)
                .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_BLUE_HEADER))
                .Add(new Paragraph(text));

            return cell;
        }

        private Cell CreateDataCell(string text)
        {
            var cell = new Cell();
            cell.SetFontSize(CELL_FONT_SIZE)
                .SetPaddingTop(0)
                .SetPaddingBottom(0);
            cell.Add(new Paragraph(text ?? string.Empty));
            return cell;
        }

        private Cell CreateDataCell(string text, bool hasBg, string colorHex)
        {
            var cell = new Cell();
            cell.SetFontSize(CELL_FONT_SIZE)
                .SetPaddingTop(0)
                .SetPaddingBottom(0);
            if (hasBg)
                cell.SetBackgroundColor(WebColors.GetRGBColor(colorHex));
            cell.Add(new Paragraph(text ?? string.Empty));
            return cell;
        }

        private Image GetGraphImage(List<EffortCount> effortCounts)
        {

            if (effortCounts.All(z => z.IsEmpty))
                return null;

            try
            {
                var graphFileName = IO.Path.Combine(_graphImageRootDir, DateTime.Now.ToString("ddMMyyyy_HHmm") + ".png");
                var options = new { fileName = graphFileName };

                var task = StaticNodeJSService.InvokeFromFileAsync<string>("Scripts/effort-chart.js", args: new object[] { options, effortCounts.ToArray() });
                var success = task.Result == "OK";

                if (!success)
                    throw new ApplicationException("Create graph failed");

                if (success && !IO.File.Exists(graphFileName))
                    throw new ApplicationException($"Graph image not found. File Name: {graphFileName}");

                var graphImage = new Image(ImageDataFactory.Create(graphFileName)).SetHeight(101);

                IO.File.Delete(graphFileName);

                return graphImage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.StackTrace);
            }
            return null;
        }
    }
}
