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
using static iText.Kernel.Pdf.Function.PdfFunction;
using Dropbox.Api.Users;
using Microsoft.AspNetCore.Hosting;
using CityWatch.RadioCheck.Helpers;
using CityWatch.RadioCheck.Services;

using iText.Kernel.Crypto.Securityhandler;
using System.Net.NetworkInformation;
using CityWatch.RadioCheck.Models;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using CityWatch.Web.Models;
using CityWatch.Data.Services;
using CityWatch.Web.Pages.Radio;
using Dropbox.Api.Files;
using System.Web;

namespace CityWatch.RadioCheck.Pages.Radio
{
    public class RadioCheckNewModel : PageModel
    {
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        private readonly EmailOptions _EmailOptions;
        private readonly IConfiguration _configuration;
        private readonly ISmsSenderProvider _smsSenderProvider;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly Settings _settings;
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly IViewDataService _viewDataService;
        public readonly IConfigDataProvider _configDataProvider;
        private readonly ILogbookDataService _logbookDataService;

        public RadioCheckNewModel(IGuardLogDataProvider guardLogDataProvider, IOptions<EmailOptions> emailOptions,
            IConfiguration configuration, ISmsSenderProvider smsSenderProvider, IClientDataProvider clientDataProvider, IGuardDataProvider guardDataProvider,
            IOptions<Settings> settings, IViewDataService viewDataService, IConfigDataProvider configDataProvider, ILogbookDataService logbookDataService)
        {
            _guardLogDataProvider = guardLogDataProvider;
            _EmailOptions = emailOptions.Value;
            _configuration = configuration;
            _smsSenderProvider = smsSenderProvider;
            _clientDataProvider = clientDataProvider;
            _settings = settings.Value;
            _guardDataProvider = guardDataProvider;
            _viewDataService = viewDataService;
            _configDataProvider = configDataProvider;
            _logbookDataService = logbookDataService;
        }
        public int UserId { get; set; }
        public int GuardId { get; set; }

        public IViewDataService ViewDataService { get { return _viewDataService; } }

        public int InActiveGuardCount { get; set; }

        public int ActiveGuardCount { get; set; }

        public string SignalRConnectionUrl { get; set; }

        public string DisplayItem { get; set; }
        public GuardViewModel Guard { get; set; }
        public bool IsRCAccess { get; set; }
        public IActionResult OnGet(string displayItem)
        {

            //This Api call for update the values of the tables Start
             CallApi();
            //This Api call for update the values of the tables end

            DisplayItem = displayItem;

            var activeGuardDetails = _guardLogDataProvider.GetActiveGuardDetails();
            ActiveGuardCount = activeGuardDetails.Count();
            var inActiveGuardDetails = _guardLogDataProvider.GetInActiveGuardDetails();
            InActiveGuardCount = inActiveGuardDetails.Count();
            SignalRConnectionUrl = _configuration.GetSection("SignalRConnectionUrl").Value;

            var guardLoginId = HttpContext.Session.GetInt32("GuardLoginId");
            /* The following changes done for allowing guard to access the KPI*/
            var claimsIdentity = User.Identity as ClaimsIdentity;
            /* For Guard Login using securityLicenseNo*/
            string securityLicenseNo = Request.Query["Sl"];
            string LoginGuardId = Request.Query["guid"];
            /* For Guard Login using securityLicenseNo the office staff UserId*/
            string loginUserId = Request.Query["lud"];
           
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
                AuthUserHelper.IsAdminUserLoggedIn = false;
                UserId = int.Parse(loginUserId);
                GuardId = int.Parse(LoginGuardId);
                if (GuardId != 0)
                {
                    Guard = _viewDataService.GetGuards().SingleOrDefault(x => x.Id == GuardId);
                    

                }
                HttpContext.Session.SetInt32("GuardId", GuardId);
                HttpContext.Session.SetInt32("loginUserId", UserId);
                ViewData["GuardId"] = GuardId;


                var guard = _guardDataProvider.GetGuards().SingleOrDefault(z => z.Id == GuardId);

                if (guard != null)
                {
                    if((guard.IsAdminPowerUser || guard.IsAdminSOPToolsAccess || guard.IsAdminAuditorAccess || guard.IsAdminInvestigatorAccess) && (guard.IsRCAccess || guard.IsRCFusionAccess || guard.IsRCHRAccess || guard.IsRCLiteAccess))
                    {
                        if (guard.IsAdminPowerUser)
                        {
                            AuthUserHelper.IsAdminPowerUser = true;
                        }
                        return Page();
                    }
                    if ((guard.IsAdminSOPToolsAccess) && (guard.IsRCAccess || guard.IsRCFusionAccess || guard.IsRCHRAccess || guard.IsRCLiteAccess))
                    {

                        AuthUserHelper.IsAdminPowerUser = true;
                        return Page();
                    }
                    if (guard.IsAdminPowerUser || guard.IsAdminSOPToolsAccess || guard.IsAdminAuditorAccess || guard.IsAdminInvestigatorAccess)
                    {
                        if (guard.IsAdminPowerUser)
                        {
                            AuthUserHelper.IsAdminPowerUser = true;
                        }
                        return Redirect(Url.Page("/Admin/Settings"));
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


                return Page();
            }
            // Check if the user is authenticated(Normal Admin Login)
            if (claimsIdentity != null && claimsIdentity.IsAuthenticated)
            {   /*Old Code for admin only*/
                AuthUserHelper.IsAdminUserLoggedIn = true;
                AuthUserHelper.IsAdminPowerUser = false;
                AuthUserHelper.IsAdminGlobal = false;
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
            var activeGuardDetailModels = activeGuardDetails.Select(detail => new RadioCheckListGuardData
            {
                ClientSiteId = detail.ClientSiteId,
                GuardId = detail.GuardId,
                SiteName = detail.SiteName,
                Address = detail.Address,
                GPS = detail.GPS,
                GuardName=detail.GuardName,
                LogBook=detail.LogBook,
                KeyVehicle=detail.KeyVehicle,
                IncidentReport=detail.IncidentReport,
                SmartWands=detail.SmartWands,
                RcStatus=detail.RcStatus,
                RcColor=detail.RcColor,
                Status=detail.Status,
                RcColorId=detail.RcColorId,
                OnlySiteName=detail.OnlySiteName,
                LatestDate=detail.LatestDate,
                ShowColor=detail.ShowColor,
                hasmartwand=detail.hasmartwand,
                HR1= CalculateHr1GroupStatus(detail.GuardId),
                HR2= CalculateHr2GroupStatus(detail.GuardId),
                HR3= CalculateHr3GroupStatus(detail.GuardId)
                // Map other properties as needed
            }).ToList();
            return new JsonResult(activeGuardDetailModels);
        }
        public IActionResult OnGetClientSiteActivityStatusState(string State)
        {
            var activeGuardDetails = _guardLogDataProvider.GetActiveGuardDetails();
            var activeGuardDetailModels = activeGuardDetails.Select(detail => new RadioCheckListGuardData
            {
                ClientSiteId = detail.ClientSiteId,
                GuardId = detail.GuardId,
                SiteName = detail.SiteName,
                Address = detail.Address,
                GPS = detail.GPS,
                GuardName = detail.GuardName,
                LogBook = detail.LogBook,
                KeyVehicle = detail.KeyVehicle,
                IncidentReport = detail.IncidentReport,
                SmartWands = detail.SmartWands,
                RcStatus = detail.RcStatus,
                RcColor = detail.RcColor,
                Status = detail.Status,
                RcColorId = detail.RcColorId,
                OnlySiteName = detail.OnlySiteName,
                LatestDate = detail.LatestDate,
                ShowColor = detail.ShowColor,
                hasmartwand = detail.hasmartwand,
                HR1 = CalculateHr1GroupStatus(detail.GuardId),
                HR2 = CalculateHr2GroupStatus(detail.GuardId),
                HR3 = CalculateHr3GroupStatus(detail.GuardId),
                State=detail.State
                // Map other properties as needed
            }).ToList().Where(x=>x.State== State);
            return new JsonResult(activeGuardDetailModels);
        }
        public IActionResult OnGetClientSiteActivityStatusClientSite(int ClientSiteId)
        {
            var activeGuardDetails = _guardLogDataProvider.GetActiveGuardDetails();
            var activeGuardDetailModels = activeGuardDetails.Select(detail => new RadioCheckListGuardData
            {
                ClientSiteId = detail.ClientSiteId,
                GuardId = detail.GuardId,
                SiteName = detail.SiteName,
                Address = detail.Address,
                GPS = detail.GPS,
                GuardName = detail.GuardName,
                LogBook = detail.LogBook,
                KeyVehicle = detail.KeyVehicle,
                IncidentReport = detail.IncidentReport,
                SmartWands = detail.SmartWands,
                RcStatus = detail.RcStatus,
                RcColor = detail.RcColor,
                Status = detail.Status,
                RcColorId = detail.RcColorId,
                OnlySiteName = detail.OnlySiteName,
                LatestDate = detail.LatestDate,
                ShowColor = detail.ShowColor,
                hasmartwand = detail.hasmartwand,
                HR1 = CalculateHr1GroupStatus(detail.GuardId),
                HR2 = CalculateHr2GroupStatus(detail.GuardId),
                HR3 = CalculateHr3GroupStatus(detail.GuardId),
                State = detail.State
                // Map other properties as needed
            }).ToList().Where(x => x.ClientSiteId == ClientSiteId);
            return new JsonResult(activeGuardDetailModels);
        }
        //code added for HR LED Start
        public string CalculateHr1GroupStatus(int guardId)
        {
            var HR1 = "Grey";
            var hrGroupStatusesNew = LEDStatusForLoginUser(guardId);

            if (hrGroupStatusesNew != null && hrGroupStatusesNew.Count > 0)
            {
                var HR1List = hrGroupStatusesNew.Where(x => x.GroupName.Trim() == "HR1 (C4i)").ToList();
                if (HR1List != null && HR1List.Count > 0)
                {
                    if (HR1List.Any(x => x.ColourCodeStatus == "Red"))
                    {
                        HR1= "Red";
                    }
                    else if (HR1List.Any(x => x.ColourCodeStatus == "Yellow"))
                    {
                        HR1= "Yellow";
                    }
                    else
                    {
                        HR1= "Green";
                    }
                }
            }

            return HR1; 
        }
        public string CalculateHr2GroupStatus(int guardId)
        {
            var HR2 = "Grey";
            var hrGroupStatusesNew = LEDStatusForLoginUser(guardId);

            if (hrGroupStatusesNew != null && hrGroupStatusesNew.Count > 0)
            {
                var HR2List = hrGroupStatusesNew.Where(x => x.GroupName.Trim() == "HR2 (Client)").ToList();
                if (HR2List != null && HR2List.Count > 0)
                {
                    if (HR2List.Where(x => x.ColourCodeStatus == "Red").ToList().Count > 0)
                    {
                        HR2= "Red";
                    }
                    else if (HR2List.Where(x => x.ColourCodeStatus == "Yellow").ToList().Count > 0)
                    {
                        HR2= "Yellow";
                    }
                    else
                    {
                        HR2= "Green";
                    }
                }
            }

            return HR2; 
        }
        public string CalculateHr3GroupStatus(int guardId)
        {
            var HR3 = "Grey";
            var hrGroupStatusesNew = LEDStatusForLoginUser(guardId);

            if (hrGroupStatusesNew != null && hrGroupStatusesNew.Count > 0)
            {
                var HR3List = hrGroupStatusesNew.Where(x => x.GroupName.Trim() == "HR3 (Special)").ToList();
                if (HR3List != null && HR3List.Count > 0)
                {
                    if (HR3List.Where(x => x.ColourCodeStatus == "Red").ToList().Count > 0)
                    {
                        HR3= "Red";
                    }
                    else if (HR3List.Where(x => x.ColourCodeStatus == "Yellow").ToList().Count > 0)
                    {
                        HR3= "Yellow";
                    }
                    else
                    {
                        HR3= "Green";
                    }
                }
            }

            return HR3;
        }
        public List<HRGroupStatusNew> LEDStatusForLoginUser(int GuardID)
        {
            var MasterGroup = _guardDataProvider.GetHRDescFull();
            var GuardDocumentDetails = _guardDataProvider.GetGuardLicensesandcompliance(GuardID);
            var hrGroupStatusesNew = new List<HRGroupStatusNew>();

            foreach (var item in MasterGroup)
            {

                var TemDescription = item.ReferenceNo + " " + item.Description.Trim();
                var SelectedGuardDocument = GuardDocumentDetails.Where(x => x.Description == TemDescription).ToList();


                if (SelectedGuardDocument.Count > 0)
                {
                    hrGroupStatusesNew.Add(new HRGroupStatusNew
                    {

                        Status = 1,
                        GroupName = item.GroupName.Trim(),
                        ColourCodeStatus = GuardledColourCodeGenerator(SelectedGuardDocument)

                    });
                }


            }
            var Temp = hrGroupStatusesNew;

            return Temp;
        }
        public string GuardledColourCodeGenerator(List<GuardComplianceAndLicense> SelectedList)
        {
            var today = DateTime.Now;
            var ColourCode = "Green";

            if (SelectedList.Count > 0)
            {
                var SelectDatatype = SelectedList.Where(x => x.DateType == true).ToList();
                if (SelectDatatype.Count > 0)
                {
                    ColourCode = "Green";
                }
                else
                {
                    if (SelectedList.FirstOrDefault() != null)
                    {
                        if (SelectedList.FirstOrDefault().ExpiryDate != null)
                        {
                            var ExpiryDate = SelectedList.FirstOrDefault().ExpiryDate;
                            var timeDifference = ExpiryDate - today;

                            if (ExpiryDate < today)
                            {
                                ColourCode = "Red";
                            }
                            else if ((ExpiryDate - DateTime.Now).Value.Days < 45)
                            {
                                var Date = (ExpiryDate - DateTime.Now).Value.Days;
                                ColourCode = "Yellow";
                            }
                        }

                    }



                }
            }
            return ColourCode;
        }
        public class HRGroupStatusNew
        {

            public int Status { get; set; }

            public string GroupName { get; set; }
            public string ColourCodeStatus { get; set; }
        }
        //code added for HR LED Stop
        public IActionResult OnGetClientSiteInActivityStatus(string clientSiteIds)
        {
            var inActiveGuardDetails = _guardLogDataProvider.GetInActiveGuardDetails();
            return new JsonResult(inActiveGuardDetails);
        }
        public IActionResult OnGetClientSiteInActivityStatusState(string State)
        {
            var inActiveGuardDetails = _guardLogDataProvider.GetInActiveGuardDetails();
            inActiveGuardDetails = inActiveGuardDetails.Where(x => x.State == State).ToList();
            return new JsonResult(inActiveGuardDetails);
        }
        public IActionResult OnGetClientSiteInActivityStatusClientSite(int ClientSiteId)
        {
            var inActiveGuardDetails = _guardLogDataProvider.GetInActiveGuardDetails();
            inActiveGuardDetails = inActiveGuardDetails.Where(x => x.ClientSiteId == ClientSiteId).ToList();
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


        //for getting SW Details of Guards-start
        public IActionResult OnGetClientSiteSWDetails(int clientSiteId, int guardId)
        {

            return new JsonResult(_guardLogDataProvider.GetActiveGuardSWDetails(clientSiteId, guardId));
        }

        //for getting SW details of Guards-end

        public IActionResult OnGetClientSiteNotAvailableStatus(string clientSiteIds)
        {

            return new JsonResult(_guardLogDataProvider.GetNotAvailableGuardDetails());
        }
        //for getting guards not available -end

        public JsonResult OnGetGuardData(int id)
        {
            //return new JsonResult(_guardLogDataProvider.GetGuards(id));
            //return new JsonResult(_guardLogDataProvider.GetGuardsWtihProviderNumber(id));
            
            return new JsonResult(ViewDataService.GetGuards().Where(x => x.Id == id).FirstOrDefault());


        }
        public JsonResult OnGetCrmSupplierData(string name)
        {
            return new JsonResult(_guardLogDataProvider.GetCompanyDetailsVehLog(name));
        }
        //SaveRadioStatus -start
        // New changes int notificationType added for identify the notfication type and  set guard =0
        public JsonResult OnPostSaveRadioStatus(int clientSiteId, int guardId, string checkedStatus, bool active, int statusId, GuardLog tmzdata,int notificationType)
        {
            var success = true;
            var message = "success";
            try
            {
                //Email send Duress Activate start
                var colorId = _clientDataProvider.GetRadioCheckStatus(statusId);
                if (colorId == 5)
                {
                    var GuradDetails = _clientDataProvider.GetGuradName(guardId);
                    var ClientsiteDetails = _clientDataProvider.GetClientSiteName(clientSiteId);

                    var Emails = _clientDataProvider.GetGlobalDuressEmail().ToList();
                    var emailAddresses = string.Join(",", Emails.Select(email => email.Email));

                    var Subject = "Global Duress Alert Deactivated";
                    var Notifications = "The duress alarm was deactivated by the CRO for the following reason"+ "<br/>" + "Reason:" +
                     checkedStatus+ "<br/>"+
                      (string.IsNullOrEmpty(ClientsiteDetails.Name) ? string.Empty : "Site: " + ClientsiteDetails.Name) + "<br/>" ;
                    EmailSender(emailAddresses, Subject, Notifications, GuradDetails.Name, ClientsiteDetails.Name);
                }

                
                //Email send Duress Activate stop

                var loginguardid = HttpContext.Session.GetInt32("GuardId") ?? 0;
                _guardLogDataProvider.SaveClientSiteRadioCheckNew(new ClientSiteRadioCheck()
                {
                    ClientSiteId = clientSiteId,
                    GuardId = guardId,
                    Status = checkedStatus,
                    CheckedAt = DateTime.Now,
                    Active = active,
                    RadioCheckStatusId = statusId,
                }, tmzdata, loginguardid);

                if(notificationType==1 && guardId==4)
                {
                    //Notifcation send to bruno if notfication type=4 
                    //for avoid this set gaurd =0 for this area02122024
                    guardId = 0;
                }
                _guardLogDataProvider.LogBookEntryFromRcControlRoomMessages(loginguardid, guardId, null, checkedStatus, IrEntryType.Notification, 2, clientSiteId, tmzdata);
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }
        public JsonResult EmailSender(string Email, string Subject, string Notifications, string GuradName, string Name)
        {
            var success = true;
            var message = "success";
            #region Email
            if (Email != null)
            {
                var fromAddress = _EmailOptions.FromAddress.Split('|');
                var toAddress = Email.Split(','); ;
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
        //SaveRadioStatus -end

        //Send Text Notifications-start
        public JsonResult OnPostSavePushNotificationTestMessages(int clientSiteId, bool checkedLB, bool checkedSiteEmail,
                                    bool checkedSMSPersonal, bool checkedSMSSmartWand, bool checkedPersonalEmail, string Notifications, string Subject, GuardLog tmzdata)
        {
            var success = true;
            var message = "success";
            try
            {
                var guardid = HttpContext.Session.GetInt32("GuardId");
                string guardname = string.Empty;
                string clientsitename = _guardLogDataProvider.GetClientSites(clientSiteId).Select(x => x.Name).FirstOrDefault();
                if (checkedLB == true)
                {
                    var logbookdate = DateTime.Today;
                    var logbooktype = LogBookType.DailyGuardLog;
                    // var logBookId = _guardLogDataProvider.GetClientSiteLogBookId(clientSiteId, logbooktype, DateTime.Today);
                    // Get Last Logbookid and logbook Date by latest logbookid // p6#73 timezone bug - Added by binoy 24-01-2024
                    //Commneted due to the issue with latest logbook not taking 
                    //var logBookId = _guardLogDataProvider.GetClientSiteLogBookIdByLogBookMaxID(clientSiteId, logbooktype, out logbookdate);
                    //Bellow will create a logbook Id if not exist in the current date 02/12/2024
                    var logBookId = _logbookDataService.GetNewOrExistingClientSiteLogBookId(clientSiteId, logbooktype);
                    if (guardid != 0)
                    {
                        guardname = _guardLogDataProvider.GetGuards(guardid.Value).Name;

                        /* Save the push message for reload to logbook on next day Start*/
                        var radioCheckPushMessages = new RadioCheckPushMessages()
                        {
                            ClientSiteId = clientSiteId,
                            LogBookId = logBookId,
                            Notes = Subject + " : " + Notifications,
                            EntryType = (int)IrEntryType.Alarm,
                            Date = logbookdate,
                            IsAcknowledged = 0,
                            IsDuress = 0,
                            PlayNotificationSound = true
                        };
                        var pushMessageId = _guardLogDataProvider.SavePushMessage(radioCheckPushMessages);
                        /* Save the push message for reload to logbook on next day end*/



                        var guardLoginId = _guardLogDataProvider.GetGuardLoginId(Convert.ToInt32(guardid), DateTime.Today);

                        var guardLog = new GuardLog()
                        {
                            ClientSiteLogBookId = logBookId,
                            GuardLoginId = guardLoginId,
                            EventDateTime = DateTime.Now,
                            Notes = Subject + " : " + Notifications,
                            IrEntryType = IrEntryType.Alarm,
                            RcPushMessageId = pushMessageId,
                            EventDateTimeLocal = tmzdata.EventDateTimeLocal,
                            EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                            EventDateTimeZone = tmzdata.EventDateTimeZone,
                            EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                            EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                            PlayNotificationSound = true
                        };
                        _guardLogDataProvider.SaveGuardLog(guardLog);
                    }
                    else
                    {
                        /* Save the push message for reload to logbook on next day Start*/
                        var radioCheckPushMessages = new RadioCheckPushMessages()
                        {
                            ClientSiteId = clientSiteId,
                            LogBookId = logBookId,
                            Notes = Subject + " : " + Notifications,
                            EntryType = (int)IrEntryType.Alarm,
                            Date = logbookdate,
                            IsAcknowledged = 0,
                            IsDuress = 0,
                            PlayNotificationSound = true
                        };
                        var pushMessageId = _guardLogDataProvider.SavePushMessage(radioCheckPushMessages);
                        /* Save the push message for reload to logbook on next day end*/



                        var guardLog = new GuardLog()
                        {
                            ClientSiteLogBookId = logBookId,
                            EventDateTime = DateTime.Now,
                            Notes = Subject + " : " + Notifications,
                            IrEntryType = IrEntryType.Alarm,
                            RcPushMessageId = pushMessageId,
                            EventDateTimeLocal = tmzdata.EventDateTimeLocal,
                            EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                            EventDateTimeZone = tmzdata.EventDateTimeZone,
                            EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                            EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                            PlayNotificationSound = true

                        };
                        _guardLogDataProvider.SaveGuardLog(guardLog);


                    }

                    var loginguardid = HttpContext.Session.GetInt32("GuardId") ?? 0;
                    _guardLogDataProvider.LogBookEntryFromRcControlRoomMessages(loginguardid, 0, Subject, Notifications, IrEntryType.Alarm, 1, 0, tmzdata);

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
                    var guardlogins = _guardLogDataProvider.GetGuardLoginsByClientSiteId(clientSiteId, DateTime.Now);
                    List<SmsChannelEventLog> _smsChannelEventLogList = new List<SmsChannelEventLog>();
                    foreach (var item in guardlogins)
                    {
                        if (item.Guard.Mobile != null)
                        {
                            SmsChannelEventLog smslog = new SmsChannelEventLog();
                            smslog.GuardId = guardid.HasValue ? guardid.Value != 0 ? guardid.Value : null : null; // ID of guard who is sending the message
                            smslog.GuardName = guardname.Length > 0 ? guardname : null; // Name of guard who is sending the message
                            smslog.GuardNumber = item.Guard.Mobile; //  "+00 (91) -9747435124";
                            smslog.SiteId = null;  // setting as null since it is from RC else clientSiteId
                            smslog.SiteName = null;  // setting as null since it is from RC else  clientsitename
                            _smsChannelEventLogList.Add(smslog);
                        }
                    }
                    SiteEventLog svl = new SiteEventLog();
                    svl.ProjectName = "Radio Check";
                    svl.ActivityType = "SMS Personal";
                    svl.Module = "Radio Check V2";
                    svl.SubModule = "Push Notification";
                    svl.EventLocalTime = tmzdata.EventDateTimeLocal.Value;
                    svl.EventLocalOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute;
                    svl.EventLocalTimeZone = tmzdata.EventDateTimeZoneShort;
                    svl.IPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString();
                    _smsSenderProvider.SendSms(_smsChannelEventLogList, Subject + " : " + Notifications, svl);
                }

                if (checkedSMSSmartWand == true)
                {
                    var smartWands = _guardLogDataProvider.GetClientSiteSmartWands(clientSiteId);
                    List<SmsChannelEventLog> _smsChannelEventLogList = new List<SmsChannelEventLog>();
                    foreach (var item in smartWands)
                    {
                        if (item.PhoneNumber != null)
                        {
                            SmsChannelEventLog smslog = new SmsChannelEventLog();
                            smslog.GuardId = guardid.HasValue ? guardid.Value != 0 ? guardid.Value : null : null; // ID of guard who is sending the message
                            smslog.GuardName = guardname.Length > 0 ? guardname : null; // Name of guard who is sending the message
                            smslog.GuardNumber = item.PhoneNumber; // "+00 (91) -9747435124";
                            smslog.SiteId = null;  // setting as null since it is from RC else clientSiteId
                            smslog.SiteName = null;  // setting as null since it is from RC else  clientsitename
                            _smsChannelEventLogList.Add(smslog);
                        }
                    }
                    SiteEventLog svl = new SiteEventLog();
                    svl.ProjectName = "Radio Check";
                    svl.ActivityType = "SMS Smart Wand";
                    svl.Module = "Radio Check V2";
                    svl.SubModule = "Push Notification";
                    svl.EventLocalTime = tmzdata.EventDateTimeLocal.Value;
                    svl.EventLocalOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute;
                    svl.EventLocalTimeZone = tmzdata.EventDateTimeZoneShort;
                    svl.IPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString();
                    _smsSenderProvider.SendSms(_smsChannelEventLogList, Subject + " : " + Notifications, svl);
                }
                //p4-79 menucorrections-start
                if (checkedPersonalEmail == true)
                {

                    var guardEmails = _guardLogDataProvider.GetGuardLogs(clientSiteId).Select(x => x.Guard.Email);
                    string smsSiteEmails = null;
                    var guardEmailsnew = guardEmails.Distinct().ToList();
                    foreach (var item in guardEmailsnew)
                    {
                        if (item != null)
                        {
                            if (smsSiteEmails == null)
                                smsSiteEmails = item;
                            else
                                smsSiteEmails = smsSiteEmails + ',' + item;

                        }
                        //else
                        //{
                        //    success = false;
                        //    message = "Please Enter the Guard Email";
                        //    return new JsonResult(new { success, message });
                        //}

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
                //p4-79 menucorrections-end
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

        #region history
        //for getting logBook History of Guards-start
        public IActionResult OnGetClientSitelogBookHistory(int clientSiteId, int guardId)
        {
            var jsresult = _guardLogDataProvider.GetActiveGuardlogBookHistory(clientSiteId, guardId);
            return new JsonResult(jsresult);
        }
        //for getting logBook History of Guards-end

        //for getting Key Vehicle History of Guards-start
        public IActionResult OnGetClientSiteKeyVehicleHistory(int clientSiteId, int guardId)
        {
            var jsresult = _guardLogDataProvider.GetActiveGuardKeyVehicleHistory(clientSiteId, guardId);
            return new JsonResult(jsresult);
        }
        //for getting Key Vehicle History of Guards-end

        //for getting Incident Report History of Guards-start
        public IActionResult OnGetClientSiteIncidentReportHistory(int clientSiteId, int guardId)
        {
            var jsresult = _guardLogDataProvider.GetActiveGuardIncidentReportHistory(clientSiteId, guardId);
            return new JsonResult(jsresult);
        }
        //for getting Incident Report History of Guards-end

        //for getting SW Details of Guards-start
        public IActionResult OnGetClientSiteSWHistory(int clientSiteId, int guardId)
        {
            var jsresult = _guardLogDataProvider.GetActiveGuardSwHistory(clientSiteId, guardId);  // GetActiveGuardSwHistory
            return new JsonResult(jsresult);
        }
        //for getting SW details of Guards-end

        #endregion history

        //for getting ClientSiteRadiocheckStatus of Guards-start
        public IActionResult OnGetClientSiteRadiocheckStatus(int clientSiteId, int guardId, int ColorId)
        {

            return new JsonResult(_guardLogDataProvider.GetClientSiteRadiocheckStatus(clientSiteId, guardId));
        }
        //for getting ClientSiteRadiocheckStatus of Guards-end

        // Save Global Text Alert Start
        public JsonResult OnPostSaveGlobalNotificationTestMessages(bool checkedState, string state, string Notifications, string Subject,
            bool chkClientType, int[] ClientType, bool chkNationality, bool checkedSMSPersonal, bool checkedSMSSmartWand, bool chkGlobalPersonalEmail, int[] clientSiteId, GuardLog tmzdata)
        {
            var success = true;
            var message = "success";

            // For sending SMS
            SiteEventLog svl = new SiteEventLog();
            svl.ProjectName = "Radio Check";
            svl.Module = "Radio Check V2";
            svl.EventLocalTime = tmzdata.EventDateTimeLocal.Value;
            svl.EventLocalOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute;
            svl.EventLocalTimeZone = tmzdata.EventDateTimeZoneShort;
            svl.IPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString();

            try
            {
                if (checkedState == true)
                {
                    svl.SubModule = "Global Push Notification-[Global] [State]";
                    var clientSitesState = _guardLogDataProvider.GetClientSitesForState(state);
                    foreach (var item in clientSitesState)
                    {
                        LogBookDetails(item.Id, Notifications, Subject, tmzdata);

                    }
                    /* log book entry to citywtch control room */
                    var loginguardid = HttpContext.Session.GetInt32("GuardId") ?? 0;
                    _guardLogDataProvider.LogBookEntryFromRcControlRoomMessages(loginguardid, 0, Subject, Notifications, IrEntryType.Alarm, 1, 0, tmzdata);
                    foreach (var item in clientSitesState)
                    {
                        EmailSender(item.SiteEmail, item.Id, Subject, Notifications);
                    }

                    if (checkedSMSPersonal == true)
                    {
                        svl.ActivityType = "SMS Personal";
                        foreach (var item in clientSitesState)
                        {
                            var guardlogins = _guardLogDataProvider.GetGuardLoginsByClientSiteId(item.Id, DateTime.Now);
                            string guardname = string.Empty;
                            List<SmsChannelEventLog> _smsChannelEventLogList = new List<SmsChannelEventLog>();
                            foreach (var logins in guardlogins)
                            {
                                if (logins.Guard.Mobile != null)
                                {
                                    SmsChannelEventLog smslog = new SmsChannelEventLog();
                                    smslog.GuardId = loginguardid != 0 ? loginguardid : null; // ID of guard who is sending the message
                                    smslog.GuardName = guardname.Length > 0 ? guardname : null; // Name of guard who is sending the message
                                    smslog.GuardNumber = logins.Guard.Mobile;
                                    smslog.SiteId = null;  // setting as null since it is from RC else clientSiteId
                                    smslog.SiteName = null;  // setting as null since it is from RC else  clientsitename
                                    _smsChannelEventLogList.Add(smslog);
                                }
                            }
                            _smsSenderProvider.SendSms(_smsChannelEventLogList, Subject + " : " + Notifications, svl);
                        }
                    }

                    if (checkedSMSSmartWand == true)
                    {
                        svl.ActivityType = "SMS Smart Wand";
                        foreach (var item in clientSitesState)
                        {
                            var smartWands = _guardLogDataProvider.GetClientSiteSmartWands(item.Id);
                            string guardname = string.Empty;
                            List<SmsChannelEventLog> _smsChannelEventLogList = new List<SmsChannelEventLog>();
                            foreach (var sw in smartWands)
                            {
                                if (sw.PhoneNumber != null)
                                {
                                    SmsChannelEventLog smslog = new SmsChannelEventLog();
                                    smslog.GuardId = loginguardid != 0 ? loginguardid : null; // ID of guard who is sending the message
                                    smslog.GuardName = guardname.Length > 0 ? guardname : null; // Name of guard who is sending the message
                                    smslog.GuardNumber = sw.PhoneNumber;
                                    smslog.SiteId = null;  // setting as null since it is from RC else clientSiteId
                                    smslog.SiteName = null;  // setting as null since it is from RC else  clientsitename
                                    _smsChannelEventLogList.Add(smslog);
                                }
                            }
                            _smsSenderProvider.SendSms(_smsChannelEventLogList, Subject + " : " + Notifications, svl);
                        }
                    }
                    //p4-79 menucorrections-start
                    if (chkGlobalPersonalEmail == true)
                    {
                        string smsSiteEmails = null;
                        foreach (var item in clientSitesState)
                        {
                            var guardEmails = _guardLogDataProvider.GetGuardLogs(item.Id).Select(x => x.Guard.Email);


                            var guardEmailsnew = guardEmails.Distinct().ToList();


                            foreach (var item2 in guardEmailsnew)
                            {
                                if (item2 != null)
                                {
                                    if (smsSiteEmails == null)
                                        smsSiteEmails = item2;
                                    else
                                        smsSiteEmails = smsSiteEmails + ',' + item2;

                                }
                                //else
                                //{
                                //    success = false;
                                //    message = "Please Enter the Guard Email";
                                //    return new JsonResult(new { success, message });
                                //}

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
                    //p4-79 menucorrections-end


                }
                if (chkClientType == true)
                {
                    if (clientSiteId.Length == 0)
                    {
                        svl.SubModule = "Global Push Notification-[Global] [ClientType]";
                        var clientSitesClientType = _guardLogDataProvider.GetAllClientSites().Where(x => ClientType.Contains(x.TypeId));
                        foreach (var clientSiteTypeID in clientSitesClientType)
                        {
                            LogBookDetails(clientSiteTypeID.Id, Notifications, Subject, tmzdata);
                        }
                        /* log book entry to citywtch control room */
                        var loginguardid = HttpContext.Session.GetInt32("GuardId") ?? 0;
                        _guardLogDataProvider.LogBookEntryFromRcControlRoomMessages(loginguardid, 0, Subject, Notifications, IrEntryType.Alarm, 1, 0, tmzdata);
                        foreach (var clientSiteTypeID in clientSitesClientType)
                        {
                            EmailSender(clientSiteTypeID.SiteEmail, clientSiteTypeID.Id, Subject, Notifications);
                        }

                        if (checkedSMSPersonal == true)
                        {
                            svl.ActivityType = "SMS Personal";
                            foreach (var item in clientSitesClientType)
                            {
                                var guardlogins = _guardLogDataProvider.GetGuardLoginsByClientSiteId(item.Id, DateTime.Now);
                                string guardname = string.Empty;
                                List<SmsChannelEventLog> _smsChannelEventLogList = new List<SmsChannelEventLog>();
                                foreach (var logins in guardlogins)
                                {
                                    if (logins.Guard.Mobile != null)
                                    {
                                        SmsChannelEventLog smslog = new SmsChannelEventLog();
                                        smslog.GuardId = loginguardid != 0 ? loginguardid : null; // ID of guard who is sending the message
                                        smslog.GuardName = guardname.Length > 0 ? guardname : null; // Name of guard who is sending the message
                                        smslog.GuardNumber = logins.Guard.Mobile;
                                        smslog.SiteId = null;  // setting as null since it is from RC else clientSiteId
                                        smslog.SiteName = null;  // setting as null since it is from RC else  clientsitename
                                        _smsChannelEventLogList.Add(smslog);
                                    }
                                }
                                _smsSenderProvider.SendSms(_smsChannelEventLogList, Subject + " : " + Notifications, svl);
                            }
                        }

                        if (checkedSMSSmartWand == true)
                        {
                            svl.ActivityType = "SMS Smart Wand";
                            foreach (var item in clientSitesClientType)
                            {
                                var smartWands = _guardLogDataProvider.GetClientSiteSmartWands(item.Id);
                                string guardname = string.Empty;
                                List<SmsChannelEventLog> _smsChannelEventLogList = new List<SmsChannelEventLog>();
                                foreach (var sw in smartWands)
                                {
                                    if (sw.PhoneNumber != null)
                                    {
                                        SmsChannelEventLog smslog = new SmsChannelEventLog();
                                        smslog.GuardId = loginguardid != 0 ? loginguardid : null; // ID of guard who is sending the message
                                        smslog.GuardName = guardname.Length > 0 ? guardname : null; // Name of guard who is sending the message
                                        smslog.GuardNumber = sw.PhoneNumber;
                                        smslog.SiteId = null;  // setting as null since it is from RC else clientSiteId
                                        smslog.SiteName = null;  // setting as null since it is from RC else  clientsitename
                                        _smsChannelEventLogList.Add(smslog);
                                    }
                                }
                                _smsSenderProvider.SendSms(_smsChannelEventLogList, Subject + " : " + Notifications, svl);
                            }
                        }
                        //p4-79 menucorrections-start
                        if (chkGlobalPersonalEmail == true)
                        {
                            string smsSiteEmails = null;
                            foreach (var item in clientSitesClientType)
                            {
                                var guardEmails = _guardLogDataProvider.GetGuardLogs(item.Id).Select(x => x.Guard.Email);

                                var guardEmailsnew = guardEmails.Distinct().ToList();




                                foreach (var item2 in guardEmailsnew)
                                {
                                    if (item2 != null)
                                    {
                                        if (smsSiteEmails == null)
                                            smsSiteEmails = item2;
                                        else
                                            smsSiteEmails = smsSiteEmails + ',' + item2;

                                    }
                                    //else
                                    //{
                                    //    success = false;
                                    //    message = "Please Enter the Guard Email";
                                    //    return new JsonResult(new { success, message });
                                    //}

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
                        //p4-79 menucorrections-end

                    }
                    else
                    {
                        svl.SubModule = "Global Push Notification-[Global] [ClientType]";
                        var clientSitesClientType = _guardLogDataProvider.GetAllClientSites().Where(x => clientSiteId.Contains(x.Id));
                        foreach (var clientSiteTypeID in clientSitesClientType)
                        {
                            LogBookDetails(clientSiteTypeID.Id, Notifications, Subject, tmzdata);
                        }
                        /* log book entry to citywtch control room */
                        var loginguardid = HttpContext.Session.GetInt32("GuardId") ?? 0;
                        _guardLogDataProvider.LogBookEntryFromRcControlRoomMessages(loginguardid, 0, Subject, Notifications, IrEntryType.Alarm, 1, 0, tmzdata);
                        foreach (var clientSiteTypeID in clientSitesClientType)
                        {
                            EmailSender(clientSiteTypeID.SiteEmail, clientSiteTypeID.Id, Subject, Notifications);
                        }

                        if (checkedSMSPersonal == true)
                        {
                            svl.ActivityType = "SMS Personal";
                            foreach (var item in clientSitesClientType)
                            {
                                var guardlogins = _guardLogDataProvider.GetGuardLoginsByClientSiteId(item.Id, DateTime.Now);
                                string guardname = string.Empty;
                                List<SmsChannelEventLog> _smsChannelEventLogList = new List<SmsChannelEventLog>();
                                foreach (var logins in guardlogins)
                                {
                                    if (logins.Guard.Mobile != null)
                                    {
                                        SmsChannelEventLog smslog = new SmsChannelEventLog();
                                        smslog.GuardId = loginguardid != 0 ? loginguardid : null; // ID of guard who is sending the message
                                        smslog.GuardName = guardname.Length > 0 ? guardname : null; // Name of guard who is sending the message
                                        smslog.GuardNumber = logins.Guard.Mobile;
                                        smslog.SiteId = null;  // setting as null since it is from RC else clientSiteId
                                        smslog.SiteName = null;  // setting as null since it is from RC else  clientsitename
                                        _smsChannelEventLogList.Add(smslog);
                                    }
                                }
                                _smsSenderProvider.SendSms(_smsChannelEventLogList, Subject + " : " + Notifications, svl);
                            }
                        }

                        if (checkedSMSSmartWand == true)
                        {
                            svl.ActivityType = "SMS Smart Wand";
                            foreach (var item in clientSitesClientType)
                            {
                                var smartWands = _guardLogDataProvider.GetClientSiteSmartWands(item.Id);
                                string guardname = string.Empty;
                                List<SmsChannelEventLog> _smsChannelEventLogList = new List<SmsChannelEventLog>();
                                foreach (var sw in smartWands)
                                {
                                    if (sw.PhoneNumber != null)
                                    {
                                        SmsChannelEventLog smslog = new SmsChannelEventLog();
                                        smslog.GuardId = loginguardid != 0 ? loginguardid : null; // ID of guard who is sending the message
                                        smslog.GuardName = guardname.Length > 0 ? guardname : null; // Name of guard who is sending the message
                                        smslog.GuardNumber = sw.PhoneNumber;
                                        smslog.SiteId = null;  // setting as null since it is from RC else clientSiteId
                                        smslog.SiteName = null;  // setting as null since it is from RC else  clientsitename
                                        _smsChannelEventLogList.Add(smslog);
                                    }
                                }
                                _smsSenderProvider.SendSms(_smsChannelEventLogList, Subject + " : " + Notifications, svl);
                            }
                        }
                        //p4-79 menucorrections-start
                        if (chkGlobalPersonalEmail == true)
                        {
                            string smsSiteEmails = null;
                            foreach (var item in clientSitesClientType)
                            {
                                var guardEmails = _guardLogDataProvider.GetGuardLogs(item.Id).Select(x => x.Guard.Email);

                                var guardEmailsnew = guardEmails.Distinct().ToList();




                                foreach (var item2 in guardEmailsnew)
                                {
                                    if (item2 != null)
                                    {
                                        if (smsSiteEmails == null)
                                            smsSiteEmails = item2;
                                        else
                                            smsSiteEmails = smsSiteEmails + ',' + item2;

                                    }
                                    //else
                                    //{
                                    //    success = false;
                                    //    message = "Please Enter the Guard Email";
                                    //    return new JsonResult(new { success, message });
                                    //}

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
                        //p4-79 menucorrections-end

                    }
                }
                if (chkNationality == true)
                {
                    svl.SubModule = "Global Push Notification-[Global] [National]";
                    var clientsiteIDNationality = _guardLogDataProvider.GetAllClientSites();
                    foreach (var itemAll in clientsiteIDNationality)
                    {
                        LogBookDetails(itemAll.Id, Notifications, Subject, tmzdata);

                    }
                    /* log book entry to citywtch control room */
                    var loginguardid = HttpContext.Session.GetInt32("GuardId") ?? 0;
                    _guardLogDataProvider.LogBookEntryFromRcControlRoomMessages(loginguardid, 0, Subject, Notifications, IrEntryType.Alarm, 1, 0, tmzdata);
                    foreach (var itemAll in clientsiteIDNationality)
                    {
                        EmailSender(itemAll.SiteEmail, itemAll.Id, Subject, Notifications);
                    }

                    if (checkedSMSPersonal == true)
                    {
                        svl.ActivityType = "SMS Personal";
                        foreach (var item in clientsiteIDNationality)
                        {
                            var guardlogins = _guardLogDataProvider.GetGuardLoginsByClientSiteId(item.Id, DateTime.Now);
                            string guardname = string.Empty;
                            List<SmsChannelEventLog> _smsChannelEventLogList = new List<SmsChannelEventLog>();
                            foreach (var logins in guardlogins)
                            {
                                if (logins.Guard.Mobile != null)
                                {
                                    SmsChannelEventLog smslog = new SmsChannelEventLog();
                                    smslog.GuardId = loginguardid != 0 ? loginguardid : null; // ID of guard who is sending the message
                                    smslog.GuardName = guardname.Length > 0 ? guardname : null; // Name of guard who is sending the message
                                    smslog.GuardNumber = logins.Guard.Mobile;
                                    smslog.SiteId = null;  // setting as null since it is from RC else clientSiteId
                                    smslog.SiteName = null;  // setting as null since it is from RC else  clientsitename
                                    _smsChannelEventLogList.Add(smslog);
                                }
                            }
                            _smsSenderProvider.SendSms(_smsChannelEventLogList, Subject + " : " + Notifications, svl);
                        }
                    }

                    if (checkedSMSSmartWand == true)
                    {
                        svl.ActivityType = "SMS Smart Wand";
                        foreach (var item in clientsiteIDNationality)
                        {
                            var smartWands = _guardLogDataProvider.GetClientSiteSmartWands(item.Id);
                            string guardname = string.Empty;
                            List<SmsChannelEventLog> _smsChannelEventLogList = new List<SmsChannelEventLog>();
                            foreach (var sw in smartWands)
                            {
                                if (sw.PhoneNumber != null)
                                {
                                    SmsChannelEventLog smslog = new SmsChannelEventLog();
                                    smslog.GuardId = loginguardid != 0 ? loginguardid : null; // ID of guard who is sending the message
                                    smslog.GuardName = guardname.Length > 0 ? guardname : null; // Name of guard who is sending the message
                                    smslog.GuardNumber = sw.PhoneNumber;
                                    smslog.SiteId = null;  // setting as null since it is from RC else clientSiteId
                                    smslog.SiteName = null;  // setting as null since it is from RC else  clientsitename
                                    _smsChannelEventLogList.Add(smslog);
                                }
                            }
                            _smsSenderProvider.SendSms(_smsChannelEventLogList, Subject + " : " + Notifications, svl);
                        }
                    }
                    //p4-79 menucorrections-start
                    if (chkGlobalPersonalEmail == true)
                    {
                        string smsSiteEmails = null;
                        foreach (var item in clientsiteIDNationality)
                        {
                            var guardEmails = _guardLogDataProvider.GetGuardLogs(item.Id).Select(x => x.Guard.Email);

                            var guardEmailsnew = guardEmails.Distinct().ToList();




                            foreach (var item2 in guardEmailsnew)
                            {
                                if (item2 != null)
                                {
                                    if (smsSiteEmails == null)
                                        smsSiteEmails = item2;
                                    else
                                        smsSiteEmails = smsSiteEmails + ',' + item2;

                                }
                                //else
                                //{
                                //    success = false;
                                //    message = "Please Enter the Guard Email";
                                //    return new JsonResult(new { success, message });
                                //}

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
                    //p4-79 menucorrections-end

                }

            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }


        public void LogBookDetailsMsg(int Id, string Notifications, string Subject, GuardLog tmzdata,string ClientSiteName)
        {
            #region Logbook
            if (Id != null)
            {

                var logbooktype = LogBookType.DailyGuardLog;
                //var logBookId = _guardLogDataProvider.GetClientSiteLogBookIdGloablmessage(Id, logbooktype, DateTime.Today);

                var logbookdate = DateTime.Today;
                // Get Last Logbookid and logbook Date by latest logbookid // p6#73 timezone bug - Modified by binoy 29-01-2024
                var logBookId = _guardLogDataProvider.GetClientSiteLogBookIdByLogBookMaxID(Id, logbooktype, out logbookdate);
                var ControlRoomMessage = "CRO STEPS message to " + ClientSiteName + ":";

                if (logBookId != 0)
                {
                    var guardid = HttpContext.Session.GetInt32("GuardId");
                    if (guardid != 0)
                    {

                        /* Save the push message for reload to logbook on next day Start*/
                        var radioCheckPushMessages = new RadioCheckPushMessages()
                        {
                            ClientSiteId = Id,
                            LogBookId = logBookId,
                            Notes = Subject + " : " + ControlRoomMessage + " <br/> " + Notifications,
                            EntryType = (int)IrEntryType.Alarm,
                            Date = DateTime.Today,
                            IsAcknowledged = 0,
                            IsDuress = 0,
                            PlayNotificationSound = false
                        };
                        var pushMessageId = _guardLogDataProvider.SavePushMessage(radioCheckPushMessages);
                        /* Save the push message for reload to logbook on next day end*/
                        var guardLoginId = _guardLogDataProvider.GetGuardLoginId(Convert.ToInt32(guardid), DateTime.Today);
                        // var guardName = _guardLogDataProvider.GetGuards(ClientSiteRadioChecksActivity.GuardId).Name;
                        var guardLog = new GuardLog()
                        {
                            ClientSiteLogBookId = logBookId,
                            GuardLoginId = guardLoginId,
                            EventDateTime = DateTime.Now,
                            Notes = Subject + " : " + ControlRoomMessage + " <br/> " + Notifications,
                            //Notes = "Caution Alarm: There has been '0' activity in KV & LB for 2 hours from guard[" + guardName + "]",
                            //IsSystemEntry = true,
                            IrEntryType = IrEntryType.Alarm,
                            RcPushMessageId = pushMessageId,
                            EventDateTimeLocal = tmzdata.EventDateTimeLocal,
                            EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                            EventDateTimeZone = tmzdata.EventDateTimeZone,
                            EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                            EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                            PlayNotificationSound = true
                        };
                        _guardLogDataProvider.SaveGuardLog(guardLog);
                    }
                    else
                    {
                        

                        /* Save the push message for reload to logbook on next day Start*/
                        var radioCheckPushMessages = new RadioCheckPushMessages()
                        {
                            ClientSiteId = Id,
                            LogBookId = logBookId,
                            Notes = Subject + " : " + ControlRoomMessage + " <br/> " + Notifications,
                            EntryType = (int)IrEntryType.Alarm,
                            Date = DateTime.Today,
                            IsAcknowledged = 0,
                            IsDuress = 0,
                            PlayNotificationSound = false
                        };
                        var pushMessageId = _guardLogDataProvider.SavePushMessage(radioCheckPushMessages);


                        var guardLog = new GuardLog()
                        {
                            ClientSiteLogBookId = logBookId,
                            EventDateTime = DateTime.Now,
                            Notes = Subject + " : "+ ControlRoomMessage + " <br/> " + Notifications,
                            //Notes = "Caution Alarm: There has been '0' activity in KV & LB for 2 hours from guard[" + guardName + "]",
                            //IsSystemEntry = true,
                            IrEntryType = IrEntryType.Alarm,
                            RcPushMessageId = pushMessageId,
                            EventDateTimeLocal = tmzdata.EventDateTimeLocal,
                            EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                            EventDateTimeZone = tmzdata.EventDateTimeZone,
                            EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                            EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                            PlayNotificationSound = true
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
        public void LogBookDetails(int Id, string Notifications, string Subject, GuardLog tmzdata)
        {
            #region Logbook
            if (Id != null)
            {

                var logbooktype = LogBookType.DailyGuardLog;
                //var logBookId = _guardLogDataProvider.GetClientSiteLogBookIdGloablmessage(Id, logbooktype, DateTime.Today);

                var logbookdate = DateTime.Today;
                // Get Last Logbookid and logbook Date by latest logbookid // p6#73 timezone bug - Modified by binoy 29-01-2024
                var logBookId = _guardLogDataProvider.GetClientSiteLogBookIdByLogBookMaxID(Id, logbooktype, out logbookdate);

                if (logBookId != 0)
                {
                    var guardid = HttpContext.Session.GetInt32("GuardId");
                    if (guardid != 0)
                    {

                        /* Save the push message for reload to logbook on next day Start*/
                        var radioCheckPushMessages = new RadioCheckPushMessages()
                        {
                            ClientSiteId = Id,
                            LogBookId = logBookId,
                            Notes = Subject + " : " + Notifications,
                            EntryType = (int)IrEntryType.Alarm,
                            Date = DateTime.Today,
                            IsAcknowledged = 0,
                            IsDuress = 0,
                            PlayNotificationSound = false
                        };
                        var pushMessageId = _guardLogDataProvider.SavePushMessage(radioCheckPushMessages);
                        /* Save the push message for reload to logbook on next day end*/
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
                            IrEntryType = IrEntryType.Alarm,
                            RcPushMessageId = pushMessageId,
                            EventDateTimeLocal = tmzdata.EventDateTimeLocal,
                            EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                            EventDateTimeZone = tmzdata.EventDateTimeZone,
                            EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                            EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                            PlayNotificationSound = true
                        };
                        _guardLogDataProvider.SaveGuardLog(guardLog);
                    }
                    else
                    {
                        /* Save the push message for reload to logbook on next day Start*/
                        var radioCheckPushMessages = new RadioCheckPushMessages()
                        {
                            ClientSiteId = Id,
                            LogBookId = logBookId,
                            Notes = Subject + " : " + Notifications,
                            EntryType = (int)IrEntryType.Alarm,
                            Date = DateTime.Today,
                            IsAcknowledged = 0,
                            IsDuress = 0,
                            PlayNotificationSound = false
                        };
                        var pushMessageId = _guardLogDataProvider.SavePushMessage(radioCheckPushMessages);


                        var guardLog = new GuardLog()
                        {
                            ClientSiteLogBookId = logBookId,
                            EventDateTime = DateTime.Now,
                            Notes = Subject + " : " + Notifications,
                            //Notes = "Caution Alarm: There has been '0' activity in KV & LB for 2 hours from guard[" + guardName + "]",
                            //IsSystemEntry = true,
                            IrEntryType = IrEntryType.Alarm,
                            RcPushMessageId = pushMessageId,
                            EventDateTimeLocal = tmzdata.EventDateTimeLocal,
                            EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                            EventDateTimeZone = tmzdata.EventDateTimeZone,
                            EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                            EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                            PlayNotificationSound = true
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
        public JsonResult OnGetClientSitesRadioSearch(int? page, int? limit, int? typeId, string searchTerm)
        {
            return new JsonResult(_guardLogDataProvider.GetUserClientSitesHavingAccessRadio(typeId, AuthUserHelperRadio.LoggedInUserId, searchTerm));
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

        //p4#48 AudioNotification - Binoy - 12-01-2024 -- Start
        public JsonResult OnPostUpdateDuressAlarmPlayedStatus()
        {
            _guardLogDataProvider.UpdateDuressAlarmPlayedStatus();
            return new JsonResult(new { status = "played" });
        }
        //p4#48 AudioNotification - Binoy - 12-01-2024 -- End

        //code added for ActionList Send start
        public JsonResult OnPostSaveActionList(string Notifications, string Subject, int[] ClientType, int[] clientSiteId, string AlarmKeypadCode,
            string Action1, string Physicalkey, string Action2, string SiteCombinationLook, string Action3, string Action4, string Action5,
            string CommentsForControlRoomOperator, GuardLog tmzdata)
        {
            var success = true;
            var message = "success";
            //Coomented  as per the client request 07062024
            //var ActionListMessage = (string.IsNullOrEmpty(AlarmKeypadCode) ? string.Empty : "AlarmKeypadCode: " + AlarmKeypadCode + "\n") +
            //       (string.IsNullOrEmpty(Physicalkey) ? string.Empty : "PhysicalKey: " + Physicalkey + "\n") +
            //       (string.IsNullOrEmpty(AlarmKeypadCode) ? string.Empty : "AlarmKeypadCode: " + AlarmKeypadCode + "\n") +
            //       (string.IsNullOrEmpty(SiteCombinationLook) ? string.Empty : "CombinationLook: " + SiteCombinationLook + "\n") +
            //       (string.IsNullOrEmpty(Action1) ? string.Empty : "Action1: " + Action1 + "\n") +
            //      (string.IsNullOrEmpty(Action2) ? string.Empty : "Action2: " + Action2 + "\n") +
            //       (string.IsNullOrEmpty(Action3) ? string.Empty : "Action3: " + Action3 + "\n") +
            //       (string.IsNullOrEmpty(Action4) ? string.Empty : "Action4: " + Action4 + "\n") +
            //       (string.IsNullOrEmpty(CommentsForControlRoomOperator) ? string.Empty : "CommentsForControlRoomOperator: " + CommentsForControlRoomOperator + "\n") +
            //       (string.IsNullOrEmpty(Notifications) ? string.Empty : "Message: " + Notifications + "\n");


            var ActionListMessage = (string.IsNullOrEmpty(Notifications) ? string.Empty : "Message: " + Notifications);
            try
            {


                if (clientSiteId.Length == 0)
                {
                    //var clientsitename = _guardLogDataProvider.GetClientSites(clientSiteId).Select(x => x.Name).FirstOrDefault();

                    var clientSitesClientType = _guardLogDataProvider.GetAllClientSites().Where(x => ClientType.Contains(x.TypeId));
                    foreach (var clientSiteTypeID in clientSitesClientType)
                    {

                        LogBookDetails(clientSiteTypeID.Id, ActionListMessage, Subject, tmzdata);


                    }
                    /* log book entry to citywtch control room */
                    var loginguardid = HttpContext.Session.GetInt32("GuardId") ?? 0;
                    _guardLogDataProvider.LogBookEntryFromRcControlRoomMessages(loginguardid, 0, Subject, Notifications, IrEntryType.Alarm, 1, 0, tmzdata);
                    foreach (var clientSiteTypeID in clientSitesClientType)
                    {
                        EmailSender(clientSiteTypeID.SiteEmail, clientSiteTypeID.Id, Subject, ActionListMessage);
                    }

                }
                else
                {
                    var clientSitesClientType = _guardLogDataProvider.GetAllClientSites().Where(x => clientSiteId.Contains(x.Id));
                    foreach (var clientSiteTypeID in clientSitesClientType)
                    {

                        //LogBookDetails(clientSiteTypeID.Id, ActionListMessage, Subject, tmzdata);
                        var clientsitename = _guardLogDataProvider.GetClientSites(clientSiteTypeID.Id).Select(x => x.Name).FirstOrDefault();

                        LogBookDetailsMsg(clientSiteTypeID.Id, ActionListMessage, Subject, tmzdata, clientsitename);
                    }
                    /* log book entry to citywtch control room */
                    var loginguardid = HttpContext.Session.GetInt32("GuardId") ?? 0;
                    _guardLogDataProvider.LogBookEntryFromRcControlRoomMessagesActionList(loginguardid, 0, Subject, ActionListMessage, IrEntryType.Alarm, 1, 0, tmzdata);
                    foreach (var clientSiteTypeID in clientSitesClientType)
                    {
                        EmailSender(clientSiteTypeID.SiteEmail, clientSiteTypeID.Id, Subject, ActionListMessage);
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

        public JsonResult OnPostSaveActionListGlobal(string Notifications, string Subject, int[] ClientType, int[] clientSiteId, string AlarmKeypadCode, string Action1, string Physicalkey, string Action2, string SiteCombinationLook, string Action3, string Action4, string Action5, string CommentsForControlRoomOperator, GuardLog tmzdata)
        {
            var success = true;
            var message = "success";

            var ActionListMessage = (string.IsNullOrEmpty(AlarmKeypadCode) ? string.Empty : "AlarmKeypadCode: " + AlarmKeypadCode + "\n") +
                    (string.IsNullOrEmpty(Physicalkey) ? string.Empty : "PhysicalKey: " + Physicalkey + "\n") +
                    (string.IsNullOrEmpty(AlarmKeypadCode) ? string.Empty : "AlarmKeypadCode: " + AlarmKeypadCode + "\n") +
                    (string.IsNullOrEmpty(SiteCombinationLook) ? string.Empty : "CombinationLook: " + SiteCombinationLook + "\n") +
                    (string.IsNullOrEmpty(Action1) ? string.Empty : "Action1: " + Action1 + "\n") +
                   (string.IsNullOrEmpty(Action2) ? string.Empty : "Action2: " + Action2 + "\n") +
                    (string.IsNullOrEmpty(Action3) ? string.Empty : "Action3: " + Action3 + "\n") +
                    (string.IsNullOrEmpty(Action4) ? string.Empty : "Action4: " + Action4 + "\n") +
                    (string.IsNullOrEmpty(CommentsForControlRoomOperator) ? string.Empty : "CommentsForControlRoomOperator: " + CommentsForControlRoomOperator + "\n") +
                    (string.IsNullOrEmpty(Notifications) ? string.Empty : "Message: " + Notifications + "\n");

            try
            {


                if (clientSiteId.Length == 0)
                {
                    var clientSitesClientType = _guardLogDataProvider.GetAllClientSites().Where(x => ClientType.Contains(x.TypeId));
                    foreach (var clientSiteTypeID in clientSitesClientType)
                    {

                        LogBookDetails(clientSiteTypeID.Id, ActionListMessage, Subject, tmzdata);


                    }
                    /* log book entry to citywtch control room */
                    var loginguardid = HttpContext.Session.GetInt32("GuardId") ?? 0;
                    _guardLogDataProvider.LogBookEntryFromRcControlRoomMessages(loginguardid, 0, Subject, Notifications, IrEntryType.Alarm, 1, 0, tmzdata);
                    foreach (var clientSiteTypeID in clientSitesClientType)
                    {
                        EmailSender(clientSiteTypeID.SiteEmail, clientSiteTypeID.Id, Subject, ActionListMessage);
                    }

                }
                else
                {
                    var clientSitesClientType = _guardLogDataProvider.GetAllClientSites().Where(x => clientSiteId.Contains(x.Id));
                    foreach (var clientSiteTypeID in clientSitesClientType)
                    {

                        LogBookDetails(clientSiteTypeID.Id, ActionListMessage, Subject, tmzdata);
                    }
                    /* log book entry to citywtch control room */
                    var loginguardid = HttpContext.Session.GetInt32("GuardId") ?? 0;
                    _guardLogDataProvider.LogBookEntryFromRcControlRoomMessages(loginguardid, 0, Subject, ActionListMessage, IrEntryType.Alarm, 1, 0, tmzdata);
                    foreach (var clientSiteTypeID in clientSitesClientType)
                    {
                        EmailSender(clientSiteTypeID.SiteEmail, clientSiteTypeID.Id, Subject, ActionListMessage);
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
        public JsonResult OnPostActionList(int clientSiteId)
        {
            var rtn = _guardLogDataProvider.GetActionlist(clientSiteId);

            if (rtn != null)
            {
                var LandLine = _configDataProvider.GetClientSiteLandline(clientSiteId);
                rtn.Landline = LandLine.LandLine;
                var SmartWandIDs = _configDataProvider.GetClientSiteSmartwands(clientSiteId);
                rtn.SmartWandID = SmartWandIDs.Select(x => x.PhoneNumber).ToList();
                if (rtn.Imagepath != null)
                {
                    rtn.Imagepath = rtn.Imagepath + ":-:" + ConvertFileToBase64(rtn.Imagepath);
                }
                var sopAlarmfileType= _configDataProvider.GetStaffDocumentsUsingType(6).Where(z => z.ClientSite == clientSiteId);
                if (sopAlarmfileType.Count() != 0)
                {
                    rtn.SOPAlarmFileNme = sopAlarmfileType.Select(x=>x.FileName).ToList();
                    rtn.SOPAlarmFilePath = sopAlarmfileType.Select(x => x.FilePath).ToList();
                }
                    var sopfiletype = _configDataProvider.GetStaffDocumentsUsingType(4).Where(z => z.ClientSite == clientSiteId);
                if (sopfiletype.Count() != 0)
                {
                    rtn.SOPFileNme = sopfiletype.FirstOrDefault().FileName;
                    rtn.SOPAlarmFilePath = sopAlarmfileType.Select(x => x.FilePath).ToList();

                }
                else
                {
                    rtn.SOPFileNme = null;
                }
            }
            else
            {
                var LandLine = _configDataProvider.GetClientSiteLandline(clientSiteId);
               
                //if null assign the value of the SOPFileNme
                rtn = new RCActionList();

                var sopAlarmfileType = _configDataProvider.GetStaffDocumentsUsingType(6).Where(z => z.ClientSite == clientSiteId);
                if (sopAlarmfileType.Count() != 0)
                {
                    rtn.SOPAlarmFileNme = sopAlarmfileType.Select(x => x.FileName).ToList();
                    rtn.SOPAlarmFilePath = sopAlarmfileType.Select(x => x.FilePath).ToList();
                }

                rtn.Landline = LandLine.LandLine;
                var SmartWandIDs = _configDataProvider.GetClientSiteSmartwands(clientSiteId);
                rtn.SmartWandID = SmartWandIDs.Select(x => x.PhoneNumber).ToList();

                var sopfiletype = _configDataProvider.GetStaffDocumentsUsingType(4).Where(z => z.ClientSite == clientSiteId);
                if (sopfiletype.Count() != 0)
                {
                    rtn.SOPFileNme = sopfiletype.FirstOrDefault().FileName;
                }
                else
                {
                    rtn.SOPFileNme = null;
                }
            }
            return new JsonResult(rtn);
        }
        public JsonResult OnPostGetClientType(int clientSiteId)
        {
            return new JsonResult(_guardLogDataProvider.GetClientTypeByClientSiteId(clientSiteId));
        }
        public JsonResult OnPostSearchClientsite(string searchTerm)
        {
            var clientSites = "";
            if (!string.IsNullOrEmpty(searchTerm))
            {
                clientSites = _guardLogDataProvider.GetUserClientSites(searchTerm);
            }

            return new JsonResult(clientSites);
        }  

        public JsonResult OnPostSearchClientsiteRCList(string searchTerm, int clientSiteId)
        {
            //int clientSiteId;
            if (!string.IsNullOrEmpty(searchTerm))
            {
                clientSiteId = _guardLogDataProvider.GetUserClientSitesRCList(searchTerm);
            }

            return new JsonResult(_guardLogDataProvider.GetActionlist(clientSiteId));
        }

        public JsonResult OnPostGetClientsiteAddressAndMapDetails(int searchclientSiteId)
        {
            ClientSite sitesDetails = new ClientSite();
            if (searchclientSiteId > 0)
            {
                sitesDetails = _guardLogDataProvider.GetClientSites(searchclientSiteId).FirstOrDefault();
            }
            return new JsonResult(sitesDetails);
        }
        public JsonResult OnPostClientSiteManningKpiSettings(ClientSiteKpiSetting clientSiteKpiSetting)
        {
            var success = 0;
            var clientSiteId = 0;
            var erorrMessage = string.Empty;
            try
            {
                if (clientSiteKpiSetting != null)
                {
                    if (clientSiteKpiSetting.Id != 0)
                    {
                        clientSiteId = clientSiteKpiSetting.ClientSiteId;
                        var positionIdGuard = clientSiteKpiSetting.ClientSiteManningGuardKpiSettings.Where(x => x.PositionId != 0).FirstOrDefault();
                        var positionIdPatrolCar = clientSiteKpiSetting.ClientSiteManningPatrolCarKpiSettings.Where(x => x.PositionId != 0).FirstOrDefault();
                        var InvalidTimes = _clientDataProvider.ValidDateTime(clientSiteKpiSetting);
                        if (InvalidTimes.Trim() == string.Empty)
                        {
                            if (positionIdGuard != null || positionIdPatrolCar != null)
                            {
                                var rulenumberOne = _clientDataProvider.CheckRulesOneinKpiManningInput(clientSiteKpiSetting);

                                if (rulenumberOne.Trim() == string.Empty)
                                {
                                    var rulenumberTwo = _clientDataProvider.CheckRulesTwoinKpiManningInput(clientSiteKpiSetting);
                                    if (rulenumberTwo.Trim() == string.Empty)
                                    {
                                        success = _clientDataProvider.SaveClientSiteManningKpiSetting(clientSiteKpiSetting);
                                        /* If change in the status update start */
                                        _clientDataProvider.UpdateClientSiteStatus(clientSiteKpiSetting.ClientSiteId, clientSiteKpiSetting.ClientSite.StatusDate, clientSiteKpiSetting.ClientSite.Status, clientSiteKpiSetting.Id, clientSiteKpiSetting.KPITelematicsFieldID);
                                        /* If change in the status update end */
                                    }
                                    else
                                    {
                                        erorrMessage = rulenumberTwo;
                                        success = 7;

                                    }

                                }
                                else
                                {
                                    erorrMessage = rulenumberOne;
                                    success = 6;

                                }



                            }
                            else
                            {
                                success = 3;
                            }
                        }
                        else
                        {
                            erorrMessage = InvalidTimes;
                            success = 5;
                        }
                    }
                    else
                    {
                        success = 2;
                    }
                }
                else
                {
                    success = 4;
                }
            }
            catch
            {
                success = 4;
            }

            return new JsonResult(new { success, clientSiteId, erorrMessage });
        }

        public JsonResult OnPostDeleteWorker(string settingsId)
        {
            var status = true;
            var message = "Success";
            var clientSiteId = 0;
            try
            {
                if (settingsId != string.Empty)
                {
                    var split = settingsId.Split('_');
                    if (split.Length > 0)
                    {
                        var settId = int.Parse(split[0]);
                        var orderId = int.Parse(split[1]);
                        clientSiteId = int.Parse(split[2]);
                        _clientDataProvider.RemoveWorker(settId, orderId);
                    }
                }
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }
            return new JsonResult(new { status, message, clientSiteId });
        }


        public JsonResult OnPostDeleteWorkerADHOC(string settingsId)
        {
            var status = true;
            var message = "Success";
            var clientSiteId = 0;
            try
            {
                if (settingsId != string.Empty)
                {
                    var split = settingsId.Split('_');
                    if (split.Length > 0)
                    {
                        var settId = int.Parse(split[0]);
                        var orderId = int.Parse(split[1]);
                        clientSiteId = int.Parse(split[2]);
                        _clientDataProvider.RemoveWorkerADHOC(settId, orderId);
                    }
                }
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }
            return new JsonResult(new { status, message, clientSiteId });
        }

        public IActionResult OnGetOfficerPositions(OfficerPositionFilterManning filter)
        {
            return new JsonResult(ViewDataService.GetOfficerPositions((OfficerPositionFilterManning)filter));
        }
        //code added for ActionListSend stop

        #region RcActionListEdit
        public PartialViewResult OnGetClientSiteKpiSettings(int siteId)
        {
            var clientSiteKpiSetting = _clientDataProvider.GetClientSiteKpiSetting(siteId);
            clientSiteKpiSetting ??= new ClientSiteKpiSetting() { ClientSiteId = siteId };
            if (clientSiteKpiSetting.rclistKP.ClientSiteID == 0)
            {
                clientSiteKpiSetting.rclistKP.ClientSiteID = siteId;

            }
            if (clientSiteKpiSetting.rclistKP.Imagepath != null)
            {
                if (clientSiteKpiSetting.rclistKP.Imagepath.Length > 0 && clientSiteKpiSetting.rclistKP.Imagepath.Trim() != "")
                {
                    clientSiteKpiSetting.rclistKP.Imagepath = clientSiteKpiSetting.rclistKP.Imagepath + ":-:" + ConvertFileToBase64(clientSiteKpiSetting.rclistKP.Imagepath);
                }

            }
            return Partial("../admin/_ClientSiteKpiSetting", clientSiteKpiSetting);
        }

        //code added For RcAction List start
        public JsonResult OnPostClientSiteRCActionList(RCActionList rcActionList)
        {
            var status = true;
            var message = "Success";
            var id = -1;
            try
            {

                if (rcActionList.SettingsId != 0)
                {

                    id = _clientDataProvider.SaveRCList(rcActionList);

                }
                else
                {
                    status = false;
                    message = "Please add site settings in KPI first to proceed with RC Action list.";
                }
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status, message, id });
        }

        //code added For RcAction List stop

        public JsonResult OnPostUploadRCImage()
        {
            var success = true;
            var files = Request.Form.Files;
            var fileName = string.Empty;
            var dtm = DateTime.Now;
            var Imagepath = "";
            try
            {
                if (files.Count == 1)
                {
                    var file = files[0];
                    fileName = file.FileName;
                    var scheduleId = Convert.ToInt32(Request.Form["scheduleId"]);
                    var Id = Convert.ToInt32(Request.Form["id"]);
                    dtm = Convert.ToDateTime(Request.Form["updatetime"]);

                    var summaryImageDir = _settings.RCActionListKpiImageFolder;  // Path.Combine(_webHostEnvironment.WebRootPath, "RCImage");
                    if (!Directory.Exists(summaryImageDir))
                        Directory.CreateDirectory(summaryImageDir);

                    using (var stream = System.IO.File.Create(Path.Combine(summaryImageDir, fileName)))
                    {
                        file.CopyTo(stream);
                    }
                    _clientDataProvider.SaveUpdateRCListFile(Id, fileName, dtm);
                    Imagepath = fileName + ":-:" + ConvertFileToBase64(fileName);
                }
            }
            catch
            {
                success = false;
            }

            return new JsonResult(new { success, fileName, LastUpdated = dtm, Imagepath = Imagepath });
        }

        public JsonResult OnPostDeleteRCImage(string imageName)
        {
            var status = true;
            var message = "Success";
            try
            {
                if (!string.IsNullOrEmpty(imageName))
                {
                    var fileToDelete = Path.Combine(_settings.RCActionListKpiImageFolder, imageName);
                    if (System.IO.File.Exists(fileToDelete))
                        System.IO.File.Delete(fileToDelete);
                }

            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status, message });
        }

        //to delete RC
        public JsonResult OnPostDeleteRC(int RCId)
        {
            var status = true;
            var message = "Success";
            try
            {
                _clientDataProvider.RemoveRCList(RCId);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status, message });
        }

        public string ConvertFileToBase64(string imageName)
        {
            string rtnstring = "";

            if (!string.IsNullOrEmpty(imageName))
            {
                var fileToConvert = Path.Combine(_settings.RCActionListKpiImageFolder, imageName);
                if (System.IO.File.Exists(fileToConvert))
                {
                    byte[] AsBytes = System.IO.File.ReadAllBytes(fileToConvert);
                    rtnstring = "data:application/octet-stream;base64," + Convert.ToBase64String(AsBytes);
                }
            }

            return rtnstring;
        }

        #endregion RcActionListEdit

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

        public JsonResult OnGetGuards(int id)
        {
            var data = _guardLogDataProvider.GetGuardsWtihProviderNumber(id);
            return new JsonResult(data);
        }



        public JsonResult OnPostDownLoadHelpPDF(string filename, int loginGuardId, GuardLog tmdata, string pageName, int loginUserId)
        {

            var Issuccess = false;
            var exMessage = "";
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    var userid = loginUserId;
                    if (userid != 0)
                    {
                        var IPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString();

                        if (loginGuardId != 0)
                        {

                            FileDownloadAuditLogs fdal = new FileDownloadAuditLogs()
                            {
                                UserID = (int)userid,
                                GuardID = loginGuardId,
                                IPAddress = IPAddress,
                                DwnlCatagory = "Help Document RC",
                                DwnlFileName = filename,
                                EventDateTimeLocal = tmdata.EventDateTimeLocal,
                                EventDateTimeLocalWithOffset = tmdata.EventDateTimeLocalWithOffset,
                                EventDateTimeZone = tmdata.EventDateTimeZone,
                                EventDateTimeZoneShort = tmdata.EventDateTimeZoneShort,
                                EventDateTimeUtcOffsetMinute = tmdata.EventDateTimeUtcOffsetMinute
                            };

                            _guardLogDataProvider.CreateDownloadFileAuditLogEntry(fdal);
                            Issuccess = true;

                        }
                        else
                        {
                            exMessage = "Error: Guard details not found.";
                        }

                    }
                    else
                    {
                        exMessage = "Error: User not authenticated.";
                    }

                    Issuccess = true;
                }
                else
                {
                    exMessage = "Error: User not authenticated.";
                }


            }
            catch (Exception ex)
            {
                //_logger.LogError(ex.StackTrace);
                exMessage = $"Error: {ex.Message}.";
            }
            Issuccess = true;
            return new JsonResult(new { success = Issuccess, message = exMessage });
        }



        public JsonResult OnPostClientSiteManningKpiSettingsADHOC(ClientSiteKpiSetting clientSiteKpiSetting)
        {
            var success = 0;
            var clientSiteId = 0;
            var erorrMessage = string.Empty;
            try
            {


                if (clientSiteKpiSetting != null)
                {
                    if (clientSiteKpiSetting.Id != 0)
                    {


                        clientSiteId = clientSiteKpiSetting.ClientSiteId;
                        var positionIdGuard = clientSiteKpiSetting.ClientSiteManningGuardKpiSettingsADHOC.Where(x => x.PositionId != 0).FirstOrDefault();
                        var positionIdPatrolCar = clientSiteKpiSetting.ClientSiteManningPatrolCarKpiSettingsADHOC.Where(x => x.PositionId != 0).FirstOrDefault();

                        var InvalidTimes = _clientDataProvider.ValidDateTimeADHOC(clientSiteKpiSetting);

                        if (InvalidTimes.Trim() == string.Empty)
                        {

                            if (positionIdGuard != null || positionIdPatrolCar != null)
                            {
                                var rulenumberOne = _clientDataProvider.CheckRulesOneinKpiManningInputADHOC(clientSiteKpiSetting);

                                if (rulenumberOne.Trim() == string.Empty)
                                {
                                    var rulenumberTwo = _clientDataProvider.CheckRulesTwoinKpiManningInputADHOC(clientSiteKpiSetting);
                                    if (rulenumberTwo.Trim() == string.Empty)
                                    {
                                        success = _clientDataProvider.SaveClientSiteManningKpiSettingADHOC(clientSiteKpiSetting);
                                        /* If change in the status update start */
                                        //_clientDataProvider.UpdateClientSiteStatus(clientSiteKpiSetting.ClientSiteId, clientSiteKpiSetting.ClientSite.StatusDate, clientSiteKpiSetting.ClientSite.Status, clientSiteKpiSetting.Id);
                                        /* If change in the status update end */
                                    }
                                    else
                                    {
                                        erorrMessage = rulenumberTwo;
                                        success = 7;

                                    }

                                }
                                else
                                {
                                    erorrMessage = rulenumberOne;
                                    success = 6;

                                }



                            }
                            else
                            {
                                success = 3;
                            }

                        }
                        else
                        {
                            erorrMessage = InvalidTimes;
                            success = 5;

                        }
                    }
                    else
                    {
                        success = 2;
                    }

                }
                else
                {
                    success = 4;

                }

            }
            catch
            {
                success = 4;
            }

            return new JsonResult(new { success, clientSiteId, erorrMessage });
        }

        public JsonResult OnGetStaffDocsUsingTypeNew(int type, int ClientSiteId)
        {
            return new JsonResult(_configDataProvider.GetStaffDocumentsUsingTypeNew(type, Convert.ToInt32(ClientSiteId)));
        }
        public JsonResult OnGetGuardDetails(int GuardID)
        {
            
            return new JsonResult(ViewDataService.GetGuardsDetails(GuardID));


        }


    }
}
