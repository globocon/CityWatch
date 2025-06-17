
using CityWatch.Data.Helpers;
using CityWatch.Data.Providers;
using CityWatch.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private readonly IConfigDataProvider _configDataProvider;
        public string ClientNameTitle { get; set; }
        public NotifyModel(
          IConfigDataProvider configDataProvider
          )
        {
            
            _configDataProvider = configDataProvider;
            
        }
        public void OnGet()
        {
            FilePath = "#";
            string fileName = Convert.ToString(TempData["ReportFileName"]);
            if (!string.IsNullOrEmpty(fileName))
                FilePath = @Url.Content($"~/Pdf/ToDropbox/{ fileName }");

            ErrorMessage = Convert.ToString(TempData["Error"]);
            ReportGenerated = Convert.ToBoolean(TempData["ReportGenerated"]);

            HttpContext.Session.Remove("ReportReference");
            var host = HttpContext.Request.Host.Host;
            var hostParts = host.Split('.');

            // Extract the client name
            string clientName = hostParts.Length > 1 && hostParts[0].Trim().ToLower() == "www"
                                ? hostParts[1]
                                : hostParts[0];
            if (!string.IsNullOrEmpty(clientName))
            {
                if (
                    clientName.Trim().ToLower() != "www" &&
                    clientName.Trim().ToLower() != "cws-ir" &&
                    clientName.Trim().ToLower() != "test"
                    &&
                    clientName.Trim().ToLower() != "localhost"
                )
                {
                    int domain = _configDataProvider.GetSubDomainDetails(clientName).TypeId;
                    if (domain != 0)
                    {

                        ClientNameTitle = _configDataProvider.GetSubDomainDetails(clientName).Domain;
                    }
                    else
                    {

                        ClientNameTitle = "Citywatch Security";
                    }
                }
                else
                {

                    ClientNameTitle = "Citywatch Security";
                }
            }
            }
    }
}
