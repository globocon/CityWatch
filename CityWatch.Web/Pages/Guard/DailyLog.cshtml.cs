using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Helpers;
using CityWatch.Web.Services;
using ConvertApiDotNet;
//using iText.Kernel.Geom;
using iText.Layout;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using IO = System.IO;

namespace CityWatch.Web.Pages.Guard
{
    public class DailyLogModel : PageModel
    {
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IViewDataService _viewDataService;
        private readonly IClientSiteWandDataProvider _clientSiteWandDataProvider;
        private readonly ILogger<DailyLogModel> _logger;
        private readonly EmailOptions _emailOptions;
        private readonly ISiteEventLogDataProvider _SiteEventLogDataProvider;
        private readonly ISmsSenderProvider _smsSenderProvider;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IUserDataProvider _userDataProvider;

        [BindProperty]
        public GuardLog GuardLog { get; set; }

        public IViewDataService ViewDataService { get { return _viewDataService; } }
        public IClientDataProvider ClientDataProvider { get { return _clientDataProvider; } }

        public DailyLogModel(IGuardDataProvider guardDataProvider,
            IGuardLogDataProvider guardLogDataProvider,
             IClientDataProvider clientDataProvider,
             IViewDataService viewDataService,
             IClientSiteWandDataProvider clientSiteWandDataProvider,
             IOptions<EmailOptions> emailOptions,
             ILogger<DailyLogModel> logger,
             ISiteEventLogDataProvider siteEventLogDataProvider,
             ISmsSenderProvider smsSenderProvider,
             IWebHostEnvironment webHostEnvironment,
             IUserDataProvider userDataProvider
             )
        {
            _guardDataProvider = guardDataProvider;
            _guardLogDataProvider = guardLogDataProvider;
            _clientDataProvider = clientDataProvider;
            _viewDataService = viewDataService;
            _clientSiteWandDataProvider = clientSiteWandDataProvider;
            _logger = logger;
            _emailOptions = emailOptions.Value;
            _SiteEventLogDataProvider = siteEventLogDataProvider;
            _smsSenderProvider = smsSenderProvider;
            _webHostEnvironment = webHostEnvironment;
            _userDataProvider = userDataProvider;
        }

        public void OnGet()
        {
            var logBookId = HttpContext.Session.GetInt32("LogBookId");
            if (logBookId == null)
                throw new InvalidOperationException("Session timeout due to user inactivity. Failed to get client site log book");

            var guardLoginId = HttpContext.Session.GetInt32("GuardLoginId");
            if (guardLoginId == null)
                throw new InvalidOperationException("Session timeout due to user inactivity. Failed to get guard details");

            ViewData["PreviousLogBookId"] = HttpContext.Session.GetInt32("PreviousLogBookId");
            ViewData["PreviousLogBookDateString"] = HttpContext.Session.GetString("PreviousLogBookDateString");

            //  var clientSiteLogBook = _clientDataProvider.GetClientSiteLogBooks().SingleOrDefault(z => z.Id == logBookId && z.Type == LogBookType.DailyGuardLog);
            var clientSiteLogBook = _clientDataProvider.GetClientSiteLogBooks(logBookId, LogBookType.DailyGuardLog).SingleOrDefault();
            var guardLogin = _guardDataProvider.GetGuardLoginById(guardLoginId.Value);
            GuardLog ??= new GuardLog()
            {
                ClientSiteLogBookId = logBookId.Value,
                ClientSiteLogBook = clientSiteLogBook,
                GuardLoginId = guardLoginId.Value,
                GuardLogin = guardLogin,
            };

            ViewData["IsDuressEnabled"] = _viewDataService.IsClientSiteDuressEnabled(clientSiteLogBook.ClientSiteId);
        }

        public JsonResult OnGetGuardLogs(int logBookId, DateTime? logBookDate)
        {
            DateTime? logBook_Date = null;
            if (logBookId > 0)
            {
                logBook_Date = _guardDataProvider.GetLogbookDateFromLogbook(logBookId);
            }
            if (logBook_Date != null)
            {
                logBookDate = logBook_Date;
            }
            else
            {
                logBookDate = DateTime.Today;
            }


            var guardLogs = _guardLogDataProvider.GetGuardLogswithKvLogData(logBookId, logBookDate ?? DateTime.Today)
                .OrderByDescending(z => z.Id)
                .ThenByDescending(z => z.EventDateTime);
            foreach (var guardlog in guardLogs)
            {
                var guardlogImages = _guardLogDataProvider.GetGuardLogDocumentImaes(guardlog.Id);
                guardlog.NotesNew = string.Empty;
                foreach (var guardLogImage in guardlogImages)
                {
                    if (guardLogImage.IsRearfile == true)
                    {
                        guardlog.Notes = guardlog.Notes + "</br>See attached file <a href =\"" + guardLogImage.ImagePath + "\" target=\"_blank\">" + Path.GetFileName(guardLogImage.ImagePath) + "</a>";
                        guardlog.NotesNew = guardlog.NotesNew + "</br>See attached file <a href =\"" + guardLogImage.ImagePath + "\" target=\"_blank\">" + Path.GetFileName(guardLogImage.ImagePath) + "</a>";
                    }
                    if (guardLogImage.IsTwentyfivePercentfile == true)
                    {
                        guardlog.Notes = guardlog.Notes + "</br> <a href =\"" + guardLogImage.ImagePath + " \" target=\"_blank\"><img src =\"" + guardLogImage.ImagePath + "\"height=\"200px\" width=\"200px\"/></a>";

                        guardlog.NotesNew = guardlog.NotesNew + "</br> <a href =\"" + guardLogImage.ImagePath + " \" target=\"_blank\"><img src =\"" + guardLogImage.ImagePath + "\"height=\"200px\" width=\"200px\"/></a>";
                    }
                }
            }
            return new JsonResult(guardLogs);
        }

        // Project 4 , Task 48, Audio notification, Added By Binoy -- Start
        public JsonResult OnPostGuardLogsUpdateNotificationSoundStatus(List<int> logBookId, bool isControlRoomLogBook)
        {
            foreach (var r in logBookId)
            {
                int id = Convert.ToInt32(r);
                _guardLogDataProvider.UpdateNotificationSoundPlayedStatusForGuardLogs(id, isControlRoomLogBook);
            }
            return new JsonResult(new { status = "Ok" });
        }

        public JsonResult OnGetGuardLogsAcknowledgedForControlroom()
        {
            List<int> audioToPlayId = new List<int> { };
            audioToPlayId = _guardLogDataProvider.GetGuardLogsNotAcknowledgedForNotificationSound();
            return new JsonResult(audioToPlayId);
        }

        // Project 4 , Task 48, Audio notification, Added By Binoy -- End

        public JsonResult OnPostSaveGuardLog()
        {
            //for solving the date format in azure  server 
            // passing the time part from jquery and create new date 
            if (GuardLog != null)
            {
                if (!string.IsNullOrEmpty(GuardLog.TimePartOnly))
                {

                    var lbdtm = GuardLog.ClientSiteLogBook.Date;

                    var dateParts = GuardLog.TimePartOnly.Split(":");
                    if (dateParts.Length >= 2)
                    {
                        DateTime desiredDateTime = new DateTime(
                        DateTime.Now.Year,
                        DateTime.Now.Month,
                        DateTime.Now.Day,
                        int.Parse(dateParts[0]),
                        int.Parse(dateParts[1]),
                        0);

                        GuardLog.EventDateTime = desiredDateTime;
                        var GPSCoordinates = GuardLog.GpsCoordinates;
                        ModelState.Remove("GuardLog.EventDateTime");

                    }

                }
            }

            if (!ModelState.IsValid)
            {
                return new JsonResult(new
                {
                    success = false,
                    errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new
                    {
                        PropertyName = x.Key,
                        ErrorList = x.Value.Errors.Select(y => y.ErrorMessage).ToArray()
                    })
                    .AsEnumerable()
                });
            }

            var success = true;
            var message = "Success";
            try
            {
                _guardLogDataProvider.SaveGuardLog(GuardLog);
                var guardlogins = _guardLogDataProvider.GetGuardLogins(Convert.ToInt32(GuardLog.GuardLoginId));
                foreach (var item in guardlogins)
                {

                    var ClientSiteRadioChecksActivityDetails = _guardLogDataProvider.GetClientSiteRadioChecksActivityDetails().Where(x => x.GuardId == item.GuardId && x.ClientSiteId == item.ClientSiteId && x.GuardLoginTime != null);
                    foreach (var ClientSiteRadioChecksActivity in ClientSiteRadioChecksActivityDetails)
                    {
                        ClientSiteRadioChecksActivity.NotificationCreatedTime = GuardLog.EventDateTime;
                        _guardLogDataProvider.UpdateRadioChecklistEntry(ClientSiteRadioChecksActivity);
                    }
                }
                //logBookId entry for radio checklist-start
                //if (GuardLog.Id == 0)
                //{
                //    GuardLog.Id = _clientDataProvider.GetGuardLogs(Convert.ToInt32(GuardLog.GuardLoginId), GuardLog.ClientSiteLogBookId).Max(x => x.Id);
                //}
                //var gaurdlogin = _clientDataProvider.GetGuardLogin(Convert.ToInt32(GuardLog.GuardLoginId), GuardLog.ClientSiteLogBookId);
                //if (gaurdlogin.Count != 0)
                //{
                //    foreach (var item in gaurdlogin)
                //    {
                //        var logbookcl = new GuardLogin();

                //        logbookcl.Id = item.Id;
                //        logbookcl.ClientSiteId = item.ClientSiteId;
                //        logbookcl.GuardId = item.GuardId;


                //        GuardLog.GuardLogin = logbookcl;
                //    }

                //    var clientsiteRadioCheck = new ClientSiteRadioChecksActivityStatus()
                //    {
                //        ClientSiteId = GuardLog.GuardLogin.ClientSiteId,
                //        GuardId = GuardLog.GuardLogin.GuardId,
                //        LastLBCreatedTime = DateTime.Now,
                //        LBId = GuardLog.Id,
                //        ActivityType = "LB"
                //    };
                //    _guardLogDataProvider.SaveRadioChecklistEntry(clientsiteRadioCheck);
                //}
                //logBookId entry for radio checklist-end
            }
            catch (Exception ex)
            {
                success = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { success, message });
        }

        public JsonResult OnPostDeleteGuardLog(int id)
        {
            var status = true;
            var message = "Success";
            try
            {
                //logBookId delete for radio checklist-start


                _guardLogDataProvider.DeleteClientSiteRadioCheckActivityStatusForLogBookEntry(id);
                //logBookId delete for radio checklist-end
                _guardLogDataProvider.DeleteGuardLog(id);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status, message });
        }

        public JsonResult OnPostUpdateOffDuty(int guardLoginId, int clientSiteLogBookId, GuardLog tmdata)
        {
            var status = true;
            var message = "Success";
            try
            {
                AuthUserHelper.IsAdminPowerUser = false;
                AuthUserHelper.IsAdminGlobal = false;
                var signOffEntry = new GuardLog()
                {
                    ClientSiteLogBookId = clientSiteLogBookId,
                    GuardLoginId = guardLoginId,
                    EventDateTime = DateTime.Now,
                    Notes = "Guard Off Duty (Logbook Signout)",
                    IsSystemEntry = true,
                    EventDateTimeLocal = tmdata.EventDateTimeLocal, // Task p6#73_TimeZone issue -- added by Binoy - Start
                    EventDateTimeLocalWithOffset = tmdata.EventDateTimeLocalWithOffset,
                    EventDateTimeZone = tmdata.EventDateTimeZone,
                    EventDateTimeZoneShort = tmdata.EventDateTimeZoneShort,
                    EventDateTimeUtcOffsetMinute = tmdata.EventDateTimeUtcOffsetMinute // Task p6#73_TimeZone issue -- added by Binoy - End
                };
                _guardLogDataProvider.SaveGuardLog(signOffEntry);
                _guardDataProvider.UpdateGuardOffDuty(guardLoginId, DateTime.Now);
                //logOff entry for radio checklist-start

                //var gaurdlogin = _clientDataProvider.GetGuardLogin(guardLoginId, clientSiteLogBookId);
                //if (gaurdlogin.Count != 0)
                //{
                //    foreach (var item in gaurdlogin)
                //    {
                //        var logbookcl = new GuardLogin();

                //        logbookcl.Id = item.Id;
                //        logbookcl.ClientSiteId = item.ClientSiteId;
                //        logbookcl.GuardId = item.GuardId;

                //        _guardLogDataProvider.SignOffClientSiteRadioCheckActivityStatusForLogBookEntry(item.GuardId, item.ClientSiteId);

                //    }
                //}

                //logOff entry for radio checklist-end
                /* new Change 07/11/2023 no need to remove the all the deatils when logoff remove after a buffer time Start */
                var guardlogins = _guardLogDataProvider.GetGuardLogins(Convert.ToInt32(GuardLog.GuardLoginId));
                foreach (var item in guardlogins)
                {

                    var ClientSiteRadioChecksActivityDetails = _guardLogDataProvider.GetClientSiteRadioChecksActivityDetails().Where(x => x.GuardId == item.GuardId && x.ClientSiteId == item.ClientSiteId && x.GuardLoginTime != null);
                    foreach (var ClientSiteRadioChecksActivity in ClientSiteRadioChecksActivityDetails)
                    {
                        ClientSiteRadioChecksActivity.GuardLogoutTime = DateTime.Now;
                        _guardLogDataProvider.UpdateRadioChecklistLogOffEntry(ClientSiteRadioChecksActivity);
                        /* Update Radio check status logOff*/

                    }
                }

                if (guardlogins != null)
                {
                    _guardLogDataProvider.SaveClientSiteRadioCheckStatusFromlogBookNewUpdate(new ClientSiteRadioCheck()
                    {
                        ClientSiteId = guardlogins.FirstOrDefault().ClientSiteId,
                        GuardId = guardlogins.FirstOrDefault().GuardId,
                        Status = "Off Duty",
                        RadioCheckStatusId = 1,
                        CheckedAt = DateTime.Now,
                        Active = true
                    });

                }
                /* new Change 07/11/2023 no need to remove the all the deatils when logoff remove after a buffer time end */
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status, message });
        }

        public JsonResult OnGetPatrolCarLogs(int logBookId, int clientSiteId)
        {
            var patrolCarLogs = _guardLogDataProvider.GetPatrolCarLogs(logBookId);
            if (!patrolCarLogs.Any())
            {
                var clientSitePatrolCars = _clientSiteWandDataProvider.GetClientSitePatrolCars(clientSiteId).Select(z => new PatrolCarLog()
                {
                    ClientSiteLogBookId = logBookId,
                    PatrolCarId = z.Id,
                });
                _guardLogDataProvider.SavePatrolCarLogs(clientSitePatrolCars);
                patrolCarLogs = _guardLogDataProvider.GetPatrolCarLogs(logBookId);
            }

            return new JsonResult(patrolCarLogs);
        }

        public JsonResult OnPostPatrolCarLog(PatrolCarLog record)
        {
            var success = true;
            var message = "Success";
            try
            {
                _guardLogDataProvider.SavePatrolCarLog(record);
            }
            catch (Exception ex)
            {
                success = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { success, message });
        }

        public JsonResult OnGetCustomFieldConfig(int clientSiteId)
        {
            var columns = new Dictionary<string, string>()
            {
                { "timeSlot", "Time Slot"}
            };
            var clientSiteCustomFields = _guardLogDataProvider.GetCustomFieldsByClientSiteId(clientSiteId);
            var fields = clientSiteCustomFields.Select(z => z.Name).Distinct();
            foreach (var field in fields)
            {
                columns.Add(field, field);
            }

            return new JsonResult(columns.ToArray());
        }

        public JsonResult OnGetCustomFieldLogs(int logBookId, int clientSiteId)
        {
            var customFieldLogs = _guardLogDataProvider.GetCustomFieldLogs(logBookId);
            if (!customFieldLogs.Any())
            {
                var clientSiteCustomFields = _guardLogDataProvider.GetCustomFieldsByClientSiteId(clientSiteId)
                                                .Select(z => new CustomFieldLog()
                                                {
                                                    ClientSiteLogBookId = logBookId,
                                                    CustomFieldId = z.Id
                                                }).ToList();
                _guardLogDataProvider.SaveCustomFieldLogs(clientSiteCustomFields);
                customFieldLogs = _guardLogDataProvider.GetCustomFieldLogs(logBookId);
            }

            var timeSlotGroups = customFieldLogs.GroupBy(z => z.ClientSiteCustomField.TimeSlot);
            var rows = new List<Dictionary<string, string>>();
            foreach (var group in timeSlotGroups)
            {
                var columns = new Dictionary<string, string>();
                if (!columns.ContainsKey(group.Key))
                {
                    columns.Add("timeSlot", group.Key);
                }

                foreach (var field in group.ToList())
                {
                    columns.Add(field.ClientSiteCustomField.Name, field.DayValue);
                }
                rows.Add(columns);
            }

            return new JsonResult(rows.ToArray());
        }

        public JsonResult OnPostSaveCustomFieldLog(int logBookId, Dictionary<string, string> records)
        {
            var timeSlot = records["timeSlot"];
            var success = true;
            try
            {
                var customFieldLogs = _guardLogDataProvider.GetCustomFieldLogs(logBookId);
                foreach (var record in records.Where(z => z.Key != "timeSlot"))
                {
                    if (record.Value != null)
                    {
                        var customFieldLog = customFieldLogs.SingleOrDefault(x => x.ClientSiteCustomField.Name.Equals(record.Key) &&
                                                                x.ClientSiteCustomField.TimeSlot.Equals(timeSlot));
                        if (customFieldLog != null)
                        {
                            customFieldLog.DayValue = record.Value;
                            _guardLogDataProvider.SaveCustomFieldLog(customFieldLog);
                        }
                    }
                }
            }
            catch
            {
                success = false;
            }

            return new JsonResult(success);
        }

        public JsonResult OnPostResetClientSiteLogBook(int clientSiteId, int guardLoginId, GuardLog tmdata)
        {
            var exMessage = new StringBuilder();
            try
            {
                var currentGuardLogin = _guardDataProvider.GetGuardLoginById(guardLoginId);
                var currentGuardLoginOffDutyActual = currentGuardLogin.OffDuty;
                var logOffDateTime = GuardLogBookHelper.GetLogOffDateTime();
                // Task p6#73_TimeZone issue -- added by Binoy - Start
                var logOffDateTimeLocal = GuardLogBookHelper.GetLocalLogOffDateTime((DateTime)tmdata.EventDateTimeLocal);
                var logOffDateTimeLocalWithOffSet = GuardLogBookHelper.GetLocalLogOffDateTimeWithOffset((DateTimeOffset)tmdata.EventDateTimeLocalWithOffset);
                // Task p6#73_TimeZone issue -- added by Binoy - End

                var signOffEntry = new GuardLog()
                {
                    ClientSiteLogBookId = currentGuardLogin.ClientSiteLogBookId,
                    GuardLoginId = currentGuardLogin.Id,
                    EventDateTime = DateTime.Now,
                    Notes = "Guard Off Duty (Logbook Signout)",
                    IsSystemEntry = true,
                    EventDateTimeLocal = logOffDateTimeLocal, // Task p6#73_TimeZone issue -- added by Binoy - Start
                    EventDateTimeLocalWithOffset = logOffDateTimeLocalWithOffSet,
                    EventDateTimeZone = tmdata.EventDateTimeZone,
                    EventDateTimeZoneShort = tmdata.EventDateTimeZoneShort,
                    EventDateTimeUtcOffsetMinute = tmdata.EventDateTimeUtcOffsetMinute // Task p6#73_TimeZone issue -- added by Binoy - End
                };
                _guardLogDataProvider.SaveGuardLog(signOffEntry);
                _guardDataProvider.UpdateGuardOffDuty(guardLoginId, logOffDateTime);

                var newLogBookId = _viewDataService.GetNewClientSiteLogBookId(clientSiteId, LogBookType.DailyGuardLog);
                if (newLogBookId <= 0)
                    throw new InvalidOperationException("Failed to get client site log book");

                var newGuardLoginId = _viewDataService.GetNewGuardLoginId(currentGuardLogin, currentGuardLoginOffDutyActual, newLogBookId);
                if (newGuardLoginId <= 0)
                    throw new InvalidOperationException("Failed to login");
                var logBook = _clientDataProvider.GetClientSiteLogBookWithOutType(clientSiteId, DateTime.Now.Date);
                // var localDateTimeNow = DateTimeHelper.GetCurrentLocalTimeFromUtcMinute((int)tmdata.EventDateTimeUtcOffsetMinute);
                var signInEntry = new GuardLog()
                {
                    ClientSiteLogBookId = newLogBookId,
                    GuardLoginId = newGuardLoginId,
                    EventDateTime = DateTime.Now,
                    Notes = "Logbook Logged In",
                    IsSystemEntry = true,
                    EventDateTimeLocal = tmdata.EventDateTimeLocal, // Task p6#73_TimeZone issue -- added by Binoy - Start
                    EventDateTimeLocalWithOffset = tmdata.EventDateTimeLocalWithOffset,
                    EventDateTimeZone = tmdata.EventDateTimeZone,
                    EventDateTimeZoneShort = tmdata.EventDateTimeZoneShort,
                    EventDateTimeUtcOffsetMinute = tmdata.EventDateTimeUtcOffsetMinute // Task p6#73_TimeZone issue -- added by Binoy - End
                };
                _guardLogDataProvider.SaveGuardLog(signInEntry);

                HttpContext.Session.SetInt32("LogBookId", newLogBookId);
                HttpContext.Session.SetInt32("GuardLoginId", newGuardLoginId);
                HttpContext.Session.SetInt32("PreviousLogBookId", currentGuardLogin.ClientSiteLogBookId);
                HttpContext.Session.SetString("PreviousLogBookDateString", DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd HH:mm:ss"));

                // p6#73 timezone bug - Added by binoy 24-01-2024 - Start
                /* get previous day push messages Start */
                /*check if id is Citywatch M1 - Romeo Patrol Cars site id=625*/

                // p6#73 timezone bug - Added by binoy 24-01-2024
                if (logBook.Count == 0)
                {




                    if (clientSiteId == 625)
                    {
                        var previousPuShMessages = _clientDataProvider.GetPushMessagesNotAcknowledged(clientSiteId, DateTime.Today.AddDays(-1));
                        if (previousPuShMessages != null)
                        {
                            _guardLogDataProvider.CopyPreviousDaysPushMessageToLogBook(previousPuShMessages, newLogBookId, guardLoginId, signInEntry);
                        }
                    }

                    /* Copy Previous duress message not deactivated (Repete in each logbook untill deactivated )*/
                    var isDurssEnabled = _viewDataService.IsClientSiteDuressEnabled(clientSiteId);
                    if (isDurssEnabled)
                    {
                        var previousDuressMessages = _clientDataProvider.GetDuressMessageNotAcknowledged(clientSiteId, DateTime.Today.AddDays(-1));
                        if (previousDuressMessages != null)
                        {
                            _guardLogDataProvider.CopyPreviousDaysDuressToLogBook(previousDuressMessages, newLogBookId, guardLoginId, signInEntry);
                        }
                        var clientSiteForLogbook = _clientDataProvider.GetClientSiteForRcLogBook();
                        if (clientSiteForLogbook.Count != 0)
                        {
                            if (clientSiteForLogbook.FirstOrDefault().Id == clientSiteId)
                            {
                                var previousDuressMessagesForControlRoom = _clientDataProvider.GetDuressMessageNotAcknowledgedForControlRoom(DateTime.Today.AddDays(-1));
                                if (previousDuressMessagesForControlRoom != null)
                                {
                                    foreach (var items in previousDuressMessagesForControlRoom)
                                    {
                                        _guardLogDataProvider.LogBookEntryForRcControlRoomMessages(currentGuardLogin.GuardId, currentGuardLogin.GuardId, null, "Duress Alarm Activated", IrEntryType.Alarm, 1, 0, signInEntry);
                                    }
                                    //_guardLogDataProvider.CopyPreviousDaysDuressToLogBook(previousDuressMessagesForControlRoom, logBookId, guardLoginId);
                                }

                            }

                        }

                    }



                }
                /* get previous day push messages end */
                // p6#73 timezone bug - Added by binoy 24-01-2024 - End

                return new JsonResult(new { success = true, newLogBookId, newLogBookDate = DateTime.Today.ToString("dd MMM yyyy"), guardLoginId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.StackTrace);

                exMessage.AppendFormat("Error: {0}. ", ex.Message);

                if (ex.InnerException != null &&
                    ex.InnerException is SqlException &&
                    ex.InnerException.Message.StartsWith("Violation of UNIQUE KEY constraint"))
                {
                    exMessage.Append("Attempt to create duplicate log book on same date. ");
                }
                exMessage.Append("Please logout and login again.");
            }

            return new JsonResult(new { success = false, message = exMessage.ToString() });
        }

        public JsonResult OnPostSaveClientSiteDuress(int clientSiteId, int guardLoginId, int logBookId,
                                                     int guardId, string gpsCoordinates, string enabledAddress, GuardLog tmdata)
        {
            var status = true;
            var message = "Success";
            try
            {

                var ClientsiteDetails = _clientDataProvider.GetClientSiteName(clientSiteId);
                var Emails = _clientDataProvider.GetGlobalDuressEmail().ToList();
                var emailAddresses = string.Join(",", Emails.Select(email => email.Email));
                var GuradDetails = _clientDataProvider.GetGuradName(guardId);

                _viewDataService.EnableClientSiteDuress(clientSiteId, guardLoginId, logBookId, guardId, gpsCoordinates, enabledAddress, tmdata, ClientsiteDetails.Name, GuradDetails.Name);
                /* Save log for duress button enable Start 02032024 dileep*/
                _SiteEventLogDataProvider.SaveSiteEventLogData(
                    new SiteEventLog()
                    {
                        GuardId = guardId,
                        SiteId = clientSiteId,
                        GuardName = GuradDetails.Name,
                        SiteName = ClientsiteDetails.Name,
                        ProjectName = "ClientPortal",
                        ActivityType = "Duress Button Enable",
                        Module = "Guard",
                        SubModule = "LogBook",
                        GoogleMapCoordinates = gpsCoordinates,
                        IPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString(),
                        ToAddress = string.Empty,
                        ToMessage = string.Empty,
                        EventTime = DateTime.Now,
                        EventLocalTime = DateTime.Now
                    }
                 );
                /* Save log for duress button enable end*/

                #region GlobalDuressEmailAndSms
                var Subject = "Global Duress Alert";
                var Notifications = "C4i Duress Button Activated By:" +
                         (string.IsNullOrEmpty(GuradDetails.Name) ? string.Empty : GuradDetails.Name) + "[" + GuradDetails.Initial + "]" + "<br/>" +
                         (string.IsNullOrEmpty(GuradDetails.Mobile) ? string.Empty : "Mobile No: " + GuradDetails.Mobile) + "<br/>" +
                        (string.IsNullOrEmpty(ClientsiteDetails.Name) ? string.Empty : "From: " + ClientsiteDetails.Name) + "<br/>" +
                        (string.IsNullOrEmpty(ClientsiteDetails.Address) ? string.Empty : "Address:: " + ClientsiteDetails.Address) + "<br/>" +
                        (string.IsNullOrEmpty(ClientsiteDetails.LandLine) ? string.Empty : "Mobile No: " + ClientsiteDetails.LandLine);
                var SmsNotifications = Notifications.Replace("<br/>", "\n");
                if (gpsCoordinates != null)
                {
                    var googleMapsLink = "https://www.google.com/maps?q=" + HttpUtility.UrlEncode(gpsCoordinates);
                    Notifications += "\n<a href=\"" + googleMapsLink + "\" target=\"_blank\" data-toggle=\"tooltip\" title=\"View on Google Maps\"><i class=\"fa fa-map-marker\" aria-hidden=\"true\"></i> Location</a>";
                    SmsNotifications += "\n" + googleMapsLink;
                }

                EmailSender(emailAddresses, Subject, Notifications, GuradDetails.Name, ClientsiteDetails.Name, gpsCoordinates);

                var GlobalDuressSmsNumbers = _clientDataProvider.GetDuressSms();
                if (ClientsiteDetails.DuressSms != null)
                {// Adding Site Duress Sms number.
                    GlobalDuressSms SiteDuressSmsNumbers = new GlobalDuressSms() { SmsNumber = ClientsiteDetails.DuressSms };
                    GlobalDuressSmsNumbers.Add(SiteDuressSmsNumbers);
                }
                if (_webHostEnvironment.IsDevelopment())
                {
                    string smsnumber = "+61 (0) 423 404 982"; // Sending to Jino sir number for testing purpose
                    GlobalDuressSmsNumbers = new List<GlobalDuressSms>();
                    GlobalDuressSms gd = new GlobalDuressSms() { SmsNumber = smsnumber };
                    GlobalDuressSmsNumbers.Add(gd);
                }

                if (GlobalDuressSmsNumbers != null)
                {
                    List<SmsChannelEventLog> _smsChannelEventLogList = new List<SmsChannelEventLog>();
                    foreach (var item in GlobalDuressSmsNumbers)
                    {
                        if (item.SmsNumber != null)
                        {
                            SmsChannelEventLog smslog = new SmsChannelEventLog();
                            smslog.GuardId = guardId != 0 ? guardId : null; // ID of guard who is sending the message
                            smslog.GuardName = GuradDetails.Name.Length > 0 ? GuradDetails.Name : null; // Name of guard who is sending the message
                            smslog.GuardNumber = item.SmsNumber;
                            smslog.SiteId = clientSiteId;
                            smslog.SiteName = ClientsiteDetails.Name;
                            _smsChannelEventLogList.Add(smslog);
                        }
                    }
                    SiteEventLog svl = new SiteEventLog();
                    svl.ProjectName = "ClientPortal";
                    svl.ActivityType = "C4i Duress Enable - Global Duress SMS Alert";
                    svl.Module = "Guard";
                    svl.SubModule = "LogBook";
                    svl.GoogleMapCoordinates = gpsCoordinates;
                    svl.IPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString();
                    svl.EventLocalTime = tmdata.EventDateTimeLocal.Value;
                    svl.EventLocalOffsetMinute = tmdata.EventDateTimeUtcOffsetMinute;
                    svl.EventLocalTimeZone = tmdata.EventDateTimeZoneShort;
                    svl.IPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString();
                    _smsSenderProvider.SendSms(_smsChannelEventLogList, Subject + " : " + SmsNotifications, svl);
                }
                else
                {
                    _SiteEventLogDataProvider.SaveSiteEventLogData(
                       new SiteEventLog()
                       {
                           GuardName = GuradDetails.Name,
                           SiteName = ClientsiteDetails.Name,
                           ProjectName = "ClientPortal",
                           ActivityType = "C4i Duress Enable - Global Duress SMS Alert",
                           Module = "Guard",
                           SubModule = "LogBook",
                           GoogleMapCoordinates = gpsCoordinates,
                           IPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString(),
                           EventTime = DateTime.Now,
                           EventLocalTime = DateTime.Now,
                           ToAddress = null,
                           ToMessage = Subject + " : " + SmsNotifications,
                           EventStatus = "Error",
                           EventErrorMsg = "No Global duress sms numbers configured.",
                           EventServerOffsetMinute = TimeZoneHelper.GetCurrentTimeZoneOffsetMinute(),
                           EventServerTimeZone = TimeZoneHelper.GetCurrentTimeZoneShortName()
                       }
                    );
                }
                #endregion
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }
            return new JsonResult(new { status, message });
        }
        public JsonResult EmailSender(string Email, string Subject, string Notifications, string GuradName, string Name, string gpsCoordinates)
        {
            var success = true;
            var message = "success";
            #region Email
            if (Email != null)
            {
                var fromAddress = _emailOptions.FromAddress.Split('|');
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
                    client.Connect(_emailOptions.SmtpServer, _emailOptions.SmtpPort, MailKit.Security.SecureSocketOptions.None);
                    if (!string.IsNullOrEmpty(_emailOptions.SmtpUserName) &&
                        !string.IsNullOrEmpty(_emailOptions.SmtpPassword))
                        client.Authenticate(_emailOptions.SmtpUserName, _emailOptions.SmtpPassword);
                    client.Send(messagenew);
                    client.Disconnect(true);
                    /* Save log for duress button enable Start 02032024 dileep*/
                    _SiteEventLogDataProvider.SaveSiteEventLogData(
                        new SiteEventLog()
                        {
                            GuardName = GuradName,
                            SiteName = Name,
                            ProjectName = "ClientPortal",
                            ActivityType = "Duress Button Enable Email",
                            Module = "Guard",
                            SubModule = "LogBook",
                            GoogleMapCoordinates = gpsCoordinates,
                            IPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString(),
                            EventTime = DateTime.Now,
                            EventLocalTime = DateTime.Now,
                            ToAddress = Email,
                            ToMessage = "Global Duress Alert",
                        }
                     );
                    /* Save log for duress button enable end*/
                }
            }

            #endregion

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

        public JsonResult OnPostSavePushNotificationTestMessages(int guardLoginId, int clientSiteLogBookId, string Notifications, int rcPushMessageId, GuardLog tmdata)
        {
            var success = true;
            var message = "success";
            try
            {
                var localDateTime = DateTimeHelper.GetCurrentLocalTimeFromUtcMinute((int)tmdata.EventDateTimeUtcOffsetMinute);

                var signOffEntry = new GuardLog()
                {
                    ClientSiteLogBookId = clientSiteLogBookId,
                    GuardLoginId = guardLoginId,
                    EventDateTime = DateTime.Now,
                    Notes = Notifications,
                    IrEntryType = IrEntryType.Normal,
                    EventDateTimeLocal = tmdata.EventDateTimeLocal, // Task p6#73_TimeZone issue -- added by Binoy - Start
                    EventDateTimeLocalWithOffset = tmdata.EventDateTimeLocalWithOffset,
                    EventDateTimeZone = tmdata.EventDateTimeZone,
                    EventDateTimeZoneShort = tmdata.EventDateTimeZoneShort,
                    EventDateTimeUtcOffsetMinute = tmdata.EventDateTimeUtcOffsetMinute // Task p6#73_TimeZone issue -- added by Binoy - End
                };
                _guardLogDataProvider.SaveGuardLog(signOffEntry);

                /* update UpdateIsAcknowledged to 1 start */
                _guardLogDataProvider.UpdateIsAcknowledged(rcPushMessageId);
                /* update UpdateIsAcknowledged to 1 end */

                /* message to Citywatch Conrol room logbook from (settings in RC)  Start*/

                var clientSiteForLogbook = _clientDataProvider.GetClientSiteForRcLogBook();
                if (clientSiteForLogbook.Count != 0)
                {
                    //var logBookId = _guardLogDataProvider.GetClientSiteLogBookIdGloablmessage(clientSiteForLogbook.FirstOrDefault().Id, LogBookType.DailyGuardLog, DateTime.Today);
                    // p6#73 timezone bug - Added by binoy 24-01-2024 -- Start
                    var logbookdate = DateTime.Today;
                    var logbooktype = LogBookType.DailyGuardLog;
                    var logBookId = _guardLogDataProvider.GetClientSiteLogBookIdByLogBookMaxID(clientSiteForLogbook.FirstOrDefault().Id, logbooktype, out logbookdate); // Get Last Logbookid and logbook Date by latest logbookid  of the client site
                    // var entryTime = new DateTime(logbookdate.Year, logbookdate.Month, logbookdate.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);  //DateTimeHelper.GetLogbookEndTimeFromDate(logbookdate);
                    // p6#73 timezone bug - Added by binoy 24-01-2024 -- end

                    var selectedGuardId = _guardLogDataProvider.GetGuardLogins(guardLoginId).FirstOrDefault().GuardId;
                    var guardInitials = _guardLogDataProvider.GetGuards(selectedGuardId).Name + " [" + _guardLogDataProvider.GetGuards(selectedGuardId).Initial + "]";
                    var clientSiteId = _guardLogDataProvider.GetGuardLogins(guardLoginId).FirstOrDefault().ClientSiteId;
                    var clientsitename = _guardLogDataProvider.GetClientSites(clientSiteId).FirstOrDefault().Name;
                    var notifcationtoCitywatchHD = new GuardLog()
                    {
                        ClientSiteLogBookId = logBookId,
                        GuardLoginId = guardLoginId,
                        EventDateTime = DateTime.Now,
                        Notes = Notifications + " - " + guardInitials + " - " + clientsitename,
                        IrEntryType = IrEntryType.Normal,
                        EventDateTimeLocal = tmdata.EventDateTimeLocal, // Task p6#73_TimeZone issue -- added by Binoy - Start
                        EventDateTimeLocalWithOffset = tmdata.EventDateTimeLocalWithOffset,
                        EventDateTimeZone = tmdata.EventDateTimeZone,
                        EventDateTimeZoneShort = tmdata.EventDateTimeZoneShort,
                        EventDateTimeUtcOffsetMinute = tmdata.EventDateTimeUtcOffsetMinute // Task p6#73_TimeZone issue -- added by Binoy - End
                    };

                    _guardLogDataProvider.SaveGuardLog(notifcationtoCitywatchHD);

                }
                /* message to Citywatch Conrol room logbook from (settings in RC)  Start */
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }

        public IActionResult OnGetDailyGuardLog(int id)
        {

            if (id != 0)
            {


                /* Change in Attachement*/
                ViewData["DailyGuardLog_Attachments"] = _viewDataService.GetDailyGuardLogAttachments(
                    IO.Path.Combine(_webHostEnvironment.WebRootPath, "DGlUploads"), id.ToString())
                    .ToList();
                ViewData["DailyGuardLog_FilesPathRoot"] = "/DGlUploads/" + id.ToString();


            }

            return new PartialViewResult
            {
                ViewName = "_KeyVehicleLogPopup",
                ViewData = new ViewDataDictionary<GuardLog>(ViewData)
            };
        }

        public async Task<JsonResult> OnPostRearFileUpload()
        {
            var success = false;
            var message = "Uploaded successfully";
            var files = Request.Form.Files;
            int id = 0;
            string url = string.Empty;
            foreach (string key in Request.Form.Keys)
            {

                // Retrieve the value using the key
                bool isNumeric1 = int.TryParse(Request.Form[key], out _);
                if (isNumeric1)
                {
                    id = Convert.ToInt32(Request.Form[key]);
                }
                else
                {
                    url = Request.Form[key].ToString();
                }

            }
            if (files.Count == 1)
            {
                var file = files[0];
                if (file.Length > 0)
                {
                    try
                    {
                        var uploadFileName = Path.GetFileNameWithoutExtension(file.FileName) + "_" + DateTime.Today.Ticks.ToString() + Path.GetExtension(file.FileName);
                        var fileExtension = Path.GetExtension(file.FileName).ToLower();
                        string[] allowedExtensions = { ".jpg", ".jpeg", ".bmp", ".gif", ".heic", ".png" };

                        // Validate file type
                        if (!allowedExtensions.Contains(fileExtension))
                            throw new ArgumentException("Unsupported file type. Please upload a .jpg/.jpeg/.bmp/.gif/.heic/.png file.");
                        string reportReference = HttpContext.Session.GetString("ReportReference");
                        var folderPath = Path.Combine(Path.Combine(_webHostEnvironment.WebRootPath, "DglUploads", id.ToString()), "RearFiles");
                        if (!Directory.Exists(folderPath))
                            Directory.CreateDirectory(folderPath);
                        using (var stream = System.IO.File.Create(Path.Combine(folderPath, uploadFileName)))
                        {
                            file.CopyTo(stream);
                        }

                        var Newfilename = Path.GetFileName(uploadFileName);
                        //p1-102 add photos with heic extension-start
                        if (fileExtension == ".heic")
                        {
                            var newuploadheic = Path.Combine(folderPath, Path.GetFileNameWithoutExtension(file.FileName) + "_" + DateTime.Today.Ticks.ToString() + ".jpg");
                            await ConvertHeicToJpgAsync(Path.Combine(folderPath, uploadFileName), folderPath);
                            Newfilename = Path.GetFileName(newuploadheic);
                            var fileToDelete = Path.Combine(folderPath, uploadFileName);
                            if (System.IO.File.Exists(fileToDelete))
                                System.IO.File.Delete(fileToDelete);


                        }
                        //p1 - 102 add photos with heic extension - end
                        //var folderPathnew = Path.Combine(Path.Combine("https://localhost:44356/", "DglUploads", id.ToString()), "RearFiles");
                        var folderPathnew = Path.Combine(Path.Combine(url + "/", "DglUploads", id.ToString()), "RearFiles");

                        var LogImages = new GuardLogsDocumentImages()
                        {
                            GuardLogId = id,
                            ImagePath = Path.Combine(folderPathnew, Newfilename),
                            IsRearfile = true
                        };

                        _guardLogDataProvider.SaveGuardLogDocumentImages(LogImages);
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        message = ex.Message;
                    }
                }
            }

            //return "";

            return new JsonResult(new { success, message });
        }

        public async Task<JsonResult> OnPostTwentyfivePercentFileUpload()
        {
            var success = false;
            var message = "Uploaded successfully";
            var files = Request.Form.Files;
            int id = 0;
            string url = string.Empty;
            foreach (string key in Request.Form.Keys)
            {

                bool isNumeric1 = int.TryParse(Request.Form[key], out _);
                if (isNumeric1)
                {
                    id = Convert.ToInt32(Request.Form[key]);
                }
                else
                {
                    url = Request.Form[key].ToString();
                }
            }
            if (files.Count == 1)
            {
                var file = files[0];
                if (file.Length > 0)
                {
                    try
                    {

                        var uploadFileName = Path.GetFileNameWithoutExtension(file.FileName) + "_" + DateTime.Today.Ticks.ToString() + Path.GetExtension(file.FileName);
                        var fileExtension = Path.GetExtension(file.FileName).ToLower();
                        string[] allowedExtensions = { ".jpg", ".jpeg", ".bmp", ".gif", ".heic", ".png" };

                        // Validate file type
                        if (!allowedExtensions.Contains(fileExtension))
                            throw new ArgumentException("Unsupported file type. Please upload a .jpg/.jpeg/.bmp/.gif/.heic/.png file.");


                        string reportReference = HttpContext.Session.GetString("ReportReference");
                        var folderPath = Path.Combine(Path.Combine(_webHostEnvironment.WebRootPath, "DglUploads", id.ToString()), "TwentyfivePercentFiles");
                        if (!Directory.Exists(folderPath))
                            Directory.CreateDirectory(folderPath);
                        using (var stream = System.IO.File.Create(Path.Combine(folderPath, uploadFileName)))
                        {
                            file.CopyTo(stream);
                        }
                        //p1 - 102 add photos with heic extension - start
                        var Newfilename = Path.GetFileName(uploadFileName);
                        //p1-102 add photos with heic extension-start
                        if (fileExtension == ".heic")
                        {
                            var newuploadheic = Path.Combine(folderPath, Path.GetFileNameWithoutExtension(file.FileName) + "_" + DateTime.Today.Ticks.ToString() + ".jpg");
                            await ConvertHeicToJpgAsync(Path.Combine(folderPath, uploadFileName), folderPath);
                            Newfilename = Path.GetFileName(newuploadheic);
                            var fileToDelete = Path.Combine(folderPath, uploadFileName);
                            if (System.IO.File.Exists(fileToDelete))
                                System.IO.File.Delete(fileToDelete);


                        }
                        //p1 - 102 add photos with heic extension - end
                        //var folderPathnew = Path.Combine(Path.Combine("https://localhost:44356/", "DglUploads", id.ToString()), "TwentyfivePercentFiles");
                        var folderPathnew = Path.Combine(Path.Combine(url + "/", "DglUploads", id.ToString()), "TwentyfivePercentFiles");

                        var LogImages = new GuardLogsDocumentImages()
                        {
                            GuardLogId = id,
                            ImagePath = Path.Combine(folderPathnew, Newfilename),
                            IsTwentyfivePercentfile = true
                        };

                        _guardLogDataProvider.SaveGuardLogDocumentImages(LogImages);
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        message = ex.Message;
                    }
                }
            }

            return new JsonResult(new { success, message });

        }

        public async Task<string> ConvertHeicToJpgAsync(string heicFilePath, string outputDirectory)
        {
            try
            {
                var secretkey = string.Empty;
                var companydetail = _userDataProvider.GetCompanyDetails().SingleOrDefault(x => x.Id == 1);
                if (companydetail != null)
                {
                    secretkey = companydetail.ApiSecretkey;
                }
                // Initialize ConvertAPI with your API secret key
                var convertApi = new ConvertApi(secretkey);

                // Check if the HEIC file exists
                if (!System.IO.File.Exists(heicFilePath))
                {
                    throw new FileNotFoundException("The specified HEIC file does not exist.");
                }

                // Convert HEIC to JPG
                var conversionResult = await convertApi.ConvertAsync("heic", "jpg", new ConvertApiFileParam(heicFilePath));

                // Ensure the output directory exists
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                // Path to save the converted JPG file
                //string jpgFilePath = Path.Combine(outputDirectory, $"{Path.GetFileNameWithoutExtension(heicFilePath)}.jpg");

                // Save the converted JPG file
                await conversionResult.SaveFilesAsync(outputDirectory);

                Console.WriteLine($"Conversion successful! JPG saved at: {outputDirectory}");
                return outputDirectory;  // Return the path to the converted file
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during conversion: {ex.Message}");
                throw;  // Rethrow the exception for further handling if needed
            }

        }
        //public JsonResult OnPostRearFileUpload()
        //{
        //    var success = false;
        //    var message = "Uploaded successfully";
        //    var files = Request.Form.Files;
        //    int id = 0;
        //    string url = string.Empty;
        //    foreach (string key in Request.Form.Keys)
        //    {

        //        // Retrieve the value using the key
        //        bool isNumeric1 = int.TryParse(Request.Form[key], out _);
        //        if (isNumeric1)
        //        {
        //            id = Convert.ToInt32(Request.Form[key]);
        //        }
        //        else
        //        {
        //            url = Request.Form[key].ToString();
        //        }

        //    }
        //    if (files.Count == 1)
        //    {
        //        var file = files[0];
        //        if (file.Length > 0)
        //        {
        //            try
        //            {
        //                var uploadFileName = Path.GetFileName(file.FileName);
        //                //if (Path.GetExtension(file.FileName) != ".JPG" && Path.GetExtension(file.FileName) != ".jpg" && Path.GetExtension(file.FileName) != ".JPEG" && Path.GetExtension(file.FileName) != ".jpeg" && Path.GetExtension(file.FileName) != ".bmp" && Path.GetExtension(file.FileName) != ".BMP" && Path.GetExtension(file.FileName) != ".gif" && Path.GetExtension(file.FileName) != ".GIF" && Path.GetExtension(file.FileName) != ".heic" && Path.GetExtension(file.FileName) != ".HEIC")
        //                //    throw new ArgumentException("Unsupported file type. Please upload a .jpg/.jpeg/.bmp/.gif/.heic file");
        //                if (Path.GetExtension(file.FileName) != ".JPG" && Path.GetExtension(file.FileName) != ".jpg" && Path.GetExtension(file.FileName) != ".JPEG" && Path.GetExtension(file.FileName) != ".jpeg" && Path.GetExtension(file.FileName) != ".bmp" && Path.GetExtension(file.FileName) != ".BMP" && Path.GetExtension(file.FileName) != ".gif" && Path.GetExtension(file.FileName) != ".GIF")
        //                    throw new ArgumentException("Unsupported file type. Please upload a .jpg/.jpeg/.bmp/.gif file");
        //                string reportReference = HttpContext.Session.GetString("ReportReference");
        //                var folderPath = Path.Combine(Path.Combine(_webHostEnvironment.WebRootPath, "DglUploads", id.ToString()), "RearFiles");
        //                if (!Directory.Exists(folderPath))
        //                    Directory.CreateDirectory(folderPath);
        //                using (var stream = System.IO.File.Create(Path.Combine(folderPath, uploadFileName)))
        //                {
        //                    file.CopyTo(stream);
        //                }
        //                //p1-102 add photos with heic extension-start
        //                //if (Path.GetExtension(file.FileName) == ".heic" || Path.GetExtension(file.FileName) == ".HEIC")
        //                //{
        //                //    var newuploadheic = Path.Combine(folderPath, Path.GetFileNameWithoutExtension(file.FileName)+".jpg");


        //                //    using (MagickImage image = new MagickImage(Path.Combine(folderPath, file.FileName)))
        //                //    {
        //                //        // Save the image as JPEG
        //                //        image.Format = MagickFormat.Jpg;
        //                //        image.Write(newuploadheic);



        //                //    }
        //                //    uploadFileName = Path.GetFileName(newuploadheic);
        //                //    var fileToDelete = Path.Combine(folderPath, file.FileName);
        //                //    if (System.IO.File.Exists(fileToDelete))
        //                //        System.IO.File.Delete(fileToDelete);
        //                //}
        //                //p1 - 102 add photos with heic extension - end
        //                //var folderPathnew = Path.Combine(Path.Combine("https://localhost:44356/", "DglUploads", id.ToString()), "RearFiles");
        //                var folderPathnew = Path.Combine(Path.Combine(url + "/", "DglUploads", id.ToString()), "RearFiles");

        //                var LogImages = new GuardLogsDocumentImages()
        //                {
        //                    GuardLogId = id,
        //                    ImagePath = Path.Combine(folderPathnew, uploadFileName),
        //                    IsRearfile = true
        //                };

        //                _guardLogDataProvider.SaveGuardLogDocumentImages(LogImages);
        //                success = true;
        //            }
        //            catch (Exception ex)
        //            {
        //                message = ex.Message;
        //            }
        //        }
        //    }

        //    return new JsonResult(new {  success,message });
        //}

        //public JsonResult OnPostTwentyfivePercentFileUpload()
        //{
        //    var success = false;
        //    var message = "Uploaded successfully";
        //    var files = Request.Form.Files;
        //    int id = 0;
        //    string url = string.Empty;
        //    foreach (string key in Request.Form.Keys)
        //    {

        //        bool isNumeric1 = int.TryParse(Request.Form[key], out _);
        //        if (isNumeric1)
        //        {
        //            id = Convert.ToInt32(Request.Form[key]);
        //        }
        //        else
        //        {
        //            url = Request.Form[key].ToString();
        //        }
        //    }
        //    if (files.Count == 1)
        //    {
        //        var file = files[0];
        //        if (file.Length > 0)
        //        {
        //            try
        //            {
        //                var uploadFileName = Path.GetFileName(file.FileName);
        //                //if (Path.GetExtension(file.FileName) != ".JPG" && Path.GetExtension(file.FileName) != ".jpg" && Path.GetExtension(file.FileName) != ".JPEG" && Path.GetExtension(file.FileName) != ".jpeg" && Path.GetExtension(file.FileName) != ".bmp" && Path.GetExtension(file.FileName) != ".BMP" && Path.GetExtension(file.FileName) != ".gif" && Path.GetExtension(file.FileName) != ".GIF" && Path.GetExtension(file.FileName) != ".heic" && Path.GetExtension(file.FileName) != ".HEIC")
        //                if (Path.GetExtension(file.FileName) != ".JPG" && Path.GetExtension(file.FileName) != ".jpg" && Path.GetExtension(file.FileName) != ".JPEG" && Path.GetExtension(file.FileName) != ".jpeg" && Path.GetExtension(file.FileName) != ".bmp" && Path.GetExtension(file.FileName) != ".BMP" && Path.GetExtension(file.FileName) != ".gif" && Path.GetExtension(file.FileName) != ".GIF")
        //                    throw new ArgumentException("Unsupported file type. Please upload a .jpg/.jpeg/.bmp/.gif file");

        //                string reportReference = HttpContext.Session.GetString("ReportReference");
        //                var folderPath = Path.Combine(Path.Combine(_webHostEnvironment.WebRootPath, "DglUploads", id.ToString()), "TwentyfivePercentFiles");
        //                if (!Directory.Exists(folderPath))
        //                    Directory.CreateDirectory(folderPath);
        //                using (var stream = System.IO.File.Create(Path.Combine(folderPath, uploadFileName)))
        //                {
        //                    file.CopyTo(stream);
        //                }
        //                //p1 - 102 add photos with heic extension - start
        //                //if (Path.GetExtension(file.FileName) == ".heic" || Path.GetExtension(file.FileName) == ".HEIC")
        //                //{
        //                //    var newuploadheic = Path.Combine(folderPath, Path.GetFileNameWithoutExtension(file.FileName) + ".jpg");


        //                //    using (MagickImage image = new MagickImage(Path.Combine(folderPath, file.FileName)))
        //                //    {
        //                //        // Save the image as JPEG
        //                //        image.Format = MagickFormat.Jpg;
        //                //        image.Write(newuploadheic);



        //                //    }
        //                //     uploadFileName = Path.GetFileName(newuploadheic);
        //                //    var fileToDelete = Path.Combine(folderPath, file.FileName);
        //                //    if (System.IO.File.Exists(fileToDelete))
        //                //        System.IO.File.Delete(fileToDelete);
        //                //}
        //                //p1 - 102 add photos with heic extension - end
        //                //var folderPathnew = Path.Combine(Path.Combine("https://localhost:44356/", "DglUploads", id.ToString()), "TwentyfivePercentFiles");
        //                var folderPathnew = Path.Combine(Path.Combine(url+ "/", "DglUploads", id.ToString()), "TwentyfivePercentFiles");

        //                var LogImages = new GuardLogsDocumentImages()
        //                {
        //                    GuardLogId = id,
        //                    ImagePath = Path.Combine(folderPathnew, uploadFileName),
        //                    IsTwentyfivePercentfile = true
        //                };

        //                _guardLogDataProvider.SaveGuardLogDocumentImages(LogImages);
        //                success = true;
        //            }
        //            catch (Exception ex)
        //            {
        //                message = ex.Message;
        //            }
        //        }
        //    }

        //    return new JsonResult(new {  success,message });
        //}
        public JsonResult OnPostDeleteAttachment(int id)

        {

            var success = false;
            var message = "Success";
            try
            {
                if (id != 0)
                {
                    var image = _guardLogDataProvider.GetGuardLogDocumentImaesById(id);
                    var filePath = string.Empty;
                    foreach (var item in image)
                    {

                        if (item.IsRearfile == true)
                        {
                            filePath = IO.Path.Combine(_webHostEnvironment.WebRootPath, "DglUploads", item.GuardLogId.ToString(), "RearFiles", IO.Path.GetFileName(item.ImagePath));
                            if (IO.File.Exists(filePath))
                            {

                                IO.File.Delete(filePath);
                                success = true;


                            }

                        }
                        if (item.IsTwentyfivePercentfile == true)
                        {
                            filePath = IO.Path.Combine(_webHostEnvironment.WebRootPath, "DglUploads", item.GuardLogId.ToString(), "TwentyfivePercentFiles", IO.Path.GetFileName(item.ImagePath));
                            if (IO.File.Exists(filePath))
                            {

                                IO.File.Delete(filePath);
                                success = true;


                            }

                        }
                        _guardLogDataProvider.DeleteGuardLogDocumentImaes(id);
                    }

                }



            }
            catch (Exception ex)
            {
                success = false;
                message = "Error " + ex.Message;
            }
            return new JsonResult(new { success, message });
        }
        public JsonResult OnGetGuardLogsDocumentImages(int id)
        {


            var guardlogImages = _guardLogDataProvider.GetGuardLogDocumentImaes(id);
            foreach (var item in guardlogImages)
            {
                item.ImageFile = IO.Path.GetFileName(item.ImagePath);
            }

            return new JsonResult(guardlogImages);
        }


        public JsonResult OnPostDownLoadHelpPDF(string filename, int loginGuardId, GuardLog tmdata,string pageName,string jsname)
        {

            var Issuccess = false;
            var exMessage = "";
            var status = pageName == "IR" ? ((jsname == "lb") ? true : false) : true;
            try
            {
                // avoid two calls from lb and Kv js in IR
                if (status)
                {
                    if (User.Identity.IsAuthenticated)
                    {
                        var userid = AuthUserHelper.GetLoggedInUserId;
                        if (userid != null)
                        {
                            var IPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString();

                            if (loginGuardId != 0)
                            {

                                FileDownloadAuditLogs fdal = new FileDownloadAuditLogs()
                                {
                                    UserID = (int)userid,
                                    GuardID = loginGuardId,
                                    IPAddress = IPAddress,
                                    DwnlCatagory = "Help Document " + pageName,
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
                    }
                    else
                    {
                        exMessage = "Error: User not authenticated.";
                    }

                }
               else
                {
                    Issuccess = false;

                }


            }
            catch (Exception ex)
            {
                _logger.LogError(ex.StackTrace);
                exMessage = $"Error: {ex.Message}.";
            }

            return new JsonResult(new { success = Issuccess, message = exMessage });
        }

    }


}
