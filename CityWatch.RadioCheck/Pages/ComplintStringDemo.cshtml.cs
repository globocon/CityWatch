using ClosedXML.Excel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CityWatch.RadioCheck.Pages
{
    public class ComplintStringDemo : PageModel
    {

        public List<List<string>> TableData { get; set; }
        private readonly string filePath;

        private readonly IWebHostEnvironment _env;
        [BindProperty]
        public List<string> NewRowData { get; set; }
        public ComplintStringDemo(IWebHostEnvironment env)
        {
            _env = env;
            filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "jotform", "tableDataDemo.xlsx");
        }







        public void OnGet()
        {
            TableData = ReadExcel();
        }

        public IActionResult OnPostDeleteRow(int rowIndex)
        {
            // Load the workbook
            using (var workbook = new XLWorkbook(filePath))
            {
                var worksheet = workbook.Worksheet(1);

                // Delete the row from Excel (note: Excel is 1-based)
                worksheet.Row(rowIndex + 1).Delete();

                // Save back
                workbook.Save();
            }

            return RedirectToPage(); // Refresh the page
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
                        rowData.Add(cell.GetValue<string>());
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
    }
}

