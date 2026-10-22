using CityWatch.Common.Models;
using CityWatch.Data.Models;
using CityWatch.Data.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<SiteEventLogDataProvider> _logger;


        public SiteEventLogDataProvider(IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration,
            ISignalRNotificationService signalRNotificationService,
            CityWatchDbContext context,
            ILogger<SiteEventLogDataProvider> logger)
        {
            _context = context;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
            _signalRNotificationService = signalRNotificationService;
            _logger = logger;
        }

        /* Every call site fires this without awaiting or catching, so it must never throw.
           It was 'async void': a failure (e.g. the SaveChanges FK violation on ClientSites)
           was rethrown on a ThreadPool thread where nothing could catch it, killing w3wp and
           recycling the app pool. Event logging must not be able to take the site down. */
        public void SaveSiteEventLogData(SiteEventLog siteEventLog)
        {
            if (siteEventLog == null)
            {
                _logger.LogWarning("SaveSiteEventLogData called with a null siteEventLog; ignoring.");
                return;
            }

            var entityWasAdded = false;

            try
            {
                if (siteEventLog.Id <= 0)
                {
                    _context.SiteEventLog.Add(siteEventLog);
                    entityWasAdded = true;
                }

                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                /* SaveChanges() flushes every pending change on this scoped context, so a
                   failed entity left tracked would be retried - and fail again - by the next
                   save from an unrelated module. Detach only the row we added. */
                if (entityWasAdded)
                {
                    try { _context.Entry(siteEventLog).State = EntityState.Detached; }
                    catch (Exception detachEx)
                    {
                        _logger.LogWarning(detachEx, "Could not detach the failed SiteEventLog entity.");
                    }
                }

                _logger.LogError(ex, "Failed to save site event log (SiteId={SiteId}, GuardId={GuardId}, Module={Module}/{SubModule}).",
                    siteEventLog.SiteId, siteEventLog.GuardId, siteEventLog.Module, siteEventLog.SubModule);

                // As before, the duress broadcast only runs when the save succeeded.
                return;
            }

            try
            {
                SendDuressAlertToControlRoom();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast the duress alarm notification.");
            }
        }

        public void SendDuressAlertToControlRoom()
        {
            // To broadcast without a message
             _signalRNotificationService.BroadcastDuressAlarmNotification();       
        }

    }
}
