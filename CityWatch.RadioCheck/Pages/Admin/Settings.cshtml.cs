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
using CityWatch.Kpi.Models;
using CityWatch.RadioCheck.Services;
using CityWatch.RadioCheck.Helpers;
using System.ComponentModel.DataAnnotations;

namespace CityWatch.RadioCheck.Pages.Admin
{
    public class SettingsModel : PageModel
    {
        private readonly IClientDataProvider _clientDataProvider;
        //private readonly IUserDataProvider _userDataProvider;
        public readonly IConfigDataProvider _configDataProvider;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IViewDataService _viewDataService;
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly IGuardLogDataProvider _guardLogDataProvider;

        public SettingsModel(IWebHostEnvironment webHostEnvironment,
            IClientDataProvider clientDataProvider,
            IConfigDataProvider configDataProvider,
            IViewDataService viewDataService
            , IGuardDataProvider guardDataProvider,
            IGuardLogDataProvider guardLogDataProvider
            //,
            //IUserDataProvider userDataProvider
            )
        {
            _clientDataProvider = clientDataProvider;
            _configDataProvider = configDataProvider;
            _viewDataService = viewDataService;
            //_userDataProvider = userDataProvider;
            _webHostEnvironment = webHostEnvironment;
            _guardDataProvider = guardDataProvider;
            _guardLogDataProvider = guardLogDataProvider;
        }



        public IConfigDataProvider ConfigDataProiver { get { return _configDataProvider; } }

        //public IUserDataProvider UserDataProvider { get { return _userDataProvider; } }
        public IViewDataService ViewDataService { get { return _viewDataService; } }
        public IClientDataProvider ClientDataProvider { get { return _clientDataProvider; } }

        public int GuardId { get; set; }
        public int loginUserIdNew { get; set; }

        [BindProperty]
        public FeedbackTemplate FeedbackTemplate { get; set; }
        [BindProperty]
        public FeedbackType FeedbackNewType { get; set; }
        [BindProperty]
        public CompanyDetails CompanyDetails { get; set; }

        [BindProperty]
        public ReportTemplate ReportTemplate { get; set; }

        [BindProperty]
        public string GuardIdCheck { get; set; }

        [BindProperty]
        public string loginUserId { get; set; }


        public IActionResult OnGet()
        {

            GuardId = HttpContext.Session.GetInt32("GuardId") ?? 0;
            GuardIdCheck = GuardId.ToString();

            loginUserIdNew = HttpContext.Session.GetInt32("GuardId") ?? 0;
            loginUserId = loginUserIdNew.ToString();


            var guard = _guardDataProvider.GetGuards().SingleOrDefault(z => z.Id == GuardId);

            if (guard != null)
            {
                if (guard.IsAdminPowerUser)
                {
                    AuthUserHelper.IsAdminPowerUser = true;
                }
                else
                {
                    AuthUserHelper.IsAdminPowerUser = false;
                }
                if (guard.IsAdminGlobal)
                {
                    AuthUserHelper.IsAdminGlobal = true;
                }
                else
                {
                    AuthUserHelper.IsAdminGlobal = false;
                }
            }
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
                    if (record.ReferenceNo != null)
                    {
                        record.ReferenceNo = record.ReferenceNo.Trim();
                        int refno;
                        if (int.TryParse(record.ReferenceNo, out refno) == false)
                        {
                            return new JsonResult(new { status = false, message = "Reference number should only contains numbers. !!!" });
                        }

                        var eventReferenceExists = _configDataProvider.GetRadioCheckStatusWithOutcome().Where(x => x.ReferenceNo.Equals(record.ReferenceNo));
                        if (eventReferenceExists.Count() > 0)
                        {
                            return new JsonResult(new { status = false, message = "Similar reference number already exists !!!" });
                        }
                    }
                    else
                    {
                        // Set Referenece number.
                        int LastOne = _configDataProvider.GetRadioCheckStatusCount();
                        string newrefnumb = "";
                        bool refok = false;
                        do
                        {

                            LastOne++;
                            newrefnumb = LastOne.ToString("00");
                            var eventReferenceExists = _configDataProvider.GetRadioCheckStatusWithOutcome().Where(x => x.ReferenceNo.Equals(newrefnumb));
                            if (eventReferenceExists.Count() < 1)
                            {
                                refok = true;
                            }


                        } while (refok == false);
                        record.ReferenceNo = newrefnumb;
                    }
                    //int LastOne = _configDataProvider.GetRadioCheckStatusCount();
                    //if (LastOne != null)
                    //{
                    //    LastOne++;
                    //    string numberAsString = LastOne.ToString();
                    //    if (numberAsString.Length == 1)
                    //    {

                    //        record.ReferenceNo = "0" + LastOne;
                    //    }
                    //    else
                    //    {
                    //        record.ReferenceNo = LastOne.ToString();
                    //    }


                    //}
                }
                else
                {
                    if (record.ReferenceNo != null)
                    {
                        record.ReferenceNo = record.ReferenceNo.Trim();
                        int refno;
                        if (int.TryParse(record.ReferenceNo, out refno) == false)
                        {
                            return new JsonResult(new { status = false, message = "Reference number should only contains numbers. !!!" });
                        }

                        var oldrefNo = _configDataProvider.GetRadioCheckStatusWithOutcome().Where(x => x.Id == record.Id).FirstOrDefault().ReferenceNo;
                        if (!oldrefNo.Equals(record.ReferenceNo))
                        {
                            var eventReferenceExists = _configDataProvider.GetRadioCheckStatusWithOutcome().Where(x => x.ReferenceNo.Equals(record.ReferenceNo));
                            if (eventReferenceExists.Count() > 0)
                            {
                                return new JsonResult(new { status = false, message = "Similar reference number already exists !!!" });
                            }
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

        //Broadcast Live events-start
        public JsonResult OnPostLiveEvents(BroadcastBannerLiveEvents record)
        {
            var status = true;
            var message = "Success";
            try
            {


                _clientDataProvider.SaveLiveEvents(record);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }
        public JsonResult OnPostDeleteLiveEvents(int id)
        {
            var status = true;
            var message = "Success";
            try
            {
                _clientDataProvider.DeleteLiveEvents(id);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }
        //To save the Gloabl Duress Email Start
        public JsonResult OnGetDuressEmail()
        {
            var Emails = _clientDataProvider.GetDuressEmails();
            var emailAddresses = string.Join(",", Emails.Select(email => email.Email));
            return new JsonResult(new { Emails = emailAddresses });
        }
        public JsonResult OnPostSaveDuressEmail(string Email)
        {
            var status = true;
            var message = "Success";
            try
            {
                _clientDataProvider.DuressGloablEmail(Email);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = Email, message = message });
        }
        //To save the Gloabl Duress Email Stop
        public JsonResult OnPostSaveHyperLinks(string Email,string TvNews,string wether)
        {
            var status = true;
            var message = "Success";
            try
            {
                _clientDataProvider.AddHyperLinks(Email, TvNews, wether);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = Email, message = message });
        }
        public JsonResult OnGetHyperLinks()
        {
            var links = _clientDataProvider.GetHyperLinksDetails().FirstOrDefault();
            
            return new JsonResult(new { Webmail = links.Webmail,TVNewsFeed= links.TVNewsFeed,WetherFeed=links.WeatherFeed });
        }
        public JsonResult OnGetGlobalComplianceAlertEmail()
        {
            var Emails = _clientDataProvider.GetGlobalComplianceAlertEmail();
            var emailAddresses = string.Join(",", Emails.Select(email => email.Email));
            return new JsonResult(new { Emails = emailAddresses });
        }
        public JsonResult OnPostSaveGlobalComplianceAlertEmail(string Email)
        {
            var status = true;
            var message = "Success";
            try
            {
                _clientDataProvider.GlobalComplianceAlertEmail(Email);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = Email, message = message });
        }

        //To save the Clobal Duress SMS numbers start
        public JsonResult OnGetGetGlobalSmsNumberList()
        {
            var SmsNumbers = _clientDataProvider.GetDuressSms();
            return new JsonResult(SmsNumbers);
        }
        public JsonResult OnPostDeleteGlobalSmsNumber(int SmsNumberId)
        {
            bool Success = false;
            string message = string.Empty;
            message = _clientDataProvider.DeleteDuressGloablSMSNumber(SmsNumberId, out Success);
            return new JsonResult(new { status = Success, message = message });
        }
        public JsonResult OnPostAddGlobalSmsNumber(GlobalDuressSms SmsNumber)
        {
            bool Success = false;
            string message = string.Empty;
            var claimsIdentity = User.Identity as ClaimsIdentity;
            string sidValue = "";
            var UserId1 = claimsIdentity.Claims;
            foreach (var item in UserId1)
            {
                if (item.Type == ClaimTypes.Sid)
                {
                    sidValue = item.Value;
                    break;
                }
            }
            SmsNumber.RecordCreateUserId = Convert.ToInt32(sidValue);
            message = _clientDataProvider.SaveDuressGloablSMS(SmsNumber, out Success);
            return new JsonResult(new { status = Success, message = message });
        }
        //To save the Clobal Duress SMS numbers stop
        public JsonResult OnGetBroadcastLiveEvents()
        {
            return new JsonResult(_configDataProvider.GetBroadcastLiveEvents());
        }

        //Broadcast Live events-end
        //Broadcast Calendar events-start

        public JsonResult OnPostCalendarEvents(BroadcastBannerCalendarEvents record)
        {
            var status = true;
            var message = "";
            try
            {
                if ((record.StartDate == null) || (record.ExpiryDate == null))
                {
                    return new JsonResult(new { status = false, message = "Please check the dates. !!!" });
                }
                if (record.StartDate > record.ExpiryDate)
                {
                    return new JsonResult(new { status = false, message = "Please check the dates. !!!" });
                }
                if (record.TextMessage == null)
                {
                    return new JsonResult(new { status = false, message = "Event message cannot be empty. !!!" });
                }

                if (record.id == -1)
                {
                    message = "Event added successfully.";
                    var existsevents = _configDataProvider.GetBroadcastCalendarEvents().Where(x => x.ExpiryDate == record.ExpiryDate && x.StartDate == record.StartDate);
                    if (existsevents.Count() > 0)
                    {
                        return new JsonResult(new { status = false, message = "Another event with similar dates exists !!!" });
                    }

                    if (record.ReferenceNo != null)
                    {
                        record.ReferenceNo = record.ReferenceNo.Trim();
                        int refno;
                        if (int.TryParse(record.ReferenceNo, out refno) == false)
                        {
                            return new JsonResult(new { status = false, message = "Reference number should only contains numbers. !!!" });
                        }

                        //var eventReferenceExists = _configDataProvider.GetBroadcastCalendarEvents().Where(x => x.ReferenceNo.Equals(record.ReferenceNo));
                        //if (eventReferenceExists.Count() > 0)
                        //{
                        //    return new JsonResult(new { status = false, message = "Similar reference number already exists !!!" });
                        //}
                    }
                    else
                    {
                        // Set Referenece number.
                        int LastOne = _configDataProvider.GetCalendarEventsCount();
                        string newrefnumb = "";
                        bool refok = false;
                        do
                        {

                            LastOne++;
                            newrefnumb = LastOne.ToString("00");
                            var eventReferenceExists = _configDataProvider.GetBroadcastCalendarEvents().Where(x => x.ReferenceNo.Equals(newrefnumb));
                            if (eventReferenceExists.Count() < 1)
                            {
                                refok = true;
                            }


                        } while (refok == false);
                        record.ReferenceNo = newrefnumb;
                    }

                }
                else
                {
                    if (record.ReferenceNo != null)
                    {
                        record.ReferenceNo = record.ReferenceNo.Trim();
                        int refno;
                        if (int.TryParse(record.ReferenceNo, out refno) == false)
                        {
                            return new JsonResult(new { status = false, message = "Reference number should only contains numbers. !!!" });
                        }
                    }
                    message = "Event updated successfully.";
                }
                _clientDataProvider.SaveCalendarEvents(record);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }
        public JsonResult OnPostDeleteCalendarEvents(int id)
        {
            var status = true;
            var message = "Success";
            try
            {
                _clientDataProvider.DeleteCalendarEvents(id);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }
        public JsonResult OnGetBroadcastCalendarEvents()
        {
            return new JsonResult(_configDataProvider.GetBroadcastCalendarEvents());
        }
        //Broadcast Calendar events-end
        /* log book site settings*/
        public JsonResult OnGetClientSiteWithSettings(string type, string searchTerm)
        {
            if (!string.IsNullOrEmpty(type) || !string.IsNullOrEmpty(searchTerm))
            {
                var clientType = _clientDataProvider.GetClientTypes().SingleOrDefault(z => z.Name == type);
                if (clientType != null)
                {
                    var clientSites = _clientDataProvider.GetUserClientSites(type, searchTerm);


                    return new JsonResult(clientSites.Select(z => new
                    {
                        z.Id,
                        ClientTypeName = z.ClientType.Name,
                        ClientSiteName = z.Name,
                        HasSettings = true
                    }));

                }
                else
                {
                    var clientSites = _clientDataProvider.GetUserClientSites(type, searchTerm);
                    return new JsonResult(clientSites.Select(z => new
                    {
                        z.Id,
                        ClientTypeName = z.ClientType.Name,
                        ClientSiteName = z.Name,
                        HasSettings = true
                    }));
                }
            }
            return new JsonResult(new { });
        }


        public JsonResult OnPostSaveSiteIdForRcLogBook(int id)
        {
            var status = true;
            var message = "Success";
            try
            {
                _clientDataProvider.SaveClientSiteForRcLogBook(id);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status, message });
        }

        public JsonResult OnGetClientSiteForRcLogBook()
        {
            var clientSite = _clientDataProvider.GetClientSiteForRcLogBook();
            return new JsonResult(clientSite.Select(z => new
            {
                z.Id,
                ClientTypeName = z.ClientType.Name,
                ClientSiteName = z.Name,
                HasSettings = true
            }));

        }
        //API Call Settings-start
        /*SWChannels - start*/
        public JsonResult OnPostSWChannel(SWChannels record)
        {
            var status = true;
            var message = "Success";
            try
            {


                _clientDataProvider.SaveSWChannel(record);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }
        public JsonResult OnPostDeleteSWChannel(int id)
        {
            var status = true;
            var message = "Success";
            try
            {
                _clientDataProvider.DeleteSWChannel(id);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }
        public JsonResult OnGetSWChannels()
        {
            return new JsonResult(_configDataProvider.GetSWChannels());
        }
        /*SWChannels - end*/
        /*GeneralFeeds - start*/
        public JsonResult OnPostGeneralFeeds(GeneralFeeds record)
        {
            var status = true;
            var message = "Success";
            try
            {


                _clientDataProvider.SaveGeneralFeeds(record);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }
        public JsonResult OnPostDeleteGeneralFeeds(int id)
        {
            var status = true;
            var message = "Success";
            try
            {
                _clientDataProvider.DeleteGeneralFeeds(id);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }
        public JsonResult OnGetGeneralFeeds()
        {
            return new JsonResult(_configDataProvider.GetGeneralFeeds());
        }
        /*GeneralFeeds - end*/
        //API Call Settings-end


        #region SMS Channel
        public JsonResult OnPostSmsChannel(SmsChannel record)
        {
            var status = true;
            var message = "Success";
            try
            {
                _clientDataProvider.SaveSmsChannel(record);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }
        public JsonResult OnPostDeleteSmsChannel(int id)
        {
            var status = true;
            var message = "Success";
            try
            {
                _clientDataProvider.DeleteSmsChannel(id);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }
        public JsonResult OnGetSmsChannels()
        {
            return new JsonResult(_configDataProvider.GetSmsChannels());
        }

        #endregion

        public JsonResult OnGetRcLinkedDuress(int type, string searchTerm)
        {

            return new JsonResult(_clientDataProvider.GetAllRCLinkedDuress()
                .Select(z => RCLinkedDuressViewModel.FromDataModel(z))
                .Where(z => (string.IsNullOrEmpty(searchTerm) || z.ClientSites.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) != -1))
                .OrderBy(x => x.GroupName)
                .ThenBy(x => x.ClientTypes));

        }

        public IActionResult OnGetClientSites(string type)
        {
            GuardId = HttpContext.Session.GetInt32("GuardId") ?? 0;
            if (GuardId == 0)
            {
                return new JsonResult(_viewDataService.GetClientSites(type));
            }
            else
            {
                return new JsonResult(_viewDataService.GetClientSitesUsingLoginUserId(GuardId, type));
            }



        }

        public JsonResult OnGetClientSitesNew(int? page, int? limit, int? typeId, string searchTerm, string searchTermtwo)
        {
            GuardId = HttpContext.Session.GetInt32("GuardId") ?? 0;
            if (GuardId == 0)
            {
                return new JsonResult(_viewDataService.GetUserClientSitesHavingAccess(typeId, null, searchTerm, searchTermtwo));

            }
            else
            {
                return new JsonResult(_viewDataService.GetUserClientSitesHavingAccess(typeId, GuardId, searchTerm, searchTermtwo));

            }
        }

        public JsonResult OnGetRCLinkedDuressbyId(int id)
        {


            return new JsonResult(_clientDataProvider.GetRCLinkedDuressById(id));

        }

        public JsonResult OnPostDeleteRCLinkedDuress(int id)
        {
            var status = true;
            var message = "Success";
            try
            {
                _clientDataProvider.DeleteRCLinkedDuress(id);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status, message });
        }

        public JsonResult OnPostSaveRCLinkedDuress(RCLinkedDuressViewModel rcLinkedDuressViewModel)
        {
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(rcLinkedDuressViewModel, new ValidationContext(rcLinkedDuressViewModel), results, true))
                return new JsonResult(new { success = false, message = string.Join(",", results.Select(z => z.ErrorMessage).ToArray()) });
            var rclinkedDuress = RCLinkedDuressViewModel.ToDataModel(rcLinkedDuressViewModel);
            var success = true;
            var message = "Saved successfully";
            if (_clientDataProvider.CheckAlreadyExistTheGroupName(rclinkedDuress, true))
            {

                try
                {


                    _clientDataProvider.SaveRCLinkedDuress(rclinkedDuress, true);
                }
                catch (Exception ex)
                {
                    success = false;
                    message = ex.Message;
                }
            }
            else
            {
                success = false;
                message = "Group Name already Exists";

            }

            return new JsonResult(new { success, message });
        }
        public JsonResult OnGetCrmSupplierData(string companyName)
        {
            return new JsonResult(_guardLogDataProvider.GetCompanyDetailsVehLog(companyName));
        }
    }
}
