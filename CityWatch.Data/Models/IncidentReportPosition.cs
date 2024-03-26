using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        public bool IsLogbook { get; set; }
        public int? ClientsiteId { get; set; }
        public string ClientsiteName { get; set; }

    }
}
