using System;
using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Models
{
    public class ClientSiteDuress
    {
        [Key]
        public int Id { get; set; }

        public int ClientSiteId { get; set; }

        public bool IsEnabled { get; set; }

        public int EnabledBy { get; set; }

        public DateTime EnabledDate { get; set; }
    }
}
