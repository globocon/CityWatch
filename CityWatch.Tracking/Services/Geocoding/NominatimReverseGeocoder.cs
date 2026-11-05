using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CityWatch.Tracking.Configuration;
using Microsoft.Extensions.Logging;

namespace CityWatch.Tracking.Services.Geocoding
{
    /// <summary>
    /// OpenStreetMap Nominatim reverse geocoding.
    /// </summary>
    /// <remarks>
    /// Nominatim's usage policy is one request per second with an identifying User-Agent —
    /// both enforced here process-wide (a static gate), not left to callers. The cache in
    /// front of this class means the limit is only ever felt on the first visit to a street.
    /// Every failure path returns null: an address is decoration on the truth, never the
    /// truth itself, so the map must keep working with the geocoder down.
    /// </remarks>
    public sealed class NominatimReverseGeocoder : IReverseGeocoder
    {
        private static readonly SemaphoreSlim Gate = new(1, 1);
        private static DateTime _lastCallUtc = DateTime.MinValue;

        private readonly HttpClient _http;
        private readonly TrackingOptions _options;
        private readonly ILogger<NominatimReverseGeocoder> _logger;

        public NominatimReverseGeocoder(HttpClient http, TrackingOptions options,
            ILogger<NominatimReverseGeocoder> logger)
        {
            _http = http;
            _options = options;
            _logger = logger;
            if (_http.BaseAddress == null)
                _http.BaseAddress = new Uri(_options.Geocoding.BaseUrl);
            if (!_http.DefaultRequestHeaders.Contains("User-Agent"))
                _http.DefaultRequestHeaders.Add("User-Agent", "CityWatch-ControlRoom/1.0 (support@c4i-system.com)");
        }

        public async Task<string?> ResolveAsync(decimal lat, decimal lon, CancellationToken ct)
        {
            try
            {
                await Gate.WaitAsync(ct);
                try
                {
                    /* Policy: at most one request per MinIntervalMs, process-wide. */
                    var wait = _lastCallUtc.AddMilliseconds(_options.Geocoding.MinIntervalMs) - DateTime.UtcNow;
                    if (wait > TimeSpan.Zero)
                        await Task.Delay(wait, ct);
                    _lastCallUtc = DateTime.UtcNow;

                    using var response = await _http.GetAsync(
                        $"reverse?format=jsonv2&lat={lat}&lon={lon}&zoom=17&addressdetails=1", ct);
                    if (!response.IsSuccessStatusCode)
                        return null;

                    using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                    return Compose(doc.RootElement);
                }
                finally
                {
                    Gate.Release();
                }
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Reverse geocode failed for {Lat},{Lon}.", lat, lon);
                return null;
            }
        }

        /// <summary>"road, locality" — short enough for a marker label, specific enough to
        /// mean something ("Main Road, Pala"), never the 9-part display_name.</summary>
        internal static string? Compose(JsonElement root)
        {
            if (!root.TryGetProperty("address", out var addr))
                return root.TryGetProperty("display_name", out var dn) ? Truncate(dn.GetString()) : null;

            string? part(params string[] keys)
            {
                foreach (var key in keys)
                    if (addr.TryGetProperty(key, out var v) && !string.IsNullOrWhiteSpace(v.GetString()))
                        return v.GetString();
                return null;
            }

            var road = part("road", "pedestrian", "footway", "neighbourhood", "hamlet");
            var place = part("suburb", "village", "town", "city_district", "city", "municipality", "county");
            var text = road != null && place != null ? $"{road}, {place}" : road ?? place;
            return Truncate(text);
        }

        private static string? Truncate(string? s) =>
            string.IsNullOrWhiteSpace(s) ? null : (s.Length <= 120 ? s : s[..117] + '…');
    }
}
