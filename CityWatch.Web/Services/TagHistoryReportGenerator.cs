using CityWatch.Data.Models;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.IO.Font.Constants;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IO = System.IO;

namespace CityWatch.Web.Services
{
    public interface ITagHistoryReportGenerator
    {
        /// <summary>The Tag History report as a PDF, rows in the order given.</summary>
        byte[] GeneratePdf(List<TagHistoryReportViewModel> rows, TagHistoryReportRequest request);
    }

    public class TagHistoryReportGenerator : ITagHistoryReportGenerator
    {
        private const float CELL_FONT_SIZE = 7f;
        private const string COLOR_HEADER = "#27C2F5";
        private const string COLOR_GROUP = "#d9e2f3";

        private static readonly string[] Headers = { "Action Date Time", "Site Name", "Tag Action", "Tag UID", "Tag Type", "User", "Guard", "Changed From" };
        private static readonly float[] ColumnWidths = { 11, 15, 28, 10, 7, 9, 14, 6 };

        public byte[] GeneratePdf(List<TagHistoryReportViewModel> rows, TagHistoryReportRequest request)
        {
            using var stream = new IO.MemoryStream();
            using (var pdfDoc = new PdfDocument(new PdfWriter(stream)))
            {
                pdfDoc.SetDefaultPageSize(PageSize.A4.Rotate());
                using var doc = new Document(pdfDoc);
                doc.SetMargins(20f, 25f, 30f, 25f);

                var bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                doc.Add(new Paragraph("Tag History Report").SetFont(bold).SetFontSize(14).SetMarginBottom(2));
                var version = "v" + Assembly.GetExecutingAssembly().GetName().Version;
                doc.Add(new Paragraph($"Period: {request.LogFromDate:dd MMM yyyy} to {request.LogToDate:dd MMM yyyy}    " +
                                      $"Records: {rows.Count}    Generated: {DateTime.Now:dd MMM yyyy HH:mm}    {version}")
                    .SetFontSize(8).SetMarginBottom(8));

                var table = new Table(UnitValue.CreatePercentArray(ColumnWidths)).UseAllAvailableWidth();

                foreach (var header in Headers)
                {
                    table.AddHeaderCell(new Cell()
                        .Add(new Paragraph(header).SetFont(bold).SetFontSize(CELL_FONT_SIZE))
                        .SetBackgroundColor(WebColors.GetRGBColor(COLOR_HEADER)));
                }

                if (rows.Count == 0)
                {
                    table.AddCell(new Cell(1, Headers.Length)
                        .Add(new Paragraph("No tag changes found for the selected filters.").SetFontSize(CELL_FONT_SIZE))
                        .SetTextAlignment(TextAlignment.CENTER));
                }

                // Day headings only make sense when the rows run in date order.
                var groupByDay = string.IsNullOrEmpty(request.SortBy) || request.SortBy == TagHistorySortBy.DateTime;
                string lastGroup = null;

                foreach (var row in rows)
                {
                    if (groupByDay && row.GroupText != lastGroup)
                    {
                        table.AddCell(new Cell(1, Headers.Length)
                            .Add(new Paragraph(row.GroupText).SetFont(bold).SetFontSize(CELL_FONT_SIZE))
                            .SetBackgroundColor(WebColors.GetRGBColor(COLOR_GROUP)));
                        lastGroup = row.GroupText;
                    }

                    AddCell(table, row.ActionDateTimeText);
                    AddCell(table, row.SiteName);
                    AddCell(table, row.TagAction);
                    AddCell(table, row.TagUId);
                    AddCell(table, row.TagType);
                    AddCell(table, row.UserDisplay);
                    AddCell(table, row.GuardDisplay);
                    AddCell(table, row.AppType);
                }

                doc.Add(table);
            }

            return stream.ToArray();
        }

        private static void AddCell(Table table, string text)
        {
            table.AddCell(new Cell()
                .Add(new Paragraph(text ?? string.Empty).SetFontSize(CELL_FONT_SIZE))
                .SetBorder(new SolidBorder(ColorConstants.LIGHT_GRAY, 0.5f)));
        }
    }
}
