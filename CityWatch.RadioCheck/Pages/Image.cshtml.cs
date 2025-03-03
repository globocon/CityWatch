using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

namespace CityWatch.RadioCheck.Pages
{
    public class ImageModel : PageModel
    {
        private readonly IWebHostEnvironment _env;

        public ImageModel(IWebHostEnvironment env)
        {
            _env = env;
        }

        public List<string> Files { get; set; } = new List<string>();
        public string FolderId { get; set; } = string.Empty;

        public void OnGet()
        {
            FolderId = Request.Query["folderId"];
            Console.WriteLine($"FolderId: {FolderId}");

            if (!string.IsNullOrEmpty(FolderId))
            {
                string folderPath = Path.Combine(_env.WebRootPath, "uploads", FolderId);
                Console.WriteLine($"Checking folder: {folderPath}");

                if (Directory.Exists(folderPath))
                {
                    Files = Directory.GetFiles(folderPath)
                        .Where(file => !file.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                        .Select(Path.GetFileName)
                        .ToList();

                    Console.WriteLine($"Found {Files.Count} files.");
                }
                else
                {
                    Console.WriteLine("Folder does not exist.");
                }
            }
        }
    }
}
