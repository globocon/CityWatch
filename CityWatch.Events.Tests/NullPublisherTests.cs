using System;
using CityWatch.Events;
using CityWatch.Events.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CityWatch.Events.Tests
{
    /// <summary>
    /// RT3 from the architecture document: when nothing subscribes, the registered publisher is
    /// the no-op, and publishing through it does nothing observable. This is the property that
    /// makes publish sites in production code safe to ship ahead of any subscriber.
    /// </summary>
    [TestClass]
    public class NullPublisherTests
    {
        [TestMethod]
        public void AddDomainEvents_WithNoSubscribers_ResolvesNullPublisher()
        {
            var services = new ServiceCollection();
            services.AddDomainEvents();
            using var provider = services.BuildServiceProvider();

            var publisher = provider.GetRequiredService<IDomainEventPublisher>();

            Assert.IsInstanceOfType(publisher, typeof(NullDomainEventPublisher));
        }

        [TestMethod]
        public void AddDomainEvents_WithNoSubscribers_RegistersNoHostedService()
        {
            var services = new ServiceCollection();
            services.AddDomainEvents();
            using var provider = services.BuildServiceProvider();

            var hosted = provider.GetServices<Microsoft.Extensions.Hosting.IHostedService>();

            Assert.AreEqual(0, System.Linq.Enumerable.Count(hosted),
                "No subscribers means no dispatcher: the bus must cost nothing when unused.");
        }

        [TestMethod]
        public void NullPublisher_Publish_DoesNotThrow_EvenForNull()
        {
            var publisher = NullDomainEventPublisher.Instance;

            publisher.Publish(null!);
            publisher.Publish(new OfficerLoggedOut(guardId: 1, smartWandId: 2, occurredUtc: DateTime.UtcNow));
        }

        [TestMethod]
        public void AddDomainEvents_CalledTwice_IsIdempotent()
        {
            var services = new ServiceCollection();
            services.AddDomainEvents();
            services.AddDomainEvents(o => o.Capacity = 42);
            using var provider = services.BuildServiceProvider();

            // First registration wins (TryAdd semantics); no duplicate registrations.
            var options = provider.GetRequiredService<DomainEventBusOptions>();
            Assert.AreEqual(10_000, options.Capacity);
            Assert.AreEqual(1, System.Linq.Enumerable.Count(provider.GetServices<IDomainEventPublisher>()));
        }
    }
}
