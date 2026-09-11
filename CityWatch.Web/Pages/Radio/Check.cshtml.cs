using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Helpers;
using CityWatch.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace CityWatch.Web.Pages.Radio
{
    public class CheckModel : PageModel
    {
        public readonly IClientSiteRadioStatusDataProvider _radioStatusDataProvider;
        private readonly IClientSiteActivityStatusDataProvider _clientSiteActivityStatusDataProvider;
        private readonly IRadioCheckViewDataService _radioCheckViewDataService;
        private readonly IClientSiteViewDataService _clientViewDataService;

        public CheckModel(IClientSiteRadioStatusDataProvider radioStatusDataProvider,
            IClientSiteActivityStatusDataProvider clientSiteActivityStatusDataProvider,
            IRadioCheckViewDataService radioCheckViewDataService,
            IClientSiteViewDataService clientViewDataService)
        {
            _radioStatusDataProvider = radioStatusDataProvider;
            _clientSiteActivityStatusDataProvider = clientSiteActivityStatusDataProvider;
            _radioCheckViewDataService = radioCheckViewDataService;
            _clientViewDataService = clientViewDataService;
        }

        public ActionResult OnGet()
        {


            if (AuthUserHelper.IsAdminUserLoggedIn)
            {
                _radioCheckViewDataService.ResetClientSiteActivityStatus();

                return Page();
            }
            else if (AuthUserHelper.LoggedInUserId == null)
            {
                return Redirect(Url.Page("/Account/Unauthorized"));
            }
            else
            {
                _radioCheckViewDataService.ResetClientSiteActivityStatus();

                return Page();
            }
        }

        public IActionResult OnGetClientSites(string type)
        {
            return new JsonResult(_clientViewDataService.GetUserClientSitesWithId(type).OrderBy(z => z.Text));
        }

        public IActionResult OnGetClientSiteActivityStatus(string clientSiteIds)
        {
            var arClientSiteIds = !string.IsNullOrEmpty(clientSiteIds) ?
                                    clientSiteIds.Split(',').Select(z => int.Parse(z)).ToArray() :
                                    Array.Empty<int>();
            return new JsonResult(_radioCheckViewDataService.GetClientSiteActivityStatuses(arClientSiteIds));
        }

        public JsonResult OnPostSaveRadioStatus(int clientSiteId, int guardId, string checkedStatus)
        {
            var success = true;
            var message = "success";
            try
            {
                _clientSiteActivityStatusDataProvider.SaveClientSiteRadioCheck(new ClientSiteRadioCheck()
                {
                    ClientSiteId = clientSiteId,
                    GuardId = guardId,
                    Status = checkedStatus,
                    CheckedAt = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }

        public IActionResult OnPostUpdateLatestActivityStatus()
        {
            _radioCheckViewDataService.UpdateLastActivityStatus();

            return new JsonResult(true);
        }
    }
}
