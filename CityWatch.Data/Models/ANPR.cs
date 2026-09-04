using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class ANPR
    {
        public int Id { get; set; }

        public int ClientSiteId { get; set; }

        public string profile { get; set; }

      
        public string Apicalls { get; set; }

        [ForeignKey("ClientSiteId")]
        public ClientSite ClientSite { get; set; }
        public string LaneLabel { get; set; }
        public bool IsDisabled { get; set; }
        public bool IsSingleLane { get; set; }
        public bool IsSeperateEntryAndExitLane { get; set; }

    }
}
