using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using CityWatch.Data;
using Microsoft.EntityFrameworkCore;

namespace CityWatch.Web.Services
{
    public class ShiftCancellationEmailBackgroundService : BackgroundService
    {
        private readonly ILogger<ShiftCancellationEmailBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        
        // TEMPORARY FOR TESTING: Set to 1 minute. (Default was 15 minutes period and 60 minutes wait)
        private readonly TimeSpan _period = TimeSpan.FromMinutes(1);
        private readonly TimeSpan _waitBeforeSend = TimeSpan.FromMinutes(1);

        public ShiftCancellationEmailBackgroundService(
            ILogger<ShiftCancellationEmailBackgroundService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(_period);
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await ProcessEmailQueueAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing shift cancellation email queue.");
                }
            }
        }

        private async Task ProcessEmailQueueAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CityWatchDbContext>();
            var alertEmailServices = scope.ServiceProvider.GetRequiredService<IAlertEmailServices>();

            var pendingCancellations = await context.ShiftCancellationEmailQueues
                .Include(q => q.Guard)
                .Include(q => q.ClientSite)
                .Where(q => !q.IsProcessed)
                .ToListAsync();

            if (!pendingCancellations.Any())
                return;

            // Group by Guard and Source, then only process if the latest cancellation is older than _waitBeforeSend
            var grouped = pendingCancellations.GroupBy(q => new { q.GuardId, q.Source });

            var now = DateTime.Now;

            foreach (var group in grouped)
            {
                var latestCreatedAt = group.Max(q => q.CreatedAt);
                
                // If they are still modifying shifts (latest cancellation is too new), skip for now
                if (now - latestCreatedAt < _waitBeforeSend)
                {
                    continue;
                }

                var itemsToProcess = group.ToList();
                var guard = itemsToProcess.First().Guard;
                string licenseNo = guard?.SecurityNo ?? "N/A";
                string cancelledBy = itemsToProcess.First().CancelledBy;
                string source = itemsToProcess.First().Source;

                try
                {
                    bool sent = await alertEmailServices.SendAggregatedShiftCancelledAlertMail(
                        guard, licenseNo, cancelledBy, source, itemsToProcess);

                    if (sent)
                    {
                        foreach (var item in itemsToProcess)
                        {
                            item.IsProcessed = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send aggregated email for GuardId {group.Key.GuardId}, Source: {group.Key.Source}");
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
