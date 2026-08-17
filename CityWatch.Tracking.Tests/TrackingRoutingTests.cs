using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CityWatch.Tracking.Configuration;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CityWatch.Tracking.Tests
{
    /// <summary>
    /// RT2 — with tracking disabled, the pack must expose NO routes at all.
    ///
    /// This test exists because the original implementation got it wrong: skipping
    /// AddApplicationPart is not enough, since MVC auto-discovers controllers from every
    /// referenced assembly. The routes stayed alive with the flag off, and because the
    /// services were unregistered a request would have surfaced a 500 rather than a 404.
    /// Caught by probing a real flag-off server, now pinned here.
    /// </summary>
    [TestClass]
    public class TrackingRoutingTests
    {
        private static ControllerFeature BuildFeature(bool enabled)
        {
            var feature = new ControllerFeature();
            // Seed it the way MVC's own discovery would, then let the provider filter.
            feature.Controllers.Add(typeof(Api.TrackingController).GetTypeInfo());
            feature.Controllers.Add(typeof(UnrelatedHostController).GetTypeInfo());

            new TrackingControllerFeatureProvider(enabled)
                .PopulateFeature(new List<ApplicationPart>(), feature);

            return feature;
        }

        /// <summary>Stands in for a controller belonging to the host application.</summary>
        private sealed class UnrelatedHostController { }

        [TestMethod]
        public void Disabled_RemovesTheTrackingController()
        {
            var feature = BuildFeature(enabled: false);

            Assert.IsFalse(feature.Controllers.Any(c => c.AsType() == typeof(Api.TrackingController)),
                "RT2: /api/tracking/* must not be routable when the flag is off.");
        }

        [TestMethod]
        public void Disabled_LeavesHostControllersAlone()
        {
            var feature = BuildFeature(enabled: false);

            Assert.IsTrue(feature.Controllers.Any(c => c.AsType() == typeof(UnrelatedHostController)),
                "The provider must only ever remove the pack's own controllers.");
        }

        [TestMethod]
        public void Enabled_KeepsTheTrackingController()
        {
            var feature = BuildFeature(enabled: true);

            Assert.IsTrue(feature.Controllers.Any(c => c.AsType() == typeof(Api.TrackingController)));
            Assert.IsTrue(feature.Controllers.Any(c => c.AsType() == typeof(UnrelatedHostController)));
        }
    }
}
