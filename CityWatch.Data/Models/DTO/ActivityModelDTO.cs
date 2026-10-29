using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models.DTO
{
    public class ActivityModelDTO
    {
        public int Id { get; set; }
        public int ClienSiteId { get; set; }
        public string Name { get; set; }
        public string Label { get; set; }
    }
}
