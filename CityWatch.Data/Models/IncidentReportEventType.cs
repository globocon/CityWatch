using CityWatch.Data.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CityWatch.Data.Models
{
    public class IncidentReportEventType
    {
        [Key]
        public int Id { get; set; }
        
        public int ReportId { get; set; }
                
        public IrEventType EventType { get; set; }

        [JsonIgnore]
        [ForeignKey("ReportId")]
        public IncidentReport Report { get; set; }
    }
}
