using CityWatch.Data.Models;
using System;

namespace CityWatch.Kpi.Services
{
    public interface ISummaryReportGenerator
    {
        public string GeneratePdfReport(KpiSendSchedule schedule, DateTime fromDate, DateTime toDate);
    }
}
