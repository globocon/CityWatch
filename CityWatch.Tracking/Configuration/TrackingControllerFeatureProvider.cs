using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace CityWatch.Tracking.Configuration
{
    /// <summary>
    /// Removes the pack's controllers from MVC's discovered set when tracking is disabled.
    /// </summary>
    /// <remarks>
    /// ASP.NET Core discovers controllers from every referenced assembly automatically, so
    /// simply not calling AddApplicationPart is NOT enough — TrackingController would still be
    /// routed with the flag off, and because its services are unregistered a request would
    /// surface a 500 instead of a clean 404.
    ///
    /// This was found by probing the endpoints on a flag-off run rather than by reading the
    /// code, which is why RT2 now asserts the status codes directly (see TrackingRoutingTests).
    /// </remarks>
    public sealed class TrackingControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        private readonly bool _enabled;

        public TrackingControllerFeatureProvider(bool enabled) => _enabled = enabled;

        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            if (_enabled)
                return;

            var trackingControllers = feature.Controllers
                .Where(c => c.Assembly == typeof(Api.TrackingController).Assembly)
                .ToList();

            foreach (var controller in trackingControllers)
                feature.Controllers.Remove(controller);
        }
    }
}
