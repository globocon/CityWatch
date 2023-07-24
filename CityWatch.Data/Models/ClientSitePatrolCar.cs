using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class ClientSitePatrolCar
    {
        [Key]
        public int Id { get; set; }

        public string Model { get; set; }

        public string Rego { get; set; }

        public int ClientSiteId { get; set; }

        [ForeignKey("ClientSiteId")]
        public ClientSite ClientSite { get; set; }
    }
}
