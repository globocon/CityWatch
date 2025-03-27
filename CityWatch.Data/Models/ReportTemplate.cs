using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class ReportTemplate
    {
        [Key]
        public int Id { get; set; }
        public string Path { get; set; }
        public DateTime LastUpdated { get; set; }
        public string DefaultEmail { get; set; }
        public int SubDomainId { get; set; }
        public string FileName { get; set; }

        [NotMapped]
        public string Domain { get; set; }

        [NotMapped]
        public int DomainId { get; set; }
    }
}
