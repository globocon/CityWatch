using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    [Keyless]
    //public class PcarRoutesForClients
    //{
    //    public int PcarRouteId { get; set; }
    //    public PcarRoute PcarRoute { get; set; }
    //    public ClientSite ClientSite { get; set; }
    //    public List<PcarRouteDetails> Details { get; set; }
    //}
    public class PcarRoutesForClients
    {
        public int PcarRouteId { get; set; }
     
        public string RouteName { get; set; }

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }

        public string Gps { get; set; }
      

      

        public List<RouteSiteDto> Sites { get; set; }
    }

    public class RouteSiteDto
    {
        public int Sequence { get; set; }

        public int ClientSiteId { get; set; }

        public string SiteName { get; set; }

        public string Gps { get; set; }

       

        public int Visits { get; set; }
    }
}
