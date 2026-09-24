using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CityWatch.Data.Models
{
    public  class RadioCheckListGuardIncidentReportData
    {
        public int Id { get; set; }
        public int ClientSiteId { get; set; }
        public int GuardId { get; set; }
        public string SiteName { get; set; }
        public string GuardName { get; set; }
        public int IncidentReportId { get; set; }
        public string FileName { get; set; }
     
        public string Activity { get; set; }
        public string IncidentReportCreatedTime { get; set; }
    }
}
