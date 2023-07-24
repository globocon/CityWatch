using CityWatch.Data.Models;
using System;
using System.Linq;

namespace CityWatch.Kpi.Models
{
    public class DailyKpiResult
    {
        private readonly DailyClientSiteKpi _dailyClientSiteKpi;
        private readonly ClientSiteKpiSetting _clientSiteKpiSetting;

        public DailyKpiResult(DailyClientSiteKpi dailyClientSiteKpi, ClientSiteKpiSetting clientSiteKpiSetting)
        {
            _dailyClientSiteKpi = dailyClientSiteKpi;
            _clientSiteKpiSetting = clientSiteKpiSetting;
        }

        public int ClientSiteId { get { return _dailyClientSiteKpi.ClientSiteId; } }

        public ClientSiteKpiSetting ClientSiteKpiSetting { get { return _clientSiteKpiSetting; } }

        public DateTime Date { get { return _dailyClientSiteKpi.Date; } }

        public decimal? EmployeeHours { get { return _dailyClientSiteKpi.EmployeeHours; } }

        public decimal? ActualEmployeeHours { get { return _dailyClientSiteKpi.ActualEmployeeHours; } }

        public decimal? EffectiveEmployeeHours { get { return ActualEmployeeHours ?? EmployeeHours; } }

        public int? ImageCount { get { return _dailyClientSiteKpi.ImageCount; } }

        public int? WandScanCount { get { return _dailyClientSiteKpi.WandScanCount; } }

        public int? IncidentCount { get { return _dailyClientSiteKpi.IncidentCount; } }

        public int DayOfDate { get { return _dailyClientSiteKpi.Date.Day; } }

        public int DailyKpiClientSiteId { get { return _dailyClientSiteKpi.Id; } }        

        public string NameOfDay { get { return _dailyClientSiteKpi.Date.DayOfWeek.ToString(); } }

        public decimal? ImageCountPerHr
        {
            get
            {
                if (_dailyClientSiteKpi.EffectiveEmployeeHours.HasValue &&
                    _dailyClientSiteKpi.EffectiveEmployeeHours.Value == 0 &&
                    Date <= DateTime.Today)
                    return null;

                var dailyImageCount = (_clientSiteKpiSetting != null && _clientSiteKpiSetting.IsThermalCameraSite) ?
                    _dailyClientSiteKpi.ImageCount.GetValueOrDefault() / 2 :
                    _dailyClientSiteKpi.ImageCount.GetValueOrDefault();

                if (_dailyClientSiteKpi.EffectiveEmployeeHours.GetValueOrDefault() > 0)
                {
                    var divideBy = _dailyClientSiteKpi.EffectiveEmployeeHours.Value;
                    if (_clientSiteKpiSetting != null &&
                        _clientSiteKpiSetting.ClientSiteDayKpiSettings.SingleOrDefault(z => z.WeekDay == Date.DayOfWeek)?.PatrolFrequency == 1)
                        divideBy = 1;

                    return Math.Round(dailyImageCount / divideBy, 1);
                }

                return decimal.Zero;
            }
        }

        public decimal? WandScanCountPerHr
        {
            get
            {
                if (_dailyClientSiteKpi.EffectiveEmployeeHours.HasValue &&
                    _dailyClientSiteKpi.EffectiveEmployeeHours.Value == 0 &&
                    Date <= DateTime.Today)
                    return null;

                if (_dailyClientSiteKpi.EffectiveEmployeeHours.GetValueOrDefault() > 0)
                {
                    var divideBy = _dailyClientSiteKpi.EffectiveEmployeeHours.Value;
                    if (_clientSiteKpiSetting != null &&
                        _clientSiteKpiSetting.ClientSiteDayKpiSettings.SingleOrDefault(z => z.WeekDay == Date.DayOfWeek)?.PatrolFrequency == 1)
                        divideBy = 1;

                    return Math.Round((decimal)_dailyClientSiteKpi.WandScanCount.GetValueOrDefault() / divideBy, 1);
                }

                return decimal.Zero;
            }
        }

        public int? EffortCounterImage { get; set; }

        public int? EffortCounterWand { get; set; }

        public bool? IsAcceptableLogFreq { get { return _dailyClientSiteKpi.IsAcceptableLogFreq; }}

        public string HasFireOrAlarm { get { return _dailyClientSiteKpi.FireOrAlarmCount.GetValueOrDefault() > 0 ? "Yes" : string.Empty; } }

        public decimal? ImagesTarget { get { return _clientSiteKpiSetting?.ClientSiteDayKpiSettings.SingleOrDefault(z => z.WeekDay == _dailyClientSiteKpi.Date.DayOfWeek)?.ImagesTarget; } }

        public decimal? WandScansTarget { get { return _clientSiteKpiSetting?.ClientSiteDayKpiSettings.SingleOrDefault(z => z.WeekDay == _dailyClientSiteKpi.Date.DayOfWeek)?.WandScansTarget; } }

        public decimal? WandPatrolsRatio
        {
            get
            {
                if (_dailyClientSiteKpi.EffectiveEmployeeHours.HasValue &&
                    _dailyClientSiteKpi.EffectiveEmployeeHours.Value == 0 &&
                    Date <= DateTime.Today)
                    return null;

                if (_clientSiteKpiSetting.WandPointsPerPatrol.GetValueOrDefault() > 0)
                    return Math.Round(WandScanCountPerHr.GetValueOrDefault() / _clientSiteKpiSetting.WandPointsPerPatrol.GetValueOrDefault(), 2);

                return decimal.Zero;
            }
        }
    }
}
