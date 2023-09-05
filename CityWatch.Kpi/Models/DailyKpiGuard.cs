using CityWatch.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Kpi.Models
{
    public class DailyKpiGuard
    {
        private readonly DateTime _date;
        private readonly DailyClientSiteKpi _dailyClientSiteKpi;

        private readonly Dictionary<int, Guard> _shift1Guards = new();
        private readonly Dictionary<int, Guard> _shift2Guards = new();
        private readonly Dictionary<int, Guard> _shift3Guards = new();

        public DailyKpiGuard(DailyClientSiteKpi dailyClientSiteKpi, IEnumerable<GuardLogin> dayGuardLogins)
        {
            _dailyClientSiteKpi = dailyClientSiteKpi;

            _date = dailyClientSiteKpi.Date;
            var shift1Start = new DateTime(_date.Year, _date.Month, _date.Day, 00, 01, 00);
            var shift1End = new DateTime(_date.Year, _date.Month, _date.Day, 07, 59, 00);
            var shift2Start = new DateTime(_date.Year, _date.Month, _date.Day, 08, 00, 00);
            var shift2End = new DateTime(_date.Year, _date.Month, _date.Day, 15, 59, 00); ;
            var shift3Start = new DateTime(_date.Year, _date.Month, _date.Day, 16, 00, 00);
            var shift3End = new DateTime(_date.Year, _date.Month, _date.Day, 23, 59, 00);

            foreach (var guardLogin in dayGuardLogins)
            {
                if (((shift1Start <= guardLogin.OnDuty && shift1End >= guardLogin.OnDuty) ||
                    (shift1Start <= guardLogin.OffDuty && shift1End >= guardLogin.OffDuty)) &&
                    !_shift1Guards.ContainsKey(guardLogin.GuardId))
                {
                    _shift1Guards.Add(guardLogin.GuardId, guardLogin.Guard);
                }

                if (((shift2Start <= guardLogin.OnDuty && shift2End >= guardLogin.OnDuty) ||
                    (shift2Start <= guardLogin.OffDuty && shift2End >= guardLogin.OffDuty)) &&
                    !_shift2Guards.ContainsKey(guardLogin.GuardId))
                {
                    _shift2Guards.Add(guardLogin.GuardId, guardLogin.Guard);
                }

                if (((shift3Start <= guardLogin.OnDuty && shift3End >= guardLogin.OnDuty) ||
                    (shift3Start <= guardLogin.OffDuty && shift3End >= guardLogin.OffDuty)) &&
                    !_shift3Guards.ContainsKey(guardLogin.GuardId))
                {
                    _shift3Guards.Add(guardLogin.GuardId, guardLogin.Guard);
                }
            }
        }

        public DateTime Date { get { return _date; } }

        public decimal? EmployeeHours { get { return _dailyClientSiteKpi.EmployeeHours; } }

        public decimal? ActualEmployeeHours { get { return _dailyClientSiteKpi.ActualEmployeeHours; } }

        public decimal? EffectiveEmployeeHours { get { return ActualEmployeeHours ?? EmployeeHours; } }

        public string Shift1GuardName
        {
            get
            {
                return string.Join("\n", _shift1Guards.Select(z => z.Value.Name));
            }
        }

        public string Shift1GuardSecurityNo
        {
            get
            {
                return string.Join("\n", _shift1Guards.Select(z => z.Value.SecurityNo));
            }
        }

        public string Shift1GuardHr
        {
            get
            {
                return string.Join("\n", _shift1Guards.Select(z => "-"));
            }
        }

        public string Shift1GuardVisy
        {
            get
            {
                return string.Join("\n", _shift1Guards.Select(z => "-"));
            }
        }

        public string Shift1GuardFire
        {
            get
            {
                return string.Join("\n", _shift1Guards.Select(z => "-"));
            }
        }

        public string Shift2GuardName
        {
            get
            {
                return string.Join("\n", _shift2Guards.Select(z => z.Value.Name));
            }
        }

        public string Shift2GuardSecurityNo
        {
            get
            {
                return string.Join("\n", _shift2Guards.Select(z => z.Value.SecurityNo));
            }
        }

        public string Shift2GuardHr
        {
            get
            {
                return string.Join("\n", _shift2Guards.Select(z => "-"));
            }
        }

        public string Shift2GuardVisy
        {
            get
            {
                return string.Join("\n", _shift2Guards.Select(z => "-"));
            }
        }

        public string Shift2GuardFire
        {
            get
            {
                return string.Join("\n", _shift2Guards.Select(z => "-"));
            }
        }

        public string Shift3GuardName
        {
            get
            {
                return string.Join("\n", _shift3Guards.Select(z => z.Value.Name));
            }
        }

        public string Shift3GuardSecurityNo
        {
            get
            {
                return string.Join("\n", _shift3Guards.Select(z => z.Value.SecurityNo));
            }
        }

        public string Shift3GuardHr
        {
            get
            {
                return string.Join("\n", _shift3Guards.Select(z => "-"));
            }
        }

        public string Shift3GuardVisy
        {
            get
            {
                return string.Join("\n", _shift3Guards.Select(z => "-"));
            }
        }

        public string Shift3GuardFire
        {
            get
            {
                return string.Join("\n", _shift3Guards.Select(z => "-"));
            }
        }
    }
}