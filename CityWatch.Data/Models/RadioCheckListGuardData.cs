using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CityWatch.Data.Models
{
    [Keyless]
    public class RadioCheckListGuardData
    {
        public int ClientSiteId { get; set; }
        public int GuardId { get; set; }
        public string SiteName { get; set; }
        public string Address { get; set; }
        public string GPS { get; set; }
        public string GuardName { get; set; }
        public int LogBook { get; set; }
        public int KeyVehicle { get; set; }
        public int IncidentReport { get; set; }
        public int SmartWands { get; set; }
        public int? RcStatus { get; set; }
        public string RcColor { get; set; }
        public string Status { get; set; }
        public int? RcColorId { get; set; }
        public string OnlySiteName { get; set; } // Task p4#41_A~Z and Z~A sorting issue -- added by Binoy - 31-01-2024

        public int LatestDate { get; set; }
        public int ShowColor { get; set; }

        public int hasmartwand { get; set; }
        
    }

}
