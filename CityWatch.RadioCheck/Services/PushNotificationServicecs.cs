using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using CityWatch.Web.Models;
using DocumentFormat.OpenXml.Spreadsheet;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using SMSGlobal.api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace CityWatch.RadioCheck.Services
{
    public interface IPushNotificationServicecs
    {
        public void SendActionListLater();
    }
    public class PushNotificationServicecs : IPushNotificationServicecs
    {
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        private readonly EmailOptions _emailOptions;
        private readonly ISmsSenderProvider _smsSenderProvider;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IConfigDataProvider _configDataProvider;
        public PushNotificationServicecs(
            IOptions<EmailOptions> emailOptions,
            IGuardLogDataProvider guardLogDataProvider, ISmsSenderProvider smsSenderProvider,IClientDataProvider clientDataProvider,IConfigDataProvider configDataProvider)
        {
            
            _emailOptions = emailOptions.Value;
            _guardLogDataProvider = guardLogDataProvider;
           _smsSenderProvider= smsSenderProvider;
            _clientDataProvider = clientDataProvider;
            _configDataProvider = configDataProvider;
        }
        public void SendActionListLater()
        {
            //var messagelist = _guardLogDataProvider.GetRCActionListMessages().Where(x=>x.messagetime.ToString("dd-MM-yyyy HH:mm") == DateTime.Now.ToString("dd-MM-yyyy  HH:mm"));
            var messagelist = _guardLogDataProvider.GetRCActionListMessages().Where(x => (x.Radiofrequencystatus == "OnceOff" && x.messagetime <= DateTime.Now) || (x.Radiofrequencystatus == "EveryDay" && x.Endmessagetime >= DateTime.Now));
            foreach (var message in messagelist)
            {
                if (message.Radiofrequencystatus == "EveryDay")
                {
                    // Check if already sent today
                    bool alreadySentToday = _guardLogDataProvider
                        .HasMessageBeenSentToday(message.Id, DateTime.Now);

                    if (alreadySentToday)
                        continue; // Skip to next message
                }
                var ActionListMessage = string.Empty;
                //var ActionListMessage = new StringBuilder();
                if (message.ClientSiteId !=null)
                {
                    var clientsite = _clientDataProvider.GetClientSiteDetailsWithId(Convert.ToInt32(message.ClientSiteId)).FirstOrDefault();
                    var clientType = _clientDataProvider
            .GetClientTypes()
            .FirstOrDefault(x => x.Id == message.ClientTypeId);

                    //ActionListMessage.AppendLine("Client Type: " + clientType?.Name);
                    //ActionListMessage.AppendLine(); // blank line before Client Site

                    //ActionListMessage.AppendLine("Client Site: " + clientsite.Name);
                    //ActionListMessage.AppendLine("Address: " + clientsite.Address);
                    //ActionListMessage.AppendLine("Google Map Link: " + clientsite.Gps);
                    //ActionListMessage.AppendLine("Site Access");
                    //ActionListMessage.AppendLine("===========");
                    //ActionListMessage.AppendLine("Alarm Keypad Code: " + message.AlarmKeypadCode);
                    //ActionListMessage.AppendLine("Physical key: " + message.Physicalkey);
                    //ActionListMessage.AppendLine("Combination Lock: " + message.SiteCombinationLook);
                    //ActionListMessage.AppendLine("Alarm Response");
                    //ActionListMessage.AppendLine("===========");
                    //ActionListMessage.AppendLine("Action 1: " + message.Action1);
                    //ActionListMessage.AppendLine("Action 2: " + message.Action2);
                    //ActionListMessage.AppendLine("Action 3: " + message.Action3);
                    //ActionListMessage.AppendLine("Action 4: " + message.Action4);
                    //ActionListMessage.AppendLine("Action 5: " + message.Action5);
                    //var sopfiletype = _configDataProvider.GetStaffDocumentsUsingType(4).Where(z => z.ClientSite == message.ClientSiteId);
                    //if (sopfiletype.Count() != 0)
                    //{
                    //    ActionListMessage.AppendLine("SOP's Site: https://cws-ir.com/StaffDocs/" + sopfiletype.Select(x => x.FileName).ToList());

                    //}
                    //else
                    //{
                    //    ActionListMessage.AppendLine("SOP's Site:");
                    //}
                    //var sopAlarmfileType = _configDataProvider.GetStaffDocumentsUsingType(6).Where(z => z.ClientSite == message.ClientSiteId);
                    //if (sopAlarmfileType.Count() != 0)
                    //{
                    //    ActionListMessage.AppendLine("SOP's Alarm: https://cws-ir.com/StaffDocs/" + sopAlarmfileType.Select(x => x.FileName).ToList());

                    //}
                    //else
                    //{
                    //    ActionListMessage.AppendLine("SOP's Alarm:");
                    //}

                    //ActionListMessage.AppendLine(); // blank line before Message


                    ActionListMessage = "Client Type: " + _clientDataProvider.GetClientTypes().Where(x => x.Id == message.ClientTypeId).FirstOrDefault().Name + "\r\n";
                    ActionListMessage += "Client Site: " + clientsite.Name + "\r\n";
                    ActionListMessage += "Address: " + clientsite.Address + "\r\n";
                    ActionListMessage += "Google Map Link: " + clientsite.Gps + "\r\n";
                    ActionListMessage += "Site Access \r\n";
                    ActionListMessage += "=========== \r\n";
                    ActionListMessage += "Alarm Keypad Code: " + message.AlarmKeypadCode + "\r\n";
                    ActionListMessage += "Physical key: " + message.Physicalkey + "\r\n";
                    ActionListMessage += "Combination Lock: " + message.SiteCombinationLook + "\r\n";
                    ActionListMessage += "Alarm Response \r\n";
                    ActionListMessage += "=========== \r\n";
                    ActionListMessage += "Action 1: " + message.Action1 + "\r\n";
                    ActionListMessage += "Action 2: " + message.Action2 + "\r\n";
                    ActionListMessage += "Action 3: " + message.Action3 + "\r\n";
                    ActionListMessage += "Action 4: " + message.Action4 + "\r\n";
                    ActionListMessage += "Action 5: " + message.Action5 + "\r\n";
                    ActionListMessage += "\r\n";
                    var sopfiletype = _configDataProvider.GetStaffDocumentsUsingType(4).Where(z => z.ClientSite == clientsite.Id);
                    if (sopfiletype.Count() != 0)
                    {
                        // ActionListMessage += "SOP's Site:https://cws-ir.com/StaffDocs/" + sopfiletype.Select(x => x.FileName).ToList();
                        //ActionListMessage += "SOP's Site:" + $" < a href='https://cws-ir.com/StaffDocs/{sopfiletype.Select(x => x.FileName).ToList()}'>Click here</a><br/>";
                        foreach (var item in sopfiletype)
                        {
                            ActionListMessage += "SOP's Site:" + $"<a href=\"https://cws-ir.com/StaffDocs/{item.FileName}\">Click here</a><br/>";
                        }
                        ActionListMessage += "\r\n";
                        ActionListMessage += "\r\n";

                    }
                    else
                    {
                        ActionListMessage += "SOP's Site:";
                        ActionListMessage += "\r\n";
                        ActionListMessage += "\r\n";
                    }
                    var sopAlarmfileType = _configDataProvider.GetStaffDocumentsUsingType(6).Where(z => z.ClientSite == clientsite.Id);
                    if (sopAlarmfileType.Count() != 0)
                    {
                        //ActionListMessage += "SOP's Alarm: https://cws-ir.com/StaffDocs/" + sopAlarmfileType.Select(x => x.FileName).ToList();
                        //ActionListMessage += "SOP's Alarm:" + $" < a href='https://kpi.cws-ir.com/StaffDocs/{sopfiletype.Select(x => x.FileName).ToList()}'>Click here</a><br/>";
                        foreach (var item in sopAlarmfileType)
                        {
                            ActionListMessage += "SOP's Alarm:" + $"<a href=\"https://kpi.cws-ir.com/StaffDocs/{item.FileName}\">Click here</a><br/>";
                        }
                        ActionListMessage += "\r\n";
                        ActionListMessage += "\r\n";

                    }
                    else
                    {
                        ActionListMessage += "SOP's Alarm:";
                        ActionListMessage += "\r\n";
                        ActionListMessage += "\r\n";
                    }
                }

            
                if (!string.IsNullOrEmpty(message.Notifications))
                {
                    //ActionListMessage.AppendLine("Message");
                    //ActionListMessage.AppendLine("===========");
                    //ActionListMessage.AppendLine(message.Notifications);
                    ActionListMessage += "Message";
                    ActionListMessage += "\r\n";
                    ActionListMessage += "========";
                    ActionListMessage += "\r\n";
                    ActionListMessage += (string.IsNullOrEmpty(message.Notifications) ? string.Empty : message.Notifications);
                }
                // ActionListMessage += (string.IsNullOrEmpty(message.Notifications) ? string.Empty : "Message: " + message.Notifications);
                var rcguardlogs = _guardLogDataProvider.GetRCActionListMessagesGuardLogs().Where(x => x.RCActionListMessagesId == message.Id).FirstOrDefault();
                
                var guardLog = new GuardLog()
                {
                    
                    EventDateTime = rcguardlogs.EventDateTime,
                    
                    EventDateTimeLocal = rcguardlogs.EventDateTimeLocal,
                    EventDateTimeLocalWithOffset = rcguardlogs.EventDateTimeLocalWithOffset,
                    EventDateTimeZone = rcguardlogs.EventDateTimeZone,
                    EventDateTimeZoneShort = rcguardlogs.EventDateTimeZoneShort,
                    EventDateTimeUtcOffsetMinute = rcguardlogs.EventDateTimeUtcOffsetMinute

                };
                _guardLogDataProvider.LogBookEntryFromRcControlRoomMessages(rcguardlogs.GuardId, 0, message.Subject, ActionListMessage.ToString(), IrEntryType.Alarm, 1, 0, guardLog);

                var clientSiteList = _guardLogDataProvider.GetRCActionListMessagesClientsites().Where(x => x.RCActionListMessagesId == message.Id).ToList();
                foreach(var clientSite in clientSiteList)
                {
                    var clientsitedetail = _guardLogDataProvider.GetClientSites(clientSite.ClientSiteId).FirstOrDefault();
                    LogBookDetails(clientSite.Id, ActionListMessage.ToString(), message.Subject, guardLog, rcguardlogs.GuardId);
                    _guardLogDataProvider.LogBookEntryFromRcControlRoomMessages(rcguardlogs.GuardId, 0, message.Subject, message.Notifications, IrEntryType.Alarm, 1, 0, guardLog);
                    if (clientsitedetail.SiteEmail != null)
                    {
                        EmailSender(clientsitedetail.SiteEmail, clientsitedetail.Id, message.Subject, ActionListMessage.ToString());
                    }
                    if (message.IsSMSPersonal==true)
                    {
                        SMSPersonal(message, guardLog, rcguardlogs, clientSite.ClientSiteId);
                    }
                    if (message.IsSMSSmartWand == true)
                    {
                        SMSSmartWand(message, guardLog, rcguardlogs, clientSite.ClientSiteId);
                    }
                    //if (message.IsPersonalEmail == true)
                    //{
                    //    PersonalEmails(message, guardLog, rcguardlogs, clientSite.ClientSiteId);
                    //}
                    //if ((message.Radiofrequencystatus == "OnceOff") || (message.Radiofrequencystatus == "EveryDay" && Convert.ToDateTime(message.Endmessagetime).Date <= DateTime.Now.Date))
                    //{
                    //    _guardLogDataProvider.UpdateRCActionListMessagesClientSites(clientSite.Id);
                    //}
                    if (message.Radiofrequencystatus == "OnceOff")
                    {
                        // Mark OnceOff as permanently sent
                        _guardLogDataProvider.UpdateRCActionListMessagesClientSites(clientSite.Id);
                    }
                    if (message.Radiofrequencystatus == "EveryDay")
                    {
                        if (Convert.ToDateTime(message.Endmessagetime).Date <= DateTime.Now.Date)
                        {
                            // Final day reached - mark as sent and complete
                            _guardLogDataProvider.UpdateRCActionListMessagesClientSites(clientSite.Id);
                        }
                    }
                }

                //_guardLogDataProvider.UpdateRCActionListMessages(message.Id);
                // ---- Update main message ----
                if (message.Radiofrequencystatus == "OnceOff")
                {
                    // Mark OnceOff as permanently sent
                    _guardLogDataProvider.UpdateRCActionListMessages(message.Id);
                }
                else if (message.Radiofrequencystatus == "EveryDay")
                {
                    if (Convert.ToDateTime(message.Endmessagetime).Date <= DateTime.Now.Date)
                    {
                        // Final day reached - mark as sent and complete
                        _guardLogDataProvider.UpdateRCActionListMessages(message.Id);
                    }
                    else
                    {
                        // Mark that today's message was sent
                        _guardLogDataProvider.MarkMessageSentToday(message.Id, DateTime.Now);
                    }
                }

            }
        }
        public void LogBookDetails(int Id, string Notifications, string Subject, GuardLog tmzdata,int GuardId)
        {
            #region Logbook
            if (Id != null)
            {

                var logbooktype = LogBookType.DailyGuardLog;
                //var logBookId = _guardLogDataProvider.GetClientSiteLogBookIdGloablmessage(Id, logbooktype, DateTime.Today);

                var logbookdate = DateTime.Today;
                // Get Last Logbookid and logbook Date by latest logbookid // p6#73 timezone bug - Modified by binoy 29-01-2024
                var logbooks = _guardLogDataProvider.GetClientSiteLogBooks(Id, logbooktype, logbookdate);
                if (logbooks.Count() > 0)
                {
                    var logBookId = _guardLogDataProvider.GetClientSiteLogBookIdByLogBookMaxID(Id, logbooktype, out logbookdate);

                    if (logBookId != 0)
                    {

                        if (GuardId != 0)
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
                            var guardLoginId = _guardLogDataProvider.GetGuardLoginId(Convert.ToInt32(GuardId), DateTime.Today);
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



                var fromAddress = _emailOptions.FromAddress.Split('|');
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
                    client.Connect(_emailOptions.SmtpServer, _emailOptions.SmtpPort, MailKit.Security.SecureSocketOptions.None);
                    if (!string.IsNullOrEmpty(_emailOptions.SmtpUserName) &&
                        !string.IsNullOrEmpty(_emailOptions.SmtpPassword))
                        client.Authenticate(_emailOptions.SmtpUserName, _emailOptions.SmtpPassword);
                    client.Send(messagenew);
                    client.Disconnect(true);
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
                if (MailboxAddress.TryParse(item, out var address))
                {
                    emailAddressList.Add(new MailboxAddress(string.Empty, item));
                }
            }
            return emailAddressList;
        }
        public void SMSPersonal(RCActionListMessages message,GuardLog guardLog,RCActionListMessagesGuardLogs rcguardlogs,int clientSiteId)
        {
            // For sending SMS
            SiteEventLog svl = new SiteEventLog();
            svl.ProjectName = "Radio Check";
            svl.Module = "Radio Check V2";
            svl.EventLocalTime = guardLog.EventDateTimeLocal.Value;
            svl.EventLocalOffsetMinute = guardLog.EventDateTimeUtcOffsetMinute;
            svl.EventLocalTimeZone = guardLog.EventDateTimeZoneShort;
            svl.IPAddress = rcguardlogs.RemoteIPAddress;
            svl.ActivityType = "SMS Personal";
            if (message.IsState == true)
            {
                svl.SubModule = "Global Push Notification-[Global] [State]";
            }
            if(message.IsClientType==true)
            {
                svl.SubModule = "Global Push Notification-[Global] [ClientType]";
            }
            if (message.IsNational == true)
            {
                svl.SubModule = "Global Push Notification-[Global] [National]";
            }
            var guardlogins = _guardLogDataProvider.GetGuardLoginsByClientSiteId(clientSiteId, DateTime.Now);
            string guardname = string.Empty;
            if(rcguardlogs.GuardId != 0)
            {
                guardname = _guardLogDataProvider.GetGuards(rcguardlogs.GuardId).Name;
            }
                List<SmsChannelEventLog> _smsChannelEventLogList = new List<SmsChannelEventLog>();
            foreach (var logins in guardlogins)
            {
                if (logins.Guard.Mobile != null)
                {
                    SmsChannelEventLog smslog = new SmsChannelEventLog();
                    smslog.GuardId = rcguardlogs.GuardId != 0 ? rcguardlogs.GuardId : null; // ID of guard who is sending the message
                    smslog.GuardName = guardname.Length > 0 ? guardname : null; // Name of guard who is sending the message
                    smslog.GuardNumber = logins.Guard.Mobile;
                    smslog.SiteId = null;  // setting as null since it is from RC else clientSiteId
                    smslog.SiteName = null;  // setting as null since it is from RC else  clientsitename
                    _smsChannelEventLogList.Add(smslog);
                }
            }
            var ActionListMessage = (string.IsNullOrEmpty(message.Notifications) ? string.Empty : "Message: " + message.Notifications);
            _smsSenderProvider.SendSms(_smsChannelEventLogList, message.Subject + " : " + ActionListMessage, svl);

        }
        public void SMSSmartWand(RCActionListMessages message, GuardLog guardLog, RCActionListMessagesGuardLogs rcguardlogs, int clientSiteId)
        {
            // For sending SMS
            SiteEventLog svl = new SiteEventLog();
            svl.ProjectName = "Radio Check";
            svl.Module = "Radio Check V2";
            svl.EventLocalTime = guardLog.EventDateTimeLocal.Value;
            svl.EventLocalOffsetMinute = guardLog.EventDateTimeUtcOffsetMinute;
            svl.EventLocalTimeZone = guardLog.EventDateTimeZoneShort;
            svl.IPAddress = rcguardlogs.RemoteIPAddress;
            svl.ActivityType = "SMS Smart Wand";
            if (message.IsState == true)
            {
                svl.SubModule = "Global Push Notification-[Global] [State]";
            }
            if (message.IsClientType == true)
            {
                svl.SubModule = "Global Push Notification-[Global] [ClientType]";
            }
            if (message.IsNational == true)
            {
                svl.SubModule = "Global Push Notification-[Global] [National]";
            }
           
            string guardname = string.Empty;
            if (rcguardlogs.GuardId != 0)
            {
                guardname = _guardLogDataProvider.GetGuards(rcguardlogs.GuardId).Name;
            }
            List<SmsChannelEventLog> _smsChannelEventLogList = new List<SmsChannelEventLog>();
            var smartWands = _guardLogDataProvider.GetClientSiteSmartWands(clientSiteId);
            
            foreach (var sw in smartWands)
            {
                if (sw.PhoneNumber != null)
                {
                    SmsChannelEventLog smslog = new SmsChannelEventLog();
                    smslog.GuardId = rcguardlogs.GuardId != 0 ? rcguardlogs.GuardId : null; // ID of guard who is sending the message
                    smslog.GuardName = guardname.Length > 0 ? guardname : null; // Name of guard who is sending the message
                    smslog.GuardNumber = sw.PhoneNumber;
                    smslog.SiteId = null;  // setting as null since it is from RC else clientSiteId
                    smslog.SiteName = null;  // setting as null since it is from RC else  clientsitename
                    _smsChannelEventLogList.Add(smslog);
                }
            }
            var ActionListMessage = (string.IsNullOrEmpty(message.Notifications) ? string.Empty : "Message: " + message.Notifications);
            _smsSenderProvider.SendSms(_smsChannelEventLogList, message.Subject + " : " + ActionListMessage, svl);

        }
        public void PersonalEmails(RCActionListMessages message, GuardLog guardLog, RCActionListMessagesGuardLogs rcguardlogs, int clientSiteId)
        {
            string smsSiteEmails = null;
            
                var guardEmails = _guardLogDataProvider.GetGuardLogs(clientSiteId).Select(x => x.Guard.Email);


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
            var ActionListMessage = (string.IsNullOrEmpty(message.Notifications) ? string.Empty : "Message: " + message.Notifications);

            EmailSender(smsSiteEmails, clientSiteId, message.Subject, ActionListMessage);


        }

    }
}
