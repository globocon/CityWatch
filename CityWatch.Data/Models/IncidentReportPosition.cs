using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Models
{
    public class IncidentReportPosition
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }

        public string EmailTo { get; set; }

        public bool IsPatrolCar { get; set; }

        public string DropboxDir { get; set; }
    }
}
