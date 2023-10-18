using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class RadioCheckListNotAvailableGuardData
    {
        public int Id { get; set; }
        public int ClientSiteId { get; set; }
        public int GuardId { get; set; }
        public string SiteName { get; set; }
        public string GuardName { get; set; }
        public string GuardLastLoginDate { get; set; }
    }
}
