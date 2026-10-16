using CityWatch.Data.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class ClientSite
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }

        public int TypeId { get; set; }

        public string Emails { get; set; }

        public string Address { get; set; }

        public string State { get; set; }

        public string Gps { get; set; }

        public string Billing { get; set; }

        public int Status { get;set; }
        
        public DateTime? StatusDate { get; set; }

        [NotMapped]
        public string FormattedStatusDate { get { return StatusDate.HasValue ? StatusDate.Value.ToString("dd MMM yyyy") : string.Empty; } }

        [ForeignKey("TypeId")]
        public ClientType ClientType { get; set; }

        public string SiteEmail { get; set; }

        public string LandLine { get; set; }

        public string DuressEmail { get; set; }

        public string DuressSms { get; set; }

        public bool UploadGuardLog { get; set; }
        public bool UploadKVLog { get; set; }
        public bool UploadSWLog { get; set; }
        public bool UploadFusionLog { get; set; }

        public string GuardLogEmailTo { get; set; }

        public bool DataCollectionEnabled { get; set; }
        public bool IsActive { get; set; }
        public bool IsDosDontList { get; set; }
        [NotMapped]
        public string AccountManager { get; set; }
        public PatrolTouringMode PatrolTourMode { get; set; }

        public bool MobAppShowClientTypeandSite { get; set; }

        public bool UploadGuardWeeklyLog { get; set; }
        public bool UploadFusionWeeklyLog { get; set; }
        public bool UploadKVWeeklyLog { get; set; }
        public bool UploadSWWeeklyLog { get; set; }
        public string GuardLogEmailWeeklyLogTo { get; set; }

        public bool UploadGuardMonthlyLog { get; set; }
        public bool UploadFusionMonthlyLog { get; set; }
        public bool UploadKVMonthlyLog { get; set; }
        public bool UploadSWMonthlyLog { get; set; }
        public string GuardLogEmailMonthlyLogTo { get; set; }

        // "Disable PCAR" per period: leave patrol car entries out of the LB/SW/Fusion dumps.
        // Never applies to a PCAR site itself - see IsDisablePcarApplicable.
        public bool DisablePcarDailyLog { get; set; }
        public bool DisablePcarWeeklyLog { get; set; }
        public bool DisablePcarMonthlyLog { get; set; }

        // "Schedule PCAR": dumps of the patrol car entries only, to the same recipients as above.
        public bool UploadPcarGuardLog { get; set; }
        public bool UploadPcarSWLog { get; set; }
        public bool UploadPcarFusionLog { get; set; }

        public bool UploadPcarGuardWeeklyLog { get; set; }
        public bool UploadPcarSWWeeklyLog { get; set; }
        public bool UploadPcarFusionWeeklyLog { get; set; }

        public bool UploadPcarGuardMonthlyLog { get; set; }
        public bool UploadPcarSWMonthlyLog { get; set; }
        public bool UploadPcarFusionMonthlyLog { get; set; }

        /// <summary>
        /// Every entry in a PCAR site's own log book is a PCAR entry, so "Disable PCAR" would empty
        /// its reports. It is therefore off and not settable for those sites.
        /// </summary>
        [NotMapped]
        public bool IsDisablePcarApplicable => PatrolTourMode != PatrolTouringMode.PCAR;
    }
}
