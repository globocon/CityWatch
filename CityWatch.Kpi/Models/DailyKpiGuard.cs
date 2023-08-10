using CityWatch.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Kpi.Models
{
    public class DailyKpiGuard
    {
        private readonly DailyClientSiteKpi _dailyClientSiteKpi;
        private readonly IEnumerable<GuardLogin> _dayGuardLogins;

        public DailyKpiGuard(DailyClientSiteKpi dailyClientSiteKpi, IEnumerable<GuardLogin> dayGuardLogins)
        {
            _dailyClientSiteKpi = dailyClientSiteKpi;
            _dayGuardLogins = dayGuardLogins;
        }

        public DateTime Date { get { return _dailyClientSiteKpi.Date; } }

        public decimal? EmployeeHours { get { return _dailyClientSiteKpi.EmployeeHours; } }

        public decimal? ActualEmployeeHours { get { return _dailyClientSiteKpi.ActualEmployeeHours; } }

        public decimal? EffectiveEmployeeHours { get { return ActualEmployeeHours ?? EmployeeHours; } }

        public string Shift1GuardName
        {
            get
            {
                var shift1Start = new DateTime(Date.Year, Date.Month, Date.Day, 00, 01, 00);
                var shift1End = new DateTime(Date.Year, Date.Month, Date.Day, 08, 00, 00);

                return _dayGuardLogins.FirstOrDefault(z => z.OnDuty >= shift1Start && z.OffDuty < shift1End)?.Guard.Name;
            }
        }

        public string Shift1GuardSecurityNo
        {
            get
            {
                var shift1Start = new DateTime(Date.Year, Date.Month, Date.Day, 00, 01, 00);
                var shift1End = new DateTime(Date.Year, Date.Month, Date.Day, 08, 00, 00);

                return _dayGuardLogins.FirstOrDefault(z => z.OnDuty >= shift1Start && z.OffDuty < shift1End)?.Guard.SecurityNo;
            }
        }
        public bool Shift1GuardHr { get { return false; } }

        public bool Shift1GuardVisy { get { return false; } }

        public bool Shift1GuardFire { get { return false; } }

        public string Shift2GuardName {
            get
            {
                var shift2Start = new DateTime(Date.Year, Date.Month, Date.Day, 08, 00, 00);
                var shift2End = new DateTime(Date.Year, Date.Month, Date.Day, 16, 00, 00);

                return _dayGuardLogins.FirstOrDefault(z => z.OnDuty >= shift2Start && z.OffDuty < shift2End)?.Guard.Name;
            }
        }

        public string Shift2GuardSecurityNo
        {
            get
            {
                var shift2Start = new DateTime(Date.Year, Date.Month, Date.Day, 08, 00, 00);
                var shift2End = new DateTime(Date.Year, Date.Month, Date.Day, 16, 00, 00);

                return _dayGuardLogins.FirstOrDefault(z => z.OnDuty >= shift2Start && z.OffDuty < shift2End)?.Guard.SecurityNo;
            }
        }

        public bool Shift2GuardHr { get { return false; } }

        public bool Shift2GuardVisy { get { return false; } }

        public bool Shift2GuardFire { get { return false; } }

        public string Shift3GuardName {
            get
            {
                var shift3Start = new DateTime(Date.Year, Date.Month, Date.Day, 16, 00, 00);
                var shift3End = new DateTime(Date.Year, Date.Month, Date.Day, 23, 59, 00);

                return _dayGuardLogins.FirstOrDefault(z => z.OnDuty >= shift3Start && z.OffDuty < shift3End)?.Guard.Name;
            }
        }

        public string Shift3GuardSecurityNo
        {
            get
            {
                var shift3Start = new DateTime(Date.Year, Date.Month, Date.Day, 16, 00, 00);
                var shift3End = new DateTime(Date.Year, Date.Month, Date.Day, 23, 59, 00);

                return _dayGuardLogins.FirstOrDefault(z => z.OnDuty >= shift3Start && z.OffDuty < shift3End)?.Guard.SecurityNo;
            }
        }

        public bool Shift3GuardHr { get { return false; } }

        public bool Shift3GuardVisy { get { return false; } }

        public bool Shift3GuardFire { get { return false; } }
    }
}