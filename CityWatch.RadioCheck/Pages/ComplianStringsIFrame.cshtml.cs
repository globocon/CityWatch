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
    public class ComplianStringsIFrame : PageModel
    {

        private readonly CityWatchDbContext _context;
        public List<UserInput> UserInputs { get; set; } = new List<UserInput>();

        public List<List<string>> TableData { get; set; }
        private readonly string filePath;

        private readonly IWebHostEnvironment _env;
        [BindProperty]
        public List<string> NewRowData { get; set; }
        public ComplianStringsIFrame(CityWatchDbContext context)
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

                var headerRow = range.FirstRowUsed();

                var excludedHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "String Dropped",
            "Drop Location Start (KM)",
            "Drop Location End(KM)"
        };

                // Get trimmed headers with indexes
                var headerCells = headerRow.Cells()
                    .Select((cell, index) => new
                    {
                        Header = cell.GetValue<string>().Trim(),
                        Index = index
                    })
                    .ToList();

                // Find all indexes of "Contractor"
                var contractorIndexes = headerCells
                    .Where(h => string.Equals(h.Header, "Contractor", StringComparison.OrdinalIgnoreCase))
                    .Select(h => h.Index)
                    .ToList();

                // Exclude the last Contractor
                int contractorIndexToExclude = contractorIndexes.Count > 1 ? contractorIndexes.Last() : -1;

                // Final included column indexes
                var includedColumnIndexes = headerCells
                    .Where(h =>
                        !excludedHeaders.Contains(h.Header) &&
                        !(string.Equals(h.Header, "Contractor", StringComparison.OrdinalIgnoreCase) && h.Index == contractorIndexToExclude)
                    )
                    .Select(h => h.Index)
                    .ToList();

                // Read data rows
                foreach (var row in range.Rows())
                {
                    var rowData = new List<string>();
                    var cells = row.Cells().ToList();

                    foreach (var colIndex in includedColumnIndexes)
                    {
                        if (colIndex >= cells.Count)
                        {
                            rowData.Add("");
                            continue;
                        }

                        var cell = cells[colIndex];

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
    }
}
