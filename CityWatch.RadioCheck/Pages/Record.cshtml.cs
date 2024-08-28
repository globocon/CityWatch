using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

            var filePath = Path.Combine("wwwroot/AudioRecordings", audioFile.FileName); // Adjust the path as needed

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await audioFile.CopyToAsync(stream);
            }

            // Optionally, return some result or status
            return new JsonResult(new { success = true, fileName = audioFile.FileName });
        }
    }
}
