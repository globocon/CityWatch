using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CityWatch.Data.Enums;

namespace CityWatch.Data.Models
{
    public class RosterSchedule
    {
        [Key]
        public int Id { get; set; }

        public int RosterGroupId { get; set; }

        [ForeignKey("RosterGroupId")]
        public virtual RosterGroup RosterGroup { get; set; }

        public int ClientSiteId { get; set; }

        [ForeignKey("ClientSiteId")]
        public virtual ClientSite ClientSite { get; set; }

        public int? GuardId { get; set; }

        [ForeignKey("GuardId")]
        public virtual Guard Guard { get; set; }

        [StringLength(255)]
        public string ProviderName { get; set; }

        public DateTime ShiftStart { get; set; }

        public DateTime ShiftEnd { get; set; }

        public RosterShiftStatus Status { get; set; }

        public bool IsDeleted { get; set; }
    }
}
