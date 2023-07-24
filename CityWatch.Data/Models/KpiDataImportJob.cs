using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class KpiDataImportJob
    {
        [Key]
        public int Id { get; set; }

        public int ClientSiteId { get; set; }

        public DateTime ReportDate { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? CompletedDate { get; set; }

        public bool? Success { get; set; }

        public string StatusMessage { get; set; }  

        [ForeignKey("ClientSiteId")]
        public ClientSite ClientSite { get; set; }
    }
}
