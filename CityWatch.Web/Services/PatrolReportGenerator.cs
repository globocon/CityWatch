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
    }
}
