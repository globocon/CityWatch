using CityWatch.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using static Dropbox.Api.FileRequests.GracePeriod;

namespace CityWatch.Kpi.Models
{
    public class DailyKpiGuard
    {
        private readonly DateTime _date;
        private readonly DailyClientSiteKpi _dailyClientSiteKpi;
        private readonly IEnumerable<GuardCompliance> _guardCompliance;

        private readonly Dictionary<int, Guard> _shift1Guards = new();
        private readonly Dictionary<int, Guard> _shift2Guards = new();
        private readonly Dictionary<int, Guard> _shift3Guards = new();

        public DailyKpiGuard(DailyClientSiteKpi dailyClientSiteKpi, IEnumerable<GuardLogin> dayGuardLogins, IEnumerable<GuardCompliance> guardCompliances)
        {
            _dailyClientSiteKpi = dailyClientSiteKpi;
            _guardCompliance = guardCompliances;

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

        public string Shift1GuardHr1
        {
            get
            {
                var text = string.Empty;
                foreach (var y in _shift1Guards)
                {
                    var data = _guardCompliance.Where(z => z.GuardId.Equals(y.Value.Id));
                    if (data != null)
                    {
                        foreach (var x in data)
                        {
                            if (x.HrGroup == "HR 1")
                            {
                                if (x.ExpiryDate == null)
                                    text = "Y";
                                if (x.ExpiryDate <= DateTime.Today)
                                    text = "N";
                                if (x.ExpiryDate > DateTime.Today)
                                    text = "Y";
                            }
                        }
                    }
                }
                return string.Join("\n", text);
            }
        }

        public string Shift1GuardHr2
        {
            get
            {
                var text =string.Empty;
                foreach(var y in _shift1Guards)
                {
                    var data = _guardCompliance.Where(z => z.GuardId.Equals(y.Value.Id));
                    if (data != null)
                    {
                        foreach (var x in data)
                        {
                            if (x.HrGroup == "HR 2")
                            {
                                if (x.ExpiryDate == null)
                                    text = "Y";
                                if (x.ExpiryDate <= DateTime.Today)
                                    text = "N";
                                if (x.ExpiryDate > DateTime.Today)
                                    text = "Y";
                            }
                        }
                    }

                }              
                return string.Join("\n", text);
            }
        }        

        public string Shift1GuardHr3
        {
            get
            {
                var text = string.Empty;
                foreach (var y in _shift1Guards)
                {
                    var data = _guardCompliance.Where(z => z.GuardId.Equals(y.Value.Id));
                    if (data != null)
                    {
                        foreach (var x in data)
                        {
                            if (x.HrGroup == "HR 3")
                            {
                                if (x.ExpiryDate == null)
                                    text = "Y";
                                if (x.ExpiryDate <= DateTime.Today)
                                    text = "N";
                                if (x.ExpiryDate > DateTime.Today)
                                    text = "Y";
                            }
                        }
                    }
                }
                return string.Join("\n", text);
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

        public string Shift2GuardHr1
        {
            get
            {
                var text = string.Empty;
                foreach (var y in _shift2Guards)
                {                    
                    var data = _guardCompliance.Where(z => z.GuardId.Equals(y.Value.Id));
                    if (data != null)
                    {
                        foreach (var x in data)
                        {
                            if (x.HrGroup == "HR 1")
                            {
                                if (x.ExpiryDate == null)
                                    text = "Y";
                                if (x.ExpiryDate <= DateTime.Today)
                                    text = "N";
                                if (x.ExpiryDate > DateTime.Today)
                                    text = "Y";
                            }
                        }
                    }
                }
                return string.Join("\n", text);
            }
        }

        public string Shift2GuardHr2
        {
            get
            {
                var text = string.Empty;
                foreach (var y in _shift2Guards)
                {                   
                    var data = _guardCompliance.Where(z => z.GuardId.Equals(y.Value.Id));
                    if (data != null)
                    {
                        foreach (var x in data)
                        {
                            if (x.HrGroup == "HR 2")
                            {
                                if (x.ExpiryDate == null)
                                    text = "Y";
                                if (x.ExpiryDate <= DateTime.Today)
                                    text = "N";
                                if (x.ExpiryDate > DateTime.Today)
                                    text = "Y";
                            }
                        }
                    }
                }
                return string.Join("\n", text);
            }
        }

        public string Shift2GuardHr3
        {
            get
            {
                var text = string.Empty;
                foreach (var y in _shift2Guards)
                {
                    var data = _guardCompliance.Where(z => z.GuardId.Equals(y.Value.Id));
                    if (data != null)
                    {
                        foreach (var x in data)
                        {
                            if (x.HrGroup == "HR 3")
                            {
                                if (x.ExpiryDate == null)
                                    text = "Y";
                                if (x.ExpiryDate <= DateTime.Today)
                                    text = "N";
                                if (x.ExpiryDate > DateTime.Today)
                                    text = "Y";
                            }
                        }
                    }
                }
                return string.Join("\n", text);
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

        public string Shift3GuardHr1
        {
            get
            {
                var text = string.Empty;
                foreach (var y in _shift3Guards)
                {                    
                    var data = _guardCompliance.Where(z => z.GuardId.Equals(y.Value.Id));
                    if (data != null)
                    {
                        foreach (var x in data)
                        {
                            if (x.HrGroup == "HR 1")
                            {
                                if (x.ExpiryDate == null)
                                    text = "Y";
                                if (x.ExpiryDate <= DateTime.Today)
                                    text = "N";
                                if (x.ExpiryDate > DateTime.Today)
                                    text = "Y";
                            }
                        }
                    }
                }
                return string.Join("\n", text);
            }
        }

        public string Shift3GuardHr2
        {
            get
            {
                var text = string.Empty;
                foreach (var y in _shift3Guards)
                {                    
                    var data = _guardCompliance.Where(z => z.GuardId.Equals(y.Value.Id));
                    if (data != null)
                    {
                        foreach (var x in data)
                        {
                            if (x.HrGroup == "HR 2")
                            {
                                if (x.ExpiryDate == null)
                                    text = "Y";
                                if (x.ExpiryDate <= DateTime.Today)
                                    text = "N";
                                if (x.ExpiryDate > DateTime.Today)
                                    text = "Y";
                            }
                        }
                    }
                }
                return string.Join("\n", text);
            }
        }

        public string Shift3GuardHr3
        {
            get
            {
                var text = string.Empty;
                foreach (var y in _shift3Guards)
                {
                    var data = _guardCompliance.Where(z => z.GuardId.Equals(y.Value.Id));
                    if (data != null)
                    {
                        foreach (var x in data)
                        {
                            if (x.HrGroup == "HR 3")
                            {
                                if (x.ExpiryDate == null)
                                    text = "Y";
                                if (x.ExpiryDate <= DateTime.Today)
                                    text = "N";
                                if (x.ExpiryDate > DateTime.Today)
                                    text = "Y";
                            }
                        }
                    }
                }
                return string.Join("\n", text);
            }
        }
    }
}