using System.Text.Json.Serialization;
using System;

namespace CityWatch.Kpi.Models
{
    public class DailyLogTimer
    {
        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("isAcceptable")]
        public bool? IsAcceptable { get; set; }
    }
}
