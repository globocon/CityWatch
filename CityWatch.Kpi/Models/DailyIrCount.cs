using System;
using System.Text.Json.Serialization;

namespace CityWatch.Kpi.Models
{
    public class DailyIrCount
    {
        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("fireOrAlarmCount")]
        public int FireOrAlarmCount { get; set; }
    }
}
