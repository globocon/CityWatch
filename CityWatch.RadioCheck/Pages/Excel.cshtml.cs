using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

namespace CityWatch.RadioCheck.Pages
{
    public class ExcelModel : PageModel
    {
        

        private readonly IWebHostEnvironment _env;

        public ExcelModel(IWebHostEnvironment env)
        {
            _env = env;
        }

        public List<string> Files { get; set; } = new List<string>();
        public string FormName { get; set; } = string.Empty;
        public string WorkOrder { get; set; } = string.Empty;

        public void OnGet()
        {
            FormName = Request.Query["formName"];
            WorkOrder = Request.Query["workOrder"];

            Console.WriteLine($"Received request - FormName: {FormName}, WorkOrder: {WorkOrder}");

            if (!string.IsNullOrEmpty(FormName) && !string.IsNullOrEmpty(WorkOrder))
            {
                // Construct folder path
                string folderPath = Path.Combine(_env.WebRootPath, "uploads", "jotform", FormName, WorkOrder);
                Console.WriteLine($"Checking folder path: {folderPath}");

                if (Directory.Exists(folderPath))
                {
                    try
                    {
                        Files = Directory.GetFiles(folderPath, "*.xlsx") // Fetch only .xlsx files
                                       .Select(Path.GetFileName)
                                       .ToList();

                        Console.WriteLine($"Found {Files.Count} Excel files.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error accessing folder: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("Target folder does not exist.");
                }
            }
            else
            {
                Console.WriteLine("Invalid query parameters. Ensure 'formName' and 'workOrder' are provided.");
            }
        }

    }
}
