using System;
using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Models
{   
    public class ClientSiteMobileCrowdControl
    {
        [Key]
        public int Id { get; set; }
        public int ClientSiteId { get; set; }
        public int Tcount { get; set; }
        public int Ccount { get; set; }
        public DateOnly? CrowdControlDate { get; set; }
        public DateTime? LastUpdateTime { get; set; }

    }
}
