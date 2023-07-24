
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.IO;

namespace CityWatch.Web.Pages.Incident
{
    public class NotifyModel : PageModel
    {
        [BindProperty]
        public string FilePath { get; set; }

        [BindProperty]
        public string ErrorMessage { get; set; }

        [BindProperty]
        public bool ReportGenerated { get; set; }

        public void OnGet()
        {
            FilePath = "#";
            string fileName = Convert.ToString(TempData["ReportFileName"]);
            if (!string.IsNullOrEmpty(fileName))
                FilePath = @Url.Content($"~/Pdf/ToDropbox/{ fileName }");

            ErrorMessage = Convert.ToString(TempData["Error"]);
            ReportGenerated = Convert.ToBoolean(TempData["ReportGenerated"]);

            HttpContext.Session.Remove("ReportReference");
        }
    }
}
