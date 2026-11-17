using System;
using System.Collections.Generic;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class LanguageMaster
    {


        [Key]
        public int Id { get; set; }
        public string Language { get; set; }
        public bool IsDeleted { get; set; }
       


    }
}
