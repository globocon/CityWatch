﻿using CityWatch.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using CityWatch.Data.Enums;
using CityWatch.Kpi.Services;
using CityWatch.Data.Providers;
using static Dropbox.Api.TeamLog.SpaceCapsType;
using System.Text.RegularExpressions;

namespace CityWatch.Kpi.Models
{

    public class DailyKpiGuard
    {
        private readonly DateTime _date;
        private readonly DailyClientSiteKpi _dailyClientSiteKpi;
        private readonly IEnumerable<GuardComplianceAndLicense> _guardCompliance;
        private readonly IGuardDataProvider _guardDataProvider;

        private readonly Dictionary<int, Guard> _shift1Guards = new();
        private readonly Dictionary<int, Guard> _shift2Guards = new();
        private readonly Dictionary<int, Guard> _shift3Guards = new();


        public DailyKpiGuard(DailyClientSiteKpi dailyClientSiteKpi, IEnumerable<GuardLogin> dayGuardLogins, IEnumerable<GuardComplianceAndLicense> guardCompliances, IGuardDataProvider guardDataProvider)
        {
            _dailyClientSiteKpi = dailyClientSiteKpi;
            _guardCompliance = guardCompliances;
            _guardDataProvider = guardDataProvider;

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
        //Added For 3rd Page of Report start
       
        public IEnumerable<GuardComplianceAndLicense> GuardCompliance
        {
            get { return _guardCompliance; }
        }
        public IEnumerable<Guard> Guards
        {
            get
            {
                List<Guard> allGuards = new List<Guard>();

                foreach (var guardCompliance in _guardCompliance)
                {
                    
                    if (guardCompliance.Guard != null)
                    {
                        
                        allGuards.AddRange(Enumerable.Repeat(guardCompliance.Guard, 1));
                    }
                }

                return allGuards;
            }
        }
        //Added For 3rd Page of Report stop
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
                var hr1Values = new List<string>();
                foreach (var guard in _shift1Guards)
                {
                    var ddd = _guardCompliance.Where(x => x.Id == 106).FirstOrDefault();
                    var hr1Compliance = _guardCompliance.FirstOrDefault(z => z.GuardId.Equals(guard.Value.Id) && z.HrGroup == HrGroup.HR1);
                    if (hr1Compliance != null)
                    {
                        
                                hr1Values.Add(GetHrValue(hr1Compliance));
                    }
                    else
                    {
                        hr1Values.Add("-");
                    }
                }
                return string.Join(",", hr1Values);
            }
        }

        public string Shift1GuardHr2
        {
            get
            {
                var hr2Values = new List<string>();
                foreach (var guard in _shift1Guards)
                {
                    var hr2Compliance = _guardCompliance.FirstOrDefault(z => z.GuardId.Equals(guard.Value.Id) && z.HrGroup == HrGroup.HR2);
                    if (hr2Compliance != null)
                    {
                        hr2Values.Add(GetHrValue(hr2Compliance));
                    }
                    else
                    {
                        hr2Values.Add("-");
                    }
                }
                return string.Join(",", hr2Values);
            }
        }        

        public string Shift1GuardHr3
        {
            get
            {
                var hr3Values = new List<string>();
                foreach (var guard in _shift1Guards)
                {
                    var hr3Compliance = _guardCompliance.FirstOrDefault(z => z.GuardId.Equals(guard.Value.Id) && z.HrGroup == HrGroup.HR3);
                    if (hr3Compliance != null)
                    {
                        hr3Values.Add(GetHrValue(hr3Compliance));
                    }
                    else
                    {
                        hr3Values.Add("-");

                    }
                }
                return string.Join(",", hr3Values);
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
                var hr1Values = new List<string>();
                foreach (var guard in _shift2Guards)
                {
                    var hr1Compliance = _guardCompliance.FirstOrDefault(z => z.GuardId.Equals(guard.Value.Id) && z.HrGroup == HrGroup.HR1);
                    if (hr1Compliance != null)
                    {
                        hr1Values.Add(GetHrValue(hr1Compliance));
                    }
                    else
                    {
                        hr1Values.Add("-");
                    }
                }
                return string.Join(",", hr1Values);
            }
        }

        public string Shift2GuardHr2
        {
            get
            {
                var hr2Values = new List<string>();
                foreach (var guard in _shift2Guards)
                {
                    var hr2Compliance = _guardCompliance.FirstOrDefault(z => z.GuardId.Equals(guard.Value.Id) && z.HrGroup == HrGroup.HR2);
                    if (hr2Compliance != null)
                    {
                        hr2Values.Add(GetHrValue(hr2Compliance));
                    }
                    else
                    {
                        hr2Values.Add("-");
                    }
                }
                return string.Join(",", hr2Values);
            }
        }

        public string Shift2GuardHr3
        {
            get
            {
                var hr3Values = new List<string>();
                foreach (var guard in _shift2Guards)
                {
                    var hr3Compliance = _guardCompliance.FirstOrDefault(z => z.GuardId.Equals(guard.Value.Id) && z.HrGroup == HrGroup.HR3);
                    if (hr3Compliance != null)
                    {
                        hr3Values.Add(GetHrValue(hr3Compliance));
                    }
                    else
                    {
                        hr3Values.Add("-");

                    }
                }
                return string.Join(",", hr3Values);
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
                var hr1Values = new List<string>();
                foreach (var guard in _shift3Guards)
                {
                    var hr1Compliance = _guardCompliance.FirstOrDefault(z => z.GuardId.Equals(guard.Value.Id) && z.HrGroup == HrGroup.HR1);
                    if (hr1Compliance != null)
                    {
                        var gg= GetHrValue(hr1Compliance);
                        hr1Values.Add(GetHrValue(hr1Compliance));
                    }
                    else
                    {
                        hr1Values.Add("-");
                    }
                }
                return string.Join(",", hr1Values);
            }
        }

        public string Shift3GuardHr2
        {
            get
            {
                var hr2Values = new List<string>();
                foreach (var guard in _shift3Guards)
                {
                    var hr2Compliance = _guardCompliance.FirstOrDefault(z => z.GuardId.Equals(guard.Value.Id) && z.HrGroup == HrGroup.HR2);
                    if (hr2Compliance != null)
                    {
                        hr2Values.Add(GetHrValue(hr2Compliance));
                    }
                    else { 
                        hr2Values.Add("-");
                    }
                }
                return string.Join(",", hr2Values);
            }
        }

        public string Shift3GuardHr3
        {
            get
            {
                var hr3Values = new List<string>();
                foreach (var guard in _shift3Guards)
                {
                    var hr3Compliance = _guardCompliance.FirstOrDefault(z => z.GuardId.Equals(guard.Value.Id) && z.HrGroup == HrGroup.HR3);
                    if (hr3Compliance != null)
                    {
                        hr3Values.Add(GetHrValue(hr3Compliance));
                    }
                    else
                    {
                        hr3Values.Add("-");
                    }
                }
                return string.Join(",", hr3Values);
            }
        }

        private string GetHrValue(GuardComplianceAndLicense compliance)
        {
            var HR = "";
            var hrGroupStatusesNew = LEDStatusForLoginUser(compliance.GuardId);
            if (hrGroupStatusesNew != null && hrGroupStatusesNew.Count > 0)
            {
                
                var HR1List = hrGroupStatusesNew
    .Where(x => Regex.Replace(x.GroupName, @"\s+", "") == Regex.Replace(compliance.HrGroupText, @"\s+", ""))
    .ToList();
                if (HR1List != null && HR1List.Count > 0)
                {
                    if (HR1List.Where(x => x.ColourCodeStatus == "Red").ToList().Count > 0)
                    {
                        HR = "E";
                    }
                    else if (HR1List.Where(x => x.ColourCodeStatus == "Yellow").ToList().Count > 0)
                    {
                        HR = "N";
                        
                    }
                    else
                    {
                        HR = "Y";
                    }
                }
            }
            return HR;
            //if (compliance.ExpiryDate.HasValue)
            //{
            //    if (compliance.ExpiryDate < _date)
            //        return "N";

            //    if (compliance.ExpiryDate >= _date &&
            //        compliance.ExpiryDate <= _date.AddDays(45))
            //        return "E";
            //}

            //return "Y";
        }
        public List<HRGroupStatusNew> LEDStatusForLoginUser(int GuardID)
        {
            var MasterGroup = _guardDataProvider.GetHRDescFull();
            var GuardDocumentDetails = _guardDataProvider.GetGuardLicensesandcompliance(GuardID);
            var hrGroupStatusesNew = new List<HRGroupStatusNew>();

            foreach (var item in MasterGroup)
            {

                var TemDescription = item.ReferenceNo + " " + item.Description.Trim();
                var SelectedGuardDocument = GuardDocumentDetails.Where(x => x.Description == TemDescription).ToList();


                if (SelectedGuardDocument.Count > 0)
                {
                    hrGroupStatusesNew.Add(new HRGroupStatusNew
                    {

                        Status = 1,
                        GroupName = item.GroupName.Trim(),
                        ColourCodeStatus = GuardledColourCodeGenerator(SelectedGuardDocument)

                    });
                }


            }
            var Temp = hrGroupStatusesNew;

            return Temp;
        }
        public string GuardledColourCodeGenerator(List<GuardComplianceAndLicense> SelectedList)
        {
            var today = DateTime.Now;
            var ColourCode = "Green";

            if (SelectedList.Count > 0)
            {
                var SelectDatatype = SelectedList.Where(x => x.DateType == true).ToList();
                if (SelectDatatype.Count > 0)
                {
                    ColourCode = "Green";
                }
                else
                {
                    if (SelectedList.FirstOrDefault() != null)
                    {
                        if (SelectedList.FirstOrDefault().ExpiryDate != null)
                        {
                            var ExpiryDate = SelectedList.FirstOrDefault().ExpiryDate;
                            var timeDifference = ExpiryDate - today;

                            if (ExpiryDate < today)
                            {
                                ColourCode = "Red";
                            }
                            else if ((ExpiryDate - DateTime.Now).Value.Days < 45)
                            {
                                var Date = (ExpiryDate - DateTime.Now).Value.Days;
                                ColourCode = "Yellow";
                            }
                        }

                    }



                }
            }
            return ColourCode;
        }
        public class HRGroupStatusNew
        {

            public int Status { get; set; }

            public string GroupName { get; set; }
            public string ColourCodeStatus { get; set; }
        }
    }
}