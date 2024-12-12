using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class LanguageMaster
    {
        public int Id { get; set; }
        public string Language { get; set; }
        public bool IsDeleted { get; set; }
    }
}
