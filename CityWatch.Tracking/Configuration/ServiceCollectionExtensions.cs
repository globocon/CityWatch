using CityWatch.Tracking.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CityWatch.Tracking.Configuration
{
    /// <summary>
    /// The feature pack's entire integration surface with the host applications:
    /// one call in ConfigureServices, one call after the existing hub mappings.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the tracking feature pack. When <c>Tracking:Enabled</c> is false —
        /// the default, including when the section is absent entirely — this registers the
        /// options object and nothing else. No DbContext, no hosted services, no allocations
        /// on any request path. Disabled is indistinguishable from not deployed (§3.2).
        /// </summary>
        public static IServiceCollection AddCityWatchTracking(this IServiceCollection services, IConfiguration configuration)
        {
            var options = configuration.GetSection(TrackingOptions.SectionName).Get<TrackingOptions>()
                          ?? new TrackingOptions();
            services.AddSingleton(options);

            if (!options.Enabled)
                return services;                       // ← the registration-time branch

            services.AddSingleton<ILiveStateStore, InMemoryLiveStateStore>();

            /* M1.3+: TrackingDbContext, ingest pipeline, hosted services, hub, controller
               are registered here as each milestone lands. Everything stays behind this
               same branch so Level-1 rollback always covers the whole pack. */

            return services;
        }

        /// <summary>
        /// Maps the feature pack's endpoints and hub. A no-op when disabled, so the host
        /// application's routing table is byte-identical to today (RT2).
        /// </summary>
        public static WebApplication MapCityWatchTracking(this WebApplication app)
        {
            var options = app.Services.GetService<TrackingOptions>();
            if (options is not { Enabled: true })
                return app;

            /* M1.4 maps the ingest controller; M1.7 maps the hub. */

            return app;
        }
    }
}
