using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class IncidentReport
    {
        [Key]
        public int Id { get; set; }
        public string FileName { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? ReportDateTime { get; set; }
        public DateTime? IncidentDateTime { get; set; }
        public string JobNumber { get; set; }
        public string JobTime { get; set; }               
        public string CallSign { get; set; }
        public string NotifiedBy { get; set; }
        public string Billing { get; set; }
        public string OccurNo { get; set; }
        public string ActionTaken { get; set; }
        public bool IsPatrol { get; set; }
        public string Position { get; set; }
        public int? ClientSiteId { get; set; }
        public bool IsEventFireOrAlarm { get; set; }
        public string ClientArea { get; set; }
        [ForeignKey("ClientSiteId")]
        public ClientSite ClientSite { get; set; }
        public string SerialNo { get; set; }
        public bool DbxUploaded { get; set; }
        public int? ColourCode { get; set; }
        public bool IsPlateLoaded { get; set; }
        public ICollection<IncidentReportEventType> IncidentReportEventTypes { get; set; }
    }
}
