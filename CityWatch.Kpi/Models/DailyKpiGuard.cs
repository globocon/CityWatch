using CityWatch.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Kpi.Models
{
    public class DailyKpiGuard
    {
        private readonly DateTime _date;
        private readonly Guard _shift1Guard;
        private readonly Guard _shift2Guard;
        private readonly Guard _shift3Guard;
        private readonly DailyClientSiteKpi _dailyClientSiteKpi;
        private readonly IEnumerable<GuardLogin> _dayGuardLogins;

        public DailyKpiGuard(DailyClientSiteKpi dailyClientSiteKpi, IEnumerable<GuardLogin> dayGuardLogins)
        {
            _dailyClientSiteKpi = dailyClientSiteKpi;
            _dayGuardLogins = dayGuardLogins;

            _date = dailyClientSiteKpi.Date;

            var shift1Start = new DateTime(_date.Year, _date.Month, _date.Day, 00, 01, 00);
            var shift1End = new DateTime(_date.Year, _date.Month, _date.Day, 08, 00, 00);
            _shift1Guard = _dayGuardLogins.FirstOrDefault(z => z.OnDuty >= shift1Start && z.OffDuty < shift1End)?.Guard;

            var shift2Start = new DateTime(_date.Year, _date.Month, _date.Day, 08, 00, 00);
            var shift2End = new DateTime(_date.Year, _date.Month, _date.Day, 16, 00, 00);
            _shift2Guard = _dayGuardLogins.FirstOrDefault(z => z.OnDuty >= shift2Start && z.OffDuty < shift2End)?.Guard;

            var shift3Start = new DateTime(_date.Year, _date.Month, _date.Day, 16, 00, 00);
            var shift3End = new DateTime(_date.Year, _date.Month, _date.Day, 23, 59, 00);
            _shift3Guard = _dayGuardLogins.FirstOrDefault(z => z.OnDuty >= shift3Start && z.OffDuty < shift3End)?.Guard;
        }

        public DateTime Date { get { return _date; } }

        public decimal? EmployeeHours { get { return _dailyClientSiteKpi.EmployeeHours; } }

        public decimal? ActualEmployeeHours { get { return _dailyClientSiteKpi.ActualEmployeeHours; } }

        public decimal? EffectiveEmployeeHours { get { return ActualEmployeeHours ?? EmployeeHours; } }

        public string Shift1GuardName { get { return _shift1Guard?.Name; } }

        public string Shift1GuardSecurityNo { get { return _shift1Guard?.SecurityNo; } }

        public bool? Shift1GuardHr { get { return null; } }

        public bool? Shift1GuardVisy { get { return null; } }

        public bool? Shift1GuardFire { get { return null; } }

        public string Shift2GuardName { get { return _shift2Guard?.Name; } }

        public string Shift2GuardSecurityNo { get { return _shift2Guard?.SecurityNo; } }

        public bool? Shift2GuardHr { get { return null; } }

        public bool? Shift2GuardVisy { get { return null; } }

        public bool? Shift2GuardFire { get { return null; } }

        public string Shift3GuardName { get { return _shift3Guard?.Name; } }

        public string Shift3GuardSecurityNo { get { return _shift3Guard?.SecurityNo; } }

        public bool? Shift3GuardHr { get { return null; } }

        public bool? Shift3GuardVisy { get { return null; } }

        public bool? Shift3GuardFire { get { return null; } }
    }
}