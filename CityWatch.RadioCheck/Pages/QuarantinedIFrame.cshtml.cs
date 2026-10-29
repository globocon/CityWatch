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
    public class QuarantinedIFrame : PageModel
    {

        private readonly CityWatchDbContext _context;
        public List<UserInput> UserInputs { get; set; } = new List<UserInput>();

        public List<List<string>> TableData { get; set; }
        private readonly string filePath;

        private readonly IWebHostEnvironment _env;
        [BindProperty]
        public List<string> NewRowData { get; set; }
        public QuarantinedIFrame(CityWatchDbContext context)
        {
            filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "jotform", "QuarantinedStringsData", "QuarantinedStringsData.xlsx");
            _context = context;
        }
        public async Task OnGetAsync()
        {
            UserInputs = await _context.UserInput
                                   .OrderByDescending(u => u.UpdatedDate)
                                   .ToListAsync();
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
