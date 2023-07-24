using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public enum ReportFieldType
    {
        Position = 1,
        NotifiedBy = 2,
        CallSign = 3,
        ClientArea = 4, 
    }

    public class IncidentReportField
    {
        [Key]
        public int Id { get; set; }
        public ReportFieldType TypeId { get; set; }
        public string Name { get; set; }    
        public string EmailTo { get;set; }
    }
}
