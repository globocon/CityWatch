using CityWatch.Data.Providers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CityWatch.Web.Pages.Incident
{
    public class ToolSelecterModel : PageModel
    {
        private readonly IClientDataProvider _clientDataProvider;
        public ToolSelecterModel(
            IClientDataProvider clientDataProvider
            )
        {
            _clientDataProvider = clientDataProvider;
           
        }
        public IClientDataProvider ClientDataProvider { get { return _clientDataProvider; } }
        public void OnGet()
        {
        }
    }
}
