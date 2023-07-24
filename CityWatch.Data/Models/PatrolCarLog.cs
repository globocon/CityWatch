using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class PatrolCarLog
    {
        [Key]
        public int Id { get; set; }

        public int PatrolCarId { get; set; }

        public int ClientSiteLogBookId { get; set; }

        public decimal Mileage { get; set; }

        [NotMapped]
        public string MileageText { get { return Mileage.ToString("N0"); }  }

        [ForeignKey("PatrolCarId")]
        public ClientSitePatrolCar ClientSitePatrolCar { get; set; }

        [ForeignKey("ClientSiteLogBookId")]
        public ClientSiteLogBook ClientSiteLogBook { get; set; }
    }
}
