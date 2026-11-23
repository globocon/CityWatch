using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CityWatch.Data.Services
{
    public class MobileAppSignalRHub : Hub
    {
        private readonly IClientDataProvider _clientDataProvider;

        public MobileAppSignalRHub(IClientDataProvider clientDataProvider)
        {
            _clientDataProvider = clientDataProvider;
        }

        #region "SignalRHubCommon"

        public override async Task OnConnectedAsync()
        {
            //await Groups.AddToGroupAsync(Context.ConnectionId, "SignalR Users");
            await base.OnConnectedAsync();
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine(exception);
            Debug.WriteLine(exception);
            await base.OnDisconnectedAsync(exception);
        }
        public async Task<string> JoinGroup(MobileCrowdControlGuard JoinGaurd)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, JoinGaurd.ClientSiteId.ToString());
            return "Joined successfully";
        }

        /// <summary>
        /// Patrol Randomisation: the handset joins this once logged in to a site, and receives
        /// "PatrolRandomisationNextCheckpoint" (a PatrolRandomisationInstruction) whenever the
        /// server hands this guard a new checkpoint. Per guard and site so one guard's route is
        /// never sent to another guard's phone.
        /// </summary>
        public async Task<string> JoinPatrolRandomisationGroup(int clientSiteId, int guardId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, PatrolRandomisationGroupName(clientSiteId, guardId));
            return "Joined successfully";
        }

        public async Task LeavePatrolRandomisationGroup(int clientSiteId, int guardId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, PatrolRandomisationGroupName(clientSiteId, guardId));
        }

        public static string PatrolRandomisationGroupName(int clientSiteId, int guardId) => $"PatrolRandomisation_{clientSiteId}_{guardId}";
        #endregion "SignalRHubCommon"

        #region "MobileCrowdControl"       

        //public Task<ClientSiteMobileCrowdControl> JoinGroup(MobileCrowdControlGuard JoinGaurd)
        //{
        //    Groups.AddToGroupAsync(Context.ConnectionId, JoinGaurd.ClientSiteId.ToString());
        //    var currentCount = _clientDataProvider.GetCrowdControlCount(JoinGaurd);
        //    return currentCount;
        //}

        public async Task<ClientSiteMobileCrowdControl> GetCurrentCrowdControlData(MobileCrowdControlGuard JoinGaurd)
        {
            var currentCount = await _clientDataProvider.GetCrowdControlCount(JoinGaurd);
            return currentCount;
        }

        public async Task UpdateCCCToMobileSiteGroup(ClientSiteMobileCrowdControlData CountData)
        {
            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, CountData.ClientSiteId.ToString());
                var currentCount = await _clientDataProvider.UpdateCrowdControlCount(CountData);
                Clients.Group(CountData.ClientSiteId.ToString()).SendAsync("UpdateCrowdControl", currentCount);
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        public async Task ResetSiteCrowdControlCount(MobileCrowdControlGuard JoinGaurd)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, JoinGaurd.ClientSiteId.ToString());
            var currentCount = await _clientDataProvider.ResetSiteCrowdControlCount(JoinGaurd);
            Clients.Group(JoinGaurd.ClientSiteId.ToString()).SendAsync("ResetSiteCrowdControlCount", currentCount);
            return;
        }

        public async Task ResetGuardCrowdControlCount(MobileCrowdControlGuard JoinGaurd)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, JoinGaurd.ClientSiteId.ToString());
            var currentCount = await _clientDataProvider.ResetGuardCrowdControlCount(JoinGaurd);
            Clients.Group(JoinGaurd.ClientSiteId.ToString()).SendAsync("ResetGuardCrowdControlCount", currentCount);
            return;
        }

        #endregion "MobileCrowdControl"
       
    }
}