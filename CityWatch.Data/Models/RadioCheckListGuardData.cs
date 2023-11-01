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
        public string SmartWands { get; set; }
        

    }

}
