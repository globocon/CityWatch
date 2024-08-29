using CityWatch.Common.Services;
using CityWatch.Data.Helpers;
using CityWatch.Data.Providers;
using CityWatch.Web.Pages.Guard;
using CityWatch.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CityWatch.Web.Pages
{
    public class RecordModel : PageModel
    {
        private readonly IWebHostEnvironment _WebHostEnvironment;
        public RecordModel(IWebHostEnvironment webHostEnvironment     
            )
        {
          
            _WebHostEnvironment = webHostEnvironment;
            
        }
        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostSaveAudioAsync(IFormFile audioFile)
        {
            if (audioFile == null || audioFile.Length == 0)
            {
                return BadRequest("No file received or file is empty.");
            }

            // Get the current date and time ticks for unique file naming
            var ticks = DateTime.UtcNow.Ticks;

            // Extract the file extension
            var fileExtension = Path.GetExtension(audioFile.FileName);

            // Generate a unique file name using ticks and current date
            var formattedDate = DateTime.Today.ToString("dd_MM_yyyy");
            var uniqueFileName = $"recording_{formattedDate}_{ticks}{fileExtension}";

            // Define the folder name and path for audio recording
            var folderNameForAudioRecording = formattedDate;
            var folderPath = Path.Combine(_WebHostEnvironment.WebRootPath, "AudioRecordings", folderNameForAudioRecording);

            // Ensure the directory exists
            Directory.CreateDirectory(folderPath);  // CreateDirectory checks for existence before creating, no need for if-check

            // Save the file to the server
            var filePath = Path.Combine(folderPath, uniqueFileName);
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await audioFile.CopyToAsync(stream);
            }

            // Return the result with the unique file name
            return new JsonResult(new { success = true, fileName = uniqueFileName });
        }
    }
}
