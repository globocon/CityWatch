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
        public string TemplateUrl { get; set; } = string.Empty;
        public string DeliveriesExcelDataUrl { get; set; } = string.Empty;
        public string ExecutionExcelDataUrl { get; set; } = string.Empty;

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

            if (!string.IsNullOrWhiteSpace(FormName))
            {
                string folderPath = Path.Combine(_env.WebRootPath, "uploads", "jotform", FormName);
                Console.WriteLine($"Checking folder path: {folderPath}");

                if (Directory.Exists(folderPath))
                {
                    try
                    {
                      

                        // Check for Template.xlsx specifically
                        string templatePath = Path.Combine(folderPath, "Template.xlsx");
                        if (System.IO.File.Exists(templatePath))
                        {
                            // Construct the downloadable URL
                            string downloadUrl = Path.Combine("/uploads/jotform", FormName, "Template.xlsx").Replace("\\", "/");
                            Console.WriteLine($"Template file found. Download URL: {downloadUrl}");

                            TemplateUrl = downloadUrl; 
                        }
                        else
                        {
                            Console.WriteLine("Template.xlsx not found.");
                        }

                        // Check for Delivery Data.xlsx specifically
                        string deliveriesExcelDataPath = Path.Combine(folderPath, "Delivery Data.xlsx");
                        if (System.IO.File.Exists(deliveriesExcelDataPath))
                        {
                            // Construct the downloadable URL
                            string downloadUrl = Path.Combine("/uploads/jotform", FormName, "Delivery Data.xlsx").Replace("\\", "/");
                            Console.WriteLine($"Deliveries Excel Data file found. Download URL: {downloadUrl}");

                            DeliveriesExcelDataUrl = downloadUrl;
                        }
                        else
                        {
                            Console.WriteLine("Delivery Data.xlsx not found.");
                        }

                        // Check for Delivery Data.xlsx specifically
                        string executionExcelDataPath = Path.Combine(folderPath, "Execution Data.xlsx");
                        if (System.IO.File.Exists(executionExcelDataPath))
                        {
                            // Construct the downloadable URL
                            string downloadUrl = Path.Combine("/uploads/jotform", FormName, "Execution Data.xlsx").Replace("\\", "/");
                            Console.WriteLine($"Execution Excel Data file found. Download URL: {downloadUrl}");

                            ExecutionExcelDataUrl = downloadUrl;
                        }
                        else
                        {
                            Console.WriteLine("Execution Data.xlsx not found.");
                        }
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

        }
    }
}
