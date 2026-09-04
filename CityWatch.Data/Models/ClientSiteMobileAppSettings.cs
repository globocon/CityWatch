using System;
using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Models
{   
    public class ClientSiteMobileAppSettings
    {
        [Key]
        public int Id { get; set; }
        public int ClientSiteId { get; set; }
        public bool IsCrowdCountEnabled { get; set; } = false;
        public bool IsDoorEnabled { get; set; } = false;
        public bool IsGateEnabled { get; set; } = false;
        public bool IsLevelFloorEnabled { get; set; } = false;
        public bool IsRoomEnabled { get; set; } = false;
        public int CounterQuantity { get; set; } = 0;

    }
}
