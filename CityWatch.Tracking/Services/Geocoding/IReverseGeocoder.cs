using System.Threading;
using System.Threading.Tasks;

namespace CityWatch.Tracking.Services.Geocoding
{
    /// <summary>
    /// A provider that turns coordinates into a short human address ("Main Road, Pala").
    /// </summary>
    /// <remarks>
    /// Behind an interface because the provider is a deployment decision (Nominatim today;
    /// Azure Maps or Google later without touching callers), and because the UI must never
    /// depend on any provider being available: null is a normal answer, not an error.
    /// </remarks>
    public interface IReverseGeocoder
    {
        /// <summary>Resolves a short address, or null when the provider cannot answer.
        /// Never throws for availability problems.</summary>
        Task<string?> ResolveAsync(decimal lat, decimal lon, CancellationToken ct);
    }
}
