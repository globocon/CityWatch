using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Models
{
    public class MobileLogActivityProfile
    {
        [Key]
        public int Id { get; set; }
        public string ProfileName { get; set; }
        public bool IsDefault { get; set; } = false;
    }
}
