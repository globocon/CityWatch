using System.Threading;
using System.Threading.Tasks;
using CityWatch.Events;
using CityWatch.Events.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CityWatch.Events.Tests
{
    /// <summary>
    /// RT4 — the load-bearing test for the whole feature pack.
    ///
    /// The 5 edited production classes (GuardDataProvider, GuardLogDataProvider,
    /// MobileAppDataServices, GuardSecurityNumberController, …) take the publisher as an
    /// OPTIONAL constructor parameter defaulting to null. With Tracking:Enabled=false the
    /// publisher is never registered in DI at all.
    ///
    /// If the container did not honour optional parameters, every one of those services would
    /// fail to construct and the entire portal would break the moment this branch deployed —
    /// with the flag OFF. These tests pin that behaviour so it can never regress silently.
    /// </summary>
    [TestClass]
    public class OptionalInjectionTests
    {
        /// <summary>Mirrors the shape of the edited production providers.</summary>
        private sealed class ProviderLikeService
        {
            public readonly IDomainEventPublisher Events;

            public ProviderLikeService(IDomainEventPublisher events = null!)
                => Events = events ?? NullDomainEventPublisher.Instance;
        }

        private interface IProviderLike { }

        private sealed class InterfaceRegisteredService : IProviderLike
        {
            public readonly IDomainEventPublisher Events;

            public InterfaceRegisteredService(IDomainEventPublisher events = null!)
                => Events = events ?? NullDomainEventPublisher.Instance;
        }

        [TestMethod]
        public void PublisherNotRegistered_ServiceStillResolves_AndFallsBackToNullPublisher()
        {
            var services = new ServiceCollection();
            services.AddScoped<ProviderLikeService>();          // note: NO AddDomainEvents()
            using var provider = services.BuildServiceProvider();

            var resolved = provider.GetRequiredService<ProviderLikeService>();

            Assert.IsNotNull(resolved, "Flag off ⇒ publisher absent ⇒ the provider must STILL construct.");
            Assert.IsInstanceOfType(resolved.Events, typeof(NullDomainEventPublisher));
        }

        [TestMethod]
        public void PublisherNotRegistered_InterfaceRegistration_AlsoResolves()
        {
            // The real registrations are AddScoped<IFoo, Foo>() — verify that path too.
            var services = new ServiceCollection();
            services.AddScoped<IProviderLike, InterfaceRegisteredService>();
            using var provider = services.BuildServiceProvider();

            var resolved = (InterfaceRegisteredService)provider.GetRequiredService<IProviderLike>();

            Assert.IsInstanceOfType(resolved.Events, typeof(NullDomainEventPublisher));
        }

        [TestMethod]
        public void PublisherNotRegistered_ControllerStyleActivation_AlsoResolves()
        {
            // MVC activates controllers via ActivatorUtilities, not the container directly.
            var services = new ServiceCollection();
            using var provider = services.BuildServiceProvider();

            var resolved = ActivatorUtilities.CreateInstance<ProviderLikeService>(provider);

            Assert.IsInstanceOfType(resolved.Events, typeof(NullDomainEventPublisher));
        }

        [TestMethod]
        public void PublishingThroughTheNullPublisher_IsHarmless()
        {
            var services = new ServiceCollection();
            services.AddScoped<ProviderLikeService>();
            using var provider = services.BuildServiceProvider();
            var resolved = provider.GetRequiredService<ProviderLikeService>();

            // Exactly what the edited production lines do, with the flag off.
            resolved.Events.Publish(new OfficerLoggedOut(1, 2, System.DateTime.UtcNow));
            resolved.Events.Publish(null!);
        }

        [TestMethod]
        public void PublisherRegistered_IsInjectedNormally()
        {
            // Flag on: the real publisher must actually reach the production classes.
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDomainEventHandler<OfficerLoggedOut, NoopHandler>();
            services.AddScoped<ProviderLikeService>();
            using var provider = services.BuildServiceProvider();

            var resolved = provider.GetRequiredService<ProviderLikeService>();

            Assert.IsInstanceOfType(resolved.Events, typeof(ChannelDomainEventPublisher),
                "Flag on ⇒ the same constructor receives the real bus, no code change required.");
        }

        private sealed class NoopHandler : IDomainEventHandler<OfficerLoggedOut>
        {
            public Task HandleAsync(OfficerLoggedOut domainEvent, CancellationToken ct) => Task.CompletedTask;
        }
    }
}
