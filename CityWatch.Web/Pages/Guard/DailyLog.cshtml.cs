using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Helpers;
using CityWatch.Web.Services;
using DocumentFormat.OpenXml.Office2010.Word;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

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

        [BindProperty]
        public GuardLog GuardLog { get; set; }

        public IViewDataService ViewDataService { get { return _viewDataService; } }

        public DailyLogModel(IGuardDataProvider guardDataProvider,
            IGuardLogDataProvider guardLogDataProvider,
             IClientDataProvider clientDataProvider,
             IViewDataService viewDataService,
             IClientSiteWandDataProvider clientSiteWandDataProvider,
             ILogger<DailyLogModel> logger)
        {
            _guardDataProvider = guardDataProvider;
            _guardLogDataProvider = guardLogDataProvider;
            _clientDataProvider = clientDataProvider;
            _viewDataService = viewDataService;
            _clientSiteWandDataProvider = clientSiteWandDataProvider;
            _logger = logger;
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
            var guardLogs = _guardLogDataProvider.GetGuardLogs(logBookId, logBookDate ?? DateTime.Today)
                .OrderByDescending(z => z.Id)
                .ThenByDescending(z => z.EventDateTime);
            return new JsonResult(guardLogs);
        }

        public JsonResult OnPostSaveGuardLog()
        {
            //for solving the date format in azure  server 
            // passing the time part from jquery and create new date 
            if (GuardLog != null)
            {
                if (!string.IsNullOrEmpty(GuardLog.TimePartOnly))
                {
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

                    var ClientSiteRadioChecksActivityDetails = _guardLogDataProvider.GetClientSiteRadioChecksActivityDetails().Where(x => x.GuardId == item.GuardId && x.ClientSiteId==item.ClientSiteId && x.GuardLoginTime!=null);
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

        public JsonResult OnPostUpdateOffDuty(int guardLoginId, int clientSiteLogBookId)
        {
            var status = true;
            var message = "Success";
            try
            {
                var signOffEntry = new GuardLog()
                {
                    ClientSiteLogBookId = clientSiteLogBookId,
                    GuardLoginId = guardLoginId,
                    EventDateTime = DateTime.Now,
                    Notes = "Guard Off Duty (Logbook Signout)",
                    IsSystemEntry = true
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
                        _guardLogDataProvider.SaveClientSiteRadioCheckStatusFromlogBook(new ClientSiteRadioCheck()
                        {
                            ClientSiteId = ClientSiteRadioChecksActivity.ClientSiteId,
                            GuardId = ClientSiteRadioChecksActivity.GuardId,
                            Status = "Off Duty",
                            RadioCheckStatusId=2,
                            CheckedAt = DateTime.Now,
                            Active = true
                        }) ;
                    }
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

        public JsonResult OnPostResetClientSiteLogBook(int clientSiteId, int guardLoginId)
        {
            var exMessage = new StringBuilder();
            try
            {
                var currentGuardLogin = _guardDataProvider.GetGuardLoginById(guardLoginId);
                var currentGuardLoginOffDutyActual = currentGuardLogin.OffDuty;
                var logOffDateTime = GuardLogBookHelper.GetLogOffDateTime();

                var signOffEntry = new GuardLog()
                {
                    ClientSiteLogBookId = currentGuardLogin.ClientSiteLogBookId,
                    GuardLoginId = currentGuardLogin.Id,
                    EventDateTime = logOffDateTime,
                    Notes = "Guard Off Duty (Logbook Signout)",
                    IsSystemEntry = true
                };
                _guardLogDataProvider.SaveGuardLog(signOffEntry);
                _guardDataProvider.UpdateGuardOffDuty(guardLoginId, logOffDateTime);

                var newLogBookId = _viewDataService.GetNewClientSiteLogBookId(clientSiteId, LogBookType.DailyGuardLog);
                if (newLogBookId <= 0)
                    throw new InvalidOperationException("Failed to get client site log book");

                var newGuardLoginId = _viewDataService.GetNewGuardLoginId(currentGuardLogin, currentGuardLoginOffDutyActual, newLogBookId);
                if (newGuardLoginId <= 0)
                    throw new InvalidOperationException("Failed to login");

                var signInEntry = new GuardLog()
                {
                    ClientSiteLogBookId = newLogBookId,
                    GuardLoginId = newGuardLoginId,
                    EventDateTime = DateTime.Now,
                    Notes = "Logbook Logged In",
                    IsSystemEntry = true
                };
                _guardLogDataProvider.SaveGuardLog(signInEntry);

                HttpContext.Session.SetInt32("LogBookId", newLogBookId);
                HttpContext.Session.SetInt32("GuardLoginId", newGuardLoginId);
                HttpContext.Session.SetInt32("PreviousLogBookId", currentGuardLogin.ClientSiteLogBookId);
                HttpContext.Session.SetString("PreviousLogBookDateString", DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd HH:mm:ss"));

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

        public JsonResult OnPostSaveClientSiteDuress(int clientSiteId, int guardLoginId, int logBookId, int guardId)
        {
            var status = true;
            var message = "Success";
            try
            {
                _viewDataService.EnableClientSiteDuress(clientSiteId, guardLoginId, logBookId, guardId);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }
            return new JsonResult(new { status, message });
        }
        public JsonResult OnPostSavePushNotificationTestMessages(int guardLoginId, int clientSiteLogBookId, string Notifications)
        {
            var success = true;
            var message = "success";
            try
            {
                var signOffEntry = new GuardLog()
                {
                    ClientSiteLogBookId = clientSiteLogBookId,
                    GuardLoginId = guardLoginId,
                    EventDateTime = DateTime.Now,
                    Notes = Notifications,
                    IrEntryType = IrEntryType.Normal
                };
                _guardLogDataProvider.SaveGuardLog(signOffEntry);
              
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }

    }
}
