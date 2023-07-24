using CityWatch.Data.Providers;
using CityWatch.Web.Extensions;
using CityWatch.Web.Helpers;
using CityWatch.Web.Models;
using CityWatch.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Web.Pages.Radio
{
    public class CheckModel : PageModel
    {
        public readonly IClientSiteRadioStatusDataProvider _radioStatusDataProvider;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IViewDataService _viewDataService;

        public CheckModel(IClientSiteRadioStatusDataProvider radioStatusDataProvider,
            IClientDataProvider clientDataProvider, IViewDataService viewDataService)
        {
            _radioStatusDataProvider = radioStatusDataProvider;
            _clientDataProvider = clientDataProvider;
            _viewDataService = viewDataService;
        }

        public ActionResult OnGet()
        {
            if (!AuthUserHelper.IsAdminUserLoggedIn)
                return Redirect(Url.Page("/Account/Unauthorized"));

            return Page();
        }

        public JsonResult OnGetRadioStatus(string clientSiteIds, DateTime? weekStart)
        {
            var results = new List<WeekRadioStatus>();
            if (!string.IsNullOrEmpty(clientSiteIds))
            {
                var arClientSiteIds = clientSiteIds.Split(',').Select(z => int.Parse(z)).ToArray();
                weekStart ??= DateTime.Today.StartOfWeek();

                var csRadioStatusThisWeek = _radioStatusDataProvider.GetClientSiteRadioStatus(arClientSiteIds, weekStart.Value);
                var clientSites = _clientDataProvider.GetClientSites(null);
                foreach (var clientSiteId in arClientSiteIds)
                {
                    var csRadioStatuses = csRadioStatusThisWeek.Where(z => z.ClientSiteId ==  clientSiteId);
                    if (!csRadioStatuses.Any())
                    {
                        var clientSite = clientSites.SingleOrDefault(z => z.Id == clientSiteId);
                        csRadioStatuses = RadioCheckHelper.GetEmptyClientSiteRadioStatus(clientSite, weekStart.Value);
                        _radioStatusDataProvider.SaveClientSiteRadioStatus(csRadioStatuses);
                    }
                    results.Add(new WeekRadioStatus(csRadioStatuses.ToList()));
                }
            }

            return new JsonResult(results);
        }

        public JsonResult OnPostSaveDayRadioStatus(int clientSiteId, DateTime? weekStart, int dateOffset, int checkNumber, string newValue)
        {
            var success = true;
            var message = "success";
            try
            {
                var statusDate = (weekStart ??= DateTime.Today.StartOfWeek()).AddDays(dateOffset);
                _radioStatusDataProvider.SaveClientSiteRadioCheck(clientSiteId, statusDate, checkNumber, newValue);
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
            }

            return new JsonResult(new { success, message });
        }
        
        public IActionResult OnGetClientSites(string type)
        {
            return new JsonResult(_viewDataService.GetUserClientSites(type, string.Empty));
        }
    }
}
