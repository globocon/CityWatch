using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Models
{   
    public class ClientSiteMobileCrowdControlDTO
    {   
        public int ClientSiteId { get; set; }
        public bool IsCrowdCountEnabled { get; set; } = false;
        public bool IsDoorEnabled { get; set; } = false;
        public bool IsGateEnabled { get; set; } = false;
        public bool IsLevelFloorEnabled { get; set; } = false;
        public bool IsRoomEnabled { get; set; } = false;
        public int CounterQuantity { get; set; } = 0;

        public int CurrentCount { get; set; } = 0;
        public int TotalCount { get; set; } = 0;
        public int TillDateCount { get; set; } = 0;
        public string TillDate { get; set; }
        public Dictionary<string, int> CounterNameAndCount { get; set; } = new Dictionary<string, int>();

    }
}
