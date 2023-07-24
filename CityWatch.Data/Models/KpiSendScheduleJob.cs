using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class KpiSendScheduleJob
    {
        [Key]
        public int Id { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? CompletedDate { get; set; }

        public bool? Success { get; set; }

        public string StatusMessage { get; set; }
    }
}
