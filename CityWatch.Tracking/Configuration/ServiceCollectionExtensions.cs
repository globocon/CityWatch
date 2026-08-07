using System.Threading.Channels;
using CityWatch.Events;
using CityWatch.Tracking.Data;
using CityWatch.Tracking.Data.Entities;
using CityWatch.Tracking.Hosted;
using CityWatch.Tracking.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

            /* Same physical database as the platform, separate context (D1, §3.3).
               Schema comes from DbScript 360–362, never from migrations.
               NoTracking by default: this context reads history and small admin rows;
               the hot write path bypasses it entirely via SqlBulkCopy. */
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<TrackingDbContext>(o => o
                .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure())
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

            /* ---- Ingest pipeline (§7.3) ----
               A bounded channel decouples HTTP ingest from storage. DropOldest: if the
               writer falls behind, old points are sacrificed and counted, and the publisher
               never blocks. At 500-vehicle scale the channel holds ~5 minutes of traffic. */
            var channel = Channel.CreateBounded<TrackPoint>(new BoundedChannelOptions(50_000)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });
            services.AddSingleton(channel.Writer);
            services.AddSingleton(channel.Reader);

            services.AddSingleton<UnitRateLimiter>();
            services.AddScoped<IIngestService, IngestService>();
            services.AddScoped<ISessionService, SessionService>();
            services.AddScoped<ILiveSnapshotService, LiveSnapshotService>();
            services.AddSignalR();

            services.AddHostedService<Hosted.BroadcastTicker>();

            services.AddHostedService(sp => new PositionWriter(
                sp.GetRequiredService<System.Threading.Channels.ChannelReader<TrackPoint>>(),
                connectionString!,
                sp.GetRequiredService<ILogger<PositionWriter>>()));

            /* The controller lives in this assembly; adding the application part only when
               enabled is what makes every /api/tracking route a 404 when disabled (RT2).
               The host's existing MapControllerRoute maps attribute-routed controllers. */
            services.AddControllers().AddApplicationPart(typeof(Api.TrackingController).Assembly);

            /* ---- Event subscriptions (§20.3) ----
               Registering the first handler is what swaps NullDomainEventPublisher for the
               real bus and starts the dispatcher. With the flag off, none of this runs and
               every publish site in the platform stays a no-op (RT3/RT4). */
            services.AddDomainEventHandler<CityWatch.Events.Events.NfcCheckpointScanned, Handlers.NfcAnchorHandler>();
            services.AddDomainEventHandler<CityWatch.Events.Events.OfficerLoggedIn, Handlers.SessionLifecycleHandler>();
            services.AddDomainEventHandler<CityWatch.Events.Events.OfficerLoggedOut, Handlers.SessionLifecycleHandler>();
            services.AddDomainEventHandler<CityWatch.Events.Events.PatrolStarted, Handlers.SessionLifecycleHandler>();
            services.AddDomainEventHandler<CityWatch.Events.Events.PatrolEnded, Handlers.SessionLifecycleHandler>();
            services.AddDomainEventHandler<CityWatch.Events.Events.DuressActivated, Handlers.DuressHandler>();

            /* M1.7+: hub + broadcast ticker; M1.8: mode command service. Same branch. */

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

            /* RadioCheck maps no controllers of its own; this makes /api/tracking/* exist
               there too. Safe alongside Web's MapControllerRoute — attribute routes are
               registered once per application model. */
            app.MapControllers();

            /* Group-scoped push channel; the 1 Hz ticker sends the frames (§10.3).
               [Authorize] on the hub class enforces the auth stance. */
            app.MapHub<Hubs.PatrolTrackingHub>("/trackingHub");

            return app;
        }
    }
}
