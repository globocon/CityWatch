using System;
using System.Collections.Generic;

namespace CityWatch.Tracking.Services
{
    /// <summary>A meaningful halt in a trail: where, when, and for how long.</summary>
    public sealed record DetectedStop(decimal Lat, decimal Lon, DateTime FromUtc, DateTime ToUtc)
    {
        public int DurationMinutes => (int)Math.Max(0, (ToUtc - FromUtc).TotalMinutes);
    }

    /// <summary>
    /// Historical stop detection over an ordered point stream (§Phase 2.2).
    /// </summary>
    /// <remarks>
    /// Deliberately NOT the live idle detector reused blindly: idle detection asks "is this
    /// unit sitting still right now", this asks "where did the journey pause" over a finished
    /// trail. The rule is shared in spirit — a cluster of fixes inside a small radius — and
    /// both ignore jitter the same way: staying within <c>radiusM</c> of the cluster's centre
    /// counts as stationary no matter how the individual fixes wobble.
    /// Pure and allocation-light so replay can run it over 5 000 points without noticing.
    /// </remarks>
    public static class StopDetector
    {
        public readonly record struct TrailPoint(decimal Lat, decimal Lon, DateTime Utc);

        /// <summary>Finds stops of at least <paramref name="minMinutes"/> where consecutive
        /// fixes stay within <paramref name="radiusM"/> of the running centroid.</summary>
        public static List<DetectedStop> Detect(IReadOnlyList<TrailPoint> points,
            double radiusM = 60, int minMinutes = 4)
        {
            var stops = new List<DetectedStop>();
            if (points == null || points.Count < 2)
                return stops;

            int anchor = 0;
            double sumLat = (double)points[0].Lat, sumLon = (double)points[0].Lon;
            int n = 1;

            for (var i = 1; i <= points.Count; i++)
            {
                var inside = false;
                if (i < points.Count)
                {
                    var cLat = (decimal)(sumLat / n);
                    var cLon = (decimal)(sumLon / n);
                    inside = GeoMath.HaversineKm(cLat, cLon, points[i].Lat, points[i].Lon) * 1000 <= radiusM;
                }

                if (inside)
                {
                    sumLat += (double)points[i].Lat;
                    sumLon += (double)points[i].Lon;
                    n++;
                    continue;
                }

                /* The cluster ended (or the trail did). Long enough to mean something? */
                var last = Math.Min(i - 1, points.Count - 1);
                if (last > anchor &&
                    (points[last].Utc - points[anchor].Utc).TotalMinutes >= minMinutes)
                {
                    stops.Add(new DetectedStop(
                        (decimal)(sumLat / n), (decimal)(sumLon / n),
                        points[anchor].Utc, points[last].Utc));
                }

                if (i < points.Count)
                {
                    anchor = i;
                    sumLat = (double)points[i].Lat;
                    sumLon = (double)points[i].Lon;
                    n = 1;
                }
            }

            return stops;
        }
    }
}
