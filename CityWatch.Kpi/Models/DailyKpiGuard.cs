using CityWatch.Data.Models;
using System;
using System.Collections.Generic;

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
                var guardName = string.Empty;
                foreach (var item in _dayGuardLogins)
                {
                    var shift1End = new DateTime(Date.Year, Date.Month, Date.Day, 08, 00, 00);
                    if (item.OffDuty < shift1End)
                        guardName += item.Guard.Name + "\n";
                }
                return guardName;
                
            }
        }

        public string Shift1GuardSecurityNo
        {
            get
            {
                var securityNo = string.Empty;
                foreach (var item in _dayGuardLogins)
                {
                    var shift1End = new DateTime(Date.Year, Date.Month, Date.Day, 08, 00, 00);
                    if (item.OffDuty < shift1End)
                        securityNo += item.Guard.SecurityNo + "\n";
                }
                return securityNo;
            }
        }
        public bool Shift1GuardHr { get { return false; } }

        public bool Shift1GuardVisy { get { return false; } }

        public bool Shift1GuardFire { get { return false; } }

        public string Shift2GuardName {
            get
            {               
                var guardName = string.Empty;
                foreach (var item in _dayGuardLogins)
                {
                    var shift1End = new DateTime(Date.Year, Date.Month, Date.Day, 08, 00, 00);
                    var shift2End = new DateTime(Date.Year, Date.Month, Date.Day, 16, 00, 00);
                    if (item.OffDuty > shift1End && item.OffDuty <= shift2End)
                        guardName += item.Guard.Name + "\n";
                }
                return guardName;
            }
        }

        public string Shift2GuardSecurityNo
        {
            get
            {               
                var securityNo = string.Empty;
                foreach (var item in _dayGuardLogins)
                {
                    var shift1End = new DateTime(Date.Year, Date.Month, Date.Day, 08, 00, 00);
                    var shift2End = new DateTime(Date.Year, Date.Month, Date.Day, 16, 00, 00);
                    if (item.OffDuty > shift1End && item.OffDuty <= shift2End)
                        securityNo += item.Guard.SecurityNo + "\n";
                }
                return securityNo;
            }
        }

        public bool Shift2GuardHr { get { return false; } }

        public bool Shift2GuardVisy { get { return false; } }

        public bool Shift2GuardFire { get { return false; } }

        public string Shift3GuardName {
            get
            {               
                var guardName = string.Empty;
                foreach (var item in _dayGuardLogins)
                {
                    var shift2End = new DateTime(Date.Year, Date.Month, Date.Day, 16, 00, 00);
                    var shift3End = new DateTime(Date.Year, Date.Month, Date.Day, 23, 59, 00);
                    if (item.OffDuty > shift2End && item.OffDuty <= shift3End)
                        guardName += item.Guard.Name + "\n";
                }
                return guardName;
            }
        }

        public string Shift3GuardSecurityNo
        {
            get
            {                
                var securityNo = string.Empty;
                foreach (var item in _dayGuardLogins)
                {
                    var shift2End = new DateTime(Date.Year, Date.Month, Date.Day, 16, 00, 00);
                    var shift3End = new DateTime(Date.Year, Date.Month, Date.Day, 23, 59, 00);
                    if (item.OffDuty > shift2End && item.OffDuty <= shift3End)
                        securityNo += item.Guard.SecurityNo + "\n";
                }
                return securityNo;
            }
        }

        public bool Shift3GuardHr { get { return false; } }

        public bool Shift3GuardVisy { get { return false; } }

        public bool Shift3GuardFire { get { return false; } }
    }
}