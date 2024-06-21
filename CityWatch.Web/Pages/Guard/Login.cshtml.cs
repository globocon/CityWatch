using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Helpers;
using CityWatch.Web.Models;
using CityWatch.Web.Services;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
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

        public LoginModel(IViewDataService viewDataService,
            IClientDataProvider clientDataProvider,
            IGuardDataProvider guardDataProvider,
            IGuardLogDataProvider guardLogDataProvider,
            IClientSiteWandDataProvider clientSiteWandDataProvider,
            IConfigDataProvider configDataProvider)
        {
            _viewDataService = viewDataService;
            _clientDataProvider = clientDataProvider;
            _guardDataProvider = guardDataProvider;
            _guardLogDataProvider = guardLogDataProvider;
            _clientSiteWandDataProvider = clientSiteWandDataProvider;
            _configDataProvider = configDataProvider;
        }

        [BindProperty]
        public GuardLoginViewModel GuardLogin { get; set; }

        [BindProperty]
        public LogBookType LogBookType { get; set; }

        public IViewDataService ViewDataService { get { return _viewDataService; } }

        public void OnGet(string t)
        {
            LogBookType = t switch
            {
                "gl" => LogBookType.DailyGuardLog,
                "vl" => LogBookType.VehicleAndKeyLog,
                _ => throw new ArgumentOutOfRangeException("Failed to identify log book type"),
            };
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
                    GpsCoordinates = GPSCoordinates
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
            }

            return new JsonResult(new { guard, lastLogin });
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

        public JsonResult OnGetCheckIsWandAvailable(string clientSiteName, string smartWandNo, int? guardId)
        {
            var smartWand = _clientSiteWandDataProvider.GetClientSiteSmartWands().SingleOrDefault(z => z.ClientSite.Name == clientSiteName & z.SmartWandId == smartWandNo);
            return new JsonResult(_viewDataService.CheckWandIsInUse(smartWand.Id, guardId));
        }

        public JsonResult OnGetIsGuardLoginActive(string guardLicNo)
        {

            var thresholdDate = DateTime.Now.AddDays(-120);
            var success = false;
            var initalsUsed = string.Empty;
            var strResult = string.Empty;
            if (!string.IsNullOrEmpty(guardLicNo))
            {
                var guard = _guardDataProvider.GetGuardDetailsbySecurityLicenseNo(guardLicNo);
                if (guard != null)
                {
                    var lastLogin = _guardDataProvider.GetGuardLastLogin(guard.Id);
                    if (guard.IsActive)
                    {
                        if (lastLogin != null)
                        {

                            if (lastLogin.LoginDate < thresholdDate && lastLogin.Guard.IsActive)
                            {
                                lastLogin.Guard.IsActive = false;
                                _guardDataProvider.UpdateGuard(lastLogin.Guard, lastLogin.ClientSite.State, out initalsUsed);
                                strResult = "You have'nt logged in for a while. Contact your administrator!.";
                                success = true;
                            }
                            else { success = false; }

                        }
                        else
                        { success = false; }
                    }
                    else
                    {
                        strResult = "You have'nt logged in for a while. Contact your administrator!.";
                        success = true;
                    }

                }
            }
            return new JsonResult(new { success, strResult });



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
                }
            }
            else
            {
                guardLogin = GetNewGuardLogin();
            }

            guardLogin.ClientSiteLogBookId = logBookId;

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
