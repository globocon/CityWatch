using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class SmartWandTagsType
    {
        [Key]
        public int Id { get; set; }


        public string value { get; set; }
       
    }
}
