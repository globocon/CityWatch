using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class TimeSheet
    {
        public int Id { get; set; }
        public string weekName { get; set; }
        public string Frequency { get; set; }
        public string Email { get; set; }
    }
}
