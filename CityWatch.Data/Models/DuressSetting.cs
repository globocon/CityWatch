using System;

namespace CityWatch.Data.Models
{
    public class DuressSetting
    {
        public int Id { get; set; }
        public int ClientSiteId { get; set; }
        public string PositionFilter { get; set; } = string.Empty;
        public int SelectedPosition { get; set; }
        public int SiteDuressNumber { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}
