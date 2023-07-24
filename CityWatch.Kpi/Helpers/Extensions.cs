using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Kpi.Helpers
{
    public static class Extensions
    {
        public static IEnumerable<DateTime> Range(this DateTime startDate, DateTime endDate)
        {
            return Enumerable.Range(0, (endDate - startDate).Days + 1).Select(d => startDate.AddDays(d));
        }
    }
}
