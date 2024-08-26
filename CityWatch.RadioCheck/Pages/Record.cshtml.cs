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

        public IActionResult OnPostSaveAudioAsync()
        {
            var file = Request.Form.Files["audioFile"];
            if (file != null && file.Length > 0)
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", file.FileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                     file.CopyToAsync(stream);
                }

                return new JsonResult("Audio uploaded successfully.");
            }

            return new JsonResult("Audio upload failed.");
        }
    }
}
