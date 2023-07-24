using System;
using System.Text.Json.Serialization;

namespace CityWatch.Kpi.Models
{
    public class DailyWandScanCount
    {
        [JsonPropertyName("row_number")]
        public int RowNumber { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("wand_scans")]
        public int Count { get; set; }

        [JsonPropertyName("client_name")]
        public string ClientName { get; set; }

        [JsonPropertyName("client_site_name")]
        public string ClientSiteName { get; set; }
    }
}