using CityWatch.Data.Providers;
using CityWatch.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System;

namespace CityWatch.RadioCheck.Pages
{
    public class ControlRoomMapModel : PageModel
    {
        private readonly IClientDataProvider _clientDataProvider;
        private readonly Settings _settings;

        public ControlRoomMapModel(IClientDataProvider clientDataProvider, IOptions<Settings> settings)
        {
            _clientDataProvider = clientDataProvider;
            _settings = settings.Value;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        public JsonResult OnGetSiteInfo(int clientSiteId)
        {
            var siteImage = string.Empty;
            var kpiSetting = _clientDataProvider.GetClientSiteKpiSetting(clientSiteId);
            if (kpiSetting != null && !string.IsNullOrEmpty(kpiSetting.SiteImage) && !string.IsNullOrEmpty(_settings.KpiWebUrl))
            {
                siteImage = $"{new Uri(_settings.KpiWebUrl)}{kpiSetting.SiteImage}";
            }
            return new JsonResult(new { siteImage });
        }
    }
}
