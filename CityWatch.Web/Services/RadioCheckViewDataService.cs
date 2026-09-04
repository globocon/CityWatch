using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Web.Services
{
    public interface IRadioCheckViewDataService
    {
        void ResetClientSiteActivityStatus();
        void UpdateLastActivityStatus();
        List<ClientSiteActivityStatusViewModel> GetClientSiteActivityStatuses(int[] clientSiteIds);
    }

    public class RadioCheckViewDataService : IRadioCheckViewDataService
    {
        private readonly IClientSiteActivityStatusDataProvider _clientSiteActivityDataProvider;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        private readonly IClientSiteActivityStatusDataProvider _clientSiteActivityStatusDataProvider;

        public RadioCheckViewDataService(IClientDataProvider clientDataProvider,
            IClientSiteActivityStatusDataProvider clientSiteActivityDataProvider,
            IGuardDataProvider guardDataProvider,
            IGuardLogDataProvider guardLogDataProvider,
            IClientSiteActivityStatusDataProvider clientSiteActivityStatusDataProvider)
        {
            _clientDataProvider = clientDataProvider;
            _clientSiteActivityDataProvider = clientSiteActivityDataProvider;
            _guardDataProvider = guardDataProvider;
            _guardLogDataProvider = guardLogDataProvider;
            _clientSiteActivityStatusDataProvider = clientSiteActivityStatusDataProvider;
        }

        public void ResetClientSiteActivityStatus()
        {
            ClearClientSiteActivityStatus();
            PopulateClientSiteActivityStatus();
            UpdateLastActivityStatus();
        }

        public void UpdateLastActivityStatus()
        {
            var activityStatuses = _clientSiteActivityDataProvider.GetClientSiteActivityStatus(Array.Empty<int>())
                                    .Where(z => z.GuardId.HasValue)
                                    .ToList();

            foreach (var activityStatus in activityStatuses)
            {
                var isActive = false;
                LastActivitySource? lastActiveSrc = null;
                DateTime? lastActiveAt = null;
                string lastActiveDescription = null;

                var latestGuardLog = _guardLogDataProvider.GetLatestGuardLog(activityStatus.ClientSiteId, activityStatus.GuardId.Value);
                if (latestGuardLog != null)
                {
                    isActive = (DateTime.Now - latestGuardLog.EventDateTime).TotalHours < 2;
                    lastActiveSrc = LastActivitySource.DailyGuardLog;
                    lastActiveAt = latestGuardLog?.EventDateTime;
                    lastActiveDescription = latestGuardLog?.Notes;
                }

                if (!isActive)                
                {
                    var latestRadioCheck = _clientSiteActivityDataProvider.GetLatestClientSiteRadioCheck(activityStatus.ClientSiteId, activityStatus.GuardId.Value);
                    if (latestRadioCheck != null && latestRadioCheck.CheckedAt >= DateTime.Today)
                    {
                        isActive = true;
                        lastActiveSrc = LastActivitySource.RadioCheck;
                        lastActiveAt = latestRadioCheck?.CheckedAt;
                        lastActiveDescription = latestRadioCheck?.Status;
                    }
                }

                activityStatus.LastActiveSrcId = lastActiveSrc;
                activityStatus.LastActiveAt = lastActiveAt;
                activityStatus.LastActiveDescription = lastActiveDescription;
                activityStatus.Status = isActive;
                _clientSiteActivityDataProvider.SaveClientSiteActivityStatus(activityStatus);
            }
        }

        public List<ClientSiteActivityStatusViewModel> GetClientSiteActivityStatuses(int[] clientSiteIds)
        {
            var clientSiteActivityStatuses = _clientSiteActivityStatusDataProvider.GetClientSiteActivityStatus(clientSiteIds);
            return clientSiteActivityStatuses.Select(z => new ClientSiteActivityStatusViewModel(z))
                .OrderBy(z => z, new ClientSiteActivityStatusViewModelComparer())
                .ToList();
        }

        private void ClearClientSiteActivityStatus()
        {
            _clientSiteActivityDataProvider.DeleteAllClientSiteActivityStatus();
        }

        private void PopulateClientSiteActivityStatus()
        {
            var siteIds = _clientDataProvider.GetClientSites(null).Where(z => z.ClientType.Name.StartsWith("VISY")).Select(z => z.Id);
            var guardLogins = _guardDataProvider.GetUniqueGuardLogins();
            var activityStatuses = new List<ClientSiteActivityStatus>();

            foreach (var siteId in siteIds)
            {
                var siteGuardLogins = guardLogins.Where(z => z.ClientSiteId == siteId);
                if (!siteGuardLogins.Any())
                {
                    activityStatuses.Add(new ClientSiteActivityStatus() { ClientSiteId = siteId });
                    continue;
                }

                activityStatuses.AddRange(siteGuardLogins.Select(z => new ClientSiteActivityStatus()
                {
                    ClientSiteId = siteId,
                    GuardId = z.GuardId
                }));
            }
            _clientSiteActivityDataProvider.SaveClientSiteActivityStatus(activityStatuses);
        }
    }
}
