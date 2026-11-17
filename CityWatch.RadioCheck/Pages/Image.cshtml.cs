using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using Newtonsoft.Json;
using CityWatch.RadioCheck.Models;

namespace CityWatch.RadioCheck.Pages
{
    public class ImageModel : PageModel
    {
        //private readonly IWebHostEnvironment _env;

        //public ImageModel(IWebHostEnvironment env)
        //{
        //    _env = env;
        //}

        //public List<string> Files { get; set; } = new List<string>();
        //public string FolderId { get; set; } = string.Empty;

        //public void OnGet()
        //{
        //    FolderId = Request.Query["folderId"];
        //    Console.WriteLine($"FolderId: {FolderId}");

        //    if (!string.IsNullOrEmpty(FolderId))
        //    {
        //        string folderPath = Path.Combine(_env.WebRootPath, "uploads", FolderId);
        //        Console.WriteLine($"Checking folder: {folderPath}");

        //        if (Directory.Exists(folderPath))
        //        {
        //            Files = Directory.GetFiles(folderPath)
        //                .Where(file => !file.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        //                .Select(Path.GetFileName)
        //                .ToList();

        //            Console.WriteLine($"Found {Files.Count} files.");
        //        }
        //        else
        //        {
        //            Console.WriteLine("Folder does not exist.");
        //        }
        //    }
        //}


        private readonly IWebHostEnvironment _env;

        public ImageModel(IWebHostEnvironment env)
        {
            _env = env;
        }

        //public List<string> Files { get; set; } = new List<string>();
        //public string FormName { get; set; } = string.Empty;
        //public string WorkOrder { get; set; } = string.Empty;

        //public void OnGet()
        //{
        //    FormName = Request.Query["formName"];
        //    WorkOrder = Request.Query["workOrder"];

        //    Console.WriteLine($"FormName: {FormName}, WorkOrder: {WorkOrder}");

        //    if (!string.IsNullOrEmpty(FormName) && !string.IsNullOrEmpty(WorkOrder))
        //    {
        //        // Construct folder path
        //        string folderPath = Path.Combine(_env.WebRootPath, "uploads", "jotform", FormName, WorkOrder);
        //        Console.WriteLine($"Checking folder: {folderPath}");

        //        if (Directory.Exists(folderPath))
        //        {
        //            Files = Directory.GetFiles(folderPath)
        //        .Where(file => !file.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) &&
        //                       !file.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) &&
        //                       !file.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
        //                       ) // Exclude .txt and .xlsx files
        //        .Select(Path.GetFileName)
        //        .ToList();

        //            Console.WriteLine($"Found {Files.Count} files.");
        //        }
        //        else
        //        {
        //            Console.WriteLine("Folder does not exist.");
        //        }
        //    }
        //    else
        //    {
        //        Console.WriteLine("Invalid query parameters.");
        //    }
        //}



        public List<string> Files { get; set; } = new List<string>();
        public Dictionary<string, string> ImageCaptions { get; set; } = new Dictionary<string, string>();
        public string FormName { get; set; } = string.Empty;
        public string WorkOrder { get; set; } = string.Empty;

        public void OnGet()
        {
            FormName = Request.Query["formName"];
            WorkOrder = Request.Query["workOrder"];

            Console.WriteLine($"FormName: {FormName}, WorkOrder: {WorkOrder}");

            if (!string.IsNullOrEmpty(FormName) && !string.IsNullOrEmpty(WorkOrder))
            {
                // Construct folder path
                string folderPath = Path.Combine(_env.WebRootPath, "uploads", "jotform", FormName, WorkOrder);
                string jsonFilePath = Path.Combine(folderPath, "image_captions.json");

                Console.WriteLine($"Checking folder: {folderPath}");

                if (Directory.Exists(folderPath))
                {
                    Files = Directory.GetFiles(folderPath)
                        .Where(file => !file.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) &&
                                       !file.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) &&
                                       !file.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) // Exclude non-image files
                        .Select(Path.GetFileName)
                        .ToList();

                    Console.WriteLine($"Found {Files.Count} files.");

                    // Read image captions if JSON exists
                    if (System.IO.File.Exists(jsonFilePath))
                    {
                        try
                        {
                            string jsonData = System.IO.File.ReadAllText(jsonFilePath);
                            List<ImageCaptionModel> captionsList = JsonConvert.DeserializeObject<List<ImageCaptionModel>>(jsonData);
                            ImageCaptions = captionsList.ToDictionary(c => c.ImageName, c => c.Caption);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error reading JSON: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No image captions found.");
                    }
                }
                else
                {
                    Console.WriteLine("Folder does not exist.");
                }
            }
            else
            {
                Console.WriteLine("Invalid query parameters.");
            }
        }
    }
}

