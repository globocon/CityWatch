using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout.Properties;

namespace CityWatch.Web.Services
{
    public class PatrolReportGenerator
    {
        public static void CreateExcelFile(DataTable table, string destination)
        {
            ExportDSToExcel(table, destination);
        }
        private static void ExportDSToExcel(DataTable table, string destination)
        {
            using (SpreadsheetDocument document =
                   SpreadsheetDocument.Create(destination, SpreadsheetDocumentType.Workbook))
            {
                WorkbookPart workbookPart = document.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                // Sheets MUST be appended, not assigned
                Sheets sheets = workbookPart.Workbook.AppendChild(new Sheets());

                WorkbookStylesPart stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
                stylesPart.Stylesheet = CreateStylesheet();
                stylesPart.Stylesheet.Save();

                WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                SheetData sheetData = new SheetData();
                worksheetPart.Worksheet = new Worksheet(sheetData);

                worksheetPart.Worksheet.Save(); // ✅ important

                Sheet sheet = new Sheet
                {
                    Id = workbookPart.GetIdOfPart(worksheetPart),
                    SheetId = 1,
                    Name = string.IsNullOrWhiteSpace(table.TableName) ? "Sheet1" : table.TableName
                };

                sheets.Append(sheet);

                Columns cols = new Columns();
                for (uint i = 1; i <= table.Columns.Count; i++)
                {
                    cols.Append(new Column
                    {
                        Min = i,
                        Max = i,
                        Width = 20,
                        CustomWidth = true
                    });
                }
                worksheetPart.Worksheet.InsertAt(cols, 0);

                // Header row
                Row headerRow = new Row();
                foreach (DataColumn column in table.Columns)
                {
                    headerRow.Append(new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(column.ColumnName),
                        StyleIndex = 1
                    });
                }
                sheetData.Append(headerRow);

                // Data rows
                foreach (DataRow row in table.Rows)
                {
                    Row newRow = new Row();
                    foreach (DataColumn column in table.Columns)
                    {
                        newRow.Append(new Cell
                        {
                            DataType = CellValues.String,
                            CellValue = new CellValue(
                                CleanInvalidXmlChars(Convert.ToString(row[column]) ?? string.Empty)
                            ),
                            StyleIndex = 0
                        });
                    }
                    sheetData.Append(newRow);
                }

                worksheetPart.Worksheet.Save();
                workbookPart.Workbook.Save();
            }
        }


        //private static void ExportDSToExcel(DataTable table, string destination)
        //{
        //    using (var workbook = SpreadsheetDocument.Create(destination, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook))
        //    {
        //        var workbookPart = workbook.AddWorkbookPart();
        //        workbook.WorkbookPart.Workbook = new Workbook();
        //        workbook.WorkbookPart.Workbook.Sheets = new Sheets();                

        //        var stylesPart = workbook.WorkbookPart.AddNewPart<WorkbookStylesPart>();
        //        stylesPart.Stylesheet = CreateStylesheet();
        //        stylesPart.Stylesheet.Save();

        //        var sheetPart = workbook.WorkbookPart.AddNewPart<WorksheetPart>();
        //        var sheetData = new SheetData();
        //        sheetPart.Worksheet = new Worksheet(sheetData);

        //        var sheets = workbook.WorkbookPart.Workbook.GetFirstChild<Sheets>();
        //        string relationshipId = workbook.WorkbookPart.GetIdOfPart(sheetPart);

        //        var sheet = new Sheet() { Id = relationshipId, SheetId = 1, Name = table.TableName };
        //        sheets.Append(sheet);

        //        var cols = new Columns();
        //        for (uint index = 1; index <= table.Columns.Count; index++)
        //            cols.Append(new Column() { Min = index, Max = index, CustomWidth = true, Width = 20 });
        //        sheetPart.Worksheet.InsertAt(cols, 0);

        //        var headerRow = new Row();

        //        var columns = new List<string>();
        //        foreach (DataColumn column in table.Columns)
        //        {
        //            columns.Add(column.ColumnName);

        //            var cell = new Cell();
        //            cell.DataType = CellValues.String;
        //            cell.CellValue = new CellValue(column.ColumnName);
        //            cell.StyleIndex = Convert.ToUInt32(1);
        //            headerRow.AppendChild(cell);
        //        }

        //        sheetData.AppendChild(headerRow);

        //        foreach (DataRow dsrow in table.Rows)
        //        {
        //            var newRow = new Row();
        //            foreach (string col in columns)
        //            {
        //                var cell = new Cell();
        //                cell.DataType = CellValues.String;
        //                cell.CellValue = new CellValue(CleanInvalidXmlChars(dsrow[col].ToString()));
        //                cell.StyleIndex = Convert.ToUInt32(0);
        //                newRow.AppendChild(cell);
        //            }

        //            sheetData.AppendChild(newRow);
        //        }
        //        sheetPart.Worksheet.Save();
        //        workbook.WorkbookPart.Workbook.Save();
        //    }


        //}
        //public static string CleanInvalidXmlChars(string text)
        //{
        //    // From xml spec valid chars: 
        //    // #x9 | #xA | #xD | [#x20-#xD7FF] | [#xE000-#xFFFD] | [#x10000-#x10FFFF]     
        //    // any Unicode character, excluding the surrogate blocks, FFFE, and FFFF. 
        //    string re = @"[^\x09\x0A\x0D\x20-\xD7FF\xE000-\xFFFD\x10000-x10FFFF]";
        //    return Regex.Replace(text, re, "");
        //}
        private static string CleanInvalidXmlChars(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var buffer = new StringBuilder(text.Length);

            foreach (char c in text)
            {
                if (XmlConvert.IsXmlChar(c))
                    buffer.Append(c);
            }

            return buffer.ToString();
        }


        private static Stylesheet CreateStylesheet()
        {
            Stylesheet styleSheet = new Stylesheet();

            // Default font
            var font0 = new Font();         

            var font1 = new Font();
            var bold = new Bold();
            // Bold font
            font1.Append(bold);

            //var fonts = new Fonts();
            //fonts.Append(font0);
            //fonts.Append(font1);
            var fonts = new Fonts() { Count = 2 };
            fonts.Append(font0);
            fonts.Append(font1);

            // <Fills>
            // Default fill
            var fill0 = new Fill();

            //var fills = new Fills();
            //fills.Append(fill0);
            var fills = new Fills() { Count = 1 };
            fills.Append(fill0);

            // <Borders>
            // Defualt border
            var border0 = new Border();

            //var borders = new Borders();
            //borders.Append(border0);
            var borders = new Borders() { Count = 1 };
            borders.Append(border0);

            // Default style : Mandatory | Style ID =0
            var cellformat0 = new CellFormat() { FontId = 0, FillId = 0, BorderId = 0 };

            var cellformat1 = new CellFormat() { FontId = 1 };
            //var cellformats = new CellFormats();
            //cellformats.Append(cellformat0);
            //cellformats.Append(cellformat1);
            var cellformats = new CellFormats() { Count = 2 };
            cellformats.Append(cellformat0);
            cellformats.Append(cellformat1);

            styleSheet.Append(fonts);
            styleSheet.Append(fills);
            styleSheet.Append(borders);
            styleSheet.Append(cellformats);

            return styleSheet;
        }

        public static void CreatePdfFile(DataTable table, string destination)
        {
            using (var writer = new iText.Kernel.Pdf.PdfWriter(destination))
            using (var pdf = new iText.Kernel.Pdf.PdfDocument(writer))
            {
                pdf.SetDefaultPageSize(iText.Kernel.Geom.PageSize.A4.Rotate()); // Landscape A4 gives plenty of horizontal space for wide columns
                using (var document = new iText.Layout.Document(pdf))
                {
                    document.SetMargins(15f, 15f, 15f, 15f);

                    // Add title
                    var title = new iText.Layout.Element.Paragraph("IR Statistics Report")
                        .SetFontSize(14f)
                        .SetBold()
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                        .SetMarginBottom(10f);
                    document.Add(title);

                    // All 21 columns matching the Excel spreadsheet
                    var allowedColumns = new List<string> {
                        "Day", "Date", "Control Room Job No.", "Site", "Address", "Desp. Time",
                        "Arrival", "Depart.", "CWS SNo.", "Total mins on Site", "Resp. Time",
                        "Alarm", "Patrol Att.", "Colour Code", "Action Taken", "Notified By", "Bill To:",
                        "File Name", "PSPF", "File Size(KB)", "Hash String"
                    };

                    var columnsToInclude = new List<int>();
                    for (int i = 0; i < table.Columns.Count; i++)
                    {
                        if (allowedColumns.Contains(table.Columns[i].ColumnName))
                        {
                            columnsToInclude.Add(i);
                        }
                    }

                    int colCount = columnsToInclude.Count;
                    if (colCount == 0) return;

                    // Compute relative column widths to fit all 21 columns beautifully
                    float[] colWidths = new float[colCount];
                    for (int i = 0; i < colCount; i++)
                    {
                        string colName = table.Columns[columnsToInclude[i]].ColumnName;
                        if (colName == "Address") colWidths[i] = 14f;
                        else if (colName == "Action Taken") colWidths[i] = 15f;
                        else if (colName == "Hash String") colWidths[i] = 16f;
                        else if (colName == "File Name") colWidths[i] = 11f;
                        else if (colName == "Site") colWidths[i] = 9f;
                        else if (colName == "Control Room Job No.") colWidths[i] = 8f;
                        else if (colName == "Total mins on Site") colWidths[i] = 6f;
                        else colWidths[i] = 4.5f; // default width for short fields like Day, Date, Time, etc.
                    }

                    var pdfTable = new iText.Layout.Element.Table(iText.Layout.Properties.UnitValue.CreatePercentArray(colWidths)).UseAllAvailableWidth();

                    // Add Headers
                    foreach (int colIdx in columnsToInclude)
                    {
                        var cell = new iText.Layout.Element.Cell()
                            .Add(new iText.Layout.Element.Paragraph(table.Columns[colIdx].ColumnName).SetFontSize(4.2f).SetBold())
                            .SetBackgroundColor(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY)
                            .SetPadding(2f)
                            .SetBorder(new iText.Layout.Borders.SolidBorder(iText.Kernel.Colors.ColorConstants.GRAY, 0.5f));
                        pdfTable.AddHeaderCell(cell);
                    }

                    // Add Data Rows
                    foreach (System.Data.DataRow row in table.Rows)
                    {
                        foreach (int colIdx in columnsToInclude)
                        {
                            string cellValue = Convert.ToString(row[colIdx]) ?? string.Empty;
                            var cell = new iText.Layout.Element.Cell()
                                .Add(new iText.Layout.Element.Paragraph(cellValue).SetFontSize(3.8f))
                                .SetPadding(2f)
                                .SetBorder(new iText.Layout.Borders.SolidBorder(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY, 0.5f));
                            pdfTable.AddCell(cell);
                        }
                    }

                    document.Add(pdfTable);
                }
            }
        }
    }
}
