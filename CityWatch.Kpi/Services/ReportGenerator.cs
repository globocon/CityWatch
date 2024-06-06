using CityWatch.Common.Helpers;
using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using CityWatch.Kpi.Helpers;
using CityWatch.Kpi.Models;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using static Dropbox.Api.TeamLog.SpaceCapsType;
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
        private const string CELL_FONT_GREEN = "#008000";
        private const string CELL_FONT_RED = "#FF0000";
        private const string COLOR_WHITE = "#ffffff";
        private const string COLOR_GREY = "#666362";

        private readonly string _reportRootDir;
        private readonly string _imageRootDir;
        private readonly string _siteImageRootDir;
        private readonly string _graphImageRootDir;

        private readonly IViewDataService _viewDataService;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly ILogger<ReportGenerator> _logger;
        private readonly Settings _settings;
        private readonly IPatrolDataReportService _patrolDataReportService;

        public ReportGenerator(IOptions<Settings> settings,
            IWebHostEnvironment webHostEnvironment,
            IViewDataService viewDataService,
            IClientDataProvider clientDataProvider,
            ILogger<ReportGenerator> logger, IPatrolDataReportService patrolDataReportService)
        {
            _viewDataService = viewDataService;
            _clientDataProvider = clientDataProvider;
            _logger = logger;
            _settings = settings.Value;

            _reportRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "Pdf");
            _imageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "images");
            _siteImageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "SiteImage");
            _graphImageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "GraphImage");
            //nEWLY ADDAED-START
            
            _patrolDataReportService = patrolDataReportService;
            //nEWLY ADDAED-END

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
            
            //to get the data in 3rd page of report
            var monthlyDataGuard = _viewDataService.GetKpiGuardDetailsData(_clientSiteKpiSetting.ClientSiteId, fromDate, toDate);
            var GuradIds = monthlyDataGuard.Select(z => z.GuardId).ToArray();
            var monthlyDataGuardCompliance = _viewDataService.GetKpiGuardDetailsComplianceData(GuradIds);

            var monthlyData = _viewDataService.GetKpiReportData(_clientSiteKpiSetting.ClientSiteId, fromDate, toDate);
            var tableData = CreateReportData(_clientSiteKpiSetting, fromDate, monthlyData.DailyKpiResults, isHrTimerPaused);
            CreateReportDataSummary(tableData, monthlyData);
            var tableSiteStats = CreateSiteStatsData(_clientSiteKpiSetting, monthlyData, fromDate);
            
            var tableLayout = new Table(UnitValue.CreatePercentArray(new float[] { 75, 25 })).UseAllAvailableWidth();
            tableLayout.AddCell(new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER).Add(tableData));
            tableLayout.AddCell(new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER).Add(tableSiteStats));
            doc.Add(tableLayout);

            if (_settings.GuardListOn)
            {
                doc.Add(new AreaBreak());

                doc.Add(headerTable);
                var monthlyGuardData = _viewDataService.GetMonthlyKpiGuardData(clientSiteId, fromDate, toDate);
                var tableGuardData = CreateGuardReportData(monthlyGuardData, fromDate);
                doc.Add(tableGuardData);
                if (monthlyDataGuard.Count>0)
                {
                    // To add 3rd Page 
                    var HRGroupList = _viewDataService.GetKpiGuardHRGroup();
                    for (int i = 0; i < HRGroupList.Count; i++) { 
                    doc.Add(new AreaBreak());

                    doc.Add(headerTable);
                        var hrGroupName = HRGroupList[i];
                        var tableGuardDetailsData = CreateGuardDetailsLicenseAndCompliance(monthlyDataGuard, monthlyDataGuardCompliance, hrGroupName.Name, hrGroupName.Id);
                    doc.Add(tableGuardDetailsData);
                        doc.Add(new Paragraph("\n"));
                        var tableGuardDetailsData1 = CreateGuardDetailsLicenseAndComplianceHR(monthlyDataGuard, monthlyDataGuardCompliance, hrGroupName.Name, hrGroupName.Id);
                        doc.Add(tableGuardDetailsData1);
                    }
                }
               
            }
            //NEWLY ADDED-START
            doc.Add(new AreaBreak());

            doc.Add(headerTable);
            List<string> list = new List<string>();
            list.Add(_clientSiteKpiSetting.ClientSite.Name);
            string[] clientsitename = list.ToArray();

            var patrolDataReport = _patrolDataReportService.GetDailyPatrolData(new PatrolRequest()
            {
                FromDate = fromDate,
                ToDate = toDate,
                DataFilter = PatrolDataFilter.Custom,
                ClientSites = clientsitename,
            });

            if (patrolDataReport.ResultsCount > 0)
            {
                var graphsTable = CreateGraphsTables(patrolDataReport);
                doc.Add(graphsTable);
            }
            //NEWLY ADDED-END
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
                .SetPaddingBottom(0)
                .SetKeepTogether(true);
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

        private Cell CreateHrDataCell(string text) 
        {
            var cell = new Cell();
            cell.SetFontSize(CELL_FONT_SIZE)
                .SetPaddingTop(0)
                .SetPaddingBottom(0);

            var p = new Paragraph();
            var arTexts = text.Split(",").ToArray();
            for (int index = 0; index < arTexts.Length; index++)
            {
                var textValue = arTexts[index] ?? string.Empty;
                string textColorHex = GetHrTextColor(textValue);
                p.Add(new Text(textValue).SetFontColor(WebColors.GetRGBColor(textColorHex)));
                
                if (index < arTexts.Length - 1)
                    p.Add("\n");
            }
            cell.Add(p);
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

        private Table CreateGuardReportData(List<DailyKpiGuard> monthlyKpiGuardData, DateTime fromDate)
        {            
            var kpiGuardTable = new Table(UnitValue.CreatePercentArray(new float[] { 2, 6, 6, 6, 12, 9, 3, 3, 3, 12, 9, 3, 3, 3, 12, 9, 3, 3, 3 })).UseAllAvailableWidth();
            CreateGuardReportHeader(kpiGuardTable, fromDate);
            foreach (var data in monthlyKpiGuardData)
            {
                kpiGuardTable.AddCell(CreateDataCell(data.Date.ToString("dd")));
                kpiGuardTable.AddCell(CreateDataCell(data.Date.ToString("dddd")));
                kpiGuardTable.AddCell(CreateDataCell(data.EmployeeHours?.ToString() ?? string.Empty));
                kpiGuardTable.AddCell(CreateDataCell(data.ActualEmployeeHours?.ToString() ?? string.Empty));
                kpiGuardTable.AddCell(CreateDataCell(data.Shift1GuardName ?? string.Empty));
                kpiGuardTable.AddCell(CreateDataCell(data.Shift1GuardSecurityNo ?? string.Empty));                
                kpiGuardTable.AddCell(CreateHrDataCell(data.Shift1GuardHr1));
                kpiGuardTable.AddCell(CreateHrDataCell(data.Shift1GuardHr2));
                kpiGuardTable.AddCell(CreateHrDataCell(data.Shift1GuardHr3));               
                kpiGuardTable.AddCell(CreateDataCell(data.Shift2GuardName ?? string.Empty));
                kpiGuardTable.AddCell(CreateDataCell(data.Shift2GuardSecurityNo ?? string.Empty));
                kpiGuardTable.AddCell(CreateHrDataCell(data.Shift2GuardHr1));
                kpiGuardTable.AddCell(CreateHrDataCell(data.Shift2GuardHr2));
                kpiGuardTable.AddCell(CreateHrDataCell(data.Shift2GuardHr3));                
                kpiGuardTable.AddCell(CreateDataCell(data.Shift3GuardName ?? string.Empty));
                kpiGuardTable.AddCell(CreateDataCell(data.Shift3GuardSecurityNo ?? string.Empty));
                kpiGuardTable.AddCell(CreateHrDataCell(data.Shift3GuardHr1));
                kpiGuardTable.AddCell(CreateHrDataCell(data.Shift3GuardHr2));
                kpiGuardTable.AddCell(CreateHrDataCell(data.Shift3GuardHr3));                
            }
            return kpiGuardTable;
        }

        private Table CreateGuardDetailsData(List<GuardLogin> monthlyDataGuard, List<GuardCompliance> monthlyDataGuardCompliance)
        {

            var guards = monthlyDataGuard
                .Select(guardLogin => guardLogin.Guard)
                .Distinct()
                .ToArray();
            List<int> complianceDataCounts = new List<int>();
            foreach (var guard in guards)
            {
                var monthlyDataGuardComplianceData = _viewDataService.GetKpiGuardDetailsCompliance(guard.Id);
                complianceDataCounts.Add(monthlyDataGuardComplianceData.Count);
            }
            int[] countsArray = complianceDataCounts.ToArray();
            int largestNumber;
            if (countsArray.Length > 0)
            {
                largestNumber = countsArray.Max();

            }
            else
            {
                largestNumber = 0;
            }

            int numColumns = monthlyDataGuardCompliance.Count;
            float[] columnPercentages = new float[largestNumber+2];
            
            var kpiGuardTable = new Table(UnitValue.CreatePercentArray(columnPercentages)).UseAllAvailableWidth();
            CreateGuardDetailsHeader(kpiGuardTable, monthlyDataGuard);

            int maxComplianceCount = guards.Select(g => _viewDataService.GetKpiGuardDetailsCompliance(g.Id).Count).Max();

            foreach (var guard in guards)
            {
                var monthlyDataGuardComplianceData = _viewDataService.GetKpiGuardDetailsCompliance(guard.Id);
                kpiGuardTable.AddCell(CreateDataCell(guard.Name));
                kpiGuardTable.AddCell(CreateDataCell(guard.SecurityNo));

                for (int i = 0; i < maxComplianceCount; i++)
                {
                    var cellColor = "";
                    DateTime? alertDate = null;
                    var compliance = i < monthlyDataGuardComplianceData.Count ? monthlyDataGuardComplianceData[i] : null;
                    if (compliance != null && compliance.ExpiryDate != null && compliance.ExpiryDate.ToString() != "")
                    {
                        alertDate = Convert.ToDateTime(compliance.ExpiryDate).AddDays(-45);
                    }

                    if (alertDate <= DateTime.Today && compliance.ExpiryDate> DateTime.Today)
                    {
                        cellColor = CELL_BG_YELLOW;
                    }
                    else if (compliance?.ExpiryDate < DateTime.Today)
                    {
                        cellColor = CELL_BG_RED;
                    }
                    else if (compliance?.ExpiryDate==null)
                    {
                        cellColor = "white";
                    }
                    else
                    {
                        cellColor = "#96e3ac";
                    }

                    DateTime? expiryDate = compliance?.ExpiryDate?.Date;
                    string expiryDateString = expiryDate.HasValue ? expiryDate.Value.ToString("dd/MM/yyyy") : "";
                    kpiGuardTable.AddCell(CreateDataCell(expiryDateString, true, cellColor));
                }

            }



            return kpiGuardTable;
        }
        private Table CreateGuardDetailsLicenseAndCompliance(List<GuardLogin> monthlyDataGuard, List<GuardCompliance> monthlyDataGuardCompliance,string hrGroupName,int Id)
        {

            var guards = monthlyDataGuard
                .Select(guardLogin => guardLogin.Guard)
                .Distinct()
                .ToArray();
            List<int> complianceDataCounts = new List<int>();
            foreach (var guard in guards)
            {
                List<GuardComplianceAndLicense> monthlyDataGuardComplianceData = null; // Declare and initialize HRGroupList
                var GropuNamee = RemoveBrackets(hrGroupName);
                if (Enum.TryParse<HrGroup>(GropuNamee, out var hrGroup))
                {

                    monthlyDataGuardComplianceData = _viewDataService.GetKpiGuardDetailsComplianceAndLicenseHR(guard.Id, hrGroup);
                }
                //var monthlyDataGuardComplianceData = _viewDataService.GetKpiGuardDetailsComplianceAndLicense(guard.Id);
                complianceDataCounts.Add(monthlyDataGuardComplianceData.Count);
            }
            int[] countsArray = complianceDataCounts.ToArray();
            int largestNumber;
            if (countsArray.Length > 0)
            {
                largestNumber = countsArray.Max();

            }
            else
            {
                largestNumber = 0;
            }

            int numColumns = monthlyDataGuardCompliance.Count;
            float[] columnPercentages = new float[largestNumber + 2];

            var kpiGuardTable = new Table(UnitValue.CreatePercentArray(columnPercentages)).UseAllAvailableWidth();
            CreateGuardDetailsNewHeader(kpiGuardTable, monthlyDataGuard, hrGroupName,Id);
           
            var GropuNamee1 = RemoveBrackets(hrGroupName);
            int maxComplianceCount=0;
            if (Enum.TryParse<HrGroup>(GropuNamee1, out var hrGroup1))
            {

                maxComplianceCount = guards.Select(g => _viewDataService.GetKpiGuardDetailsComplianceAndLicenseHR(g.Id, hrGroup1).Count).Max();
            }
            //maxComplianceCount = guards.Select(g => _viewDataService.GetKpiGuardDetailsComplianceAndLicense(g.Id).Count).Max();
           
           
            foreach (var guard in guards)
            {
                List<GuardComplianceAndLicense> monthlyDataGuardComplianceData = null; // Declare and initialize HRGroupList
                var GropuNamee = RemoveBrackets(hrGroupName);
                if (Enum.TryParse<HrGroup>(GropuNamee, out var hrGroup))
                {
                    
                    monthlyDataGuardComplianceData = _viewDataService.GetKpiGuardDetailsComplianceAndLicenseHR(guard.Id, hrGroup);
                }
                //var monthlyDataGuardComplianceData = _viewDataService.GetKpiGuardDetailsComplianceAndLicense(guard.Id);
                kpiGuardTable.AddCell(CreateDataCell(guard.Name));
                kpiGuardTable.AddCell(CreateDataCell(guard.SecurityNo));

                for (int i = 0; i < maxComplianceCount; i++)
                {
                    var cellColor = "";
                    DateTime? alertDate = null;
                    var compliance = i < monthlyDataGuardComplianceData.Count ? monthlyDataGuardComplianceData[i] : null;
                    
                    if (compliance != null && compliance.ExpiryDate != null && compliance.ExpiryDate.ToString() != "")
                    {
                        alertDate = Convert.ToDateTime(compliance.ExpiryDate).AddDays(-45);
                    }

                    if (alertDate <= DateTime.Today && compliance.ExpiryDate > DateTime.Today)
                    {
                        cellColor = CELL_BG_YELLOW;
                    }
                    else if (compliance?.ExpiryDate < DateTime.Today)
                    {
                        cellColor = CELL_BG_RED;
                    }
                    else if (compliance?.ExpiryDate == null)
                    {
                        cellColor = "white";
                    }
                    else
                    {
                        cellColor = "#96e3ac";
                    }

                    DateTime? expiryDate = compliance?.ExpiryDate?.Date;
                    string expiryDateString = expiryDate.HasValue ? expiryDate.Value.ToString("dd/MM/yyyy") : "";
                    kpiGuardTable.AddCell(CreateDataCell(expiryDateString, true, cellColor));
                }

            }



            return kpiGuardTable;
        }
        private string RemoveBrackets(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            string pattern = @"\[.*?\]|\{.*?\}|\(.*?\)";
            return Regex.Replace(input, pattern, string.Empty);
        }
        private Table CreateGuardDetailsLicenseAndComplianceHR(List<GuardLogin> monthlyDataGuard, List<GuardCompliance> monthlyDataGuardCompliance, string hrGroupName, int Id)
        {

          
            float[] columnPercentages = new float[2];

            var kpiGuardTable1 = new Table(UnitValue.CreatePercentArray(columnPercentages)).UseAllAvailableWidth();
            CreateGuardDetailsNewHeaderHR(kpiGuardTable1, monthlyDataGuard, hrGroupName, Id);

            var HTList = _viewDataService.GetHRSettings(Id);
            foreach (var item in HTList)
            {
                
                kpiGuardTable1.AddCell(CreateDataCell(item.ReferenceNo));
                kpiGuardTable1.AddCell(CreateDataCell(item.Description));


            }


            return kpiGuardTable1;
        }
        private static string GetHrTextColor(string hrValue)
        {
            if (hrValue == "Y")
                return CELL_FONT_GREEN;

            if (hrValue == "N" || hrValue == "E")
                return CELL_FONT_RED;

            return string.Empty;
        }

        private void CreateGuardReportHeader(Table table, DateTime fromDate)
        {
            table.AddCell(new Cell(1, 4)
                .SetFontSize(CELL_FONT_SIZE)
                .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_BLUE_HEADER))
                .Add(new Paragraph().Add(new Text($"MONTH/YEAR: {fromDate.ToString("MMM").ToUpper()} {fromDate.Year}\n"))));
            table.AddCell(new Cell(1, 5)
                .SetFontSize(CELL_FONT_SIZE)
                .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_BLUE_HEADER))
                .SetTextAlignment(TextAlignment.CENTER)
                .Add(new Paragraph().Add(new Text("Shift Block 1"))));
            table.AddCell(new Cell(1, 5)
                .SetFontSize(CELL_FONT_SIZE)
                .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_BLUE_HEADER))
                .SetTextAlignment(TextAlignment.CENTER)
                .Add(new Paragraph().Add(new Text("Shift Block 2"))));
            table.AddCell(new Cell(1, 5)
                .SetFontSize(CELL_FONT_SIZE)
                .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_BLUE_HEADER))
                 .SetTextAlignment(TextAlignment.CENTER)
                .Add(new Paragraph().Add(new Text("Shift Block 3"))));
            table.AddCell(CreateHeaderCell("Date"));
            table.AddCell(CreateHeaderCell("Day"));
            table.AddCell(CreateHeaderCell("Expected Hours"));
            table.AddCell(CreateHeaderCell("Hours Change"));
            table.AddCell(CreateHeaderCell("Guard Name"));
            table.AddCell(CreateHeaderCell("Security No"));
            table.AddCell(CreateHeaderCell("HR 1"));
            table.AddCell(CreateHeaderCell("HR 2"));
            table.AddCell(CreateHeaderCell("HR 3"));
            table.AddCell(CreateHeaderCell("Guard Name"));
            table.AddCell(CreateHeaderCell("Security No"));
            table.AddCell(CreateHeaderCell("HR 1"));
            table.AddCell(CreateHeaderCell("HR 2"));
            table.AddCell(CreateHeaderCell("HR 3"));
            table.AddCell(CreateHeaderCell("Guard Name"));
            table.AddCell(CreateHeaderCell("Security No"));
            table.AddCell(CreateHeaderCell("HR 1"));
            table.AddCell(CreateHeaderCell("HR 2"));
            table.AddCell(CreateHeaderCell("HR 3"));
        }
        private void CreateGuardDetailsHeader(Table table, List<GuardLogin> monthlyDataGuard)
        {
            try
            {
                List<int> complianceDataCounts = new List<int>();
                var guards = monthlyDataGuard
                    .Select(guardLogin => guardLogin.Guard)
                    .Distinct()
                    .ToArray();

                foreach (var guard in guards)
                {
                    var monthlyDataGuardComplianceData = _viewDataService.GetKpiGuardDetailsCompliance(guard.Id);
                    complianceDataCounts.Add(monthlyDataGuardComplianceData.Count);
                }

                int[] countsArray = complianceDataCounts.ToArray();
                int largestNumber;

                if (countsArray.Length > 0)
                {
                    largestNumber = countsArray.Max();
                }
                else
                {
                    largestNumber = 0;
                }

                table.AddCell(new Cell(1, 2)
                    .SetFontSize(CELL_FONT_SIZE)
                    .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_BLUE_HEADER))
                    .Add(new Paragraph().Add(new Text($"\n"))));

                for (int i = 0; i < largestNumber; i++)
                {
                    string sequentialNumber = (i + 1).ToString("D2");
                    table.AddCell(new Cell(1, 1)
                        .SetFontSize(CELL_FONT_SIZE)
                        .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_BLUE_HEADER))
                        .Add(new Paragraph().Add(new Text(sequentialNumber))));
                }

                table.AddCell(CreateHeaderCell($"Name\n"));
                table.AddCell(CreateHeaderCell("C4i+License"));

                var firstGuardId = monthlyDataGuard.Select(guardLogin => guardLogin.GuardId).Distinct().FirstOrDefault();
                var monthlyDataGuardComplianceData1 = _viewDataService.GetKpiGuardDetailsCompliance(firstGuardId);

                for (int i = 0; i < largestNumber; i++)
                {
                    if (i < monthlyDataGuardComplianceData1.Count) 
                    {
                        var Description = monthlyDataGuardComplianceData1[i].Description;

                        if (!string.IsNullOrEmpty(Description))
                        {
                            table.AddCell(CreateHeaderCell(Description));
                        }
                        else
                        {
                            table.AddCell(CreateHeaderCell(""));
                        }
                    }
                    else
                    {
                        
                        table.AddCell(CreateHeaderCell("")); 
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle the exception here, for example, log it or show an error message.
                Console.WriteLine($"An error occurred: {ex.Message}");
                // You can rethrow the exception if needed.
                throw;
            }
        }
        private void CreateGuardDetailsNewHeader(Table table, List<GuardLogin> monthlyDataGuard,string hrGroupname,int id)
        {
            try
            {
                List<int> complianceDataCounts = new List<int>();
                var guards = monthlyDataGuard
                    .Select(guardLogin => guardLogin.Guard)
                    .Distinct()
                    .ToArray();

                foreach (var guard in guards)
                {
                    List<GuardComplianceAndLicense> monthlyDataGuardComplianceData = null; // Declare and initialize HRGroupList
                    var GropuNamee1 = RemoveBrackets(hrGroupname);
                    if (Enum.TryParse<HrGroup>(GropuNamee1, out var hrGroup1))
                    {

                        monthlyDataGuardComplianceData = _viewDataService.GetKpiGuardDetailsComplianceAndLicenseHR(guard.Id, hrGroup1);
                    }
                    //var monthlyDataGuardComplianceData = _viewDataService.GetKpiGuardDetailsComplianceAndLicense(guard.Id);
                    complianceDataCounts.Add(monthlyDataGuardComplianceData.Count);
                }

                int[] countsArray = complianceDataCounts.ToArray();
                int largestNumber;

                if (countsArray.Length > 0)
                {
                    largestNumber = countsArray.Max();
                }
                else
                {
                    largestNumber = 0;
                }
                
                table.AddCell(new Cell(1, 2)
                    .SetFontSize(CELL_FONT_SIZE)
                    .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_BLUE_HEADER))
                    .Add(new Paragraph().Add(new Text(hrGroupname))));
                char[] suffixes = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z' };

                for (int i = 0; i < largestNumber; i++)
                {
                    int baseNumber = (i / suffixes.Length) + 1;
                    char suffix = suffixes[i % suffixes.Length];
                    string sequentialNumber = id.ToString("D2") + suffix;

                    table.AddCell(new Cell(1, 1)
                        .SetFontSize(CELL_FONT_SIZE)
                        .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_BLUE_HEADER))
                        .Add(new Paragraph().Add(new Text(sequentialNumber))));

                }

                table.AddCell(CreateHeaderCell($"Name\n"));
                table.AddCell(CreateHeaderCell("C4i+License"));

                var firstGuardId = monthlyDataGuard.Select(guardLogin => guardLogin.GuardId).Distinct().FirstOrDefault();
                List<GuardComplianceAndLicense> monthlyDataGuardComplianceData1 = null; // Declare and initialize HRGroupList
                var GropuNamee = RemoveBrackets(hrGroupname);
                if (Enum.TryParse<HrGroup>(GropuNamee, out var hrGroup))
                {

                    monthlyDataGuardComplianceData1 = _viewDataService.GetKpiGuardDetailsComplianceAndLicenseHR(firstGuardId, hrGroup);
                }
                //var monthlyDataGuardComplianceData1 = _viewDataService.GetKpiGuardDetailsComplianceAndLicense(firstGuardId);

                for (int i = 0; i < largestNumber; i++)
                {
                    

                        table.AddCell(CreateHeaderCell(""));
                   
                }
            }
            catch (Exception ex)
            {
                // Handle the exception here, for example, log it or show an error message.
                Console.WriteLine($"An error occurred: {ex.Message}");
                // You can rethrow the exception if needed.
                throw;
            }
        }

        private void CreateGuardDetailsNewHeaderHR(Table table, List<GuardLogin> monthlyDataGuard, string hrGroupname, int id)
        {
            try
            {
                float[] columnWidths = { 100f, 200f, 100f }; // Adjust these values as needed
                table.SetWidth(UnitValue.CreatePointValue(400)); // Total width of the table in points
                

                Color CELL_BG_GREY_HEADER = new DeviceRgb(211, 211, 211);
                const float CELL_WIDTH = 1f;
                table.AddCell(new Cell(1, 1)
                         .SetFontSize(CELL_FONT_SIZE)
                         .SetBackgroundColor(CELL_BG_GREY_HEADER)
                         
                         .Add(new Paragraph().Add(new Text($"Reference No"))));
                table.AddCell(new Cell(1, 1)
                        .SetFontSize(CELL_FONT_SIZE)
                        .SetBackgroundColor(CELL_BG_GREY_HEADER)
                        .SetWidth(CELL_WIDTH)
                        .Add(new Paragraph().Add(new Text($"Description"))));
                //table.AddCell(CreateHeaderCell($"Reference No\n"));
                

            }
            catch (Exception ex)
            {
                // Handle the exception here, for example, log it or show an error message.
                Console.WriteLine($"An error occurred: {ex.Message}");
                // You can rethrow the exception if needed.
                throw;
            }
        }
        //NEWLY ADDED-START
        private Table CreateGraphsTables(PatrolDataReport patrolDataReport)
        {
            var graphTable = new Table(UnitValue.CreatePercentArray(1)).UseAllAvailableWidth()
                .SetMarginTop(5)
                .SetKeepTogether(true);
            graphTable.AddCell(new Cell()
                .SetPadding(0)
                .SetBorder(Border.NO_BORDER)
                .Add(CreateGraphsTable1(patrolDataReport)));
            graphTable.AddCell(new Cell()
                .SetPadding(0)
                .SetBorder(Border.NO_BORDER)
                .Add(CreateGraphsTable2(patrolDataReport)));
            return graphTable;
        }

        private Table CreateGraphsTable1(PatrolDataReport patrolDataReport)
        {
            var chartDataTable = new Table(UnitValue.CreatePercentArray(new float[] { 33, 1, 32, 1, 33 })).UseAllAvailableWidth().SetMarginBottom(5);

            chartDataTable.AddCell(GetChartHeaderCell("IR RECORDS PERCENTAGE BY SITE", "\nTotal Site Count: " + patrolDataReport.SitePercentage.Count));

            // row 1 blank cell
            chartDataTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));

            chartDataTable.AddCell(GetChartHeaderCell("IR RECORDS PERCENTAGE BY AREA/WARD", "\nTotal Area/Ward Count: " + patrolDataReport.AreaWardPercentage.Count));

            // row 1 blank cell
            chartDataTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));

            chartDataTable.AddCell(GetChartHeaderCell("IR RECORDS PERCENTAGE BY COLOUR CODE", "\nTotal Color Code Count: " + patrolDataReport.ColorCodePercentage.Count));

            var sitesPieChartImage = GetChartImage(patrolDataReport.SitePercentage.OrderByDescending(z => z.Value).ToArray());
            chartDataTable.AddCell(GetChartImageCell(sitesPieChartImage));

            // row 2 blank cell
            chartDataTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));

            var areaPieChartImage = GetChartImage(patrolDataReport.AreaWardPercentage.OrderByDescending(z => z.Value).ToArray());
            chartDataTable.AddCell(GetChartImageCell(areaPieChartImage));

            // row 2 blank cell
            chartDataTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));

            var colorCodeChartImage = GetChartImage(patrolDataReport.ColorCodePercentage.OrderByDescending(z => z.Value).ToArray());
            chartDataTable.AddCell(GetChartImageCell(colorCodeChartImage));

            return chartDataTable;
        }

        private Table CreateGraphsTable2(PatrolDataReport patrolDataReport)
        {
            var chartDataTable = new Table(UnitValue.CreatePercentArray(new float[] { 70, 30 })).UseAllAvailableWidth().SetMarginTop(5);

            var eventTypeCount = patrolDataReport.EventTypeQuantity.Sum(z => z.Value);
            chartDataTable.AddCell(GetChartHeaderCell("IR EVENT TYPE QUANTITY", "Total IR Count: " + eventTypeCount, 2));

            var eventTypePieChartImage = GetChartImage(patrolDataReport.EventTypePercentage.OrderBy(z => z.Key).ToArray(), chartWidth: 615);
            chartDataTable.AddCell(GetChartImageCell(eventTypePieChartImage).SetBorderRight(Border.NO_BORDER));

            var eventTypeBarChartImage = GetChartImage(patrolDataReport.EventTypeQuantity.OrderBy(z => z.Key).ToArray(), ChartType.Bar);
            chartDataTable.AddCell(GetChartImageCell(eventTypeBarChartImage).SetBorderLeft(Border.NO_BORDER));

            return chartDataTable;
        }
        private Cell GetChartHeaderCell(string leftText, string rightText, int colspan = 1)
        {
            var cell = new Cell(1, colspan)
               .SetFont(PdfHelper.GetPdfFont())
               .SetFontSize(CELL_FONT_SIZE)
               .SetFontColor(WebColors.GetRGBColor(COLOR_WHITE))
               .SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY))
               .SetVerticalAlignment(VerticalAlignment.MIDDLE);

            var p = new Paragraph(leftText);
            p.Add(new Tab());
            p.AddTabStops(new TabStop(1000, TabAlignment.RIGHT));
            p.Add(new Text(rightText).SetFontSize(4.5f));
            cell.Add(p);

            return cell;
        }

        private Cell GetChartImageCell(Image chartImage)
        {
            var imageCell = new Cell();
            if (chartImage != null)
                imageCell.Add(chartImage).SetVerticalAlignment(VerticalAlignment.MIDDLE);

            return imageCell;
        }
        private Image GetChartImage(KeyValuePair<string, double>[] data, ChartType chartType = ChartType.Pie, int? chartWidth = null)
        {
            if (data.All(z => z.Value == 0))
                return null;

            try
            {
                var graphFileName = IO.Path.Combine(_graphImageRootDir, $"{DateTime.Now: ddMMyyyy_HHmmss}.png");
                var options = new { type = chartType, fileName = graphFileName, width = chartWidth };

                var task = StaticNodeJSService.InvokeFromFileAsync<string>("Scripts/ir-chart.js", "drawChart", args: new object[] { options, data });
                var success = task.Result == "OK";

                if (!success)
                    throw new ApplicationException("Create graph failed");

                if (success && !IO.File.Exists(graphFileName))
                    throw new ApplicationException($"Graph image not found. File Name: {graphFileName}");

                var graphImage = new Image(ImageDataFactory.Create(graphFileName)).SetHeight(101);

                IO.File.Delete(graphFileName);

                return graphImage;
            }
            catch
            {
                // no ops
            }
            return null;
        }
        //NEWLY ADDED END

    }
}
