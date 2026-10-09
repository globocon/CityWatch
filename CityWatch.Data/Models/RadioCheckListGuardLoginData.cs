using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CityWatch.Data.Models
{
    public class RadioCheckListGuardLoginData
    {
        public int Id { get; set; }
        public int ClientSiteId { get; set; }
        public int GuardId { get; set; }
        public string SiteName { get; set; }
        public string GuardName { get; set; }
        public int LogBookId { get; set; }
        public string Activity { get; set; }
        public string LogBookCreatedTime { get; set; }
        public string Notes { get; set; }
        public string GPSCoordinates { get; set; }

    }
}
