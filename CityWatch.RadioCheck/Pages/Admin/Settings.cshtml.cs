using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;

using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System;
using System.IO;
using System.Linq;
using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Collections.Generic;
using System.Security.Claims;
using static Dropbox.Api.TeamLog.EventCategory;
using MailKit.Net.Smtp;

namespace CityWatch.RadioCheck.Pages.Admin
{
    public class SettingsModel : PageModel
    {
        private readonly IClientDataProvider _clientDataProvider;
        //private readonly IUserDataProvider _userDataProvider;
        public readonly IConfigDataProvider _configDataProvider;
        private readonly IWebHostEnvironment _webHostEnvironment;
        

        public SettingsModel(IWebHostEnvironment webHostEnvironment,
            IClientDataProvider clientDataProvider,
            IConfigDataProvider configDataProvider
            //,
            //IUserDataProvider userDataProvider
            )
        {
            _clientDataProvider = clientDataProvider;
            _configDataProvider = configDataProvider;
            //_userDataProvider = userDataProvider;
            _webHostEnvironment = webHostEnvironment;
        }

      

        public IConfigDataProvider ConfigDataProiver { get { return _configDataProvider; } }

        //public IUserDataProvider UserDataProvider { get { return _userDataProvider; } }

        public IClientDataProvider ClientDataProvider { get { return _clientDataProvider; } }

        [BindProperty]
        public FeedbackTemplate FeedbackTemplate { get; set; }
        [BindProperty]
        public FeedbackType FeedbackNewType { get; set; }
        [BindProperty]
        public CompanyDetails CompanyDetails { get; set; }

        [BindProperty]
        public ReportTemplate ReportTemplate { get; set; }

        public IActionResult OnGet()
        {
            //if (!AuthUserHelper.IsAdminUserLoggedIn)
            //    return Redirect(Url.Page("/Account/Unauthorized"));

            //ReportTemplate = _configDataProvider.GetReportTemplate();
            return Page();
        }

        //public JsonResult OnGetClientTypes(int? page, int? limit)
        //{
        //    return new JsonResult(_viewDataService.GetUserClientTypesHavingAccess(AuthUserHelper.LoggedInUserId));
        //}

        //public JsonResult OnGetClientSites(int? page, int? limit, int? typeId, string searchTerm)
        //{
        //    return new JsonResult(_viewDataService.GetUserClientSitesHavingAccess(typeId, AuthUserHelper.LoggedInUserId, searchTerm));
        //}
        public JsonResult OnGetRadioCheckStatusColorCode()
        {
            return new JsonResult(_configDataProvider.GetRadioCheckStatusColorCode(null));
        }
        public JsonResult OnGetRadioCheckStatusWithOutcome()
        {
            return new JsonResult(_configDataProvider.GetRadioCheckStatusWithOutcome());
        }

        public JsonResult OnPostRadioCheckStatus(RadioCheckStatus record)
        {
            var status = true;
            var message = "Success";
            try
            {
                if (record.RadioCheckStatusColorName != null)
                {
                    record.RadioCheckStatusColorId = _configDataProvider.GetRadioCheckStatusColorCode(record.RadioCheckStatusColorName).Select(x => x.Id).FirstOrDefault();
                }
                if (record.Id == -1)
                {

                    int LastOne = _configDataProvider.GetRadioCheckStatusCount();
                    if (LastOne != null)
                    {
                        LastOne++;
                        string numberAsString = LastOne.ToString();
                        if (numberAsString.Length == 1)
                        {

                            record.ReferenceNo = "0" + LastOne;
                        }
                        else
                        {
                            record.ReferenceNo = LastOne.ToString();
                        }


                    }
                }
                _clientDataProvider.SaveRadioCheckStatus(record);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }


        public JsonResult OnPostDeleteRadioCheckStatus(int id)
        {
            var status = true;
            var message = "Success";
            try
            {
                _clientDataProvider.DeleteRadioCheckStatus(id);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }

    
      
     
    }
}
