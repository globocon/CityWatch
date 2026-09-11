using System;
using System.ComponentModel.DataAnnotations;

namespace CityWatch.Tracking.Data.Entities
{
    /// <summary>
    /// One resolved street address per ~110 m grid cell (lat/lon x 1000, floored).
    /// </summary>
    /// <remarks>
    /// The cache IS the rate-limit protection: a patrol fleet revisits the same streets all
    /// shift, so after the first day the geocoder is barely called at all. A failed lookup is
    /// cached too (Address null) so an outage never turns into a hammering retry loop - it is
    /// retried only after the failure window lapses. Deployed by DbScript 368.
    /// </remarks>
    public class GeocodeCache
    {
        [Key]
        public long Id { get; set; }

        /// <summary>floor(latitude x 1000): ~110 m of latitude per cell.</summary>
        public int CellLat { get; set; }

        /// <summary>floor(longitude x 1000).</summary>
        public int CellLon { get; set; }

        /// <summary>Resolved display address ("Main Road, Pala"); null when the last
        /// lookup failed - cached so failures are not retried per request.</summary>
        [MaxLength(300)]
        public string? Address { get; set; }

        public DateTime ResolvedUtc { get; set; }
    }
}
