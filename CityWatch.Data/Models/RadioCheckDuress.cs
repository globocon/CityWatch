using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class RadioCheckDuress
    {
        [Key]
        public int Id { get; set; }
        public int UserID { get; set; }
        public bool IsActive { get; set; }
        public DateTime CurrentDateTime { get; set; }
    }
}
