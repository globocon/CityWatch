using CityWatch.Data;
using CityWatch.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CityWatch.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System;


namespace CityWatch.RadioCheck.Pages
{
    [IgnoreAntiforgeryToken]
    public class StringsRegisterBulkEditIFrame : PageModel
    {

        private readonly CityWatchDbContext _context;
        public List<UserInput> UserInputs { get; set; } = new List<UserInput>();

        public List<List<string>> TableData { get; set; }
        private readonly string filePath;

        private readonly IWebHostEnvironment _env;
        [BindProperty]
        public List<string> NewRowData { get; set; }
        public StringsRegisterBulkEditIFrame(CityWatchDbContext context)
        {
            filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "jotform", "StringsData", "StringsData.xlsx");
            _context = context;
        }
        public async Task OnGetAsync()
        {
            //UserInputs = await _context.UserInput
            //                       .OrderByDescending(u => u.UpdatedDate)
            //                       .ToListAsync();
            TableData = ReadExcel();
        }




        public JsonResult OnPostDeleteRow(int rowIndex)
        {
            try
            {
                using (var workbook = new XLWorkbook(filePath))
                {
                    var worksheet = workbook.Worksheet(1);
                    worksheet.Row(rowIndex + 1).Delete();
                    workbook.Save();
                }

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        private List<List<string>> ReadExcel()
        {
            var table = new List<List<string>>();
            if (!System.IO.File.Exists(filePath))
                return null;

            using (var workbook = new XLWorkbook(filePath))
            {
                var worksheet = workbook.Worksheet(1);
                var range = worksheet.RangeUsed();
                if (range == null) return null;

                foreach (var row in range.Rows())
                {
                    var rowData = new List<string>();
                    foreach (var cell in row.Cells())
                    {
                        if (cell.DataType == XLDataType.DateTime)
                        {
                            var dateValue = cell.GetDateTime();
                            rowData.Add(dateValue.ToString("dd/MM/yyyy"));
                        }
                        else
                        {
                            rowData.Add(cell.GetValue<string>());
                        }
                    }
                    table.Add(rowData);
                }
            }

            return table;
        }





        public IActionResult OnPostAddRow()
        {
            if (NewRowData == null || !NewRowData.Any(value => !string.IsNullOrWhiteSpace(value)))
            {
                // Optionally, add a ModelState error or TempData message here
                return RedirectToPage(); // No meaningful data entered
            }

            using (var workbook = new XLWorkbook(filePath))
            {
                var worksheet = workbook.Worksheet(1);
                var lastRow = worksheet.LastRowUsed().RowNumber();
                var newRow = worksheet.Row(lastRow + 1);

                for (int i = 0; i < NewRowData.Count; i++)
                {
                    newRow.Cell(i + 1).Value = NewRowData[i];
                }

                workbook.Save();
            }

            return RedirectToPage();
        }


        // DTO class
        public class DropDataModel
        {
            public List<List<string>> DropData { get; set; }
        }

        [BindProperty]
        public string DropData { get; set; }

        //public async Task<IActionResult> OnPostBulkUpdateAsync()
        //{
        //    using var reader = new StreamReader(Request.Body);
        //    var dropData = await reader.ReadToEndAsync();

        //    if (string.IsNullOrWhiteSpace(dropData))
        //    {
        //        return new JsonResult("No data received");
        //    }

        //    // Now dropData is a string like: "data11,data12|data21,data22"
        //    // Process it as needed...

        //    return new JsonResult("Success");
        //}



        public async Task<IActionResult> OnPostBulkUpdateAsync()
        {
            using var reader = new StreamReader(Request.Body);
            var dropData = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(dropData))
            {
                return new JsonResult(new { success = false, message = "No data received" });
            }

            var rows = dropData.Split('|', StringSplitOptions.RemoveEmptyEntries);

            if (!System.IO.File.Exists(filePath))
            {
                return new JsonResult(new { success = false, message = "Excel file not found." });
            }

            try
            {
                using var workbook = new XLWorkbook(filePath);
                var worksheet = workbook.Worksheet(1);

                int colStringDropped = 13;
                int colDropStart = 14;
                int colDropEnd = 15;
                int colContractorLast = 16;

                for (int i = 0; i < rows.Length; i++)
                {
                    var cols = rows[i].Split(',', StringSplitOptions.None);
                    int dataRow = i + 2;

                    // Use null-coalescing and conditional length checks
                    var FistColumnData = cols.Length > 0 ? cols[0] : "";
                    var SecondColumnData = cols.Length > 1 ? cols[1] : "";
                    var ThirdColumnData = cols.Length > 2 ? cols[2] : "";
                    var fourthColumnData = cols.Length > 3 ? cols[3] : "";

                    worksheet.Cell(dataRow, colStringDropped).Value = ""; // Clear content
                    worksheet.Cell(dataRow, colStringDropped).Style.NumberFormat.Format = "@"; // Set as string
                    worksheet.Cell(dataRow, colStringDropped).Value = FistColumnData;

                    worksheet.Cell(dataRow, colDropStart).Value = "";
                    worksheet.Cell(dataRow, colDropStart).Style.NumberFormat.Format = "@";
                    worksheet.Cell(dataRow, colDropStart).Value = SecondColumnData;

                    worksheet.Cell(dataRow, colDropEnd).Value = "";
                    worksheet.Cell(dataRow, colDropEnd).Style.NumberFormat.Format = "@";
                    worksheet.Cell(dataRow, colDropEnd).Value = ThirdColumnData;

                    worksheet.Cell(dataRow, colContractorLast).Value = "";
                    worksheet.Cell(dataRow, colContractorLast).Style.NumberFormat.Format = "@";
                    worksheet.Cell(dataRow, colContractorLast).Value = fourthColumnData;
                }


                // Force overwrite
                workbook.SaveAs(filePath);

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }



        public async Task<IActionResult> OnPostUpdateRowAsync(int rowIndex, string col1, string col2, string col3, string col4)
        {
            if (!System.IO.File.Exists(filePath))
            {
                return new JsonResult(new { success = false, message = "Excel file not found." });
            }

            try
            {
                using var workbook = new XLWorkbook(filePath);
                var worksheet = workbook.Worksheet(1);

                // Adjust for Excel's 1-based index and header row
                int dataRow = rowIndex + 1;

                int colStringDropped = 13;
                int colDropStart = 14;
                int colDropEnd = 15;
                int colContractorLast = 16;

                worksheet.Cell(dataRow, colStringDropped).Style.NumberFormat.Format = "@";
                worksheet.Cell(dataRow, colStringDropped).Value = col1 ?? "";

                worksheet.Cell(dataRow, colDropStart).Style.NumberFormat.Format = "@";
                worksheet.Cell(dataRow, colDropStart).Value = col2 ?? "";

                worksheet.Cell(dataRow, colDropEnd).Style.NumberFormat.Format = "@";
                worksheet.Cell(dataRow, colDropEnd).Value = col3 ?? "";

                worksheet.Cell(dataRow, colContractorLast).Style.NumberFormat.Format = "@";
                worksheet.Cell(dataRow, colContractorLast).Value = col4 ?? "";

                workbook.SaveAs(filePath);

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }




    }


}
