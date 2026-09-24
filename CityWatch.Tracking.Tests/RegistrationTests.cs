using System.Collections.Generic;
using System.Linq;
using CityWatch.Tracking.Configuration;
using CityWatch.Tracking.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CityWatch.Tracking.Tests
{
    /// <summary>
    /// RT1 from the architecture document: with the flag off, no tracking service is resolvable.
    /// The off state must be indistinguishable from the module not being deployed, because that
    /// property is Level-1 rollback (§17).
    /// </summary>
    [TestClass]
    public class RegistrationTests
    {
        private static IConfiguration BuildConfig(Dictionary<string, string?>? values = null)
            => new ConfigurationBuilder().AddInMemoryCollection(values ?? new()).Build();

        [TestMethod]
        public void FlagOff_RegistersOnlyOptions()
        {
            var services = new ServiceCollection();
            services.AddCityWatchTracking(BuildConfig(new() { ["Tracking:Enabled"] = "false" }));
            using var provider = services.BuildServiceProvider();

            Assert.IsNotNull(provider.GetService<TrackingOptions>());
            Assert.IsNull(provider.GetService<ILiveStateStore>(),
                "RT1: no tracking service may be resolvable when the flag is off.");
        }

        [TestMethod]
        public void SectionAbsent_BehavesAsDisabled()
        {
            // A host whose appsettings.json has no Tracking section at all — the state every
            // environment is in on the day the assembly first ships.
            var services = new ServiceCollection();
            services.AddCityWatchTracking(BuildConfig());
            using var provider = services.BuildServiceProvider();

            var options = provider.GetRequiredService<TrackingOptions>();
            Assert.IsFalse(options.Enabled, "Absent configuration must mean disabled, never enabled.");
            Assert.IsNull(provider.GetService<ILiveStateStore>());
        }

        [TestMethod]
        public void FlagOn_RegistersLiveStateStore_AsSingleton()
        {
            var services = new ServiceCollection();
            services.AddCityWatchTracking(BuildConfig(new() { ["Tracking:Enabled"] = "true" }));
            using var provider = services.BuildServiceProvider();

            var store = provider.GetService<ILiveStateStore>();
            Assert.IsNotNull(store);
            Assert.AreSame(store, provider.GetService<ILiveStateStore>(),
                "Live state must be a singleton: it IS the shared picture.");
        }

        [TestMethod]
        public void Defaults_MatchTheArchitectureDocument()
        {
            var options = new TrackingOptions();

            Assert.IsFalse(options.Enabled);
            Assert.AreEqual(10, options.MaxConcurrentLiveUnits);
            Assert.AreEqual(900, options.LiveModeTtlSeconds);
            Assert.AreEqual(100, options.MaxAcceptedAccuracyMetres);
            Assert.AreEqual(250, options.PlausibilityMaxSpeedKph);
            Assert.AreEqual(90, options.RetentionDays.Points);
            Assert.AreEqual(2555, options.RetentionDays.Segments);
            Assert.AreEqual(10, options.Policy.TransitSteadySec);
            Assert.AreEqual(60, options.Policy.StationarySec);
            Assert.AreEqual(25, options.Policy.DistanceFilterM);
        }

        [TestMethod]
        public void PolicySection_BindsFromConfiguration()
        {
            var services = new ServiceCollection();
            services.AddCityWatchTracking(BuildConfig(new()
            {
                ["Tracking:Enabled"] = "true",
                ["Tracking:Policy:TransitSteadySec"] = "15",
                ["Tracking:RetentionDays:Points"] = "180"
            }));
            using var provider = services.BuildServiceProvider();

            var options = provider.GetRequiredService<TrackingOptions>();
            Assert.AreEqual(15, options.Policy.TransitSteadySec);
            Assert.AreEqual(180, options.RetentionDays.Points);
            Assert.AreEqual(60, options.Policy.StationarySec, "Unspecified values keep their defaults.");
        }
    }
}
