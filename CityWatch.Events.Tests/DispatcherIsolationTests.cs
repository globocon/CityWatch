using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CityWatch.Events;
using CityWatch.Events.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CityWatch.Events.Tests
{
    /// <summary>
    /// RT5/RT6 from the architecture document: a subscriber that throws or hangs is invisible to
    /// the publisher and to every other subscriber. These are the properties that make it
    /// acceptable to publish from inside stable production workflows.
    /// </summary>
    [TestClass]
    public class DispatcherIsolationTests
    {
        private sealed class RecordingHandler : IDomainEventHandler<OfficerLoggedOut>
        {
            public static readonly List<Guid> Seen = new();

            public Task HandleAsync(OfficerLoggedOut domainEvent, CancellationToken cancellationToken)
            {
                lock (Seen) Seen.Add(domainEvent.EventId);
                return Task.CompletedTask;
            }
        }

        private sealed class ThrowingHandler : IDomainEventHandler<OfficerLoggedOut>
        {
            public Task HandleAsync(OfficerLoggedOut domainEvent, CancellationToken cancellationToken)
                => throw new InvalidOperationException("This handler always fails.");
        }

        private sealed class SynchronousThrowingHandler : IDomainEventHandler<OfficerLoggedOut>
        {
            // Not async: throws before ever returning a Task, exercising the reflection unwrap path.
            public Task HandleAsync(OfficerLoggedOut domainEvent, CancellationToken cancellationToken)
                => throw new ApplicationException("Synchronous failure.");
        }

        private static (DomainEventDispatcher dispatcher, DomainEventBusMetrics metrics) BuildDispatcher(
            IServiceCollection services, DomainEventBusOptions options)
        {
            var provider = services.BuildServiceProvider();
            var metrics = new DomainEventBusMetrics();
            var channel = Channel.CreateBounded<IDomainEvent>(10);
            var dispatcher = new DomainEventDispatcher(
                channel.Reader,
                provider.GetRequiredService<IServiceScopeFactory>(),
                options,
                metrics,
                NullLogger<DomainEventDispatcher>.Instance);
            return (dispatcher, metrics);
        }

        [TestInitialize]
        public void Reset()
        {
            lock (RecordingHandler.Seen) RecordingHandler.Seen.Clear();
        }

        [TestMethod]
        public async Task ThrowingHandler_DoesNotPreventOtherHandlers()
        {
            var services = new ServiceCollection();
            services.AddScoped<IDomainEventHandler<OfficerLoggedOut>, ThrowingHandler>();
            services.AddScoped<IDomainEventHandler<OfficerLoggedOut>, RecordingHandler>();
            var (dispatcher, metrics) = BuildDispatcher(services, new DomainEventBusOptions());

            var evt = new OfficerLoggedOut(1, 2, DateTime.UtcNow);
            await dispatcher.DispatchAsync(evt, CancellationToken.None);

            Assert.AreEqual(1, RecordingHandler.Seen.Count, "The healthy handler must still run.");
            Assert.AreEqual(evt.EventId, RecordingHandler.Seen[0]);
            Assert.AreEqual(1, metrics.HandlerFailures);
            Assert.AreEqual(1, metrics.Handled);
        }

        [TestMethod]
        public async Task SynchronouslyThrowingHandler_IsCaughtAndCounted()
        {
            var services = new ServiceCollection();
            services.AddScoped<IDomainEventHandler<OfficerLoggedOut>, SynchronousThrowingHandler>();
            var (dispatcher, metrics) = BuildDispatcher(services, new DomainEventBusOptions());

            await dispatcher.DispatchAsync(new OfficerLoggedOut(1, null, DateTime.UtcNow), CancellationToken.None);

            Assert.AreEqual(1, metrics.HandlerFailures);
            Assert.AreEqual(0, metrics.Handled);
        }

        [TestMethod]
        public async Task HangingHandler_TimesOut_AndDoesNotBlockDispatch()
        {
            var services = new ServiceCollection();
            services.AddScoped<IDomainEventHandler<OfficerLoggedOut>>(_ => new HangingHandler());
            services.AddScoped<IDomainEventHandler<OfficerLoggedOut>, RecordingHandler>();
            var options = new DomainEventBusOptions { HandlerTimeoutSeconds = 1 };
            var (dispatcher, metrics) = BuildDispatcher(services, options);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            await dispatcher.DispatchAsync(new OfficerLoggedOut(1, 2, DateTime.UtcNow), CancellationToken.None);
            sw.Stop();

            Assert.AreEqual(1, metrics.HandlerTimeouts, "The hanging handler must be abandoned.");
            Assert.AreEqual(1, RecordingHandler.Seen.Count, "The healthy handler must still run afterwards.");
            Assert.IsTrue(sw.Elapsed < TimeSpan.FromSeconds(10),
                $"Dispatch took {sw.Elapsed}; the timeout must bound a wedged handler.");
        }

        private sealed class HangingHandler : IDomainEventHandler<OfficerLoggedOut>
        {
            public async Task HandleAsync(OfficerLoggedOut domainEvent, CancellationToken cancellationToken)
            {
                // Respects cancellation — the dispatcher's linked token cancels this.
                await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
            }
        }

        [TestMethod]
        public async Task EventWithNoHandlers_IsANoOp()
        {
            var services = new ServiceCollection();
            var (dispatcher, metrics) = BuildDispatcher(services, new DomainEventBusOptions());

            await dispatcher.DispatchAsync(
                new NfcCheckpointScanned(1, "04A2", 10, 5, null, "-33.86,151.20", DateTime.UtcNow, 1, false),
                CancellationToken.None);

            Assert.AreEqual(0, metrics.Handled);
            Assert.AreEqual(0, metrics.HandlerFailures);
        }

        [TestMethod]
        public async Task HandlersResolveInTheirOwnScope_PerEvent()
        {
            var services = new ServiceCollection();
            services.AddScoped<ScopeProbe>();
            services.AddScoped<IDomainEventHandler<OfficerLoggedOut>, ScopeProbeHandler>();
            var (dispatcher, _) = BuildDispatcher(services, new DomainEventBusOptions());

            await dispatcher.DispatchAsync(new OfficerLoggedOut(1, null, DateTime.UtcNow), CancellationToken.None);
            await dispatcher.DispatchAsync(new OfficerLoggedOut(2, null, DateTime.UtcNow), CancellationToken.None);

            Assert.AreEqual(2, ScopeProbe.InstancesCreated,
                "Each event must get a fresh DI scope, so scoped services (e.g. a DbContext) are per-event.");
        }

        private sealed class ScopeProbe
        {
            public static int InstancesCreated;
            public ScopeProbe() => Interlocked.Increment(ref InstancesCreated);
        }

        private sealed class ScopeProbeHandler : IDomainEventHandler<OfficerLoggedOut>
        {
            public ScopeProbeHandler(ScopeProbe probe) { }
            public Task HandleAsync(OfficerLoggedOut e, CancellationToken ct) => Task.CompletedTask;
        }
    }
}
