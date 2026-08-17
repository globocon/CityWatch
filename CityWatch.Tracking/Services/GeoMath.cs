using System;

namespace CityWatch.Tracking.Services
{
    /// <summary>Haversine helpers. Sufficient at patrol scale; a routing engine replaces
    /// straight-line assumptions in Phase 3, not this.</summary>
    internal static class GeoMath
    {
        private const double EarthRadiusKm = 6371.0;

        public static double HaversineKm(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
        {
            double la1 = (double)lat1 * Math.PI / 180, la2 = (double)lat2 * Math.PI / 180;
            double dLa = la2 - la1, dLo = ((double)lon2 - (double)lon1) * Math.PI / 180;
            double a = Math.Sin(dLa / 2) * Math.Sin(dLa / 2)
                     + Math.Cos(la1) * Math.Cos(la2) * Math.Sin(dLo / 2) * Math.Sin(dLo / 2);
            return 2 * EarthRadiusKm * Math.Asin(Math.Min(1, Math.Sqrt(a)));
        }

        public static double ImpliedSpeedKph(decimal lat1, decimal lon1, decimal lat2, decimal lon2, double hours)
            => hours <= 0 ? double.MaxValue : HaversineKm(lat1, lon1, lat2, lon2) / hours;
    }
}
