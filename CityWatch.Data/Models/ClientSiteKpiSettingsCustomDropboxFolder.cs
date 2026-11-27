using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class ClientSiteKpiSettingsCustomDropboxFolder
    {
        [Key]
        public int Id { get; set; }
        public int ClientSiteId { get; set; }
        public string DropboxFolderName { get; set;}
    }


    public class KpiDropboxSettings
    {        
        public int Id { get; set; }
        public int ClientSiteId { get; set; }
        public string DropboxImagesDir { get; set; }
        public bool IsThermalCameraSite { get; set; }
        public bool IsWeekendOnlySite { get; set; }
        public bool KpiTelematicsAndStatistics { get; set; } = true;
        public bool SmartWandPatrolReports { get; set; } = true;
        public bool MonthlyClientReport { get; set; } = false;
        public bool DropboxScheduleisActive { get; set; } = true;
    }
}
