using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

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
            using (var workbook = SpreadsheetDocument.Create(destination, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook))
            {
                var workbookPart = workbook.AddWorkbookPart();
                workbook.WorkbookPart.Workbook = new Workbook();
                workbook.WorkbookPart.Workbook.Sheets = new Sheets();                

                var stylesPart = workbook.WorkbookPart.AddNewPart<WorkbookStylesPart>();
                stylesPart.Stylesheet = CreateStylesheet();
                stylesPart.Stylesheet.Save();

                var sheetPart = workbook.WorkbookPart.AddNewPart<WorksheetPart>();
                var sheetData = new SheetData();
                sheetPart.Worksheet = new Worksheet(sheetData);

                var sheets = workbook.WorkbookPart.Workbook.GetFirstChild<Sheets>();
                string relationshipId = workbook.WorkbookPart.GetIdOfPart(sheetPart);

                var sheet = new Sheet() { Id = relationshipId, SheetId = 1, Name = table.TableName };
                sheets.Append(sheet);

                var cols = new Columns();
                for (uint index = 1; index <= table.Columns.Count; index++)
                    cols.Append(new Column() { Min = index, Max = index, CustomWidth = true, Width = 20 });
                sheetPart.Worksheet.InsertAt(cols, 0);

                var headerRow = new Row();

                var columns = new List<string>();
                foreach (DataColumn column in table.Columns)
                {
                    columns.Add(column.ColumnName);

                    var cell = new Cell();
                    cell.DataType = CellValues.String;
                    cell.CellValue = new CellValue(column.ColumnName);
                    cell.StyleIndex = Convert.ToUInt32(1);
                    headerRow.AppendChild(cell);
                }

                sheetData.AppendChild(headerRow);

                foreach (DataRow dsrow in table.Rows)
                {
                    var newRow = new Row();
                    foreach (string col in columns)
                    {
                        var cell = new Cell();
                        cell.DataType = CellValues.String;
                        cell.CellValue = new CellValue(dsrow[col].ToString());
                        cell.StyleIndex = Convert.ToUInt32(0);
                        newRow.AppendChild(cell);
                    }

                    sheetData.AppendChild(newRow);
                }
            }
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

            var fonts = new Fonts();
            fonts.Append(font0);
            fonts.Append(font1);

            // <Fills>
            // Default fill
            var fill0 = new Fill();        

            var fills = new Fills();
            fills.Append(fill0);

            // <Borders>
            // Defualt border
            var border0 = new Border();    

            var borders = new Borders();
            borders.Append(border0);

            // Default style : Mandatory | Style ID =0
            var cellformat0 = new CellFormat() { FontId = 0, FillId = 0, BorderId = 0 };

            var cellformat1 = new CellFormat() { FontId = 1 };
            var cellformats = new CellFormats();
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
