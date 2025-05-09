using CityWatch.Common.Models;
using CityWatch.Data.Models;
using CityWatch.Data.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace CityWatch.Data.Providers
{
    public interface ISiteEventLogDataProvider
    {
        void SaveSiteEventLogData(SiteEventLog siteEventLog);
    }
    public class SiteEventLogDataProvider : ISiteEventLogDataProvider
    {
        private readonly CityWatchDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;
        private readonly ISignalRNotificationService _signalRNotificationService;


        public SiteEventLogDataProvider(IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration,
            ISignalRNotificationService signalRNotificationService,
            CityWatchDbContext context)
        {
            _context = context;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
            _signalRNotificationService = signalRNotificationService;
        }
        public async void SaveSiteEventLogData(SiteEventLog siteEventLog)
        {
            if (siteEventLog == null)
                throw new ArgumentNullException();

            if (siteEventLog.Id <=0)
            {
                _context.SiteEventLog.Add(siteEventLog);
            }
          
            _context.SaveChanges();            
            SendDuressAlertToControlRoom();
        }

        public void SendDuressAlertToControlRoom()
        {
            // To broadcast without a message
             _signalRNotificationService.BroadcastDuressAlarmNotification();       
        }

    }
}
