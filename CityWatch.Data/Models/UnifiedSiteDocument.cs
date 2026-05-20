using System;

namespace CityWatch.Data.Models
{
    public class UnifiedSiteDocument
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string SourceTable { get; set; }
        public string Category { get; set; }
        public DateTime? LastUpdated { get; set; }
        public string FilePath { get; set; }
        public string Base64Data { get; set; }
    }
}
