using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class Courses
    {
        [Key]
        public int Id { get; set; }
        public string FileName { get; set; }
        public DateTime LastUpdated { get; set; }
        public int HRSettingsId { get; set; }
        public int TQNumberId { get; set; }
        [ForeignKey("HRSettingsId")]
        public HrSettings HrSetting { get; set; }
        [ForeignKey("TQNumberId")]
        public TQNumbers TQNumber { get; set; }
        [NotMapped]
        public string FormattedLastUpdated { get { return LastUpdated.ToString("dd MMM yyyy @ HH:mm"); } }

    }
}
