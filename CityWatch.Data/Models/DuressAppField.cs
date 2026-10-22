using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace CityWatch.Data.Models
{
    public class DuressAppField
    {
        [Key]
        public int Id { get; set; }

        public int TypeId { get; set; }
        public int? ProfileId { get; set; }
        public string Name { get; set; }
        public string Label { get; set; }

        [NotMapped]
        public MobileLogActivityProfile Profile { get; set; }
    }
}
