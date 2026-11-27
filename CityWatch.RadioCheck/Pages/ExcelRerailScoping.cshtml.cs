using CityWatch.Data.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CityWatch.RadioCheck.Pages
{
    public class ExcelRerailScopingModel : PageModel
    {


        private readonly IWebHostEnvironment _env;

        public ExcelRerailScopingModel(IWebHostEnvironment env)
        {
            _env = env;
        }

        public List<string> Files { get; set; } = new List<string>();        
        public string _formNameFolder { get; set; } = string.Empty;
        public string _workOrder { get; set; } = string.Empty;
        public string TemplateUrl { get; set; } = string.Empty;
        
        public void OnGet()
        {
            _formNameFolder = Request.Query["formName"];
            _workOrder = Request.Query["workOrder"];

            Console.WriteLine($"Received request - Form Name: {_formNameFolder}, WorkOrder: {_workOrder}");

            if (!string.IsNullOrEmpty(_formNameFolder) && !string.IsNullOrEmpty(_workOrder))
            {
                // Construct folder path                
                string folderPath = Path.Combine(_env.WebRootPath, "uploads", "jotform", _formNameFolder, _workOrder);
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

            if (!string.IsNullOrWhiteSpace(_formNameFolder))
            {
                string folderPath = Path.Combine(_env.WebRootPath, "uploads", "jotform", _formNameFolder);
                Console.WriteLine($"Checking folder path: {folderPath}");

                if (Directory.Exists(folderPath))
                {
                    try
                    {


                        // Check for Fortescue_Rerail_Scoping_Desktop.xlsx specifically
                        string templatePath = Path.Combine(folderPath, "Fortescue_Rerail_Scoping_Desktop.xlsx");
                        if (System.IO.File.Exists(templatePath))
                        {
                            // Construct the downloadable URL
                            string downloadUrl = Path.Combine("/uploads/jotform", _formNameFolder, "Fortescue_Rerail_Scoping_Desktop.xlsx").Replace("\\", "/");
                            Console.WriteLine($"Template file found. Download URL: {downloadUrl}");

                            TemplateUrl = downloadUrl; 
                        }
                        else
                        {
                            Console.WriteLine("Fortescue_Rerail_Scoping_Desktop.xlsx not found.");
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
