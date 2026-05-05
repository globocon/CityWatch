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

        public int? PayRateId { get; set; }

        [ForeignKey("PayRateId")]
        public virtual PayRate PayRate { get; set; }

        public int? CallsignId { get; set; }

        [ForeignKey("CallsignId")]
        public virtual IncidentReportField Callsign { get; set; }

        public int? ReliefGuardId { get; set; }

        [ForeignKey("ReliefGuardId")]
        public virtual Guard ReliefGuard { get; set; }

        [StringLength(255)]
        public string ReliefProviderName { get; set; }

        public string ReliefReason { get; set; }

        public string ReliefReasonOther { get; set; }
        
        [StringLength(50)]
        public string ShiftType { get; set; }

        [StringLength(255)]
        public string AdhocOffsiteText { get; set; }

        public bool IsDeleted { get; set; }
    }
}
