using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class GuardLogin
    {
        [Key]
        public int Id { get; set; }

        public int GuardId { get; set; }

        public DateTime LoginDate { get; set; }

        public int ClientSiteId { get; set; }

        public int? SmartWandId { get; set; }

        public int? PositionId { get; set; }

        public DateTime OnDuty { get; set; }

        public DateTime? OffDuty { get; set; }

        public int UserId { get; set; }

        public int ClientSiteLogBookId { get; set; }

        public string IPAddress { get; set; }

        [ForeignKey("GuardId")]
        public Guard Guard { get; set; }

        [ForeignKey("ClientSiteId")]
        public ClientSite ClientSite { get; set; }

        [ForeignKey("SmartWandId")]
        public ClientSiteSmartWand SmartWand { get; set; }

        [ForeignKey("PositionId")]
        public IncidentReportPosition Position { get; set; }

        [ForeignKey("ClientSiteLogBookId")]
        public ClientSiteLogBook ClientSiteLogBook { get; set; }


    }

}