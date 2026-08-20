using CityWatch.Data;
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
using CityWatch.RadioCheck.Services;
using CityWatch.Web.Models;
using CityWatch.RadioCheck.Helpers;
using CityWatch.Web.Pages.Radio;

namespace CityWatch.RadioCheck.Pages.Radio
{
    public class ActiveGuardSinglePage : PageModel
    {


        
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        private readonly EmailOptions _EmailOptions;
        private readonly IConfiguration _configuration;
        private readonly ILogbookDataService _logbookDataService;
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly IViewDataService _viewDataService;
        private readonly CityWatchDbContext _context;
        public ActiveGuardSinglePage(IGuardLogDataProvider guardLogDataProvider, IOptions<EmailOptions> emailOptions,
            IConfiguration configuration,ILogbookDataService logbookDataService, IGuardDataProvider guardDataProvider, IViewDataService viewDataService,
            CityWatchDbContext context)
        {

            _guardLogDataProvider = guardLogDataProvider;
            _EmailOptions = emailOptions.Value;
            _configuration = configuration;
            _logbookDataService = logbookDataService;
            _guardDataProvider = guardDataProvider;
            _viewDataService = viewDataService;
            _context = context;
        }
        public int UserId { get; set; }
        public int GuardId { get; set; }
        public int InActiveGuardCount { get; set; }

        public int ActiveGuardCount { get; set; }
        public string DisplayItem { get; set; }
        public GuardViewModel Guard { get; set; }
        public string UserRole { get; set; }
        public IActionResult OnGet(string displayItem)
        {
            /*Api call Start */
            CallApi();
            /* Api call end */
            DisplayItem = displayItem;
            //var activeGuardDetails = _guardLogDataProvider.GetActiveGuardDetails();
            //ActiveGuardCount = activeGuardDetails.Count();
            //var inActiveGuardDetails = _guardLogDataProvider.GetInActiveGuardDetails();
            //InActiveGuardCount = inActiveGuardDetails.Count();

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
            UserRole = "0";
           var guidFromQuery =  HttpContext.Session.GetString("Guid");

            if (!string.IsNullOrEmpty(guidFromQuery))
            {
                LoginGuardId = guidFromQuery;
                HttpContext.Session.SetString("Guid", guidFromQuery);
            }
            else
            {
                HttpContext.Session.Remove("Guid"); // removes it from session if empty
            }

            /* new code added for guard can view allowed sites Start*/
            List<int> allowedSiteIds = new List<int>();
            if (!string.IsNullOrEmpty(LoginGuardId))
            {
                var clientSites = _guardDataProvider.GetGuardRcClientSiteAccess(int.Parse(LoginGuardId));
                if (clientSites != null && clientSites.Any())
                {
                    allowedSiteIds = clientSites.Select(s => s.ClientSiteId).ToList();
                }
            }

            var activeGuardDetails = _guardLogDataProvider.GetActiveGuardDetails();
            if (allowedSiteIds.Any())
            {
                activeGuardDetails = activeGuardDetails
                    .Where(g => allowedSiteIds.Contains(g.ClientSiteId))
                    .ToList();
            }
            ActiveGuardCount = activeGuardDetails.Count();

            // Inactive guards
            var inActiveGuardDetails = _guardLogDataProvider.GetInActiveGuardDetails();
            if (allowedSiteIds.Any())
            {
                inActiveGuardDetails = inActiveGuardDetails
                    .Where(g => allowedSiteIds.Contains(g.ClientSiteId))
                    .ToList();
            }
            InActiveGuardCount = inActiveGuardDetails.Count();


            
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
            

            if (!string.IsNullOrEmpty(LoginGuardId))
            {
               
               
                GuardId = int.Parse(LoginGuardId);
                HttpContext.Session.SetInt32("GuardId", GuardId);
                if (GuardId != 0)
                {
                    Guard = _viewDataService.GetGuards().SingleOrDefault(x => x.Id == GuardId);


                }
                HttpContext.Session.SetInt32("GuardId", GuardId);
                HttpContext.Session.SetInt32("loginUserId", UserId);
                ViewData["GuardId"] = GuardId;

                var guard = _guardDataProvider.GetGuardDetailsUsingId(GuardId).FirstOrDefault();
                // Convert boolean to string value
                UserRole = (guard.IsRCFusionAccess || guard.IsRCHRAccess) ? "1" : "0";
                if (guard != null)
                {
                    if ((guard.IsAdminPowerUser || guard.IsAdminSOPToolsAccess || guard.IsAdminAuditorAccess || guard.IsAdminInvestigatorAccess) && (guard.IsRCAccess || guard.IsRCFusionAccess || guard.IsRCHRAccess || guard.IsRCLiteAccess))
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
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value;

                if (role == "Administrator")
                {
                  

                    HttpContext.Session.SetInt32("GuardId", 0);
                    UserRole = "1";
                }
                else if (role == "Guard")
                {
                    var sidnew = User.FindFirst(ClaimTypes.Sid)?.Value;

                    int guardId = 0;
                    int.TryParse(sidnew, out guardId);

                    if (guardId > 0)
                    {
                        var guardDetails = _guardDataProvider.GetGuardDetailsUsingId(guardId).FirstOrDefault();

                        // Convert boolean to string value
                        UserRole = (guardDetails.IsRCFusionAccess || guardDetails.IsRCHRAccess) ? "1" : "0";
                        HttpContext.Session.SetInt32("GuardId", guardId);

                       
                    }
                }
                else
                {
                    // Normal user login (Role = "User")
                  
                    HttpContext.Session.SetInt32("GuardId", 0);
                }
                return Page();
            }
            else if (GuardId != 0)
            {
                var guardDetails = _guardDataProvider.GetGuardDetailsUsingId(int.Parse(LoginGuardId)).FirstOrDefault();

                // Convert boolean to string value
                UserRole = (guardDetails.IsRCFusionAccess || guardDetails.IsRCHRAccess) ? "1" : "0";
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

        /// <summary>
        /// P4#153 Part 4: patrol-car fleet summary for the control room, built from
        /// existing RC data ONLY (activity status + today's PCAR wand-scan visits —
        /// no tracking-pack dependency). Answers the four control-room questions:
        /// how many cars, where is each now, how fresh is that, who is driving.
        /// On Site = last scan opened (TimeOn, no TimeOff); In Transit = last scan
        /// closed; Last Known = no scan yet, or nothing for 30+ minutes.
        /// </summary>
        public IActionResult OnGetPcarSummary()
        {
            try
            {
                var today = DateTime.Today;

                /* THE definition (field feedback, 20 Aug): a patrol-car guard is one whose
                   FIRST login TODAY was at a PCAR-mode site (the Romeo base). The activity
                   feed keeps one row per guard at their LATEST site, so a guard mid-patrol
                   carries no PCAR mark there — GuardLogins is where the day began. */
                var todaysLogins = _context.GuardLogins
                    .Where(x => x.OnDuty >= today)
                    .Select(x => new { x.GuardId, x.ClientSiteId, x.OnDuty })
                    .ToList();
                var pcarSiteIds = _context.ClientSites
                    .Where(cs => cs.PatrolTourMode == PatrolTouringMode.PCAR)
                    .Select(cs => new { cs.Id, cs.Name })
                    .ToList();
                var pcarSiteNames = pcarSiteIds.ToDictionary(cs => cs.Id, cs => cs.Name);
                var firstLoginByGuard = todaysLogins
                    .GroupBy(x => x.GuardId)
                    .ToDictionary(g => g.Key, g => g.OrderBy(x => x.OnDuty).First().ClientSiteId);
                var pcarGuardIds = firstLoginByGuard
                    .Where(kv => pcarSiteNames.ContainsKey(kv.Value))
                    .Select(kv => kv.Key)
                    .ToHashSet();
                if (pcarGuardIds.Count == 0)
                    return new JsonResult(Array.Empty<object>());

                /* Their CURRENT activity row — wherever the patrol has taken them. */
                var pcarGuards = _guardLogDataProvider.GetActiveGuardDetails()
                    .Where(x => pcarGuardIds.Contains(x.GuardId))
                    .GroupBy(x => x.GuardId)
                    .Select(g => g.First())
                    .ToList();
                if (pcarGuards.Count == 0)
                    return new JsonResult(Array.Empty<object>());

                var guardIds = pcarGuards.Select(x => x.GuardId).ToList();
                var visits = _context.PcarRouteDailyVisits
                    .Where(v => v.CreatedAt >= today && v.GuardId != null && guardIds.Contains(v.GuardId.Value))
                    .OrderBy(v => v.CreatedAt)
                    .ToList();
                var siteIds = visits.Select(v => v.SiteId).Distinct().ToList();
                var siteNames = _context.ClientSites
                    .Where(s => siteIds.Contains(s.Id))
                    .Select(s => new { s.Id, s.Name })
                    .ToList()
                    .ToDictionary(s => s.Id, s => s.Name);
                var routeIds = visits.Select(v => v.PcarRouteId).Distinct().ToList();
                var carByRoute = _context.PcarRoute
                    .Where(r => routeIds.Contains(r.Id))
                    .Select(r => new { r.Id, Car = r.SmartWand.PatrolCarName })
                    .ToList()
                    .ToDictionary(r => r.Id, r => r.Car);

                var now = DateTime.Now;
                var result = pcarGuards.Select(a =>
                {
                    var last = visits.LastOrDefault(v => v.GuardId == a.GuardId);
                    var onSite = last != null && !string.IsNullOrWhiteSpace(last.TimeOn) && string.IsNullOrWhiteSpace(last.TimeOff);
                    var minutes = last != null ? (int)Math.Max(0, (now - last.CreatedAt).TotalMinutes) : a.LatestDate;
                    var stale = minutes >= 30;
                    return new
                    {
                        guardId = a.GuardId,
                        guard = a.GuardName,
                        car = last != null && carByRoute.ContainsKey(last.PcarRouteId) ? carByRoute[last.PcarRouteId] : null,
                        /* The BASE site the day started at ("Citywatch M1 - Romeo Patrol
                           Cars") — the client shows its M-number as a badge. */
                        position = firstLoginByGuard.TryGetValue(a.GuardId, out var baseSiteId)
                            && pcarSiteNames.ContainsKey(baseSiteId) ? pcarSiteNames[baseSiteId] : a.OnlySiteName,
                        status = last == null || stale ? "lastknown" : onSite ? "onsite" : "transit",
                        site = last != null
                            ? (onSite || stale
                                ? (siteNames.ContainsKey(last.SiteId) ? siteNames[last.SiteId] : "Site " + last.SiteId)
                                : "Off Site")
                            : a.OnlySiteName,
                        minutesAgo = minutes
                    };
                }).ToList();

                return new JsonResult(result);
            }
            catch (Exception)
            {
                /* The fleet button shows 0 rather than the page breaking over a summary. */
                return new JsonResult(Array.Empty<object>());
            }
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
        public IActionResult OnGetClientSiteLastIncidentReportActivityStatus(int clientSiteId, int guardId)
        {
            
            var clientIncidentReports = _guardLogDataProvider.GetActiveGuardIncidentReportHistoryForRCNew( clientSiteId,  guardId);
           
            return new JsonResult(clientIncidentReports);
        }

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
