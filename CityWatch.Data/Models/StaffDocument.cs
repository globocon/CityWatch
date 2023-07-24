using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class StaffDocument
    {
        [Key]
        public int Id { get; set; }
        public string FileName { get; set; }
        public DateTime LastUpdated { get; set; }

        [NotMapped]
        public string FormattedLastUpdated { get { return LastUpdated.ToString("dd MMM yyyy @ HH:mm"); } } 
    }
}
