using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CityWatch.Tracking.Configuration;
using CityWatch.Tracking.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CityWatch.Tracking.Services.Geofencing
{
    /// <summary>Where a site is. Name is carried so a visit can be recorded without a second
    /// lookup — and so the bell shows the name that was true at the time.</summary>
    public sealed record GeofenceSite(int Id, string Name, decimal Lat, decimal Lon);

    public interface ISiteGeofenceCatalogue
    {
        /// <summary>Active sites that have a usable coordinate. Cached; refreshed on the
        /// configured interval.</summary>
        Task<IReadOnlyList<GeofenceSite>> GetAsync(CancellationToken ct);
    }

    /// <summary>
    /// The active sites, with coordinates, held in memory.
    /// </summary>
    /// <remarks>
    /// Read once per refresh interval rather than per fix: sites move roughly never, and the
    /// alternative is a query on the hot ingest path. About 800 of the 800 active sites carry
    /// a coordinate, so this is a few tens of kilobytes.
    ///
    /// A singleton reaching into a scoped DbContext is done through the scope factory — the
    /// context must not outlive the read.
    /// </remarks>
    public sealed class SiteGeofenceCatalogue : ISiteGeofenceCatalogue
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly TrackingOptions _options;
        private readonly ILogger<SiteGeofenceCatalogue> _logger;
        private readonly Func<DateTime> _utcNow;
        private readonly SemaphoreSlim _gate = new(1, 1);

        private IReadOnlyList<GeofenceSite> _sites = Array.Empty<GeofenceSite>();
        private DateTime _loadedUtc = DateTime.MinValue;

        public SiteGeofenceCatalogue(IServiceScopeFactory scopes, TrackingOptions options,
            ILogger<SiteGeofenceCatalogue> logger, Func<DateTime>? utcNow = null)
        {
            _scopes = scopes;
            _options = options;
            _logger = logger;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public async Task<IReadOnlyList<GeofenceSite>> GetAsync(CancellationToken ct)
        {
            var now = _utcNow();
            var ttl = TimeSpan.FromMinutes(Math.Max(1, _options.SiteGeofence.CatalogueRefreshMinutes));
            if (now - _loadedUtc < ttl)
                return _sites;

            await _gate.WaitAsync(ct);
            try
            {
                if (_utcNow() - _loadedUtc < ttl)
                    return _sites;                      // another caller refreshed while we queued

                using var scope = _scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TrackingDbContext>();

                var rows = await db.PlatformClientSites
                    .Where(s => s.IsActive && s.Gps != null && s.Gps != "")
                    .Select(s => new { s.Id, s.Name, s.Gps })
                    .ToListAsync(ct);

                var parsed = new List<GeofenceSite>(rows.Count);
                foreach (var row in rows)
                {
                    if (TryParseGps(row.Gps, out var lat, out var lon))
                        parsed.Add(new GeofenceSite(row.Id, row.Name ?? $"Site {row.Id}", lat, lon));
                }

                _sites = parsed;
                _loadedUtc = _utcNow();
                _logger.LogInformation("Site geofence catalogue loaded: {Usable} of {Total} active sites have a usable coordinate.",
                    parsed.Count, rows.Count);
                return _sites;
            }
            catch (Exception ex)
            {
                /* A catalogue that cannot be read must not break ingest. Serve what we have
                   (possibly nothing) and try again on the next interval: missing arrival
                   alerts are a degradation, dropped positions would be data loss. */
                _loadedUtc = _utcNow();
                _logger.LogError(ex, "Site geofence catalogue refresh failed; keeping {Count} cached sites.", _sites.Count);
                return _sites;
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>
        /// Parses the platform's free-text "lat,lon" column. Blank, malformed, out-of-range
        /// and null-island values all exist in the data and are all simply not geofenceable —
        /// they are skipped, never guessed at.
        /// </summary>
        internal static bool TryParseGps(string? gps, out decimal lat, out decimal lon)
        {
            lat = 0;
            lon = 0;
            if (string.IsNullOrWhiteSpace(gps))
                return false;

            var parts = gps.Split(',');
            if (parts.Length != 2)
                return false;

            if (!decimal.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out lat) ||
                !decimal.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out lon))
                return false;

            if (lat < -90 || lat > 90 || lon < -180 || lon > 180)
                return false;
            if (lat == 0 && lon == 0)
                return false;   // the null island fix, same rule ingest applies

            return true;
        }
    }
}
