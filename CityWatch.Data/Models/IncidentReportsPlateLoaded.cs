using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class IncidentReportsPlatesLoaded
    {
        [Key]
        //public int Id { get; set; }
        //public int IncidentReportId { get; set; }
        public int PlateId { get; set; }
        public string TruckNo { get; set; }               
       
    }
}
