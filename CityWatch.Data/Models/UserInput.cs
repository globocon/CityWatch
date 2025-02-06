using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class UserInput 
    {
        
        [Key]
        public int Id { get; set; }

        [Required]
        public string Text { get; set; } // Changed from int to string

        public DateTime UpdatedDate { get; set; } // Changed from int to DateTime
    }
}
