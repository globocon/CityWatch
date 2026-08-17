using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CityWatch.Tracking.Configuration;
using CityWatch.Tracking.Data;
using Microsoft.EntityFrameworkCore;

namespace CityWatch.Tracking.Services.Geofencing
{
    /// <summary>One visit — the client renders it as an "entered" line and, once
    /// <see cref="ExitedUtc"/> is set, a "left" line too.</summary>
    public sealed record SiteArrivalDto(
        int Id, int UnitId, int SiteId, string SiteName,
        DateTime EnteredUtc, DateTime ConfirmedUtc, DateTime? ExitedUtc, bool StillOnSite, string Source)
    {
        /// <summary>What the operator calls the unit: callsign, else the car, else the unit id.</summary>
        public string? Label { get; init; }

        public string? GuardName { get; init; }

        /// <summary>car | guard — the bell shows the same symbol as the map.</summary>
        public string Kind { get; init; } = "car";

        /// <summary>Minutes on site so far (or the length of the stay once it ended).</summary>
        public int MinutesOnSite { get; init; }
    }

    public interface ISiteArrivalFeed
    {
        /// <summary>Confirmed arrivals, newest first. Candidates and drive-pasts never appear.</summary>
        Task<IReadOnlyList<SiteArrivalDto>> GetRecentAsync(int? hours, CancellationToken ct);
    }

    /// <summary>
    /// Reads back what the detector recorded.
    /// </summary>
    /// <remarks>
    /// This is what makes the bell survive a refresh: the alerts are not a browser's memory of
    /// what it happened to witness, they are the server's record of what happened. Two
    /// operators on two screens see the same list, and an arrival at 02:00 with nobody
    /// watching is still there at 06:00.
    /// </remarks>
    public sealed class SiteArrivalFeed : ISiteArrivalFeed
    {
        /// <summary>Enough for a whole night shift; the client only ever renders the newest.</summary>
        private const int MaxRows = 200;

        private readonly TrackingDbContext _db;
        private readonly TrackingOptions _options;
        private readonly Func<DateTime> _utcNow;

        public SiteArrivalFeed(TrackingDbContext db, TrackingOptions options, Func<DateTime>? utcNow = null)
        {
            _db = db;
            _options = options;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public async Task<IReadOnlyList<SiteArrivalDto>> GetRecentAsync(int? hours, CancellationToken ct)
        {
            var now = _utcNow();
            var window = Math.Clamp(hours ?? _options.SiteGeofence.FeedHours, 1, 168);
            var cutoff = now.AddHours(-window);

            /* A visit is in the window while EITHER of its events is: a car that arrived
               13 hours ago and drives off now must still put its "left" line in the bell. */
            var visits = await _db.TrackingSiteVisits
                .Where(v => v.ConfirmedUtc != null &&
                            (v.ConfirmedUtc >= cutoff || v.ExitedUtc >= cutoff))
                .OrderByDescending(v => v.ExitedUtc ?? v.ConfirmedUtc)
                .Take(MaxRows)
                .ToListAsync(ct);
            if (visits.Count == 0)
                return Array.Empty<SiteArrivalDto>();

            /* Labels live on the session, not the visit: the callsign is a property of the
               shift. Two bounded lookups, same shape as the live snapshot's. */
            var sessionIds = visits.Select(v => v.SessionId).Distinct().ToList();
            var sessions = await _db.TrackingSessions
                .Where(s => sessionIds.Contains(s.Id))
                .Select(s => new { s.Id, s.GuardId, s.Callsign, s.PatrolCarPositionName, s.IsPatrolCar, s.UnitId })
                .ToDictionaryAsync(s => s.Id, ct);

            var guardIds = sessions.Values.Select(s => s.GuardId).Distinct().ToList();
            var guardNames = await _db.PlatformGuards
                .Where(g => guardIds.Contains(g.Id))
                .ToDictionaryAsync(g => g.Id, g => g.Name, ct);

            return visits.Select(v =>
            {
                sessions.TryGetValue(v.SessionId, out var session);
                var isCar = session?.IsPatrolCar ?? Contracts.TrackingUnitKey.IsPosition(v.UnitId);
                var label = !string.IsNullOrWhiteSpace(session?.Callsign) ? session!.Callsign
                    : !string.IsNullOrWhiteSpace(session?.PatrolCarPositionName) ? session!.PatrolCarPositionName
                    : $"Unit {v.UnitId}";
                var until = v.ExitedUtc ?? now;

                return new SiteArrivalDto(
                    v.Id, v.UnitId, v.SiteId, v.SiteName,
                    v.EnteredUtc, v.ConfirmedUtc!.Value, v.ExitedUtc, v.ExitedUtc == null, v.Source)
                {
                    Label = label,
                    GuardName = session != null && guardNames.TryGetValue(session.GuardId, out var name) ? name : null,
                    Kind = isCar ? "car" : "guard",
                    MinutesOnSite = (int)Math.Max(0, (until - v.EnteredUtc).TotalMinutes)
                };
            }).ToList();
        }
    }
}
