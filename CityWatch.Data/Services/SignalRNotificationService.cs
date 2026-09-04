using CityWatch.Common.Models;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace CityWatch.Data.Services
{
    public interface ISignalRNotificationService
    {
        void BroadcastDuressAlarmNotification();
        Task BroadcastDuressAlarmNotificationWithMessage(string message);
    }
    public class SignalRNotificationService : ISignalRNotificationService
    {
        private readonly IHubContext<UpdateHub> _hubContext;

        public SignalRNotificationService(IHubContext<UpdateHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public void BroadcastDuressAlarmNotification()
        {
            // Directly invoking the SendUpdate method
            //await _hubContext.Clients.All.SendAsync("ReceiveDuressAlarmAlert");
            _hubContext.Clients.All.SendAsync("ReceiveDuressAlarmAlert");

        }

        public async Task BroadcastDuressAlarmNotificationWithMessage(string message)
        {
            // Invoking the SendUpdate with message method
            await _hubContext.Clients.All.SendAsync("ReceiveDuressAlarmAlert", message);
        }
    }
}
