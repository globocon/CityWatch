using System;
using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Models
{
    public class ReportTemplate
    {
        [Key]
        public int Id { get; set; }
        public string Path { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
