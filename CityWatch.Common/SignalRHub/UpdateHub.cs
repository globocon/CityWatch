using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace CityWatch.Common.Models
{
    public class UpdateHub : Hub
    {
        public async Task SendUpdateWithMessage(string message)
        {
            await Clients.All.SendAsync("ReceiveDuressAlarmAlert", message);
        }

        public async Task SendUpdate()
        {
            await Clients.All.SendAsync("ReceiveDuressAlarmAlert");
        }



        #region "MobileApplication"

        public Task JoinGroup(int SiteId)
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, SiteId.ToString());
        }

        public Task UpdateCrowdControlCountToMobileSiteGroup(int SiteId, int count, bool AddCount)
        {

            return Clients.Group(SiteId.ToString()).SendAsync("UpdateCrowdControl", )

        }

        #endregion "MobileApplication"
    }
}