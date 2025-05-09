using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{

    public class GuardHoursByQuarterViewModel
    {
        [Key]
        public int GuardId { get; set; }
        public string Name { get; set; }
        public int Q1HRS2023 { get; set; }
        public int Q2HRS2023 { get; set; }
        public int Q3HRS2023 { get; set; }
        public int Q4HRS2023 { get; set; }
        public int Q1HRS2024 { get; set; }
        public int Q2HRS2024 { get; set; }
        public int Q3HRS2024 { get; set; }
        public int Q4HRS2024 { get; set; }
        
        
    }
}
