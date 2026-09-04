using CityWatch.Data.Models;
using CityWatch.Kpi.Helpers;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace CityWatch.Kpi.Services
{
    public interface ICustomWandExcelReportGenerator
    {
        string GenerateSiteExcel(List<WandStrikeAuditLogViewModel> data, string startdate, string endDate, string filePath);
        string GenerateMonToSunExcel(List<WandStrikeAuditLogExcelViewModel> rawData, string startdate, string endDate, string filePath);
    }
    public class CustomWandExcelReportGenerator : ICustomWandExcelReportGenerator
    {
        private readonly Settings _settings;
        public CustomWandExcelReportGenerator(IOptions<Settings> settings,
            IWebHostEnvironment webHostEnvironment)
        {
            _settings = settings.Value;           
        }

        public string GenerateSiteExcel(List<WandStrikeAuditLogViewModel> data, string startdate, string endDate, string filePath)
        {
            var reportFileName = $"{DateTime.Now.ToString("yyyyMMdd")} - Wand Strike Data Site Logs-{startdate} to {endDate}-_{new Random().Next()}.xlsx";
            var reportExcel = Path.Combine(filePath, reportFileName);

            using var workbook = new XLWorkbook();

            var worksheet = workbook.Worksheets.Add("WandStrikeLogs");

            // Headers
            worksheet.Cell(1, 1).Value = "Client Site";
            worksheet.Cell(1, 2).Value = "Strike DateTime";
            worksheet.Cell(1, 3).Value = "Scan";
            worksheet.Cell(1, 4).Value = "SmartWand";
            worksheet.Cell(1, 5).Value = "Tag ID";
            worksheet.Cell(1, 6).Value = "Tag Type";
            worksheet.Cell(1, 7).Value = "End User";

            // Header Style
            var headerRange = worksheet.Range(1, 1, 1, 7);

            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#27C2F5");
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            int row = 2;

            foreach (var item in data)
            {
                worksheet.Cell(row, 1).Value =
                    item.clientSiteSmartWandTagsHitLog?.LoggedInClientSite?.Name ?? "";

                worksheet.Cell(row, 2).Value =
                    item.clientSiteSmartWandTagsHitLog?.HitLocalDateTime;

                worksheet.Cell(row, 2).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";

                worksheet.Cell(row, 3).Value =
                    item.clientSiteSmartWandTagsHitLog?.LabelDescription ?? "";

                worksheet.Cell(row, 4).Value =
                    item.clientSiteSmartWandTagsHitLog?.SmartWandNameId ?? "";

                worksheet.Cell(row, 5).Value =
                    item.clientSiteSmartWandTagsHitLog?.TagUId ?? "";

                worksheet.Cell(row, 6).Value =
                    item.SmartWandType ?? "";

                worksheet.Cell(row, 7).Value =
                    item.EndUser ?? "";

                row++;
            }

            worksheet.Columns().AdjustToContents();

            worksheet.SheetView.FreezeRows(1);

            workbook.SaveAs(reportExcel);

            return reportFileName;

            //using var stream = new MemoryStream();

            //workbook.SaveAs(stream);

            //return stream.ToArray();
        }

        public string GenerateMonToSunExcel(List<WandStrikeAuditLogExcelViewModel> rawData, string startdate, string endDate, string filePath)
        {
            var reportFileName = $"{DateTime.Now.ToString("yyyyMMdd")} - Wand Strike Data Logs Mon-Sun-{startdate} to {endDate}-_{new Random().Next()}.xlsx";
            var reportExcel = Path.Combine(filePath, reportFileName);

            using var workbook = new XLWorkbook();

            var worksheet = workbook.Worksheets.Add("Patrol Report");

            var days = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

            // =========================
            // HEADER
            // =========================

            worksheet.Cell(2, 1).Value = "Client Site";
            worksheet.Cell(2, 2).Value = "Scan";
            worksheet.Cell(2, 3).Value = "Tag ID";

            int col = 4;

            foreach (var day in days)
            {
                worksheet.Range(2, col, 2, col + 1).Merge();

                worksheet.Cell(2, col).Value = day;

                col += 2;
            }

            // Sub headers

            col = 4;

            foreach (var day in days)
            {
                worksheet.Cell(3, col).Value = "Date/Time";
                worksheet.Cell(3, col + 1).Value = "GPS";

                col += 2;
            }

            // =========================
            // STYLING
            // =========================

            var headerRange = worksheet.Range(2, 1, 2, 17);

            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F81BD");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            var subHeaderRange = worksheet.Range(3, 4, 3, 17);

            subHeaderRange.Style.Font.Bold = true;
            subHeaderRange.Style.Fill.BackgroundColor =
                XLColor.FromHtml("#D9E1F2");

            subHeaderRange.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            // =========================
            // DATA
            // =========================

            var grouped = TransformData(rawData);

            int row = 4;

            foreach (var site in grouped)
            {
                int startRow = row;

                foreach (var scan in site.Scans)
                {
                    worksheet.Cell(row, 2).Value = scan.Scan;
                    worksheet.Cell(row, 3).Value = scan.TagId;

                    int currentCol = 4;

                    foreach (var day in days)
                    {
                        if (scan.Days.TryGetValue(day, out var entries))
                        {
                            var dateCell = worksheet.Cell(row, currentCol);

                            dateCell.Value = string.Join(
                                Environment.NewLine,
                                entries.Select(x =>
                                    x.DateTime?.ToString("dd/MM/yyyy @ HH:mm")));

                            dateCell.Style.Alignment.WrapText = true;

                            var gpsCell = worksheet.Cell(row, currentCol + 1);

                            if (!string.IsNullOrWhiteSpace(entries[0].GPS))
                            {
                                gpsCell.Value = "View In Map";
                                gpsCell.SetHyperlink(new XLHyperlink($"https://maps.google.com/?q={entries[0].GPS}"));
                                gpsCell.Style.Font.FontColor = XLColor.Blue;
                                gpsCell.Style.Font.Underline = XLFontUnderlineValues.Single;
                            }
                        }

                        currentCol += 2;
                    }

                    row++;
                }

                int endRow = row - 1;

                worksheet.Range(startRow, 1, endRow, 1).Merge();

                worksheet.Cell(startRow, 1).Value = site.ClientSite;

                worksheet.Cell(startRow, 1).Style.Alignment.Vertical =
                    XLAlignmentVerticalValues.Top;

                // Separator row
                worksheet.Range(row, 1, row, 17)
                    .Style.Fill.BackgroundColor = XLColor.LightGray;

                row++;
            }

            // Borders
            worksheet.RangeUsed().Style.Border.OutsideBorder =
                XLBorderStyleValues.Thin;

            worksheet.RangeUsed().Style.Border.InsideBorder =
                XLBorderStyleValues.Thin;

            // Widths
            worksheet.Column(1).Width = 60;
            worksheet.Column(2).Width = 45;
            worksheet.Column(3).Width = 16;

            for (int i = 4; i <= 17; i += 2)
            {
                worksheet.Column(i).Width = 18;
                worksheet.Column(i + 1).Width = 12;
            }

            worksheet.SheetView.FreezeRows(3);

            workbook.SaveAs(reportExcel);

            return reportFileName;

            //using var stream = new MemoryStream();

            //workbook.SaveAs(stream);

            //return stream.ToArray();
        }



        private List<PatrolGroupedData> TransformData(List<WandStrikeAuditLogExcelViewModel> data)
        {
            var result = new Dictionary<string, PatrolGroupedData>();

            foreach (var item in data)
            {
                var _StrikeDateTime = item.clientSiteSmartWandTagsHitLog.HitLocalDateTime.HasValue ? item?.clientSiteSmartWandTagsHitLog?.HitLocalDateTime.Value : null;
                var _ClientSite = item.clientSiteSmartWandTagsHitLog.LoggedInClientSite.Name;
                var _Scan = item?.clientSiteSmartWandTagsHitLog?.LabelDescription;
                var _TagID = item?.clientSiteSmartWandTagsHitLog?.TagUId;
                var day = _StrikeDateTime.HasValue ? _StrikeDateTime.Value.DayOfWeek.ToString() : null;

                if (!result.ContainsKey(_ClientSite))
                {
                    result[_ClientSite] = new PatrolGroupedData
                    {
                        ClientSite = _ClientSite
                    };
                }

                var site = result[_ClientSite];

                var scan = site.Scans.FirstOrDefault(x => x.Scan == _Scan);

                if (scan == null)
                {
                    scan = new PatrolScanData
                    {
                        Scan = _Scan,
                        TagId = _TagID
                    };

                    site.Scans.Add(scan);
                }

                if (day != null)
                {
                    if (!scan.Days.ContainsKey(day))
                    {
                        scan.Days[day] = new List<PatrolEntry>();
                    }

                    scan.Days[day].Add(new PatrolEntry
                    {
                        DateTime = _StrikeDateTime,
                        GPS = item?.clientSiteSmartWandTagsHitLog?.GPScoordinates ?? "",
                    });
                }
            }

            return result.Values.ToList();
        }
    }

    public class PatrolGroupedData
    {
        public string ClientSite { get; set; }

        public List<PatrolScanData> Scans { get; set; } = new();
    }

    public class PatrolScanData
    {
        public string Scan { get; set; }

        public string TagId { get; set; }

        public Dictionary<string, List<PatrolEntry>> Days { get; set; } = new();
    }

    public class PatrolEntry
    {
        public DateTime? DateTime { get; set; }

        public string? GPS { get; set; }
    }
}
