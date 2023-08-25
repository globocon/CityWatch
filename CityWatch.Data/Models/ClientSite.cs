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

        public string GuardLogEmailTo { get; set; }

        public bool DataCollectionEnabled { get; set; }
    }
}
