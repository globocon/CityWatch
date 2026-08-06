using CityWatch.Data;
using CityWatch.Data.Helpers;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

namespace CityWatch.Kpi.Services.FastReport
{
    public interface IFastReportScopeFactory
    {
        /// <summary>
        /// Creates an isolated DI scope for one report run. The scope owns its own
        /// <c>CityWatchDbContext</c> and its own <see cref="ReportScopeCache"/>.
        /// </summary>
        IServiceScope CreateReportScope();
    }

    /// <summary>
    /// Builds a child service provider that mirrors the application's registrations, with
    /// the report path's data providers wrapped in memoising decorators.
    ///
    /// Why a child container rather than decorating the app's registrations directly:
    /// the existing report path must keep behaving exactly as it does today. Decorating in
    /// <c>Program.cs</c> would change the providers that the existing generator, the
    /// scheduler and every Razor page resolve. Building a parallel container confines the
    /// change to the new code path - nothing outside this factory can observe it.
    ///
    /// The registration list is *copied* from the live application collection rather than
    /// hand-maintained, so a future constructor change in <c>ReportGenerator</c> or
    /// <c>ViewDataService</c> is picked up automatically instead of silently breaking here.
    /// </summary>
    public sealed class FastReportScopeFactory : IFastReportScopeFactory, IDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly ILogger<FastReportScopeFactory> _logger;

        public FastReportScopeFactory(
            IServiceCollection applicationServices,
            IServiceProvider rootProvider,
            ILogger<FastReportScopeFactory> logger)
        {
            _logger = logger;

            IServiceCollection services = new ServiceCollection();
            foreach (var descriptor in applicationServices)
                services.Add(descriptor);

            PinFrameworkServices(services, rootProvider);

            // One cache per report scope - never shared between jobs.
            services.AddScoped<ReportScopeCache>();

            Decorate<IGuardDataProvider>(services, FastReportCachePolicy.GuardDataProvider);
            Decorate<IViewDataService>(services, FastReportCachePolicy.ViewDataService);
            Decorate<IClientDataProvider>(services, FastReportCachePolicy.ClientDataProvider);
            Decorate<IGuardLogDataProvider>(services, FastReportCachePolicy.GuardLogDataProvider);
            Decorate<IClientSiteWandDataProvider>(services, FastReportCachePolicy.ClientSiteWandDataProvider);
            Decorate<IPatrolDataReportService>(services, FastReportCachePolicy.PatrolDataReportService);

            // Measured on test (schedule 81, Jul 2026): GetDailyPatrolData accounted for
            // 62.8s of a 76.9s report. Its cost is inside these two providers - the whole
            // month's incident reports reloaded per call, and GetFeedbackTemplates() run
            // once per report row from DailyPatrolData's ColourCode getter.
            // The incident-report read is additionally swapped for a no-tracking variant:
            // measured at 20.4s of a 34.6s report, against SQL that returns in under a
            // second. See ReadOnlyIrDataProvider for the full reasoning.
            Decorate<IIrDataProvider>(services, FastReportCachePolicy.IrDataProvider,
                (inner, sp) => new ReadOnlyIrDataProvider(inner, sp.GetRequiredService<CityWatchDbContext>()));

            Decorate<IConfigDataProvider>(services, FastReportCachePolicy.ConfigDataProvider);

            _provider = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateScopes = false,
                ValidateOnBuild = false
            });

            _logger.LogInformation("FastReport: child container built with {Count} registrations.", services.Count);
        }

        public IServiceScope CreateReportScope() => _provider.CreateScope();

        /// <summary>
        /// Replaces host-owned services with the live instances from the running application,
        /// so the child container shares the real environment, configuration and logging
        /// rather than constructing parallel copies of them.
        /// </summary>
        private static void PinFrameworkServices(IServiceCollection services, IServiceProvider root)
        {
            ReplaceInstance<IWebHostEnvironment>(services, root);
            ReplaceInstance<IHostEnvironment>(services, root);
            ReplaceInstance<IConfiguration>(services, root);
            ReplaceInstance<ILoggerFactory>(services, root);
            ReplaceInstance<IOptions<CityWatch.Kpi.Helpers.Settings>>(services, root);
            ReplaceInstance<IOptions<CityWatch.Data.Helpers.EmailOptions>>(services, root);

            // Keep a single job store across both containers.
            ReplaceInstance<IFastReportJobStore>(services, root);
        }

        private static void ReplaceInstance<TService>(IServiceCollection services, IServiceProvider root)
            where TService : class
        {
            var instance = root.GetService<TService>();
            if (instance == null)
                return;

            foreach (var existing in services.Where(d => d.ServiceType == typeof(TService)).ToList())
                services.Remove(existing);

            services.AddSingleton(instance);
        }

        /// <summary>
        /// Re-registers <typeparamref name="TInterface"/> so that resolving it yields a
        /// memoising proxy over the application's real implementation. The concrete type is
        /// registered alongside it so the container still builds it with its own dependencies.
        /// </summary>
        /// <param name="wrap">
        /// Optional adapter applied to the real implementation before it is memoised - used
        /// where the fast path needs a different implementation of one method, not just
        /// caching. The cache always sits outermost so its counters see every call.
        /// </param>
        private void Decorate<TInterface>(
            IServiceCollection services,
            System.Collections.Generic.IReadOnlyDictionary<string, string> policy,
            Func<TInterface, IServiceProvider, TInterface> wrap = null)
            where TInterface : class
        {
            var descriptor = services.LastOrDefault(d => d.ServiceType == typeof(TInterface));
            if (descriptor == null)
            {
                _logger.LogWarning("FastReport: {Interface} is not registered - caching skipped.", typeof(TInterface).Name);
                return;
            }

            var implementationType = descriptor.ImplementationType;
            if (implementationType == null)
            {
                // Registered by factory or instance - we cannot safely rebuild it, so leave
                // it undecorated. The report still works, just without this cache.
                _logger.LogWarning(
                    "FastReport: {Interface} has no concrete implementation type - caching skipped.",
                    typeof(TInterface).Name);
                return;
            }

            services.Remove(descriptor);
            services.Add(new ServiceDescriptor(implementationType, implementationType, descriptor.Lifetime));
            services.Add(new ServiceDescriptor(
                typeof(TInterface),
                sp =>
                {
                    var real = (TInterface)sp.GetRequiredService(implementationType);
                    if (wrap != null)
                        real = wrap(real, sp);

                    return MemoizingProxy<TInterface>.Create(
                        real,
                        sp.GetRequiredService<ReportScopeCache>(),
                        policy);
                },
                descriptor.Lifetime));
        }

        public void Dispose() => _provider?.Dispose();
    }
}
