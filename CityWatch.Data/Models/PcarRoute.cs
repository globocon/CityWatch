using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class PcarRoute
    {
        public PcarRoute()
        {
            RouteDetails = new List<PcarRouteDetails>();
        }

        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Pcarroutename { get; set; }

        [Required]
        public int Smartwandallocation { get; set; }

        [ForeignKey("Smartwandallocation")]
        public ClientSiteSmartWand SmartWand { get; set; }   // Navigation property

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        // Navigation Property
        public ICollection<PcarRouteDetails> RouteDetails { get; set; }
    }

    public class PcarRouteDetails
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PcarRouteId { get; set; }

        [Required]
        public int ClientSiteId { get; set; }   // MUST MATCH SQL COLUMN NAME

        public int OrderNo { get; set; }

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

        public string StartPho { get; set; }
        public string EndPho { get; set; }
        public int VisitPho { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [ForeignKey("PcarRouteId")]
        public PcarRoute PcarRoute { get; set; }

        [ForeignKey("ClientSiteId")]
        public ClientSite ClientSite { get; set; }
    }


    public class PcarRouteDailyVisits
    {
        public int Id { get; set; }

        public int SmartWandId { get; set; }
        public int SiteId { get; set; }
        public int GuardId { get; set; }

        public int LoginUserId { get; set; }
        public int LoginSiteId { get; set; }

        public string VisitName { get; set; }
        public int VisitNumber { get; set; }
        public string DayName { get; set; }

        public int PcarRouteId { get; set; }
        public int PcarRouteDetailsId { get; set; }

        public string TimeOn { get; set; }
        public string TimeOff { get; set; }

        public string GpsCoordinates { get; set; }
        public DateTime CreatedAt { get; set; }
        public Enums.PcarVisitStatusEnum? Status { get; set; }
        public int? PushedTo { get; set; }
    }

}
