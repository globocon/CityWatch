using CityWatch.Data.Providers;
using CityWatch.Web.Pages.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace CityWatch.Web.Pages
{
    public class ControlRoomModel : PageModel
    {
        private readonly IConfigDataProvider _configDataProvider;
        public string ClientNameTitle { get; set; }
        public ControlRoomModel( IConfigDataProvider configDataProvider)
        {
            
            _configDataProvider = configDataProvider;
        }
        public void OnGet()
        {
            var host = HttpContext.Request.Host.Host;
            var hostParts = host.Split('.');

            // Extract the client name
            string clientName = hostParts.Length > 1 && hostParts[0].Trim().ToLower() == "www"
                                ? hostParts[1]
                                : hostParts[0];
            var domain = _configDataProvider.GetSubDomainDetails(clientName);
            if (domain != null)
            {

                ClientNameTitle = _configDataProvider.GetSubDomainDetails(clientName).Domain;
            }
            else
            {

                ClientNameTitle = "Citywatch Security";
            }
        }
    }
}
