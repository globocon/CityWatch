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

        [Display(Name = "Fusion Log")]
        FusionLog = 3,

        [Display(Name = "Smart Wand Log")]
        SmartWandLog = 4,

        /* The "Schedule PCAR" dumps: the patrol car entries of the guard log, smart wand log and
           fusion log. Never written by guards - they exist only so the scheduler can record that a
           PCAR dump was sent (a ClientSiteLogBooks row for the daily dump, a
           ClientSitePeriodicLogUploads row for weekly/monthly) separately from the normal dump. */
        [Display(Name = "Daily Guard Log (PCAR)")]
        PcarGuardLog = 5,

        [Display(Name = "Smart Wand Log (PCAR)")]
        PcarSmartWandLog = 6,

        [Display(Name = "Fusion Log (PCAR)")]
        PcarFusionLog = 7,
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
