using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Models
{
    public class ClientType
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
