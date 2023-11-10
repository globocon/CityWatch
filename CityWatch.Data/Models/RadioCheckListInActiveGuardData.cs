using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    [Keyless]
    public class RadioCheckListInActiveGuardData
    {
        public int ClientSiteId { get; set; }
        public int GuardId { get; set; }
        public string SiteName { get; set; }
        public string GuardName { get; set; }
        public string GuardLoginTime { get; set; }

        public string Address { get; set; }
        public string GPS { get; set; }


        public string TwoHrAlert { get; set; }
        public string RcStatus { get; set; }

    }
}
