using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class ClientSiteToggle
    {
        [Key]
        public int Id { get; set; }

        public int ClientSiteId { get; set; }

        public int ToggleTypeId { get; set; }

        public bool IsActive { get; set; }
        [ForeignKey("ClientSiteId")]
        public ClientSite ClientSite { get; set; }
    }
}
