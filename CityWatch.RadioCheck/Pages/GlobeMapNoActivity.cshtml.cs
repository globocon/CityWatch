using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CityWatch.RadioCheck.Pages
{
    public class GlobeMapNoActivityModel : PageModel
    {
        public IActionResult OnGet(string displayItem)
        {
            return Page();
        }
    }
}
