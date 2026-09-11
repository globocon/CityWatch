using System;

namespace CityWatch.Web.Models
{
    public class PatrolCarLogRequestLocalCacheOffline
    {
        public int CacheId { get; set; }
        public int SiteId { get; set; }
        public int Id { get; set; }
        public int ClientSiteLogBookId { get; set; }
        public decimal Mileage { get; set; }
        public string MileageText { get; set; }
        public string PatrolCar { get; set; }
        public DateTime? EventDateTimeLocal { get; set; }
        public DateTimeOffset? EventDateTimeLocalWithOffset { get; set; }
        public string EventDateTimeZone { get; set; }
        public string EventDateTimeZoneShort { get; set; }
        public int? EventDateTimeUtcOffsetMinute { get; set; }
        public bool IsSynced { get; set; } = false;
        public Guid UniqueRecordId { get; set; }
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }

        //---------------------------------------------------------
        //public ClientSitePatrolCarCache ClientSitePatrolCar { get; set; }
        public int PatrolCarId { get; set; }
        public string Model { get; set; }
        public string Rego { get; set; }
        public int ClientSiteId { get; set; }
        //---------------------------------------------------------
    }
    
}
