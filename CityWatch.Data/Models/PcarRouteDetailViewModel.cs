using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class PcarRouteDetailViewModel
    {
        public int ClientSiteId { get; set; } // Selected client site ID
        public int OrderNo { get; set; }

        // Daily schedule times and visit counts
        public string StartMon { get; set; }
        public string EndMon { get; set; }
        public int VisitMon { get; set; }

        public string StartTue { get; set; }
        public string EndTue { get; set; }
        public int VisitTue { get; set; }

        public string StartWed { get; set; }
        public string EndWed { get; set; }
        public int VisitWed { get; set; }

        public string StartThu { get; set; }
        public string EndThu { get; set; }
        public int VisitThu { get; set; }

        public string StartFri { get; set; }
        public string EndFri { get; set; }
        public int VisitFri { get; set; }

        public string StartSat { get; set; }
        public string EndSat { get; set; }
        public int VisitSat { get; set; }

        public string StartSun { get; set; }
        public string EndSun { get; set; }
        public int VisitSun { get; set; }

        public string StartPho { get; set; } // Public holiday start
        public string EndPho { get; set; }   // Public holiday end
        public int VisitPho { get; set; }    // Public holiday visits
    }

    public class PcarRouteSaveViewModel
    {
        public int PcarRouteId { get; set; } // Master Route ID
        public List<PcarRouteDetailViewModel> SiteSchedules { get; set; }
    }
}
