using CityWatch.Data.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace CityWatch.Data.Models
{
    public  class SiteEquipmentsDetails
    {
        [Key]
        public int Id { get; set; }
        public string Brand { get; set; }
        public int ClientSiteId { get; set; }
        [ForeignKey("ClientSiteId")]
        public ClientSite ClientSite { get; set; }
        public int EquipmentId { get; set; }
        [ForeignKey("EquipmentId")]
        public KPITelematicsField KPITelematicsField { get; set; }
        public string SerialNo { get; set; }
        [NotMapped]
        public string  Equipment { get; set; }
        public bool IsDeleted { get; set; }
    }
}
