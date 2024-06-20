using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class HrSettingsClientStates
    {
        [Key]
        public int Id { get; set; } 

        public int HrSettingsId { get; set; }

        [ForeignKey("HrSettingsId")]
        public string State { get; set; }
    }
}
