using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace CityWatch.Data.Models
{
    public class ClientSiteSmartWand
    {
        [Key]
        public int Id { get; set; }

        public int ClientSiteId { get; set; }

        public string SmartWandId { get; set; }

        public string PhoneNumber { get; set; }
        public string SIMProvider { get; set; }
        public string IMEI { get; set; }
        public bool IsDeleted { get; set; }
        [MaxLength(100)]
        public string? DeviceType { get; set; }
        [MaxLength(150)]
        public string? DeviceId { get; set; }
        [MaxLength(250)]
        public string? DeviceName { get; set; }

        [NotMapped]
        public bool IsInUse { get; set; }

        [ForeignKey("ClientSiteId")]
        public ClientSite ClientSite { get; set; }
        public int? PatrolCarId { get; set; }

        [NotMapped]
        public string? PatrolCarName { get; set; }
    }
}
