using Dropbox.Api.TeamLog;
using iText.Layout.Element;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{   
    public class ClientSiteMobileCrowdControl
    {
        [Key]
        public int Id { get; set; }
        public int ClientSiteId { get; set; }
        public int Tcount { get; set; }
        public int Ccount { get; set; }
        public DateTime? CrowdControlDate { get; set; }
        public DateTime? LastUpdateTime { get; set; }
        [NotMapped]
        public List<ClientSiteMobileCrowdControlGuards>? ClientSiteCrowdControlGuards { get; set; }

    }
    public class ClientSiteMobileCrowdControlGuards
    {
        [Key]
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

    }

    public class MobileCrowdControlGuard
    {
        public int ClientSiteId { get; set; }
        public int UserId { get; set; }
        public int GuardId { get; set; }
        public string Location { get; set; }
        public int BadgeNo { get; set; }
    }

    public class ClientSiteMobileCrowdControlData
    {
        public int ClientSiteId { get; set; }
        public int Count { get; set; }
        public bool AddCount { get; set; }
        [NotMapped]
        public List<ClientSiteMobileCrowdControlGuards>? ClientSiteCrowdControlGuards { get; set; }
    }
}
