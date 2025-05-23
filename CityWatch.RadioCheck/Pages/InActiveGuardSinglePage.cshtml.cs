using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using static Dropbox.Api.TeamLog.EventCategory;
using MailKit.Net.Smtp;
using CityWatch.RadioCheck.Models;
using System.Net.Http;
using System.Threading.Tasks;
using CityWatch.Data.Services;
using CityWatch.Web.Models;
using CityWatch.RadioCheck.Services;

namespace CityWatch.Web.Pages.Radio
{
    public class InActiveGuardSinglePage : PageModel
    {


        
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        private readonly EmailOptions _EmailOptions;
        private readonly IConfiguration _configuration;
        private readonly ILogbookDataService _logbookDataService;
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly IViewDataService _viewDataService;
        public InActiveGuardSinglePage(IGuardLogDataProvider guardLogDataProvider, IOptions<EmailOptions> emailOptions,
            IConfiguration configuration, ILogbookDataService logbookDataService, IGuardDataProvider guardDataProvider, IViewDataService viewDataService)
        {

            _guardLogDataProvider = guardLogDataProvider;
            _EmailOptions = emailOptions.Value;
            _configuration = configuration;
            _logbookDataService = logbookDataService;
            _guardDataProvider = guardDataProvider;
            _viewDataService = viewDataService;
        }
        public int UserId { get; set; }
        public int GuardId { get; set; }

        public int InActiveGuardCount { get; set; }

        public int ActiveGuardCount { get; set; }
        public string DisplayItem { get; set; }
        public GuardViewModel Guard { get; set; }
        public IActionResult OnGet(string displayItem)
        {
            /* API call Start*/
            CallApi();
            /* API call end*/

            DisplayItem = displayItem;
            var activeGuardDetails = _guardLogDataProvider.GetActiveGuardDetails();
            ActiveGuardCount = activeGuardDetails.Count();
            var inActiveGuardDetails = _guardLogDataProvider.GetInActiveGuardDetails();
            InActiveGuardCount = inActiveGuardDetails.Count();


            var guardLoginId = HttpContext.Session.GetInt32("GuardLoginId");
            /* The following changes done for allowing guard to access the KPI*/
            var claimsIdentity = User.Identity as ClaimsIdentity;
            /* For Guard Login using securityLicenseNo*/
            string securityLicenseNo = Request.Query["Sl"];
            string LoginGuardId = Request.Query["guid"];
            /* For Guard Login using securityLicenseNo the office staff UserId*/
            string loginUserId = Request.Query["lud"];
            GuardId = HttpContext.Session.GetInt32("GuardId") ?? 0;
            string sidValue = "";
            var UserId1 = claimsIdentity.Claims;
            foreach (var item in UserId1)
            {
                if (item.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/sid")
                {
                     sidValue = item.Value;
                  
                    break;
                }
            }
            if (int.TryParse(sidValue, out int sid))
            {
                int sids = int.Parse(sidValue);
                ViewData["IsDuressEnabled"] = _guardLogDataProvider.IsRadiocheckDuressEnabled(sids);
                
            }
            

            if (!string.IsNullOrEmpty(securityLicenseNo) && !string.IsNullOrEmpty(loginUserId) && !string.IsNullOrEmpty(LoginGuardId))
            {
               
                UserId = int.Parse(loginUserId);
                GuardId = int.Parse(LoginGuardId);

                HttpContext.Session.SetInt32("GuardId", GuardId);
                return Page();
            }
            // Check if the user is authenticated(Normal Admin Login)
            if (claimsIdentity != null && claimsIdentity.IsAuthenticated)
            {   /*Old Code for admin only*/
              
                HttpContext.Session.SetInt32("GuardId", 0);
                return Page();
            }
            else if (GuardId != 0)
            {

                HttpContext.Session.SetInt32("GuardId", GuardId);
                Guard = _viewDataService.GetGuards().SingleOrDefault(x => x.Id == GuardId);
                return Page();
            }
            else
            {
                HttpContext.Session.SetInt32("GuardId", 0);
                return Redirect(Url.Page("/Account/Login"));
            }
            
        }
        //code added to save the duress button start
        public JsonResult OnPostSaveDuress()
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var UserId = claimsIdentity.Claims;
            foreach (var item in UserId)
            {
                if (item.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/sid")
                {
                    string sidValue = item.Value;
                    int Sid= int.Parse(sidValue);
                    var UserIDDuress = _guardLogDataProvider.UserIDDuress(Sid);
                    if (UserIDDuress==0)
                    {
                        _guardLogDataProvider.SaveRadioCheckDuress(sidValue);
                    }
                   
                    break; 
                }
            }
            

            var status = true;
            var message = "Success";
            
            ViewData["IsDuressEnabled"] = true;
            return new JsonResult(new { status, message });
        }
        //code added to save the duress button stop
        public IActionResult OnGetClientSiteActivityStatus(string clientSiteIds)
        {

            return new JsonResult(_guardLogDataProvider.GetActiveGuardDetails());
        }

        public IActionResult OnGetClientSiteInActivityStatus(string clientSiteIds)
        {

            return new JsonResult(_guardLogDataProvider.GetInActiveGuardDetails());
        }
        //for getting logBookDetails of Guards-start
        public IActionResult OnGetClientSitelogBookActivityStatus(int clientSiteId, int guardId)
        {

            return new JsonResult(_guardLogDataProvider.GetActiveGuardlogBookDetails(clientSiteId, guardId));
        }

        //for getting logBookDetails of Guards-end

        //for getting Key Vehicle Details of Guards-start
        public IActionResult OnGetClientSiteKeyVehicleLogActivityStatus(int clientSiteId, int guardId)
        {

            return new JsonResult(_guardLogDataProvider.GetActiveGuardKeyVehicleLogDetails(clientSiteId, guardId));
        }

        //for getting Key Vehicle of Guards-end
        //for getting Incident Report Details of Guards-start
        public IActionResult OnGetClientSiteIncidentReportActivityStatus(int clientSiteId, int guardId)
        {

            return new JsonResult(_guardLogDataProvider.GetActiveGuardIncidentReportDetails(clientSiteId, guardId));
        }

        //for getting Incident Report details of Guards-end

        //for getting guards not available -start

        public IActionResult OnGetClientSiteNotAvailableStatus(string clientSiteIds)
        {

            return new JsonResult(_guardLogDataProvider.GetNotAvailableGuardDetails());
        }
        //for getting guards not available -end

        public JsonResult OnGetGuardData(int id)
        {
            return new JsonResult(_guardLogDataProvider.GetGuards(id));
        }

        //SaveRadioStatus -start
        public JsonResult OnPostSaveRadioStatus(int clientSiteId, int guardId, string checkedStatus,bool active)
        {
            var success = true;
            var message = "success";
            try
            {
                _guardLogDataProvider.SaveClientSiteRadioCheck(new ClientSiteRadioCheck()
                {
                    ClientSiteId = clientSiteId,
                    GuardId = guardId,
                    Status = checkedStatus,
                    CheckedAt = DateTime.Now,
                    Active = active
                }) ;
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }
        //SaveRadioStatus -end

        //Send Text Notifications-start
        public JsonResult OnPostSavePushNotificationTestMessages(int clientSiteId, bool checkedLB, bool checkedSiteEmail, bool checkedSMSPersonal, bool checkedSMSSmartWand, string Notifications, string Subject)
        {
            var success = true;
            var message = "success";
            try
            {
                if (checkedLB == true)
                {
                    var logbooktype = LogBookType.DailyGuardLog;
                    //var logBookId = _guardLogDataProvider.GetClientSiteLogBookId(clientSiteId, logbooktype, DateTime.Today);
                    //Bellow will create a logbook Id if not exist in the current date 02/12/2024
                    var logBookId = _logbookDataService.GetNewOrExistingClientSiteLogBookId(clientSiteId, logbooktype);
                    var guardid = HttpContext.Session.GetInt32("GuardId");
                    if (guardid != 0)
                    {
                        var guardLoginId = _guardLogDataProvider.GetGuardLoginId(Convert.ToInt32(guardid), DateTime.Today);
                        // var guardName = _guardLogDataProvider.GetGuards(ClientSiteRadioChecksActivity.GuardId).Name;
                        var guardLog = new GuardLog()
                        {
                            ClientSiteLogBookId = logBookId,
                            GuardLoginId = guardLoginId,
                            EventDateTime = DateTime.Now,
                            Notes = Notifications,
                            //Notes = "Caution Alarm: There has been '0' activity in KV & LB for 2 hours from guard[" + guardName + "]",
                            IsSystemEntry = true,
                            IrEntryType = IrEntryType.Alarm
                        };
                        _guardLogDataProvider.SaveGuardLog(guardLog);
                    }
                    else
                    {
                        var guardLog = new GuardLog()
                        {
                            ClientSiteLogBookId = logBookId,
                            EventDateTime = DateTime.Now,
                            Notes = Notifications,
                            //Notes = "Caution Alarm: There has been '0' activity in KV & LB for 2 hours from guard[" + guardName + "]",
                            IsSystemEntry = true,
                            IrEntryType = IrEntryType.Alarm
                        };
                        _guardLogDataProvider.SaveGuardLog(guardLog);
                    }
                    
                }
                if (checkedSiteEmail == true)
                {

                    var clientSites = _guardLogDataProvider.GetClientSites(clientSiteId);
                    string smsSiteEmails = null;
                    foreach (var item in clientSites)
                    {
                        if (item.SiteEmail != null)
                        {
                            smsSiteEmails = item.SiteEmail;
                        }
                        else
                        {
                            success = false;
                            message = "Please Enter the Site Email";
                            return new JsonResult(new { success, message });
                        }

                    }
                    var guardlogins = _guardLogDataProvider.GetGuardLoginsByClientSiteId(clientSiteId, DateTime.Now);
                    string guardEmails = null;
                    foreach (var item in guardlogins)
                    {
                        if (item.Guard.Email != null )
                        {
                            
                            if (guardEmails == null)
                            {
                                guardEmails = item.Guard.Email;
                            }
                            else
                            {
                                guardEmails = guardEmails + "," + item.Guard.Email;
                            }
                        }

                    }

                    var fromAddress = _EmailOptions.FromAddress.Split('|');
                    var toAddress = smsSiteEmails.Split(',');
                    var ccAddress = guardEmails.Split(',');
                    var subject = Subject;
                    var messageHtml = Notifications;

                    var messagenew = new MimeMessage();
                    messagenew.From.Add(new MailboxAddress(fromAddress[1], fromAddress[0]));
                    foreach (var address in GetToEmailAddressList(toAddress))
                        messagenew.To.Add(address);
                    foreach (var address in GetToEmailAddressList(ccAddress))
                        messagenew.Cc.Add(address);

                    messagenew.Subject = $"{subject}";

                    var builder = new BodyBuilder()
                    {
                        HtmlBody = messageHtml
                    };

                    messagenew.Body = builder.ToMessageBody();

                    using (var client = new SmtpClient())
                    {
                        client.Connect(_EmailOptions.SmtpServer, _EmailOptions.SmtpPort, MailKit.Security.SecureSocketOptions.None);
                        if (!string.IsNullOrEmpty(_EmailOptions.SmtpUserName) &&
                            !string.IsNullOrEmpty(_EmailOptions.SmtpPassword))
                            client.Authenticate(_EmailOptions.SmtpUserName, _EmailOptions.SmtpPassword);
                        client.Send(messagenew);
                        client.Disconnect(true);
                    }


                }
                if (checkedSMSPersonal == true)
                {
                    var logbooktype = LogBookType.DailyGuardLog;
                    var guardlogins = _guardLogDataProvider.GetGuardLoginsByClientSiteId(clientSiteId, DateTime.Now);
                    string smsPersonalEmails = null;
                    foreach (var item in guardlogins)
                    {
                        if (item.Guard.Mobile != null || item.Guard.Mobile != "+61 4")
                        {
                            item.Guard.Mobile = item.Guard.Mobile.Replace(" ", "") + "@smsglobal.com";
                            item.Guard.Mobile = item.Guard.Mobile.Replace("+", "");
                            if (smsPersonalEmails == null)
                            {
                                smsPersonalEmails = item.Guard.Mobile;
                            }
                            else
                            {
                                smsPersonalEmails = smsPersonalEmails + "," + item.Guard.Mobile;
                            }
                        }

                    }
                    var fromAddress = _EmailOptions.FromAddress.Split('|');
                    var toAddress = smsPersonalEmails.Split(',');
                    var subject = Subject;
                    var messageHtml = Notifications;

                    var messagenew = new MimeMessage();
                    messagenew.From.Add(new MailboxAddress(fromAddress[1], fromAddress[0]));
                    foreach (var address in GetToEmailAddressList(toAddress))
                        messagenew.To.Add(address);
                    messagenew.Subject = $"{subject}";

                    var builder = new BodyBuilder()
                    {
                        HtmlBody = messageHtml
                    };

                    messagenew.Body = builder.ToMessageBody();

                    using (var client = new SmtpClient())
                    {
                        client.Connect(_EmailOptions.SmtpServer, _EmailOptions.SmtpPort, MailKit.Security.SecureSocketOptions.None);
                        if (!string.IsNullOrEmpty(_EmailOptions.SmtpUserName) &&
                            !string.IsNullOrEmpty(_EmailOptions.SmtpPassword))
                            client.Authenticate(_EmailOptions.SmtpUserName, _EmailOptions.SmtpPassword);
                        client.Send(messagenew);
                        client.Disconnect(true);
                    }


                }
                if (checkedSMSSmartWand == true)
                {
                    var logbooktype = LogBookType.DailyGuardLog;
                    var smartWands = _guardLogDataProvider.GetClientSiteSmartWands(clientSiteId);
                    string smsPersonalEmails = null;
                    foreach (var item in smartWands)
                    {
                        if (item.PhoneNumber != null || item.PhoneNumber != "+61 4")
                        {
                            item.PhoneNumber = item.PhoneNumber.Replace("(0)", "") + "@smsglobal.com";
                            item.PhoneNumber = item.PhoneNumber.Replace("+", "");
                            item.PhoneNumber = item.PhoneNumber.Replace(" ", "");
                            if (smsPersonalEmails == null)
                            {
                                smsPersonalEmails = item.PhoneNumber;
                            }
                            else
                            {
                                smsPersonalEmails = smsPersonalEmails + "," + item.PhoneNumber;
                            }
                        }

                    }
                    var fromAddress = _EmailOptions.FromAddress.Split('|');
                    var toAddress = smsPersonalEmails.Split(',');
                    var subject = Subject;
                    var messageHtml = Notifications;

                    var messagenew = new MimeMessage();
                    messagenew.From.Add(new MailboxAddress(fromAddress[1], fromAddress[0]));
                    foreach (var address in GetToEmailAddressList(toAddress))
                        messagenew.To.Add(address);
                    messagenew.Subject = $"{subject}";

                    var builder = new BodyBuilder()
                    {
                        HtmlBody = messageHtml
                    };

                    messagenew.Body = builder.ToMessageBody();

                    using (var client = new SmtpClient())
                    {
                        client.Connect(_EmailOptions.SmtpServer, _EmailOptions.SmtpPort, MailKit.Security.SecureSocketOptions.None);
                        if (!string.IsNullOrEmpty(_EmailOptions.SmtpUserName) &&
                            !string.IsNullOrEmpty(_EmailOptions.SmtpPassword))
                            client.Authenticate(_EmailOptions.SmtpUserName, _EmailOptions.SmtpPassword);
                        client.Send(messagenew);
                        client.Disconnect(true);
                    }


                }
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }
        private List<MailboxAddress> GetToEmailAddressList(string[] toAddress)
        {
            var emailAddressList = new List<MailboxAddress>();
            foreach (var item in toAddress)
            {
                emailAddressList.Add(new MailboxAddress(string.Empty,item ));
            }
            

            return emailAddressList;
        }

        //Send Text Notifications-end


        //to check whthere there is any siteemail or smartwand or guards exists
        //for getting guards not available -end

        //public JsonResult OnGetCompanyTextMessageData(int id)
        //{
        //    var clientsite = _guardLogDataProvider.GetClientSites(id).FirstOrDefault() ;
        //    var clientsitesmartwands = _guardLogDataProvider.GetClientSiteSmartWands(id);
        //    return new JsonResult(_guardLogDataProvider.GetGuards(id));
        //}

        #region api call
        /// <summary>
        /// this is used for regresh the radio status table when page refresh
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> CallApi()
        {
            try
            {
                var results = new RootObject();
                using (var client = new HttpClient())
                {
                    var url = $"https://rc.cws-ir.com/api/RadioChecksActivityStatus/RadioChecksActivityStatus";
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    HttpResponseMessage response = await client.SendAsync(request);

                    if (!response.IsSuccessStatusCode)
                    {
                        return StatusCode((int)response.StatusCode, $"API call failed with status code: {response.StatusCode}");

                    }

                }
                return StatusCode(200, $"Success");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        #endregion

    }
}
