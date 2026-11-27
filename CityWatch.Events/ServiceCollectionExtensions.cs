using System;
using System.Linq;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CityWatch.Events
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Makes <see cref="IDomainEventPublisher"/> resolvable. Safe to call unconditionally
        /// and safe to call more than once.
        /// </summary>
        /// <remarks>
        /// On its own this registers the no-op publisher, so an application that calls it and
        /// nothing else behaves exactly as it did before. The real bus is activated only when
        /// something actually subscribes, via <see cref="AddDomainEventHandler{TEvent, THandler}"/>.
        ///
        /// That ordering is the point: adding publish sites to production code is not the same
        /// decision as turning delivery on, and the two must be separately reversible.
        /// </remarks>
        public static IServiceCollection AddDomainEvents(
            this IServiceCollection services, Action<DomainEventBusOptions>? configure = null)
        {
            var options = new DomainEventBusOptions();
            configure?.Invoke(options);

            services.TryAddSingleton(options);
            services.TryAddSingleton<DomainEventBusMetrics>();
            services.TryAddSingleton<IDomainEventPublisher>(_ => NullDomainEventPublisher.Instance);

            return services;
        }

        /// <summary>
        /// Subscribes a handler to an event, activating the real bus on first use.
        /// </summary>
        public static IServiceCollection AddDomainEventHandler<TEvent, THandler>(this IServiceCollection services)
            where TEvent : IDomainEvent
            where THandler : class, IDomainEventHandler<TEvent>
        {
            services.AddDomainEvents();
            ActivateBus(services);
            services.AddScoped<IDomainEventHandler<TEvent>, THandler>();
            return services;
        }

        /// <summary>
        /// Swaps the no-op publisher for the channel-backed one and starts the dispatcher.
        /// Idempotent — repeated calls from several feature packs leave one bus.
        /// </summary>
        private static void ActivateBus(IServiceCollection services)
        {
            var alreadyActive = services.Any(d => d.ServiceType == typeof(Channel<IDomainEvent>));
            if (alreadyActive)
                return;

            services.AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<DomainEventBusOptions>();
                return Channel.CreateBounded<IDomainEvent>(new BoundedChannelOptions(options.Capacity)
                {
                    /* Drop the oldest rather than block the writer. Publish is called from
                       committed production workflows; it must return in constant time no matter
                       how badly a subscriber is behaving. Losing an old event is acceptable,
                       stalling an NFC scan is not. */
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleReader = true,
                    SingleWriter = false
                });
            });

            services.AddSingleton(sp => sp.GetRequiredService<Channel<IDomainEvent>>().Writer);
            services.AddSingleton(sp => sp.GetRequiredService<Channel<IDomainEvent>>().Reader);

            services.Replace(ServiceDescriptor.Singleton<IDomainEventPublisher>(sp =>
                new ChannelDomainEventPublisher(
                    sp.GetRequiredService<ChannelWriter<IDomainEvent>>(),
                    sp.GetRequiredService<DomainEventBusMetrics>(),
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ChannelDomainEventPublisher>>())));

            services.AddHostedService(sp => new DomainEventDispatcher(
                sp.GetRequiredService<ChannelReader<IDomainEvent>>(),
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<DomainEventBusOptions>(),
                sp.GetRequiredService<DomainEventBusMetrics>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DomainEventDispatcher>>()));
        }
    }
}
