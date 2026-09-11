using iText.Layout.Element;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{   
    public class ClientSiteMobileCrowdControlHistory
    {
        [Key]
        public int HistoryId { get; set; }
        public int Id { get; set; }
        public int ClientSiteId { get; set; }
        public int Tcount { get; set; }
        public int Ccount { get; set; }
        public DateTime? CrowdControlDate { get; set; }
        public DateTime? LastUpdateTime { get; set; }
        public DateTime? ArchivedOn { get; set; }
        public string ArchivedMode { get; set; }
        public int? ArchivedUserId { get; set; }
        public int? ArchivedGuardId { get; set; }

    }
    
}
