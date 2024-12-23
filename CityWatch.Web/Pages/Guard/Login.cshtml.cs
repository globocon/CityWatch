using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Helpers;
using CityWatch.Web.Models;
using CityWatch.Web.Services;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Dropbox.Api.TeamLog.PaperDownloadFormat;
using static Dropbox.Api.TeamLog.SpaceCapsType;

namespace CityWatch.Web.Pages.Guard
{
    public class LoginModel : PageModel
    {
        private readonly IViewDataService _viewDataService;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        public readonly IClientSiteWandDataProvider _clientSiteWandDataProvider;
        private readonly IConfigDataProvider _configDataProvider;
        private readonly EmailOptions _EmailOptions;
        public LoginModel(IViewDataService viewDataService,
            IClientDataProvider clientDataProvider,
            IGuardDataProvider guardDataProvider,
            IGuardLogDataProvider guardLogDataProvider,
            IClientSiteWandDataProvider clientSiteWandDataProvider,
            IConfigDataProvider configDataProvider,
             IOptions<EmailOptions> emailOptions)
        {
            _viewDataService = viewDataService;
            _clientDataProvider = clientDataProvider;
            _guardDataProvider = guardDataProvider;
            _guardLogDataProvider = guardLogDataProvider;
            _clientSiteWandDataProvider = clientSiteWandDataProvider;
            _configDataProvider = configDataProvider;
            _EmailOptions = emailOptions.Value;
        }

        [BindProperty]
        public GuardLoginViewModel GuardLogin { get; set; }

        [BindProperty]
        public LogBookType LogBookType { get; set; }
        public int ClientTypeId { get; set; }
        public string ClientTypeName { get; set; }
        public IViewDataService ViewDataService { get { return _viewDataService; } }

        public void OnGet(string t)
        {
            LogBookType = t switch
            {
                "gl" => LogBookType.DailyGuardLog,
                "vl" => LogBookType.VehicleAndKeyLog,
                _ => throw new ArgumentOutOfRangeException("Failed to identify log book type"),
            };
            var host = HttpContext.Request.Host.Host;
            var clientName = string.Empty;
            var clientLogo = string.Empty;
            var url = string.Empty;

            // Split the host by dots to separate subdomains and domain name
            var hostParts = host.Split('.');

            // If the first part is "www", take the second part as the client name
            if (hostParts.Length > 1 && hostParts[0].Trim().ToLower() == "www")
            {
                clientName = hostParts[1];
            }
            else
            {
                clientName = hostParts[0];
            }
            if (!string.IsNullOrEmpty(clientName))
            {
                if (
                    clientName.Trim().ToLower() != "www" &&
                    clientName.Trim().ToLower() != "cws-ir" &&
                    clientName.Trim().ToLower() != "test"
                    &&
                    clientName.Trim().ToLower() != "localhost"
                )
                {
                    int domain = _configDataProvider.GetSubDomainDetails(clientName).TypeId;
                    if (domain != 0)
                    {
                        ClientTypeId = domain;
                        ClientTypeName = _configDataProvider.GetClientTypeNameById(ClientTypeId);
                    }
                    else
                    {
                        ClientTypeId = 0;
                    }
                }
            }
         }

        public JsonResult OnPostLoginGuard()
        {
            if (!ModelState.IsValid)
            {
                return new JsonResult(new { success = false, errors = ModelState.Where(x => x.Value.Errors.Count > 0).Select(x => string.Join(',', x.Value.Errors.Select(y => y.ErrorMessage))) });
            }

            var smartWandOrPositionId = GetSmartWandOrPositionId();
            if (!smartWandOrPositionId.HasValue)
            {
                return new JsonResult(new { success = false, errors = new string[] { "Failed to load smart wand or position details" } });
            }

            if (GuardLogin.OffDuty <= GuardLogin.OnDuty)
            {
                GuardLogin.OffDuty = GuardLogin.OffDuty.Value.AddDays(1);
            }

            var success = false;
            var message = string.Empty;
            var guardInitals = GuardLogin.Guard.Initial;
            var initalsUsed = string.Empty;
            var GPSCoordinates = GuardLogin.GpsCoordinates;
            try
            {
                //p1-266 checking whether guard has access in login 
                


                    GuardLogin.SmartWandOrPositionId = smartWandOrPositionId;
                var clientType = _clientDataProvider.GetClientTypes().SingleOrDefault(z => z.Name == GuardLogin.ClientTypeName);
                GuardLogin.ClientSite = _clientDataProvider.GetClientSites(clientType.Id).SingleOrDefault(z => z.Name == GuardLogin.ClientSiteName);

                // Task p6#73_TimeZone issue -- added by Binoy - Start
                var guardLog = new GuardLog()
                {
                    EventDateTimeLocal = GuardLogin.EventDateTimeLocal,
                    EventDateTimeLocalWithOffset = GuardLogin.EventDateTimeLocalWithOffset,
                    EventDateTimeZone = GuardLogin.EventDateTimeZone,
                    EventDateTimeZoneShort = GuardLogin.EventDateTimeZoneShort,
                    EventDateTimeUtcOffsetMinute = GuardLogin.EventDateTimeUtcOffsetMinute,
                    PlayNotificationSound = false,
                    GpsCoordinates = GPSCoordinates,
                   
                };
                // Task p6#73_TimeZone issue -- added by Binoy - End

                //GuardLogin.ClientSite = _clientDataProvider.GetClientSites(null).SingleOrDefault(z => z.Name == GuardLogin.ClientSiteName);
                if (GuardLogin.IsNewGuard)
                {
                    GuardLogin.Guard.Id = _guardDataProvider.UpdateGuard(GuardLogin.Guard, GuardLogin.ClientSite.State, out initalsUsed);
                }
                else
                {
                    GuardLogin.Guard.IsActive = true;
                    _guardDataProvider.UpdateGuard(GuardLogin.Guard, GuardLogin.ClientSite.State, out initalsUsed);
                }

                var clientDateTime = DateTimeHelper.GetCurrentLocalTimeFromUtcMinute((int)GuardLogin.EventDateTimeUtcOffsetMinute); // p6#73 timezone bug - Added by binoy 24-01-2024
                var IsLoginInKeyVehOrLogBook = CheckIfAlreadyLoginInKeyVehicleLogBook(clientDateTime);
                var logBookId = GetLogBookId(out var isNewLogBook, clientDateTime); // p6#73 timezone bug - Modified by binoy 24-01-2024
                var guardLoginId = GetGuardLoginId(logBookId);
                // Task p6#73_TimeZone issue -- added by Binoy - Start
                var eventDateTimeLocal = GuardLogin.EventDateTimeLocal.Value;
                var eventDateTimeLocalWithOffset = GuardLogin.EventDateTimeLocalWithOffset.Value;
                var eventDateTimeZone = GuardLogin.EventDateTimeZone;
                var eventDateTimeZoneShort = GuardLogin.EventDateTimeZoneShort;
                var eventDateTimeUtcOffsetMinute = GuardLogin.EventDateTimeUtcOffsetMinute.Value;
                // Task p6#73_TimeZone issue -- added by Binoy - End
                //Save history for the Guard Login Start
                if (GuardLogin.Guard.IsLB_KV_IR)
                {
                    _guardLogDataProvider.SaveUserLoginHistoryDetails(new LoginUserHistory()
                {
                    LoginUserId = AuthUserHelper.LoggedInUserId.GetValueOrDefault(),
                    IPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString(),
                    LoginTime = DateTime.Now,
                    ClientSiteId= GuardLogin.ClientSite.Id,
                    GuardId= GuardLogin.Guard.Id,
                });
                //Save history for the Guard Login end
                if (LogBookType == LogBookType.DailyGuardLog)
                {
                    // Task p6#73_TimeZone issue -- modified by Binoy
                    
                    
                        CreateLogbookLoggedInEntry(logBookId, guardLoginId, eventDateTimeLocal,
                            eventDateTimeLocalWithOffset, eventDateTimeZone, eventDateTimeZoneShort, eventDateTimeUtcOffsetMinute, GPSCoordinates);
                   
                }
                if (LogBookType == LogBookType.VehicleAndKeyLog)
                {
                    
                        CreateKeyVehicleLoggedInEntry(logBookId, guardLoginId, eventDateTimeLocal,
                        eventDateTimeLocalWithOffset, eventDateTimeZone, eventDateTimeZoneShort, eventDateTimeUtcOffsetMinute, GPSCoordinates);
                    
                }

                if (LogBookType == LogBookType.VehicleAndKeyLog && isNewLogBook)
                {
                    var previousDayLogBook = _clientDataProvider.GetClientSiteLogBook(GuardLogin.ClientSite.Id, LogBookType.VehicleAndKeyLog, DateTime.Today.AddDays(-1));
                    if (previousDayLogBook != null)
                    {
                        _viewDataService.CopyOpenLogbookEntriesFromPreviousDay(previousDayLogBook.Id, logBookId, guardLoginId);
                    }

                }
                //logBookId entry for radio checklist-start

                /* Previous days push message show only for petrol cars*/
                /*Citywatch M1 - Romeo Patrol Cars*/
                /* get previous day push messages Start */
                if (isNewLogBook)
                {


                    if (IsLoginInKeyVehOrLogBook)
                    {
                        /*check if id is Citywatch M1 - Romeo Patrol Cars site id=625*/
                        if (GuardLogin.ClientSite.Id == 625)
                        {
                            var previousPuShMessages = _clientDataProvider.GetPushMessagesNotAcknowledged(GuardLogin.ClientSite.Id, DateTime.Today.AddDays(-1));
                            if (previousPuShMessages != null)
                            {
                                _guardLogDataProvider.CopyPreviousDaysPushMessageToLogBook(previousPuShMessages, logBookId, guardLoginId, guardLog);
                            }
                        }
                        /* Check is duress enabled for this site*/
                        var isDurssEnabled = _viewDataService.IsClientSiteDuressEnabled(GuardLogin.ClientSite.Id);
                        if (isDurssEnabled)
                        {
                            /* Copy Previous duress message not deactivated (Repete in each logbook untill deactivated )*/
                            var previousDuressMessages = _clientDataProvider.GetDuressMessageNotAcknowledged(GuardLogin.ClientSite.Id, DateTime.Today.AddDays(-1));
                            if (previousDuressMessages != null)
                            {
                                _guardLogDataProvider.CopyPreviousDaysDuressToLogBook(previousDuressMessages, logBookId, guardLoginId, guardLog);
                            }
                            var clientSiteForLogbook = _clientDataProvider.GetClientSiteForRcLogBook();
                            if (clientSiteForLogbook.Count != 0)
                            {
                                if (clientSiteForLogbook.FirstOrDefault().Id == GuardLogin.ClientSite.Id)
                                {
                                    var previousDuressMessagesForControlRoom = _clientDataProvider.GetDuressMessageNotAcknowledgedForControlRoom(DateTime.Today.AddDays(-1));
                                    if (previousDuressMessagesForControlRoom != null)
                                    {
                                        foreach (var items in previousDuressMessagesForControlRoom)
                                        {
                                            _guardLogDataProvider.LogBookEntryForRcControlRoomMessages(GuardLogin.Guard.Id, GuardLogin.Guard.Id, null, "Duress Alarm Activated", IrEntryType.Alarm, 1, 0, guardLog);
                                        }
                                        //_guardLogDataProvider.CopyPreviousDaysDuressToLogBook(previousDuressMessagesForControlRoom, logBookId, guardLoginId);
                                    }

                                }

                            }
                        }

                    }
                }
                /* get previous day push messages end */

                var gaurdlogin = _clientDataProvider.GetGuardLogin(guardLoginId, logBookId);
                if (gaurdlogin.Count != 0)
                {
                    foreach (var item in gaurdlogin)
                    {
                        var logbookcl = new GuardLogin();

                        logbookcl.Id = item.Id;
                        logbookcl.ClientSiteId = item.ClientSiteLogBook.ClientSiteId;
                        logbookcl.GuardId = item.GuardId;


                        //signInEntry.GuardLogin = logbookcl;
                        var radioChecklist = _clientDataProvider.GetClientSiteRadioChecksActivityStatus(logbookcl.GuardId, logbookcl.ClientSiteId);
                        if (radioChecklist.Count == 0)
                        {
                            /*remove NotificationType=1 no guard on duty (NotificationType=1)*/
                            _guardLogDataProvider.RemoveTheeRadioChecksActivityWithNotifcationtypeOne(logbookcl.ClientSiteId);

                            var clientsiteRadioCheck = new ClientSiteRadioChecksActivityStatus()
                            {
                                ClientSiteId = logbookcl.ClientSiteId,
                                GuardId = logbookcl.GuardId,
                                GuardLoginTime = DateTime.Now,
                                /* New Field Added for Logoff the Gurad after OffDuty in Rc*/
                                OnDuty = GuardLogin.OnDuty,
                                OffDuty = GuardLogin.OffDuty,
                                GuardLoginTimeLocal = guardLog.EventDateTimeLocal,
                                GuardLoginTimeLocalWithOffset = guardLog.EventDateTimeLocalWithOffset,
                                GuardLoginTimeZone = guardLog.EventDateTimeZone,
                                GuardLoginTimeZoneShort = guardLog.EventDateTimeZoneShort,
                                GuardLoginTimeUtcOffsetMinute = guardLog.EventDateTimeUtcOffsetMinute
                            };
                            _guardLogDataProvider.SaveRadioChecklistEntry(clientsiteRadioCheck);
                        }
                        else
                        {
                            /* Check if the NotificationType == 1 the remove  notification and insert the orginal login*/
                            var select = radioChecklist.Where(x => x.NotificationType == 1).ToList();
                            if (select.Count != 0)
                            {
                                /*remove NotificationType=1*/
                                _guardLogDataProvider.RemoveTheeRadioChecksActivityWithNotifcationtypeOne(logbookcl.ClientSiteId);
                                var radioChecklistNew = _clientDataProvider.GetClientSiteRadioChecksActivityStatus(logbookcl.GuardId, logbookcl.ClientSiteId);
                                if (radioChecklistNew.Count == 0)
                                {
                                    var clientsiteRadioCheck = new ClientSiteRadioChecksActivityStatus()
                                    {
                                        ClientSiteId = logbookcl.ClientSiteId,
                                        GuardId = logbookcl.GuardId,
                                        GuardLoginTime = DateTime.Now,
                                        /* New Field Added for Logoff the Gurad after OffDuty in Rc*/
                                        OnDuty = GuardLogin.OnDuty,
                                        OffDuty = GuardLogin.OffDuty,
                                        GuardLoginTimeLocal = guardLog.EventDateTimeLocal,
                                        GuardLoginTimeLocalWithOffset = guardLog.EventDateTimeLocalWithOffset,
                                        GuardLoginTimeZone = guardLog.EventDateTimeZone,
                                        GuardLoginTimeZoneShort = guardLog.EventDateTimeZoneShort,
                                        GuardLoginTimeUtcOffsetMinute = guardLog.EventDateTimeUtcOffsetMinute

                                    };
                                    _guardLogDataProvider.SaveRadioChecklistEntry(clientsiteRadioCheck);
                                }
                            }
                        }


                    }


                }
                //logBookId entry for radio checklist-end

                HttpContext.Session.SetInt32("LogBookId", logBookId);
                HttpContext.Session.SetInt32("GuardLoginId", guardLoginId);

                success = true;
                }
                else
                {
                    success = false;
                    message = "Not authorized to access this page";
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                if (ex.InnerException != null &&
                    ex.InnerException is SqlException &&
                    ex.InnerException.Message.StartsWith("Violation of UNIQUE KEY constraint"))
                {
                    message = "Error: A guard with same name or security number already exists";
                }
            }

            return new JsonResult(new
            {
                success,
                message,
                LogBookType,
                initalsChangedMessage = (GuardLogin.IsNewGuard && initalsUsed != guardInitals) ? $"Guard with initials '{guardInitals}' already exists. So initals changed to '{initalsUsed}'" : string.Empty
            });
        }

        public JsonResult OnGetGuardDetails(string securityNumber)
        {
            var guard = _guardDataProvider.GetGuards().SingleOrDefault(z => string.Compare(z.SecurityNo, securityNumber, StringComparison.OrdinalIgnoreCase) == 0);
            GuardLogin lastLogin = null;
            if (guard != null)
            {

                if (AuthUserHelper.LoggedInUserId != null)
                {
                    lastLogin = _guardDataProvider.GetGuardLastLogin(guard.Id, AuthUserHelper.LoggedInUserId);
                }
                else
                {
                    lastLogin = _guardDataProvider.GetGuardLastLogin(guard.Id);
                }
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

                if (guard.IsAdminThirdPartyAccess)
                {
                    AuthUserHelper.IsAdminThirdParty = true;
                }
                else
                {
                    AuthUserHelper.IsAdminThirdParty = false;
                }


                if(guard.IsAdminInvestigatorAccess)
                {
                    AuthUserHelper.IsAdminInvestigator = true;
                }
                else
                {
                    AuthUserHelper.IsAdminInvestigator = false;
                }
                if (guard.IsAdminAuditorAccess)
                {
                    AuthUserHelper.IsAdminAuditor = true;
                }
                else
                {
                    AuthUserHelper.IsAdminAuditor = false;
                }

            }
            //HRList Status start 
            var HR1 = "Grey";
            var HR2 = "Grey";
            var HR3 = "Grey";
            bool guardLockStatusBasedOnRedDoc = false;
            if (guard != null)
            {
                var hrGroupStatusesNew = LEDStatusForLoginUser(guard.Id);
                if (hrGroupStatusesNew != null && hrGroupStatusesNew.Count > 0)
                {
                    if (hrGroupStatusesNew != null || hrGroupStatusesNew.Count != 0)
                    {


                        // Group document statuses by GroupName for faster lookups
                        var statusLookup = hrGroupStatusesNew.ToLookup(x => x.GroupName.Trim());

                        // Set HR1Status
                        var HR1List = statusLookup["HR 1 (C4i)"];
                        if (HR1List.Any())
                        {
                            HR1 = HR1List.Any(x => x.ColourCodeStatus == "Red") ? "Red" :
                                              HR1List.Any(x => x.ColourCodeStatus == "Yellow") ? "Yellow" :
                                              "Green";
                        }

                        // Set HR2Status
                        var HR2List = statusLookup["HR 2 (Client)"];
                        if (HR2List.Any())
                        {
                            HR2 = HR2List.Any(x => x.ColourCodeStatus == "Red") ? "Red" :
                                              HR2List.Any(x => x.ColourCodeStatus == "Yellow") ? "Yellow" :
                                              "Green";
                        }

                        // Set HR3Status
                        var HR3List = statusLookup["HR 3 (Special)"];
                        if (HR3List.Any())
                        {
                            HR3 = HR3List.Any(x => x.ColourCodeStatus == "Red") ? "Red" :
                                              HR3List.Any(x => x.ColourCodeStatus == "Yellow") ? "Yellow" :
                                              "Green";
                        }


                        //if (HR1 == "Red" || HR2 == "Red" || HR3 == "Red")
                        //{
                        //    // Lookup to group items by their ColourCodeStatus
                        //    var statusLookupColourCode = hrGroupStatusesNew.ToLookup(x => x.ColourCodeStatus.Trim());

                        //    // Get the 'Red' status group from the lookup
                        //    var redStatuses = statusLookupColourCode["Red"];

                        //    // Fetch the HR settings list with the HR Lock enabled
                        //    var enabledHrSettingsList = _configDataProvider.GetHRSettingsWithHRLockEnable();

                        //    if (enabledHrSettingsList.Count != 0)
                        //    {
                        //        foreach (var document in enabledHrSettingsList)
                        //        {
                        //            foreach (var redStatus in redStatuses)
                        //            {
                        //                var redDescriptionParts = redStatus.documentDescription.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);

                        //                if (redDescriptionParts.Length == 2)
                        //                {
                        //                    var prefix = redDescriptionParts[0];    // "02c"
                        //                    var description = redDescriptionParts[1]; // "VISY - Recycling - QLD - State Specific Induction (600-003)"

                        //                    // Now you can compare or perform operations with prefix and description
                        //                    if (document.Description == description)
                        //                    {
                        //                        guardLockStatusBasedOnRedDoc = true;
                        //                    }
                        //                }
                        //            }
                        //        }
                        //    }
                        //}



                    }
                }

                

            }

           
            return new JsonResult(new { guard, lastLogin, HR1, HR2, HR3, guardLockStatusBasedOnRedDoc });
        }
        private List<HRGroupStatusNew> LEDStatusForLoginUser(int GuardID)
        {
            // Retrieve guard document details in one call
            var guardDocumentDetails = _guardDataProvider.GetGuardLicensesandcompliance(GuardID);
            var hrGroupStatusesNew = new List<HRGroupStatusNew>();

            // Iterate through each document detail
            foreach (var item in guardDocumentDetails)
            {
                // Directly use the item without filtering again
                hrGroupStatusesNew.Add(new HRGroupStatusNew
                {
                    documentDescription=item.Description,
                    Status = 1,
                    GroupName = item.HrGroupText.Trim(), // Assuming HrGroupText replaces GroupName
                                                         // Generate the color code based on the current item
                    ColourCodeStatus = GuardledColourCodeGenerator(new List<GuardComplianceAndLicense> { item })
                });
            }

            return hrGroupStatusesNew;
        }
        private string GuardledColourCodeGenerator(List<GuardComplianceAndLicense> selectedList)
        {
            var today = DateTime.Now;
            var colourCode = "Green"; // Default to green

            if (selectedList.Count > 0)
            {
                // Check if any entry has DateType == true
                var hasDateTypeTrue = selectedList.Any(x => x.DateType == true);

                if (hasDateTypeTrue)
                {
                    return "Green"; // Return immediately if DateType == true exists
                }

                // Get the first non-null expiry date (if any)
                var firstItem = selectedList.FirstOrDefault(x => x.ExpiryDate != null);

                if (firstItem != null)
                {
                    var expiryDate = firstItem.ExpiryDate.Value; // Assuming ExpiryDate is not null here

                    // Compare expiry date with today's date
                    if (expiryDate < today)
                    {
                        return "Red";
                    }
                    else if ((expiryDate - today).Days < 45)
                    {
                        return "Yellow";
                    }
                }
            }

            return colourCode; // Default return is green
        }
        public class HRGroupStatusNew
        {

            public int Status { get; set; }

            public string GroupName { get; set; }
            public string ColourCodeStatus { get; set; }

            public string documentDescription { get; set; }
        }
        public JsonResult OnGetSmartWands(string siteName, int? guardId)
        {
            return new JsonResult(_viewDataService.GetSmartWands(siteName, guardId));
        }

        public JsonResult OnGetOfficerPositions()
        {
            var officerPositionsWithoutSelect = _viewDataService.GetOfficerPositions(OfficerPositionFilter.NonPatrolOnly).Where(z => z.Value != string.Empty);
            return new JsonResult(officerPositionsWithoutSelect);
        }


        public JsonResult OnGetManningDeatilsForTheSite(string siteName)
        {
            if (string.IsNullOrWhiteSpace(siteName))
            {
                return new JsonResult(new { success = false, positionIdDefault = 0 });
            }

            var positionIdDefault = _clientDataProvider.GetContractedManningDetailsForSpecificSite(siteName);
            var success = positionIdDefault !=string.Empty;

            return new JsonResult(new { success, positionIdDefault });
        }


        public JsonResult OnGetCheckIsWandAvailable(string clientSiteName, string smartWandNo, int? guardId)
        {
            var smartWand = _clientSiteWandDataProvider.GetClientSiteSmartWands().SingleOrDefault(z => z.ClientSite.Name == clientSiteName & z.SmartWandId == smartWandNo);
            return new JsonResult(_viewDataService.CheckWandIsInUse(smartWand.Id, guardId));
        }

        public JsonResult OnGetCheckIfSmartwandMsgBypass(string clientSiteName, string positionName, int? guardId)
        {
            var position = _configDataProvider.GetPositions().SingleOrDefault(x => x.Name == positionName);
            bool isSmartwandbypass = position?.IsSmartwandbypass ?? false; // Use false if null
            return new JsonResult(isSmartwandbypass);
        }

        //public JsonResult OnGetIsGuardLoginActive(string guardLicNo,string clientSiteName)
        //{

        //    var thresholdDate = DateTime.Now.AddDays(-120);
        //    var success = false;
        //    var hrdocLockforThisGurad = false;
        //    var initalsUsed = string.Empty;
        //    var strResult = string.Empty;
        //    if (!string.IsNullOrEmpty(guardLicNo))
        //    {
        //        var guard = _guardDataProvider.GetGuardDetailsbySecurityLicenseNo(guardLicNo);
        //        if (guard != null)
        //        {
        //            var lastLogin = _guardDataProvider.GetGuardLastLogin(guard.Id);
        //            if (guard.IsActive)
        //            {
        //                if (lastLogin != null)
        //                {

        //                    if (lastLogin.LoginDate < thresholdDate && lastLogin.Guard.IsActive)
        //                    {
        //                        if (guard.IsReActive)
        //                        {
        //                            lastLogin.Guard.IsReActive = false;
        //                            _guardDataProvider.UpdateGuard(lastLogin.Guard, lastLogin.ClientSite.State, out initalsUsed);
        //                            success = false;
        //                        }
        //                        else
        //                        {
        //                            lastLogin.Guard.IsActive = false;
        //                            _guardDataProvider.UpdateGuard(lastLogin.Guard, lastLogin.ClientSite.State, out initalsUsed);
        //                            strResult = "You have'nt logged in for a while. Contact your administrator!.";
        //                            success = true;
        //                        }
        //                    }
        //                    else
        //                    {

        //                        success = false;

        //                    }

        //                }
        //                else
        //                { success = false; }
        //            }
        //            else
        //            {
        //                strResult = "You have'nt logged in for a while. Contact your administrator!.";
        //                success = true;
        //            }
        //          hrdocLockforThisGurad= CheckIfGuardNeedToBlockForHRDocumnetLock(guard.Id, clientSiteName);
        //        }
        //    }


        //    return new JsonResult(new { success, strResult , hrdocLockforThisGurad });



        //}

        public JsonResult OnGetIsGuardLoginActive(string guardLicNo, string clientSiteName)
        {

            var thresholdDate = DateTime.Now.AddDays(-60);
            var success = false;
            var hrdocLockforThisGurad = false;
            var initalsUsed = string.Empty;
            var strResult = string.Empty;
           
            if (!string.IsNullOrEmpty(guardLicNo))
            {
                var guard = _guardDataProvider.GetGuardDetailsbySecurityLicenseNo(guardLicNo);
                if (guard != null)
                {
                    var lastLogin = _guardDataProvider.GetGuardLastLogin(guard.Id);

                    if (lastLogin != null)
                    {
                        DateTime date1 = lastLogin.LoginDate; // Example: December 1, 2024
                        DateTime date2 = DateTime.Now;             // Current date

                        // Calculate the difference
                        TimeSpan difference = date2 - date1;

                        // Get the number of days
                        int daysBetween = Math.Abs(difference.Days);
                        if (daysBetween < 60 )
                            {
                            success = false;
                        }
                            else
                            {
                            strResult = "You have'nt logged in for a while. Please wait for 5 minutes as we re-activate your profile.";
                            success = true;
                            var emailBody = GiveGuardLoginEmailNotification(guard.Name, guard.SecurityNo,clientSiteName,guard.Provider, daysBetween);
                            SendEmailNew(emailBody, daysBetween);
                        }

                    }
                    else
                    { success = false; }
                    
                }
            }


            return new JsonResult(new { success, strResult, hrdocLockforThisGurad });



        }
        public string GiveGuardLoginEmailNotification(string guardname, string licenseNo,string clientSite,string Provider,int daysbetween)
        {
            var sb = new StringBuilder();

            var messageBody = string.Empty;
            messageBody = $" <tr><td style=\"width:2% ;border: 1px solid #000000;\"><b>Name of Guard</b></td><td style=\"width:5% ;border: 1px solid #000000;\">{guardname}</td>";
            messageBody= messageBody + $" <tr><td style=\"width:2% ;border: 1px solid #000000;\"><b>License</b></td><td style=\"width:5% ;border: 1px solid #000000;\">{licenseNo}</td>";
            messageBody = messageBody + $" <tr><td style=\"width:2% ;border: 1px solid #000000;\"><b>Site</b></td><td style=\"width:5% ;border: 1px solid #000000;\">{clientSite}</td>";
            messageBody = messageBody + $" <tr><td style=\"width:2% ;border: 1px solid #000000;\"><b>Provider</b></td><td style=\"width:5% ;border: 1px solid #000000;\">{Provider}</td>";

            sb.Append("Hi , <br/><br/>Following guard is trying to login after "+ daysbetween + " days. <br/><br/>");
            sb.Append(" <table width=\"50%\" cellpadding=\"5\" cellspacing=\"5\" border=\"1\" style=\"border:ridge;border-color:#000000;border-width:thin\">");
            sb.Append(" <tr><td style=\"width:2% ;border: 1px solid #000000;text-align:center \" colspan=\"2\"><b>Guard Details</b></td></tr>");
            sb.Append(messageBody);
            sb.Append("");
            
            
            //mailBodyHtml.Append("");
            return sb.ToString();
        }
        private void SendEmailNew(string mailBodyHtml,int daysbetween)
        {
            var fromAddress = _EmailOptions.FromAddress.Split('|');
            var Emails = _clientDataProvider.GetGlobalComplianceAlertEmail().ToList();
            var emailAddresses = string.Join(",", Emails.Select(email => email.Email));



            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromAddress[1], fromAddress[0]));
            if (emailAddresses != null && emailAddresses != "")
            {
                var toAddressNew = emailAddresses.Split(',');
                foreach (var address in GetToEmailAddressList(toAddressNew))
                    message.To.Add(address);
            }
            

            message.Subject = "Guard login after " + daysbetween +" days.";
            message.Bcc.Add(new MailboxAddress("globoconsoftware", "globoconsoftware@gmail.com"));
            var builder = new BodyBuilder()
            {
                HtmlBody = mailBodyHtml
            };
            message.Body = builder.ToMessageBody();
            using (var client = new SmtpClient())
            {
                client.Connect(_EmailOptions.SmtpServer, _EmailOptions.SmtpPort, MailKit.Security.SecureSocketOptions.None);
                if (!string.IsNullOrEmpty(_EmailOptions.SmtpUserName) &&
                    !string.IsNullOrEmpty(_EmailOptions.SmtpPassword))
                    client.Authenticate(_EmailOptions.SmtpUserName, _EmailOptions.SmtpPassword);
                client.Send(message);
                client.Disconnect(true);
            }

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
        public bool CheckIfGuardNeedToBlockForHRDocumnetLock(int guardId,string ClientSiteName)
        {
            var HR1 = "Grey";
            var HR2 = "Grey";
            var HR3 = "Grey";
            bool guardLockStatusBasedOnRedDoc = false;

            var ClientSiteID = _guardDataProvider.GetClientSiteID(ClientSiteName);
            if (ClientSiteID != null)
            {
                var hrdoumnetWithLockfortheSite = _guardDataProvider.GetHrDocumentLockDetailsForASite(ClientSiteID.Id);
                // check if lock exist for the curresponding site 
                if (hrdoumnetWithLockfortheSite.Count != 0)
                {
                    //HRList Status start 

                    if (guardId != 0)
                    {
                        var hrGroupStatusesNew = LEDStatusForLoginUser(guardId);
                        if (hrGroupStatusesNew != null && hrGroupStatusesNew.Count > 0)
                        {
                            if (hrGroupStatusesNew != null || hrGroupStatusesNew.Count != 0)
                            {


                                // Group document statuses by GroupName for faster lookups
                                var statusLookup = hrGroupStatusesNew.ToLookup(x => x.GroupName.Trim());

                                // Set HR1Status
                                var HR1List = statusLookup["HR 1 (C4i)"];
                                if (HR1List.Any())
                                {
                                    HR1 = HR1List.Any(x => x.ColourCodeStatus == "Red") ? "Red" :
                                                      HR1List.Any(x => x.ColourCodeStatus == "Yellow") ? "Yellow" :
                                                      "Green";
                                }

                                // Set HR2Status
                                var HR2List = statusLookup["HR 2 (Client)"];
                                if (HR2List.Any())
                                {
                                    HR2 = HR2List.Any(x => x.ColourCodeStatus == "Red") ? "Red" :
                                                      HR2List.Any(x => x.ColourCodeStatus == "Yellow") ? "Yellow" :
                                                      "Green";
                                }

                                // Set HR3Status
                                var HR3List = statusLookup["HR 3 (Special)"];
                                if (HR3List.Any())
                                {
                                    HR3 = HR3List.Any(x => x.ColourCodeStatus == "Red") ? "Red" :
                                                      HR3List.Any(x => x.ColourCodeStatus == "Yellow") ? "Yellow" :
                                                      "Green";
                                }


                                if (HR1 == "Red" || HR2 == "Red" || HR3 == "Red")
                                {
                                    // Lookup to group items by their ColourCodeStatus
                                    var statusLookupColourCode = hrGroupStatusesNew.ToLookup(x => x.ColourCodeStatus.Trim());

                                    // Get the 'Red' status group from the lookup
                                    var redStatuses = statusLookupColourCode["Red"];

                                    // Fetch the HR settings list with the HR Lock enabled
                                    var enabledHrSettingsList = hrdoumnetWithLockfortheSite;

                                    if (enabledHrSettingsList.Count != 0)
                                    {
                                        foreach (var document in enabledHrSettingsList)
                                        {
                                            foreach (var redStatus in redStatuses)
                                            {
                                                var redDescriptionParts = redStatus.documentDescription.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);

                                                if (redDescriptionParts.Length == 2)
                                                {
                                                    var prefix = redDescriptionParts[0];    // "02c"
                                                    var description = redDescriptionParts[1]; // "VISY - Recycling - QLD - State Specific Induction (600-003)"

                                                    // Now you can compare or perform operations with prefix and description
                                                    if (document.HrSettings.Description == description)
                                                    {
                                                        guardLockStatusBasedOnRedDoc = true;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                              
                                var enabledHrSettingsList2 = hrdoumnetWithLockfortheSite;
                                if (!guardLockStatusBasedOnRedDoc && enabledHrSettingsList2.Count > 0)
                                {
                                    guardLockStatusBasedOnRedDoc = !enabledHrSettingsList2.All(document =>
                                        hrGroupStatusesNew.Any(redStatus =>
                                        {
                                            var redDescriptionParts = redStatus.documentDescription
                                                                               .Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);

                                            return redDescriptionParts.Length == 2 &&
                                                   redDescriptionParts[1] == document.HrSettings.Description;
                                        }));

                                   
                                }

                               
                                //if(!guardLockStatusBasedOnRedDoc)
                                //{
                                //  var  guardLockStatusBasedOnAvaliableDoc2 = false;
                                //    var enabledHrSettingsList = hrdoumnetWithLockfortheSite;
                                //    if (enabledHrSettingsList.Count != 0)
                                //    {
                                //        foreach (var document in enabledHrSettingsList)
                                //        {

                                //            foreach (var redStatus in hrGroupStatusesNew)
                                //            {
                                //                var redDescriptionParts = redStatus.documentDescription.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);

                                //                if (redDescriptionParts.Length == 2)
                                //                {
                                //                    var prefix = redDescriptionParts[0];    // "02c"
                                //                    var description = redDescriptionParts[1]; // "VISY - Recycling - QLD - State Specific Induction (600-003)"

                                //                    // Now you can compare or perform operations with prefix and description
                                //                    if (document.HrSettings.Description == description)
                                //                    {
                                //                        guardLockStatusBasedOnAvaliableDoc2 = true;
                                //                    }
                                //                }
                                //            }

                                //        }

                                //        if(!guardLockStatusBasedOnAvaliableDoc2)
                                //        {
                                //            guardLockStatusBasedOnRedDoc = true;
                                //        }

                                //    }
                                //}

                            }
                        }




                    }
                }


            }
            return guardLockStatusBasedOnRedDoc;

        }


        private int GetGuardLoginId(int logBookId)
        {
            GuardLogin guardLogin = new GuardLogin();

            //GuardLogin existingGuardLogin = _guardDataProvider.GetGuardLoginsByLogBookId(logBookId).SingleOrDefault(x => x.GuardId == GuardLogin.Guard.Id && x.OnDuty == GuardLogin.OnDuty);


            GuardLogin existingGuardLogin = new GuardLogin();
            //GuardLogin existingGuardLogin = _guardDataProvider.GetGuardLoginsByLogBookId(logBookId).SingleOrDefault(x => x.GuardId == GuardLogin.Guard.Id && x.OnDuty == GuardLogin.OnDuty);
            var GuardLoginList = _guardDataProvider.GetGuardLoginsByLogBookId(logBookId).ToList();
            if (GuardLoginList != null)
            {
                existingGuardLogin = GuardLoginList.FirstOrDefault(x => x.GuardId == GuardLogin.Guard.Id && x.OnDuty == GuardLogin.OnDuty);
            }

            if (existingGuardLogin != null)
            {
                if (existingGuardLogin.ClientSiteLogBookId != 0)
                {
                    guardLogin = existingGuardLogin;
                    guardLogin.ClientSiteId = GuardLogin.ClientSite.Id;
                    guardLogin.PositionId = GuardLogin.IsPosition ? GuardLogin.SmartWandOrPositionId : null;
                    guardLogin.SmartWandId = !GuardLogin.IsPosition ? GuardLogin.SmartWandOrPositionId : null;
                    guardLogin.OnDuty = GuardLogin.OnDuty;
                    guardLogin.OffDuty = GuardLogin.OffDuty;
                    guardLogin.UserId = AuthUserHelper.LoggedInUserId.GetValueOrDefault();
                    guardLogin.IPAddress= Request.HttpContext.Connection.RemoteIpAddress.ToString();
                }
            }
            else
            {
                guardLogin = GetNewGuardLogin();
            }

            guardLogin.ClientSiteLogBookId = logBookId;
            guardLogin.IPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString();

            var guardLoginId = _guardDataProvider.SaveGuardLogin(guardLogin);

            return guardLoginId;
        }

        private int GetLogBookId(out bool isNewLogBook, DateTime clientDateTime)
        {
            isNewLogBook = false;
            int logBookId;
            //var logBook = _clientDataProvider.GetClientSiteLogBook(GuardLogin.ClientSite.Id, LogBookType, DateTime.Today);
            var logBook = _clientDataProvider.GetClientSiteLogBook(GuardLogin.ClientSite.Id, LogBookType, clientDateTime.Date); // p6#73 timezone bug - Added by binoy 24-01-2024
            if (logBook == null)
            {
                logBookId = _clientDataProvider.SaveClientSiteLogBook(new ClientSiteLogBook()
                {
                    ClientSiteId = GuardLogin.ClientSite.Id,
                    Type = LogBookType,
                    Date = clientDateTime.Date,
                });  // changed DateTime.Today to clientDateTime.Date p6#73 timezone bug modified by binoy 24-01-2024

                isNewLogBook = true;
            }
            else
            {
                logBookId = logBook.Id;
            }

            return logBookId;
        }

        private bool CheckIfAlreadyLoginInKeyVehicleLogBook(DateTime clientDateTime)
        {
            /* It will check if there is any entry in logbook or keyvehicle not considering the LogBookType
             * otherwise it will enter multiple duress 
             */
            var isNewLogBook = false;
            var logBook = _clientDataProvider.GetClientSiteLogBookWithOutType(GuardLogin.ClientSite.Id, clientDateTime.Date); // p6#73 timezone bug - Added by binoy 24-01-2024
            if (logBook.Count == 0)
            {


                isNewLogBook = true;
            }

            return isNewLogBook;


        }

        private GuardLogin GetNewGuardLogin()
        {
            return new GuardLogin()
            {
                LoginDate = DateTime.Now,
                GuardId = GuardLogin.Guard.Id,
                ClientSiteId = GuardLogin.ClientSite.Id,
                PositionId = GuardLogin.IsPosition ? GuardLogin.SmartWandOrPositionId : null,
                SmartWandId = !GuardLogin.IsPosition ? GuardLogin.SmartWandOrPositionId : null,
                OnDuty = GuardLogin.OnDuty,
                OffDuty = GuardLogin.OffDuty,
                UserId = AuthUserHelper.LoggedInUserId.GetValueOrDefault()
            };
        }

        private int? GetSmartWandOrPositionId()
        {
            if (GuardLogin.IsPosition)
            {
                var position = _configDataProvider.GetPositions().SingleOrDefault(z => z.Name == GuardLogin.SmartWandOrPosition);
                if (position != null)
                    return position.Id;
            }
            else
            {
                var smartWand = _clientSiteWandDataProvider.GetClientSiteSmartWands().Where(x => x.ClientSite.Name == GuardLogin.ClientSiteName).SingleOrDefault(z => z.SmartWandId == GuardLogin.SmartWandOrPosition);
                if (smartWand != null)
                    return smartWand.Id;
            }

            return null;
        }

        private void CreateKeyVehicleLoggedInEntry(int logBookId, int guardLoginId, DateTime? eventDateTimeLocal,
                       DateTimeOffset? eventDateTimeLocalWithOffset, string eventDateTimeZone,
                       string eventDateTimeZoneShort, int? eventDateTimeUtcOffsetMinute, string GPSCoordinates)
        {
            var signInEntry = new GuardLog()
            {
                ClientSiteLogBookId = logBookId,
                GuardLoginId = guardLoginId,
                EventDateTime = DateTime.Now,
                Notes = "KV Logged In",
                IsSystemEntry = true,
                EventDateTimeLocal = eventDateTimeLocal, // Task p6#73_TimeZone issue -- added by Binoy - Start
                EventDateTimeLocalWithOffset = eventDateTimeLocalWithOffset,
                EventDateTimeZone = eventDateTimeZone,
                EventDateTimeZoneShort = eventDateTimeZoneShort,
                EventDateTimeUtcOffsetMinute = eventDateTimeUtcOffsetMinute, // Task p6#73_TimeZone issue -- added by Binoy - End
                GpsCoordinates = GPSCoordinates
            };
            _guardLogDataProvider.SaveGuardLog(signInEntry);
        }

        private void CreateLogbookLoggedInEntry(int logBookId, int guardLoginId, DateTime? eventDateTimeLocal,
                   DateTimeOffset? eventDateTimeLocalWithOffset, string eventDateTimeZone,
                   string eventDateTimeZoneShort, int? eventDateTimeUtcOffsetMinute, string GPSCoordinates)
        {
            var signInEntry = new GuardLog()
            {
                ClientSiteLogBookId = logBookId,
                GuardLoginId = guardLoginId,
                EventDateTime = DateTime.Now,
                Notes = "Logbook Logged In",
                IsSystemEntry = true,
                EventDateTimeLocal = eventDateTimeLocal, // Task p6#73_TimeZone issue -- added by Binoy - Start
                EventDateTimeLocalWithOffset = eventDateTimeLocalWithOffset,
                EventDateTimeZone = eventDateTimeZone,
                EventDateTimeZoneShort = eventDateTimeZoneShort,
                EventDateTimeUtcOffsetMinute = eventDateTimeUtcOffsetMinute,// Task p6#73_TimeZone issue -- added by Binoy - End
                GpsCoordinates = GPSCoordinates
            };
            _guardLogDataProvider.SaveGuardLog(signInEntry);


            ////logBookId entry for radio checklist-start

            //var gaurdlogin = _clientDataProvider.GetGuardLogin(guardLoginId,logBookId);
            //if (gaurdlogin.Count != 0)
            //{
            //    foreach (var item in gaurdlogin)
            //    {
            //        var logbookcl = new GuardLogin();

            //        logbookcl.Id = item.Id;
            //        logbookcl.ClientSiteId = item.ClientSiteLogBook.ClientSiteId;
            //        logbookcl.GuardId = item.GuardId;


            //        signInEntry.GuardLogin = logbookcl;
            //    }

            //    var radioChecklist = _clientDataProvider.GetClientSiteRadioChecksActivityStatus(signInEntry.GuardLogin.GuardId, signInEntry.GuardLogin.ClientSiteId);
            //    if (radioChecklist.Count == 0)
            //    {
            //        var clientsiteRadioCheck = new ClientSiteRadioChecksActivityStatus()
            //        {
            //            ClientSiteId = signInEntry.GuardLogin.ClientSiteId,
            //            GuardId = signInEntry.GuardLogin.GuardId,
            //            GuardLoginTime = DateTime.Now,

            //        };
            //        _guardLogDataProvider.SaveRadioChecklistEntry(clientsiteRadioCheck);
            //    }
            //}
            ////logBookId entry for radio checklist-end
        }


        public JsonResult OnGetCriticalDocumentsList(string ClientSiteName)
        {
            var ClientSiteID = _guardDataProvider.GetClientSiteID(ClientSiteName);
            return new JsonResult(_guardDataProvider.GetCriticalDocs(ClientSiteID.Id));

        }


     
    }
}
