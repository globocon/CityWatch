using System;
using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Models
{
    public class ClientSiteRadioCheck
    {
        [Key]
        public int Id { get; set; }

        public int ClientSiteId { get; set; }

        public int GuardId { get; set; }

        public DateTime CheckedAt { get; set; }

        public string Status { get; set; }
    }
}
