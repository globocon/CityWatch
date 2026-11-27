using System;
using System.Collections.Generic;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class TrainingLocation
    {
        [Key]
        public int Id { get; set; }
        public string Location { get; set; }
        public bool IsDeleted { get; set; }
    }
}
