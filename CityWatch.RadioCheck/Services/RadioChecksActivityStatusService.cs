using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
namespace CityWatch.RadioCheck.Services
{
    public interface IRadioChecksActivityStatusService
    {
        void Process();
        void Process2();
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
            var ClientSiteRadioChecksActivityDetails = _guardLogDataProvider.GetClientSiteRadioChecksActivityDetails().ToList();

            foreach (var ClientSiteRadioChecksActivity in ClientSiteRadioChecksActivityDetails)
            {
                /* Check Last IR Created Time Exist */
                if (ClientSiteRadioChecksActivity.LastIRCreatedTime != null)
                {
                    /* Check Last IR Created Time less than <2 hrs then delete from table */
                    var isActive = (DateTime.Now - ClientSiteRadioChecksActivity.LastIRCreatedTime).Value.TotalHours < 2;
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
                /* Check Last SW Created Time Exist */
                /* Smartwand API implemented this delete functionality
                //if (ClientSiteRadioChecksActivity.LastSWCreatedTime != null)
                //{
                //    var isActive = (DateTime.Now - ClientSiteRadioChecksActivity.LastSWCreatedTime).Value.TotalHours < 2;
                //    if (!isActive)
                //        _guardLogDataProvider.DeleteClientSiteRadioChecksActivity(ClientSiteRadioChecksActivity);
                //}
                /* Check if guard off duty time expired  New Change In Api by Dileep for task p4 task17 Start */
                if (ClientSiteRadioChecksActivity.GuardLoginTime != null)
                {
                    if (ClientSiteRadioChecksActivity.OffDuty != null && ClientSiteRadioChecksActivity.GuardLogoutTime==null)
                    {
                        /*off duty time +90 min buffer time */
                        /*  allow90 minute buffer here ok in case guard is doing over time or working back*/
                        var isActive = (DateTime.Now - ClientSiteRadioChecksActivity.OffDuty).Value.TotalMinutes < 90;
                        if (!isActive)
                        {
                            /* if buffer time is over remove login */
                            /* Check if any actvity in 90 min if no activity signing off else no signing off */
                            if (!_guardLogDataProvider.getIfAnyActivityInbufferTime(ClientSiteRadioChecksActivity.GuardId, ClientSiteRadioChecksActivity.ClientSiteId))
                            {
                                _guardLogDataProvider.SaveClientSiteRadioCheck(new ClientSiteRadioCheck()
                                {
                                    ClientSiteId = ClientSiteRadioChecksActivity.ClientSiteId,
                                    GuardId = ClientSiteRadioChecksActivity.GuardId,
                                    Status = "Off Duty (RC automatic logoff)",
                                    CheckedAt = DateTime.Now,
                                    Active = true,
                                    RadioCheckStatusId=null,
                                });

                            }
                        }
                    }
                    if (ClientSiteRadioChecksActivity.GuardLogoutTime != null)
                    {
                        /* If log off time exist remove if log off time >2  */
                        var isActive = (DateTime.Now - ClientSiteRadioChecksActivity.GuardLogoutTime).Value.TotalHours < 2;
                        if (!isActive)
                        {
                            /* if buffer time is over remove login */
                            _guardLogDataProvider.SignOffClientSiteRadioCheckActivityStatusForLogBookEntry(ClientSiteRadioChecksActivity.GuardId, ClientSiteRadioChecksActivity.ClientSiteId);
                        }
                    }
                    if (ClientSiteRadioChecksActivity.OffDuty == null && ClientSiteRadioChecksActivity.GuardLogoutTime == null)
                    {
                        /* If not removed in the previous setps after 12 hr it will remove */
                        var isActive = (DateTime.Now - ClientSiteRadioChecksActivity.GuardLoginTime).Value.TotalHours < 12;
                        if (!isActive)
                        {
                            _guardLogDataProvider.SignOffClientSiteRadioCheckActivityStatusForLogBookEntry(ClientSiteRadioChecksActivity.GuardId, ClientSiteRadioChecksActivity.ClientSiteId);
                        }
                    }
                }
                /* Check if guard off duty time expired  New Change In Api by Dileep for task p4 task17 end */

                /* remove all the logn time >8 */
                if (ClientSiteRadioChecksActivity.GuardLoginTime != null && ClientSiteRadioChecksActivity.NotificationType == null)
                {
                    var isActive = (DateTime.Now - ClientSiteRadioChecksActivity.GuardLoginTime).Value.TotalHours < 8;
                    if (!isActive)
                    {
                        //_guardLogDataProvider.DeleteClientSiteRadioChecksActivity(ClientSiteRadioChecksActivity);
                    }
                    /* TO GIVE A WARNING TO THOSE WHO ARE DID NOT DO ANY ACTIVITY FOR 2 HOURS - start*/
                    else
                    {
                        /* getting some error due to _guardLogDataProvider.GetGuards*/
                        //if (ClientSiteRadioChecksActivity.NotificationCreatedTime != null)
                        //{
                        //    isActive = (DateTime.Now - ClientSiteRadioChecksActivity.NotificationCreatedTime).Value.TotalHours < 2;
                        //    if (!isActive)
                        //    {
                        //        var noActivity = ClientSiteRadioChecksActivityDetails.Where(x => x.GuardId == ClientSiteRadioChecksActivity.GuardId && x.ClientSiteId == ClientSiteRadioChecksActivity.ClientSiteId && x.GuardLoginTime == null).Count();
                        //        if (noActivity == 0)
                        //        {
                        //            var logbooktype = LogBookType.DailyGuardLog;
                        //            var clientsiteId = ClientSiteRadioChecksActivity.ClientSiteId;
                        //            var logBookId = _guardLogDataProvider.GetClientSiteLogBookId(clientsiteId, logbooktype, DateTime.Today);
                        //            //var guardLoginId = _guardLogDataProvider.GetGuardLoginId(logBookId, ClientSiteRadioChecksActivity.GuardId, DateTime.Today);
                        //            var guardName = _guardLogDataProvider.GetGuards(ClientSiteRadioChecksActivity.GuardId).Name;
                        //            var guardLog = new GuardLog()
                        //            {
                        //                ClientSiteLogBookId = logBookId,
                        //                // GuardLoginId = guardLoginId,
                        //                EventDateTime = DateTime.Now,
                        //                Notes = "Caution Alarm: There has been '0' activity in KV & LB for 2 hours from guard [" + guardName + "]. There is also no IR currently to justify KPI low performance",
                        //                //Notes = "Caution Alarm: There has been '0' activity in KV & LB for 2 hours from guard[" + guardName + "]",
                        //                IsSystemEntry = true,
                        //                IrEntryType = IrEntryType.Normal
                        //            };
                        //            _guardLogDataProvider.SaveGuardLog(guardLog);
                        //            ClientSiteRadioChecksActivity.NotificationCreatedTime = guardLog.EventDateTime;
                        //            _guardLogDataProvider.UpdateRadioChecklistEntry(ClientSiteRadioChecksActivity);
                        //        }
                        //    }
                        //}
                        //else
                        //{
                        //    isActive = (DateTime.Now - ClientSiteRadioChecksActivity.GuardLoginTime).Value.TotalHours < 2;
                        //    if (!isActive)
                        //    {
                        //        var noActivity = ClientSiteRadioChecksActivityDetails.Where(x => x.GuardId == ClientSiteRadioChecksActivity.GuardId && x.ClientSiteId == ClientSiteRadioChecksActivity.ClientSiteId && x.GuardLoginTime == null).Count();
                        //        if (noActivity == 0)
                        //        {
                        //            var logbooktype = LogBookType.DailyGuardLog;
                        //            var clientsiteId = ClientSiteRadioChecksActivity.ClientSiteId;
                        //            var logBookId = _guardLogDataProvider.GetClientSiteLogBookId(clientsiteId, logbooktype, DateTime.Today);
                        //         //   var guardLoginId = _guardLogDataProvider.GetGuardLoginId(logBookId, ClientSiteRadioChecksActivity.GuardId, DateTime.Today);
                        //            var guardName = _guardLogDataProvider.GetGuards(ClientSiteRadioChecksActivity.GuardId).Name;
                        //            var guardLog = new GuardLog()
                        //            {
                        //                ClientSiteLogBookId = logBookId,
                        //                //GuardLoginId = guardLoginId,
                        //                EventDateTime = DateTime.Now,
                        //                Notes = "Caution Alarm: There has been '0' activity in KV & LB for 2 hours from guard [" + guardName + "]. There is also no IR currently to justify KPI low performance",
                        //                //Notes = "Caution Alarm: There has been '0' activity in KV & LB for 2 hours from guard[" + guardName + "]",
                        //                IsSystemEntry = true,
                        //                IrEntryType = IrEntryType.Normal
                        //            };
                        //            _guardLogDataProvider.SaveGuardLog(guardLog);
                        //            ClientSiteRadioChecksActivity.NotificationCreatedTime = guardLog.EventDateTime;
                        //            _guardLogDataProvider.UpdateRadioChecklistEntry(ClientSiteRadioChecksActivity);
                        //        }
                        //    }

                        //}
                    }
                    /* TO GIVE A WARNING TO THOSE WHO ARE DID NOT DO ANY ACTIVITY FOR 2 HOURS - end*/
                    }
                    /* LogoutTime time exits remove all the activity for that  */


                }


            /*Remove the Radio check status <2 hrs*/
            _guardLogDataProvider.RemoveClientSiteRadioChecksGreaterthanTwoHours();
            Process2();
            Process3();

        }

        public void Process2()
        {
            /* Check All Contracted Manning For the day   */
            /* Using the  contracted manning deatils find out the sites that don't have any login*/
            /* Insert the message */
            /* if any gurad login remove the message */
            _guardLogDataProvider.GetGuardManningDetails(DateTime.Now.DayOfWeek);
        }

        public void Process3()
        {
            /* remove the repeated login for a guard in different sites keep the latest one ,i.e. show a guard only one site */
            _guardLogDataProvider.RemoveGuardLoginFromdifferentSites();
        }

    }
}
