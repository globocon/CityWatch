using CityWatch.Data.Providers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CityWatch.Web.Pages.Incident
{
    public class ToolsModel : PageModel
    {

        private readonly IClientDataProvider _clientDataProvider;
        public ToolsModel(
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
