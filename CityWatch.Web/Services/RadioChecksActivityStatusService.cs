using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Models;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace CityWatch.Web.Services
{
    public interface IRadioChecksActivityStatusService
    {
        void Process();
        List<ClientSiteRadioChecksActivityStatus> GetActiveGuardDetails();
    }




    public class RadioChecksActivityStatusService : IRadioChecksActivityStatusService
    {
        
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        
        public RadioChecksActivityStatusService(IGuardLogDataProvider guardLogDataProvider)
        {
          
            _guardLogDataProvider = guardLogDataProvider;
        }

        /// <summary>
        /// Returen the ActiveGuardDetails
        /// </summary>
        /// <returns></returns>
        public List<ClientSiteRadioChecksActivityStatus> GetActiveGuardDetails()
        {
            var clientSiteActivityStatuses = _guardLogDataProvider.GetClientSiteRadioChecksActivityDetails();
            return clientSiteActivityStatuses;
        }

        public void Process()
        {
            var ClientSiteRadioChecksActivityDetails = _guardLogDataProvider.GetClientSiteRadioChecksActivityDetails();

            foreach (var ClientSiteRadioChecksActivity in ClientSiteRadioChecksActivityDetails)
            {
                /* Check Last IR Created Time Exist */
                if(ClientSiteRadioChecksActivity.LastIRCreatedTime!=null)
                {
                    /* Check Last IR Created Time less than <2 hrs then delete from table */
                    var isActive = (DateTime.Now - ClientSiteRadioChecksActivity.LastIRCreatedTime).Value.TotalHours<2;
                    if (!isActive)
                        _guardLogDataProvider.DeleteClientSiteRadioChecksActivity(ClientSiteRadioChecksActivity);
                }
                /* Check Last KV Created Time Exist */
                if (ClientSiteRadioChecksActivity.LastKVCreatedTime != null)
                {
                    var isActive = (DateTime.Now - ClientSiteRadioChecksActivity.LastKVCreatedTime).Value.TotalHours < 2;
                    if (!isActive)
                        _guardLogDataProvider.DeleteClientSiteRadioChecksActivity(ClientSiteRadioChecksActivity);
                }
                /* Check Last LB Created Time Exist */
                if (ClientSiteRadioChecksActivity.LastLBCreatedTime != null)
                {
                    var isActive = (DateTime.Now - ClientSiteRadioChecksActivity.LastLBCreatedTime).Value.TotalHours < 2;
                    if (!isActive)
                        _guardLogDataProvider.DeleteClientSiteRadioChecksActivity(ClientSiteRadioChecksActivity);
                }

                /* remove all the logn time >8 */
                if (ClientSiteRadioChecksActivity.GuardLoginTime != null)
                {
                    var isActive = (DateTime.Now - ClientSiteRadioChecksActivity.GuardLoginTime).Value.TotalHours < 8;
                    if (!isActive)
                    {
                        _guardLogDataProvider.DeleteClientSiteRadioChecksActivity(ClientSiteRadioChecksActivity);
                    }
                    /* TO GIVE A WARNING TO THOSE WHO ARE DID NOT DO ANY ACTIVITY FOR 2 HOURS - start*/
                    else
                    {
                        if (ClientSiteRadioChecksActivity.NotificationCreatedTime != null)
                        {
                            isActive = (DateTime.Now - ClientSiteRadioChecksActivity.NotificationCreatedTime).Value.TotalHours < 2;
                            if (!isActive)
                            {
                                var noActivity = ClientSiteRadioChecksActivityDetails.Where(x => x.GuardId == ClientSiteRadioChecksActivity.GuardId && x.ClientSiteId == ClientSiteRadioChecksActivity.ClientSiteId && x.GuardLoginTime == null).Count();
                                if (noActivity == 0)
                                {
                                    var logbooktype = LogBookType.DailyGuardLog;
                                    var clientsiteId = ClientSiteRadioChecksActivity.ClientSiteId;
                                    var logBookId = _guardLogDataProvider.GetClientSiteLogBookId(clientsiteId, logbooktype, DateTime.Today);
                                    var guardLoginId = _guardLogDataProvider.GetGuardLoginId(logBookId, ClientSiteRadioChecksActivity.GuardId, DateTime.Today);
                                    var guardName = _guardLogDataProvider.GetGuards(ClientSiteRadioChecksActivity.GuardId).Name;
                                    var guardLog = new GuardLog()
                                    {
                                        ClientSiteLogBookId = logBookId,
                                       // GuardLoginId = guardLoginId,
                                        EventDateTime = DateTime.Now,
                                        Notes = "Caution Alarm: There has been '0' activity in KV & LB for 2 hours from guard [" + guardName + "]. There is also no IR currently to justify KPI low performance",
                                        //Notes = "Caution Alarm: There has been '0' activity in KV & LB for 2 hours from guard[" + guardName + "]",
                                        IsSystemEntry = true,
                                        IrEntryType = IrEntryType.Normal
                                    };
                                    _guardLogDataProvider.SaveGuardLog(guardLog);
                                    ClientSiteRadioChecksActivity.NotificationCreatedTime = guardLog.EventDateTime;
                                    _guardLogDataProvider.UpdateRadioChecklistEntry(ClientSiteRadioChecksActivity);
                                }
                            }
                        }
                        else
                        {
                            isActive = (DateTime.Now - ClientSiteRadioChecksActivity.GuardLoginTime).Value.TotalHours < 2;
                            if (!isActive)
                            {
                                var noActivity = ClientSiteRadioChecksActivityDetails.Where(x => x.GuardId == ClientSiteRadioChecksActivity.GuardId && x.ClientSiteId == ClientSiteRadioChecksActivity.ClientSiteId && x.GuardLoginTime == null).Count();
                                if (noActivity == 0)
                                {
                                    var logbooktype = LogBookType.DailyGuardLog;
                                    var clientsiteId = ClientSiteRadioChecksActivity.ClientSiteId;
                                    var logBookId = _guardLogDataProvider.GetClientSiteLogBookId(clientsiteId, logbooktype, DateTime.Today);
                                    var guardLoginId = _guardLogDataProvider.GetGuardLoginId(logBookId, ClientSiteRadioChecksActivity.GuardId, DateTime.Today);
                                    var guardName = _guardLogDataProvider.GetGuards(ClientSiteRadioChecksActivity.GuardId).Name;
                                    var guardLog = new GuardLog()
                                    {
                                        ClientSiteLogBookId = logBookId,
                                        //GuardLoginId = guardLoginId,
                                        EventDateTime = DateTime.Now,
                                        Notes = "Caution Alarm: There has been '0' activity in KV & LB for 2 hours from guard [" + guardName + "]. There is also no IR currently to justify KPI low performance",
                                        //Notes = "Caution Alarm: There has been '0' activity in KV & LB for 2 hours from guard[" + guardName + "]",
                                        IsSystemEntry = true,
                                        IrEntryType = IrEntryType.Normal
                                    };
                                    _guardLogDataProvider.SaveGuardLog(guardLog);
                                    ClientSiteRadioChecksActivity.NotificationCreatedTime = guardLog.EventDateTime;
                                    _guardLogDataProvider.UpdateRadioChecklistEntry(ClientSiteRadioChecksActivity);
                                }
                            }
                            
                        }
                    }
                    /* TO GIVE A WARNING TO THOSE WHO ARE DID NOT DO ANY ACTIVITY FOR 2 HOURS - end*/
                }
                /* LogoutTime time exits remove all the activity for that  */
                if (ClientSiteRadioChecksActivity.GuardLogoutTime != null)
                {

                    _guardLogDataProvider.SignOffClientSiteRadioCheckActivityStatusForLogBookEntry(ClientSiteRadioChecksActivity.GuardId, ClientSiteRadioChecksActivity.ClientSiteId);
                }
                
               
            }
        }

    }
}
