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
namespace CityWatch.Web.Pages.Radio
{
    public class RadioCheckNewModel : PageModel
    {



        private readonly IGuardLogDataProvider _guardLogDataProvider;
        private readonly EmailOptions _EmailOptions;
        private readonly IConfiguration _configuration;
        public RadioCheckNewModel(IGuardLogDataProvider guardLogDataProvider, IOptions<EmailOptions> emailOptions,
            IConfiguration configuration)
        {

            _guardLogDataProvider = guardLogDataProvider;
            _EmailOptions = emailOptions.Value;
            _configuration = configuration;
        }
        public int UserId { get; set; }
        public int GuardId { get; set; }


        public int InActiveGuardCount { get; set; }

        public int ActiveGuardCount { get; set; }
        public IActionResult OnGet()
        {

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
                    int Sid = int.Parse(sidValue);
                    var UserIDDuress = _guardLogDataProvider.UserIDDuress(Sid);
                    if (UserIDDuress == 0)
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
            var activeGuardDetails = _guardLogDataProvider.GetActiveGuardDetails();
            return new JsonResult(activeGuardDetails);
        }

        public IActionResult OnGetClientSiteInActivityStatus(string clientSiteIds)
        {
            var inActiveGuardDetails = _guardLogDataProvider.GetInActiveGuardDetails();
            return new JsonResult(inActiveGuardDetails);
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
        public JsonResult OnPostSaveRadioStatus(int clientSiteId, int guardId, string checkedStatus, bool active)
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
                });
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
                    var logBookId = _guardLogDataProvider.GetClientSiteLogBookId(clientSiteId, logbooktype, DateTime.Today);
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
                            Notes = Subject + " : " + Notifications,
                            //Notes = "Caution Alarm: There has been '0' activity in KV & LB for 2 hours from guard[" + guardName + "]",
                            //IsSystemEntry = true,
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
                            Notes = Subject + " : " + Notifications,
                            //Notes = "Caution Alarm: There has been '0' activity in KV & LB for 2 hours from guard[" + guardName + "]",
                            //IsSystemEntry = true,
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


                    var fromAddress = _EmailOptions.FromAddress.Split('|');
                    var toAddress = smsSiteEmails.Split(',');

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
                emailAddressList.Add(new MailboxAddress(string.Empty, item));
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

        // Save Global Text Alert Start
        public JsonResult OnPostSaveGlobalNotificationTestMessages(bool checkedState, string state, string Notifications, string Subject, bool chkClientType, int[] ClientType, bool chkNationality, bool checkedSMSPersonal, bool checkedSMSSmartWand, int[] clientSiteId)
        {
            var success = true;
            var message = "success";
            try
            {
                if (checkedState == true)
                {
                    var clientSitesState = _guardLogDataProvider.GetClientSitesForState(state);
                    foreach (var item in clientSitesState)
                    {
                        LogBookDetails(item.Id, Notifications, Subject);
                        EmailSender(item.SiteEmail, item.Id, Subject, Notifications);
                    }
                }
                if (chkClientType == true)
                {
                    if (clientSiteId.Length == 0)
                    {
                        var clientSitesClientType = _guardLogDataProvider.GetAllClientSites().Where(x => ClientType.Contains(x.TypeId));
                        foreach (var clientSiteTypeID in clientSitesClientType)
                        {
                            LogBookDetails(clientSiteTypeID.Id, Notifications, Subject);
                            EmailSender(clientSiteTypeID.SiteEmail, clientSiteTypeID.Id, Subject, Notifications);

                        }
                    }
                    else
                    {
                        var clientSitesClientType = _guardLogDataProvider.GetAllClientSites().Where(x => clientSiteId.Contains(x.Id));
                        foreach (var clientSiteTypeID in clientSitesClientType)
                        {
                            LogBookDetails(clientSiteTypeID.Id, Notifications, Subject);
                            EmailSender(clientSiteTypeID.SiteEmail, clientSiteTypeID.Id, Subject, Notifications);

                        }
                    }
                }
                if (chkNationality == true)
                {
                    var clientsiteIDNationality = _guardLogDataProvider.GetAllClientSites();
                    foreach (var itemAll in clientsiteIDNationality)
                    {
                        LogBookDetails(itemAll.Id, Notifications, Subject);
                        EmailSender(itemAll.SiteEmail, itemAll.Id, Subject, Notifications);
                    }
                }

                if (checkedSMSPersonal == true)
                {
                    var logbooktype = LogBookType.DailyGuardLog;
                    var guardlogins = _guardLogDataProvider.GetGuardLoginsByClientSiteId(null, DateTime.Now);
                    string smsPersonalEmails = null;
                    foreach (var item in guardlogins)
                    {
                        if (item.Guard.Mobile != null || item.Guard.Mobile != "+61 4")
                        {
                            item.Guard.Mobile = item.Guard.Mobile.Replace(" ", "") + "@smsglobal.com";
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
                    if (guardlogins.Count > 0)
                    {
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
                if (checkedSMSSmartWand == true)
                {
                    var logbooktype = LogBookType.DailyGuardLog;
                    var smartWands = _guardLogDataProvider.GetClientSiteSmartWands(null);
                    string smsPersonalEmails = null;
                    foreach (var item in smartWands)
                    {
                        if (item.PhoneNumber != null || item.PhoneNumber != "+61 4")
                        {
                            item.PhoneNumber = item.PhoneNumber.Replace("(0)", "") + "@smsglobal.com";
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
                    if (smartWands.Count > 0)
                    {
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
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }

        public void LogBookDetails(int Id, string Notifications, string Subject)
        {
            #region Logbook
            if (Id != null)
            {

                var logbooktype = LogBookType.DailyGuardLog;
                var logBookId = _guardLogDataProvider.GetClientSiteLogBookIdGloablmessage(Id, logbooktype, DateTime.Today);
                if (logBookId != 0)
                {
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
                            Notes = Subject + " : " + Notifications,
                            //Notes = "Caution Alarm: There has been '0' activity in KV & LB for 2 hours from guard[" + guardName + "]",
                            //IsSystemEntry = true,
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
                            Notes = Subject + " : " + Notifications,
                            //Notes = "Caution Alarm: There has been '0' activity in KV & LB for 2 hours from guard[" + guardName + "]",
                            //IsSystemEntry = true,
                            IrEntryType = IrEntryType.Alarm
                        };
                        if (guardLog.ClientSiteLogBookId != 0)
                        {
                            _guardLogDataProvider.SaveGuardLog(guardLog);
                        }

                    }
                }

            }
            #endregion
        }
        public JsonResult EmailSender(string SiteEmail, int Id, string Subject, string Notifications)
        {
            var success = true;
            var message = "success";
            #region Email
            if (SiteEmail != null)
            {
                var clientSites = _guardLogDataProvider.GetClientSites(Id);
                string smsSiteEmails = null;

                if (SiteEmail != null)
                {
                    smsSiteEmails = SiteEmail;
                }
                else
                {
                    success = false;
                    message = "Please Enter the Site Email";
                    return new JsonResult(new { success, message });
                }



                var fromAddress = _EmailOptions.FromAddress.Split('|');
                var toAddress = smsSiteEmails.Split(',');
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
            #endregion
            return new JsonResult(new { success, message });
        }
        // Save Global Text Alert Stop

        //code added for clientsite dropdown start
        public JsonResult OnGetClientSites(int? page, int? limit, int? typeId, string searchTerm)
        {
            return new JsonResult(_guardLogDataProvider.GetUserClientSitesHavingAccess(typeId, AuthUserHelperRadio.LoggedInUserId, searchTerm));
        }
        public JsonResult OnGetClientSitesNew(string typeId)
        {
            if (typeId != null)
            {
                string[] typeId2 = typeId.Split(';');
                int[] typeId3 = new int[typeId2.Length];
                int i = 0;
                foreach (var item in typeId2)
                {

                    typeId3[i] = Convert.ToInt32(item);
                    i++;


                }

                return new JsonResult(_guardLogDataProvider.GetAllClientSites().Where(x => typeId == null || typeId3.Contains(x.TypeId)).OrderBy(z => z.Name).ThenBy(z => z.TypeId));
            }
            return new JsonResult(_guardLogDataProvider.GetAllClientSites().Where(x => x.TypeId == 0).OrderBy(z => z.Name).ThenBy(z => z.TypeId));
        }
        public JsonResult OnGetClientStates()
        {
            return new JsonResult(_guardLogDataProvider.GetStates());
        }
        //code added for clientsite dropdown stop

    }
}
