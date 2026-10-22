using System;
using System.Threading;
using System.Threading.Tasks;
using CityWatch.Tracking.Configuration;
using CityWatch.Tracking.Data;
using CityWatch.Tracking.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CityWatch.Tracking.Services.Geocoding
{
    public interface IGeocodeService
    {
        /// <summary>Short address for the coordinates, or null. Cache-first; the provider is
        /// only consulted for a cell nobody has visited within the cache window.</summary>
        Task<string?> GetAddressAsync(decimal lat, decimal lon, CancellationToken ct);
    }

    /// <summary>
    /// The spatial cache in front of the geocoding provider (§Phase 2.1).
    /// </summary>
    /// <remarks>
    /// Cells are ~110 m squares (coordinate × 1000, floored): fine enough that the label is
    /// right for the street the vehicle is on, coarse enough that a patrol's whole shift is
    /// a handful of provider calls. Failed lookups are cached as null and only retried after
    /// the failure window — the provider being down never becomes a request storm, and the
    /// caller cannot tell a miss from an outage (both are "no address", by design).
    /// </remarks>
    public sealed class GeocodeService : IGeocodeService
    {
        private readonly TrackingDbContext _db;
        private readonly IReverseGeocoder _provider;
        private readonly TrackingOptions _options;
        private readonly ILogger<GeocodeService> _logger;
        private readonly Func<DateTime> _utcNow;

        public GeocodeService(TrackingDbContext db, IReverseGeocoder provider, TrackingOptions options,
            ILogger<GeocodeService> logger, Func<DateTime>? utcNow = null)
        {
            _db = db;
            _provider = provider;
            _options = options;
            _logger = logger;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        internal static (int CellLat, int CellLon) Cell(decimal lat, decimal lon) =>
            ((int)Math.Floor(lat * 1000), (int)Math.Floor(lon * 1000));

        public async Task<string?> GetAddressAsync(decimal lat, decimal lon, CancellationToken ct)
        {
            if (!_options.Geocoding.Enabled)
                return null;

            var (cellLat, cellLon) = Cell(lat, lon);
            var now = _utcNow();

            var cached = await _db.GeocodeCacheEntries
                .FirstOrDefaultAsync(c => c.CellLat == cellLat && c.CellLon == cellLon, ct);

            if (cached != null)
            {
                var maxAge = cached.Address == null
                    ? TimeSpan.FromMinutes(_options.Geocoding.FailureRetryMinutes)
                    : TimeSpan.FromDays(_options.Geocoding.CacheDays);
                if (now - cached.ResolvedUtc < maxAge)
                    return cached.Address;             // fresh hit — including a cached failure
            }

            var address = await _provider.ResolveAsync(lat, lon, ct);

            try
            {
                if (cached == null)
                {
                    _db.GeocodeCacheEntries.Add(new GeocodeCache
                    {
                        CellLat = cellLat,
                        CellLon = cellLon,
                        Address = address,
                        ResolvedUtc = now
                    });
                }
                else
                {
                    cached.Address = address;
                    cached.ResolvedUtc = now;
                    _db.GeocodeCacheEntries.Update(cached);
                }
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                /* Two concurrent misses on one cell race the unique index; the loser's answer
                   is identical, so losing the write costs nothing. */
                _logger.LogDebug(ex, "Geocode cache write race on cell {Lat},{Lon}.", cellLat, cellLon);
            }

            return address;
        }
    }
}

