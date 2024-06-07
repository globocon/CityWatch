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
    }
}