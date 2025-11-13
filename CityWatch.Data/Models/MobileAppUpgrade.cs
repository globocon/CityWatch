using System;
using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Models
{
    public class MobileAppUpgrade
    {
        [Key]
        public int Id { get; set; }
        [MaxLength(250)]
        public string AppType { get; set; }        
        public int AppVersionMajor { get; set; }
        public int AppVersionMinor { get; set; }
        public int AppVersionPatch { get; set; }
        public string AppDownloadUrl { get; set; }
        public string AppVersionNotes { get; set; }
        public DateTime RecordCreateDTM { get; set; } = DateTime.Now;
        public int TotalDownloadCount { get; set; } = 0;
        public bool IsActive { get; set; } = false;
        public string FileName { get; set; }
    }
}
