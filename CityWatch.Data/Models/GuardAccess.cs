using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class GuardAccess
    {
        [Key]
        public int Id { get; set; }
        public string AccessName { get; set; }
    }
}
