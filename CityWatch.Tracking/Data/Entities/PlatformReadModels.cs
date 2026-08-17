using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Tracking.Data.Entities
{
    /* Read-only projections of two PLATFORM tables, mapped column-subset only.
       This is how the pack resolves unit kind (car vs guard) and guard names without a
       project reference to CityWatch.Data — the same one-way boundary §13.3 uses for scope.
       Nothing here is ever written: the platform owns these tables; the pack reads them. */

    /// <summary>ClientSiteSmartWands, read-only. PatrolCarId decides the map symbol:
    /// a wand allocated to a patrol car renders as a car, anything else as a guard.</summary>
    [Table("ClientSiteSmartWands")]
    public class PlatformSmartWand
    {
        [Key]
        public int Id { get; set; }

        public int ClientSiteId { get; set; }

        public int? PatrolCarId { get; set; }

        public bool IsDeleted { get; set; }
    }

    /// <summary>Guards, read-only. Names for the control-room display and idle list.</summary>
    [Table("Guards")]
    public class PlatformGuard
    {
        [Key]
        public int Id { get; set; }

        public string? Name { get; set; }
    }

    /// <summary>ClientSites, read-only. The geofence catalogue: where the sites ARE, so an
    /// arrival can be detected from GPS instead of waiting for a tag that may never be
    /// scanned. Gps is the platform's own free-text "lat,lon" column — parsed defensively,
    /// never written back.</summary>
    [Table("ClientSites")]
    public class PlatformClientSite
    {
        [Key]
        public int Id { get; set; }

        public string? Name { get; set; }

        /// <summary>"-37.81805,145.1849757". Free text: blank, malformed and out-of-range
        /// values all exist in the wild and are simply skipped.</summary>
        public string? Gps { get; set; }

        public bool IsActive { get; set; }
    }
}
