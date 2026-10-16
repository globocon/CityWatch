using System;
using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Models
{   
    public class ClientSiteMobileCrowdControlAuditLog
    {
        [Key]
        public int Id { get; set; }
        public int? ClientSiteId { get; set; }
        public DateTime ActionTimeServer { get; set; } = DateTime.Now;
        public DateTime ActionTimeUTC { get; set; } = DateTime.UtcNow;
        public DateTime? ActionTimeLocal { get; set; }
        public string TimeUTC { get; set; }
        public string ActionDescription { get; set; } 
        

    }
}
