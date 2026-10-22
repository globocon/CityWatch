using CityWatch.Data;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace CityWatch.RadioCheck.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class BroadCastBannerCalendarController : ControllerBase
    {
        private readonly CityWatchDbContext _context;
        public BroadCastBannerCalendarController(CityWatchDbContext context)
        {
            _context = context;
        }

        [Route("[action]", Name = "BroadCastBannerCalendar")]
        [HttpGet]
        public bool BroadCastBannerCalendar()
        {
            var expevents = _context.BroadcastBannerCalendarEvents.Where(x => x.ExpiryDate.Date < DateTime.Today.Date && x.RepeatYearly == true).ToList();
            foreach(var expevent in expevents)
            {
                expevent.StartDate = expevent.StartDate.AddYears(1);
                expevent.ExpiryDate = expevent.ExpiryDate.AddYears(1);
                _context.SaveChanges();
            }
            return true;
        }



    }
}
