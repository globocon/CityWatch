using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Hosting;
using System;

namespace CityWatch.Kpi.Pages.Develop
{
    public class IndexModel : PageModel
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public IndexModel(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public void OnGet()
        {
            if (!_webHostEnvironment.IsDevelopment())
            {
                throw new NotSupportedException("Page is not available in production environment");
            }
        }
    }
}
