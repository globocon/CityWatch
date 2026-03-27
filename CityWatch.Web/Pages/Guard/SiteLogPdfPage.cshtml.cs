using CityWatch.Data.Providers;
using CityWatch.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Linq;

namespace CityWatch.Web.Pages.Guard
{
    public class SiteLogPdfPageModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty(SupportsGet = true)]
        public string t { get; set; }

        public string ClientFavicon { get; set; } = "~/favicon.ico"; // default
        public string ClientFaviconType { get; set; } = "image/x-icon";
        private readonly IConfigDataProvider _configDataProvider;

        public SiteLogPdfPageModel(IConfigDataProvider configDataProvider)
        {
            _configDataProvider = configDataProvider;
        }

        public void OnGet()
        {
            var host = HttpContext.Request.Host.Host;
            var clientName = host.Split('.').FirstOrDefault() ?? "default";

            if (!string.IsNullOrEmpty(clientName))
            {
                if (clientName.Trim() != "www" && clientName.Trim() != "cws-ir" && clientName.Trim() != "test" && clientName.Trim() != "localhost")
                {
                    var domain = _configDataProvider.GetSubDomainDetails(clientName);
                    if (domain != null)
                    {
                        ClientFavicon = "~/subdomainlogo/" + domain.Logo;
                        ClientFaviconType = FileTypeHelper.GetMimeType(domain.Logo) ?? "image/png";
                    }
                }                
            }
        }
    }
}
