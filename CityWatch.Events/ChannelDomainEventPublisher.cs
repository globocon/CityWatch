using System;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace CityWatch.Events
{
    /// <summary>
    /// Publisher backed by a bounded in-memory channel. Enqueues and returns.
    /// </summary>
    /// <remarks>
    /// The whole body is inside a try/catch that swallows everything. That looks careless and is
    /// deliberate: this method is called as the last statement of committed production workflows
    /// (an NFC scan that has already been saved, a patrol that has already started). If publishing
    /// could throw, adding a publish site would change the failure profile of code that works today.
    /// It cannot, so it does not.
    /// </remarks>
    public sealed class ChannelDomainEventPublisher : IDomainEventPublisher
    {
        private readonly ChannelWriter<IDomainEvent> _writer;
        private readonly DomainEventBusMetrics _metrics;
        private readonly ILogger<ChannelDomainEventPublisher> _logger;
        private readonly Func<DateTime> _utcNow;

        public ChannelDomainEventPublisher(
            ChannelWriter<IDomainEvent> writer,
            DomainEventBusMetrics metrics,
            ILogger<ChannelDomainEventPublisher> logger,
            Func<DateTime>? utcNow = null)
        {
            _writer = writer;
            _metrics = metrics;
            _logger = logger;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public void Publish(IDomainEvent domainEvent)
        {
            if (domainEvent == null)
                return;

            try
            {
                domainEvent.PublishedUtc = _utcNow();

                /* The channel is created with BoundedChannelFullMode.DropOldest, so TryWrite
                   succeeds even when full — it evicts the oldest event instead of refusing the
                   new one. A false return therefore means the channel is completed (shutdown),
                   not that we are overloaded. */
                if (_writer.TryWrite(domainEvent))
                {
                    _metrics.RecordPublished();
                }
                else
                {
                    _metrics.RecordDropped();
                }
            }
            catch (Exception ex)
            {
                /* Never propagate. The caller is a production workflow that has already committed
                   its own work and must not learn that the bus had a problem. */
                _metrics.RecordDropped();
                try
                {
                    _logger.LogError(ex, "Domain event publish failed for {EventType}. Event dropped.",
                        domainEvent.GetType().Name);
                }
                catch
                {
                    // Even logging is not allowed to escape.
                }
            }
        }
    }
}
