using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public enum LogBookType
    {
        [Display(Name = "Daily Guard Log")]
        DailyGuardLog = 1,

        [Display(Name = "Key & Vehicle Log")]
        VehicleAndKeyLog = 2,
    }

    public class ClientSiteLogBook
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Client Site is required")]
        public int ClientSiteId { get; set; }

        public LogBookType Type { get; set; }

        public DateTime Date { get; set; }

        public bool DbxUploaded { get; set; }

        public string FileName { get; set; }

        [ForeignKey("ClientSiteId")]
        public ClientSite ClientSite { get; set; }
    }
}
