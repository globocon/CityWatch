using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CityWatch.RadioCheck.Pages
{
    public class GlobeMapModel : PageModel
    {
        public IActionResult OnGet(string displayItem)
        {
            return Page();
        }
    }
}
