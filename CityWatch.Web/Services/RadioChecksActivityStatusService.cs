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
                        _guardLogDataProvider.DeleteClientSiteRadioChecksActivity(ClientSiteRadioChecksActivity);
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
