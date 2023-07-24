using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class ClientSiteRadioStatus
    {
        [Key]
        public int Id { get; set; }

        public int ClientSiteId { get; set; }

        public DateTime CheckDate { get; set; }

        public string Check1 { get; set; }

        public string Check2 { get; set; }

        public string Check3 { get; set; }

        [ForeignKey("ClientSiteId")]
        public ClientSite ClientSite { get; set; }
    }
}
