using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CityWatch.Web.Pages
{
    public class RecordModel : PageModel
    {
        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostSaveAudioAsync(IFormFile audioFile)
        {
            if (audioFile == null || audioFile.Length == 0)
            {
                return BadRequest("No file received or file is empty.");
            }

            // Get the current date and time ticks
            var ticks = DateTime.UtcNow.Ticks;

            // Extract the file extension
            var fileExtension = Path.GetExtension(audioFile.FileName);

            // Generate a unique file name using ticks
            var uniqueFileName = $"recording_{ticks}{fileExtension}";

            // Combine the file path
            var filePath = Path.Combine("wwwroot/AudioRecordings", uniqueFileName); // Adjust the path as needed

            // Ensure the directory exists
            if (!Directory.Exists("wwwroot/AudioRecordings"))
            {
                Directory.CreateDirectory("wwwroot/AudioRecordings");
            }

            // Save the file to the server
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await audioFile.CopyToAsync(stream);
            }

            // Return the result with the unique file name
            return new JsonResult(new { success = true, fileName = uniqueFileName });
        }
    }
}
