using CityWatch.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Data.Providers
{
    public interface IKpiDataProvider
    {
        List<DailyClientSiteKpi> GetDailyClientSiteKpis(int clientSiteId, DateTime fromDate, DateTime toDate);
        List<DailyClientSiteKpi> GetDailyClientSiteKpis(int[] clientSiteId, DateTime fromDate, DateTime toDate);
        void UpdateActualEmployeeHours(int id, decimal? actualEmpHours);
    }

    public class KpiDataProvider : IKpiDataProvider
    {
        private readonly CityWatchDbContext _context;

        public KpiDataProvider(CityWatchDbContext context)
        {
            _context = context;
        }

        public List<DailyClientSiteKpi> GetDailyClientSiteKpis(int clientSiteId, DateTime fromDate, DateTime toDate)
        {
            return _context.DailyClientSiteKpis
                .Where(x => x.ClientSiteId == clientSiteId && x.Date >= fromDate && x.Date <= toDate)
                .OrderBy(x => x.Date)
                .ToList();
        }

        public List<DailyClientSiteKpi> GetDailyClientSiteKpis(int[] clientSiteIds, DateTime fromDate, DateTime toDate)
        {
            return _context.DailyClientSiteKpis
               .Where(x => clientSiteIds.Contains(x.ClientSiteId) && x.Date >= fromDate && x.Date <= toDate)
               .OrderBy(x => x.Date)
               .ToList();
        }

        public void UpdateActualEmployeeHours(int id, decimal? actualEmpHours)
        {
            var dailyClientSiteKpiToUpdate = _context.DailyClientSiteKpis.SingleOrDefault(x => x.Id == id);

            if (dailyClientSiteKpiToUpdate != null)
            {
                dailyClientSiteKpiToUpdate.ActualEmployeeHours = actualEmpHours;
            }

            _context.SaveChanges();
        }
    }
}
