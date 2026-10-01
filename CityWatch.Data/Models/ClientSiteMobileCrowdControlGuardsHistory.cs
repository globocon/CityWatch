using iText.Layout.Element;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{   
    public class ClientSiteMobileCrowdControlGuardsHistory
    {
        [Key]
        public int HistoryId { get; set; }
        public int Id { get; set; }
        public int CrowdControlId { get; set; }
        public int ClientSiteId { get; set; }
        public int UserId { get; set; }
        public int GuardId { get; set; }
        public int Pcount { get; set; }
        public string Location { get; set; }
        public int BadgeNo { get; set; }
        public DateTime? CrowdControlDate { get; set; }
        public DateTime? GuardLastUpdateTime { get; set; }
        public DateTime? ArchivedOn { get; set; }
        public string ArchivedMode { get; set; }
        public int? ArchivedUserId { get; set; }
        public int? ArchivedGuardId { get; set; }

    }
    
}
