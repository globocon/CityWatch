using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CityWatch.Tracking.Configuration;
using CityWatch.Tracking.Data;
using CityWatch.Tracking.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CityWatch.Tracking.Services.Geofencing
{
    /// <summary>One accepted fix, reduced to what the geofence needs.</summary>
    public readonly record struct GeoFix(decimal Lat, decimal Lon, DateTime Utc);

    public interface ISiteArrivalDetector
    {
        /// <summary>Runs the geofence over a batch's accepted fixes and records any arrival
        /// or departure. Never throws into the ingest path.</summary>
        Task EvaluateAsync(int unitId, Guid sessionId, bool isCar, IReadOnlyList<GeoFix> fixes, CancellationToken ct);

        /// <summary>An NFC scan is a human-confirmed arrival: it counts immediately, with no
        /// dwell window, and it works for sites that have no coordinate on file.</summary>
        Task ApplyScanAsync(int unitId, Guid sessionId, int siteId, string? siteName, bool isInCarTag,
            DateTime occurredUtc, CancellationToken ct);
    }

    /// <summary>
    /// Decides when a unit has ARRIVED at a client site, from GPS alone.
    /// </summary>
    /// <remarks>
    /// The rule that matters is the one that stops false alarms. A patrol car passes within
    /// 150 m of dozens of client sites on any main road, so proximity alone would fill the
    /// control room's bell with arrivals that never happened — and an alert feed that cries
    /// wolf gets ignored, which is worse than having none.
    ///
    /// So a crossing opens a CANDIDATE visit and nothing is announced. The visit is confirmed
    /// only when the unit is still inside after <see cref="TrackingOptions.SiteGeofenceOptions.DwellSeconds"/>.
    /// A drive-past is closed unconfirmed and kept: it is evidence of a pass, and keeping it
    /// means a detector fault shows up in the data instead of hiding.
    ///
    /// Departure uses a wider radius than arrival. Without that hysteresis a fix wobbling
    /// across a single boundary produces arrive/depart/arrive for the whole night.
    ///
    /// State lives in the table, not in memory: an app pool recycle mid-visit must not lose
    /// where a car is, and a second instance must reach the same conclusion.
    /// </remarks>
    public sealed class SiteArrivalDetector : ISiteArrivalDetector
    {
        private readonly TrackingDbContext _db;
        private readonly ISiteGeofenceCatalogue _catalogue;
        private readonly TrackingOptions _options;
        private readonly ILogger<SiteArrivalDetector> _logger;

        public SiteArrivalDetector(TrackingDbContext db, ISiteGeofenceCatalogue catalogue,
            TrackingOptions options, ILogger<SiteArrivalDetector> logger)
        {
            _db = db;
            _catalogue = catalogue;
            _options = options;
            _logger = logger;
        }

        public async Task EvaluateAsync(int unitId, Guid sessionId, bool isCar, IReadOnlyList<GeoFix> fixes, CancellationToken ct)
        {
            var cfg = _options.SiteGeofence;
            if (!cfg.Enabled || !cfg.UseGpsDetection || fixes.Count == 0)
                return;
            if (cfg.CarsOnly && !isCar)
                return;

            var sites = await _catalogue.GetAsync(ct);
            if (sites.Count == 0)
                return;

            var byId = sites.ToDictionary(s => s.Id);

            /* AsTracking is not optional here: the context's DI default is NoTracking, which
               silently turns every mutation below into a no-op (the 12 Aug class of bug). */
            var open = await _db.TrackingSiteVisits
                .AsTracking()
                .Where(v => v.SessionId == sessionId && v.ExitedUtc == null)
                .OrderByDescending(v => v.Id)
                .FirstOrDefaultAsync(ct);

            var dirty = false;

            foreach (var fix in fixes.OrderBy(f => f.Utc))
            {
                if (open != null)
                {
                    /* A site that has been deactivated or lost its coordinate since the visit
                       opened can no longer be measured against — close the visit rather than
                       leave the unit permanently "on site". */
                    if (!byId.TryGetValue(open.SiteId, out var openSite))
                    {
                        open.ExitedUtc = fix.Utc;
                        open = null;
                        dirty = true;
                    }
                    else
                    {
                        var distance = MetresBetween(fix.Lat, fix.Lon, openSite.Lat, openSite.Lon);
                        if (distance <= cfg.ExitRadiusM)
                        {
                            // Still inside (the hysteresis band counts as inside).
                            if (Confirm(open, fix, distance, cfg))
                                dirty = true;
                            continue;   // one unit is at one site: no other site can claim it
                        }

                        open.ExitedUtc = fix.Utc;
                        _logger.LogInformation("Unit {Unit} left site {Site} ({Distance} m).",
                            unitId, open.SiteId, (int)Math.Round(distance));
                        open = null;
                        dirty = true;
                    }
                }

                var (nearest, nearestM) = Nearest(sites, fix.Lat, fix.Lon, cfg.EnterRadiusM);
                if (nearest == null)
                    continue;

                open = new TrackingSiteVisit
                {
                    UnitId = unitId,
                    SessionId = sessionId,
                    SiteId = nearest.Id,
                    SiteName = Truncate(nearest.Name, 200),
                    EnteredUtc = fix.Utc,
                    Source = "Gps",
                    EnteredLat = fix.Lat,
                    EnteredLon = fix.Lon,
                    DistanceM = (int)Math.Round(nearestM)
                };
                _db.TrackingSiteVisits.Add(open);
                dirty = true;

                // A zero dwell window means the crossing itself is the arrival.
                Confirm(open, fix, nearestM, cfg);
            }

            if (dirty)
                await _db.SaveChangesAsync(ct);
        }

        public async Task ApplyScanAsync(int unitId, Guid sessionId, int siteId, string? siteName, bool isInCarTag,
            DateTime occurredUtc, CancellationToken ct)
        {
            if (!_options.SiteGeofence.Enabled)
                return;

            var open = await _db.TrackingSiteVisits
                .AsTracking()
                .Where(v => v.SessionId == sessionId && v.ExitedUtc == null)
                .OrderByDescending(v => v.Id)
                .FirstOrDefaultAsync(ct);

            if (isInCarTag)
            {
                /* Back in the vehicle: the officer has said they are leaving, which is better
                   evidence than waiting for GPS to clear the exit radius. */
                if (open == null)
                    return;
                open.ExitedUtc = occurredUtc;
                await _db.SaveChangesAsync(ct);
                return;
            }

            if (open != null && open.SiteId == siteId)
            {
                /* GPS already had them here — the scan upgrades it rather than raising a
                   second arrival for the same stay. Several tags get scanned per visit, so
                   this path runs repeatedly and must stay idempotent. */
                if (open.ConfirmedUtc != null && open.Source == "Nfc")
                    return;
                open.ConfirmedUtc ??= occurredUtc;
                open.Source = "Nfc";
                await _db.SaveChangesAsync(ct);
                return;
            }

            if (open != null)
                open.ExitedUtc = occurredUtc;   // scanned in somewhere else: the old stay ended

            _db.TrackingSiteVisits.Add(new TrackingSiteVisit
            {
                UnitId = unitId,
                SessionId = sessionId,
                SiteId = siteId,
                SiteName = Truncate(string.IsNullOrWhiteSpace(siteName) ? $"Site {siteId}" : siteName!, 200),
                EnteredUtc = occurredUtc,
                ConfirmedUtc = occurredUtc,     // a person tagged the site: no dwell needed
                Source = "Nfc"
            });

            await _db.SaveChangesAsync(ct);
        }

        /// <summary>Confirms a candidate once it has held the dwell window. Returns whether
        /// anything changed.</summary>
        private bool Confirm(TrackingSiteVisit visit, GeoFix fix, double distance, TrackingOptions.SiteGeofenceOptions cfg)
        {
            if (visit.ConfirmedUtc != null)
                return false;
            /* Confirmation demands the tighter ENTER radius: drifting in the hysteresis band
               is enough to stay, not enough to arrive. */
            if (distance > cfg.EnterRadiusM)
                return false;
            if ((fix.Utc - visit.EnteredUtc).TotalSeconds < cfg.DwellSeconds)
                return false;

            visit.ConfirmedUtc = fix.Utc;
            visit.DistanceM = (int)Math.Round(distance);
            _logger.LogInformation("Unit {Unit} arrived at site {Site} ({Distance} m, dwelled {Seconds}s).",
                visit.UnitId, visit.SiteId, visit.DistanceM, (int)(fix.Utc - visit.EnteredUtc).TotalSeconds);
            return true;
        }

        /// <summary>Nearest site within the radius, or null. A latitude band prefilter keeps
        /// this cheap enough for the ingest path at any fleet size.</summary>
        internal static (GeofenceSite? Site, double Metres) Nearest(
            IReadOnlyList<GeofenceSite> sites, decimal lat, decimal lon, int radiusM)
        {
            // One degree of latitude is ~111 km anywhere; a generous band cannot exclude a
            // site that is actually within the radius.
            var band = (decimal)(radiusM / 111_000.0 * 1.5) + 0.001m;
            GeofenceSite? best = null;
            var bestM = double.MaxValue;

            foreach (var site in sites)
            {
                if (Math.Abs(site.Lat - lat) > band)
                    continue;
                var metres = MetresBetween(lat, lon, site.Lat, site.Lon);
                if (metres <= radiusM && metres < bestM)
                {
                    best = site;
                    bestM = metres;
                }
            }

            return (best, best == null ? double.MaxValue : bestM);
        }

        internal static double MetresBetween(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
            => GeoMath.HaversineKm(lat1, lon1, lat2, lon2) * 1000.0;

        private static string Truncate(string value, int max)
            => value.Length <= max ? value : value.Substring(0, max);
    }
}
