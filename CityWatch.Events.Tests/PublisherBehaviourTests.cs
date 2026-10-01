using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using CityWatch.Events;
using CityWatch.Events.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CityWatch.Events.Tests
{
    [TestClass]
    public class PublisherBehaviourTests
    {
        private static ChannelDomainEventPublisher BuildPublisher(
            Channel<IDomainEvent> channel, DomainEventBusMetrics metrics, Func<DateTime>? clock = null)
            => new(channel.Writer, metrics, NullLogger<ChannelDomainEventPublisher>.Instance, clock);

        [TestMethod]
        public void Publish_StampsPublishedUtc_FromTheServerClock()
        {
            var channel = Channel.CreateBounded<IDomainEvent>(4);
            var metrics = new DomainEventBusMetrics();
            var fixedNow = new DateTime(2026, 8, 7, 4, 0, 0, DateTimeKind.Utc);
            var publisher = BuildPublisher(channel, metrics, () => fixedNow);

            var deviceTime = fixedNow.AddMinutes(-42); // device clock lags: the offline-scan case
            var evt = new OfficerLoggedIn(1, 2, 3, "dev-1", deviceTime);
            publisher.Publish(evt);

            Assert.AreEqual(deviceTime, evt.OccurredUtc, "Device time must be preserved.");
            Assert.AreEqual(fixedNow, evt.PublishedUtc, "Server time must be stamped at publish.");
            Assert.AreEqual(1, metrics.Published);
        }

        [TestMethod]
        public void Publish_WhenChannelFull_DropsOldest_NeverBlocks()
        {
            var channel = Channel.CreateBounded<IDomainEvent>(new BoundedChannelOptions(2)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });
            var metrics = new DomainEventBusMetrics();
            var publisher = BuildPublisher(channel, metrics);

            var first = new OfficerLoggedOut(1, null, DateTime.UtcNow);
            var second = new OfficerLoggedOut(2, null, DateTime.UtcNow);
            var third = new OfficerLoggedOut(3, null, DateTime.UtcNow);

            publisher.Publish(first);
            publisher.Publish(second);
            publisher.Publish(third); // capacity 2: evicts `first`

            Assert.AreEqual(3, metrics.Published, "The writer must never observe back-pressure.");
            Assert.IsTrue(channel.Reader.TryRead(out var a));
            Assert.IsTrue(channel.Reader.TryRead(out var b));
            Assert.AreEqual(second.EventId, a!.EventId, "Oldest event is the one sacrificed.");
            Assert.AreEqual(third.EventId, b!.EventId);
        }

        [TestMethod]
        public void Publish_AfterChannelCompleted_CountsDrop_DoesNotThrow()
        {
            var channel = Channel.CreateBounded<IDomainEvent>(4);
            var metrics = new DomainEventBusMetrics();
            var publisher = BuildPublisher(channel, metrics);
            channel.Writer.Complete(); // simulates shutdown racing a late publish

            publisher.Publish(new OfficerLoggedOut(1, null, DateTime.UtcNow));

            Assert.AreEqual(0, metrics.Published);
            Assert.AreEqual(1, metrics.Dropped);
        }

        [TestMethod]
        public void Publish_NullEvent_IsIgnored()
        {
            var channel = Channel.CreateBounded<IDomainEvent>(4);
            var metrics = new DomainEventBusMetrics();
            var publisher = BuildPublisher(channel, metrics);

            publisher.Publish(null!);

            Assert.AreEqual(0, metrics.Published);
            Assert.AreEqual(0, metrics.Dropped);
        }

        [TestMethod]
        public async Task EndToEnd_SubscriberReceivesEvent_ThroughRealRegistration()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDomainEventHandler<NfcCheckpointScanned, CapturingHandler>();
            using var provider = services.BuildServiceProvider();

            // Registration swapped the null publisher for the channel-backed one.
            var publisher = provider.GetRequiredService<IDomainEventPublisher>();
            Assert.IsInstanceOfType(publisher, typeof(ChannelDomainEventPublisher));

            // Run the dispatcher pump manually against the same channel.
            var dispatcher = new DomainEventDispatcher(
                provider.GetRequiredService<ChannelReader<IDomainEvent>>(),
                provider.GetRequiredService<IServiceScopeFactory>(),
                provider.GetRequiredService<DomainEventBusOptions>(),
                provider.GetRequiredService<DomainEventBusMetrics>(),
                NullLogger<DomainEventDispatcher>.Instance);

            var evt = new NfcCheckpointScanned(
                smartWandId: 7, tagUid: "04A2B1", clientSiteId: 12, guardId: 55, loginUserId: null,
                gpsCoordinates: "-33.865143,151.209900", occurredUtc: DateTime.UtcNow,
                scanningType: 1, isOfflineRecord: false);

            publisher.Publish(evt);

            var reader = provider.GetRequiredService<ChannelReader<IDomainEvent>>();
            Assert.IsTrue(reader.TryRead(out var queued), "Event must be on the channel.");
            await dispatcher.DispatchAsync(queued!, default);

            Assert.IsNotNull(CapturingHandler.Last);
            Assert.AreEqual(evt.EventId, CapturingHandler.Last!.EventId);
            Assert.AreEqual("04A2B1", CapturingHandler.Last.TagUid);
        }

        private sealed class CapturingHandler : IDomainEventHandler<NfcCheckpointScanned>
        {
            public static NfcCheckpointScanned? Last;

            public Task HandleAsync(NfcCheckpointScanned domainEvent, System.Threading.CancellationToken ct)
            {
                Last = domainEvent;
                return Task.CompletedTask;
            }
        }
    }
}
