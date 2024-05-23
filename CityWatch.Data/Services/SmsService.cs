using CityWatch.Common.Services;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;


namespace CityWatch.Data.Services
{
    public interface ISmsService
    {
        Task<bool> SendSMS(List<SmsChannelEventLog> scev, string smsmsg, SiteEventLog svl);
    }

    public class SmsService : ISmsService
    {
        private readonly CityWatchDbContext _context;
        private readonly ISmsGlobalService _smsGlobalService;
        private readonly ISiteEventLogDataProvider _siteEventLogDataProvider;

        public SmsService(CityWatchDbContext context,ISiteEventLogDataProvider siteEventLogDataProvider, 
            ISmsGlobalService smsGlobalService)
        {
            _context = context;
            _siteEventLogDataProvider = siteEventLogDataProvider;
            _smsGlobalService = smsGlobalService;
        }

        public async Task<bool> SendSMS(List<SmsChannelEventLog> scev, string smsmsg, SiteEventLog svl)
        {
            bool rtn = true;
            string _SendingFrom = GetSmsSender();  // Max 11 character
            string _ApiKey = GetSmsApiKey();
            string _ApiSecret = GetSmsSecretKey();
            string SanitizedNumber = "";                      

            string sendingFrom = "";
            if (!string.IsNullOrEmpty(_SendingFrom))
            {
                if (_SendingFrom.Length > 11)
                {
                    sendingFrom = _SendingFrom.Substring(0, 10);
                }
                else
                {
                    sendingFrom = _SendingFrom;
                }
            }

            svl.EventChannel = "SMS";
            svl.FromAddress = sendingFrom;
            svl.ToMessage = smsmsg;

            foreach (var sendtonumber in scev)
            {
                svl.EventStatus = "";
                svl.EventErrorMsg = "";
                
                try
                {
                    SanitizedNumber = CleanupSmsNumber(sendtonumber.GuardNumber);
                    svl.ToAddress = SanitizedNumber;
                    svl.GuardId = sendtonumber.GuardId;
                    svl.GuardName = sendtonumber.GuardName;
                    svl.SiteId = sendtonumber.SiteId;
                    svl.SiteName = sendtonumber.SiteName;

                    string statusmsg = "Sending sms started: " + DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss");

                    var resp = await _smsGlobalService.SendSMSApi(SanitizedNumber, smsmsg, sendingFrom, _ApiKey, _ApiSecret);
                    // Write status to communication log table
                    var result = resp.messages;
                    string status = "";                   

                    if (resp.statuscode != 200)
                    {
                        status = "Failed";
                    }
                    else 
                    {
                        status = result.Select(x => x.status).FirstOrDefault();
                    }
                                        
                    statusmsg += "\r\n Api call status code: " + resp.statuscode.ToString();
                    statusmsg += "\r\n Api call status message: " + resp.statusmessage.ToString();
                    if(result != null && result.Length > 0)
                    {
                        if (result.Select(x => x.status).FirstOrDefault().ToLower().StartsWith("fail"))
                        {
                            string outgoingid = result.Select(x => x.outgoing_id).FirstOrDefault();
                            statusmsg += "\r\n" + "Message sending failed. Outgoing Id: " + outgoingid;
                            statusmsg += "\r\n" + JsonSerializer.Serialize(result);
                        }
                        else
                        {
                            statusmsg += "\r\n" + JsonSerializer.Serialize(result);
                        }
                    }
                    else
                    {
                        try
                        {
                            statusmsg += "\r\n" + JsonSerializer.Serialize(resp.messages);
                        }
                        catch (Exception)
                        {

                           // throw;
                        }                        
                    }

                    svl.EventStatus = status;
                    svl.EventErrorMsg = statusmsg;
                    svl.EventTime = DateTime.Now;
                    svl.EventServerOffsetMinute = TimeZoneHelper.GetCurrentTimeZoneOffsetMinute();
                    svl.EventServerTimeZone = TimeZoneHelper.GetCurrentTimeZoneShortName();
                    CreateCommunicationLog(svl);
                }
                catch (Exception ex)
                {
                    rtn = false;
                    // throw;
                    // Write status to communication log table
                    string status = "Failed";
                    string statusmsg = ex.ToString();
                    svl.EventStatus = status;
                    svl.EventErrorMsg = statusmsg;
                    svl.EventTime = DateTime.Now;
                    svl.EventServerOffsetMinute = TimeZoneHelper.GetCurrentTimeZoneOffsetMinute();
                    svl.EventServerTimeZone = TimeZoneHelper.GetCurrentTimeZoneShortName();
                    CreateCommunicationLog(svl);
                }

            }
            return rtn;          

        }


        private string GetSmsSender()
        {
            var sender = _context.SmsChannel.Select(x=> x.SmsSender).FirstOrDefault();
            return sender;
        }
        private string GetSmsApiKey()
        {
            var apikey = _context.SmsChannel.Select(x => x.ApiKey).FirstOrDefault();
            return apikey;
        }
        private string GetSmsSecretKey()
        {
            var secret = _context.SmsChannel.Select(x => x.ApiSecret).FirstOrDefault();
            return secret;
        }

        private string CleanupSmsNumber(string smsnumber)
        {
            smsnumber = smsnumber.Trim();

            if (smsnumber.Contains('+'))
                smsnumber = smsnumber.Replace("+", "");
            if (smsnumber.Contains("(0)"))
                smsnumber = smsnumber.Replace("(0)", "");
            if (smsnumber.Contains('('))
                smsnumber = smsnumber.Replace("(", "");
            if (smsnumber.Contains(' '))
                smsnumber = smsnumber.Replace(" ", "");
            if (smsnumber.Contains('-'))
                smsnumber = smsnumber.Replace("-", "");
            if (smsnumber.Contains(')'))
                smsnumber = smsnumber.Replace(")", "");
            if (smsnumber.StartsWith("00"))
                smsnumber = smsnumber.Substring(2, smsnumber.Length-2) ;
            if (smsnumber.StartsWith("0"))
                smsnumber = smsnumber.Substring(1, smsnumber.Length - 1);            
                       
            return smsnumber;
        }

        private void CreateCommunicationLog(SiteEventLog svl)
        {            
            _siteEventLogDataProvider.SaveSiteEventLogData(svl);
        }

    }
}
