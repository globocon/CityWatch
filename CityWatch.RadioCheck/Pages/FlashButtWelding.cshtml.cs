using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using Humanizer;

namespace CityWatch.RadioCheck.Pages
{
    public class FlashButtWeldingModel : PageModel
    {


        private readonly IWebHostEnvironment _env;

        public FlashButtWeldingModel(IWebHostEnvironment env)
        {
            _env = env;
        }

        public List<string> Files { get; set; } = new List<string>();        
        public string _dateWiseFolder { get; set; } = string.Empty;
        public string _dateString { get; set; } = string.Empty;
        public string _supervisor { get; set; } = string.Empty;
        public string TemplateUrl { get; set; } = string.Empty;
        
        public void OnGet()
        {            
            _dateWiseFolder = Request.Query["welddate"];
            _dateWiseFolder = _dateWiseFolder.Replace(" ", "");
            _supervisor = Request.Query["supervisor"];

            Console.WriteLine($"Received request - Welding Date: {_dateWiseFolder}, Supervisor: {_supervisor}");

            if (!string.IsNullOrEmpty(_dateWiseFolder) && !string.IsNullOrEmpty(_supervisor))
            {
                _dateString = DateTime.ParseExact(_dateWiseFolder, "ddMMyyyy", null).ToString("dd-MM-yyyy");
                // Construct folder path                
                string folderPath = Path.Combine(_env.WebRootPath, "uploads", "jotform", "Flashbutt", _dateWiseFolder, _supervisor);
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

            if (!string.IsNullOrWhiteSpace(_dateWiseFolder))
            {
                string folderPath = Path.Combine(_env.WebRootPath, "uploads", "jotform", "Flashbutt");
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
                            string downloadUrl = Path.Combine("/uploads/jotform", "Flashbutt", "Template.xlsx").Replace("\\", "/");
                            Console.WriteLine($"Template file found. Download URL: {downloadUrl}");

                            TemplateUrl = downloadUrl; 
                        }
                        else
                        {
                            Console.WriteLine("Template.xlsx not found.");
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
