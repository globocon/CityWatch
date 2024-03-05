using CityWatch.Data.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;

namespace CityWatch.Data.Providers
{
    public interface ISiteEventLogDataProvider
    {
        void SaveSiteEventLogData(SiteEventLog siteEventLog);
    }
    public class SiteEventLogDataProvider : ISiteEventLogDataProvider
    {
        private readonly CityWatchDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;


        public SiteEventLogDataProvider(IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration,
            CityWatchDbContext context)
        {
            _context = context;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
        }
        public void SaveSiteEventLogData(SiteEventLog siteEventLog)
        {
            if (siteEventLog == null)
                throw new ArgumentNullException();

            if (siteEventLog.Id <=0)
            {
                _context.SiteEventLog.Add(siteEventLog);
            }
          
            _context.SaveChanges();
        }

    }
}
