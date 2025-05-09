using CityWatch.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Kpi.Models
{
    public class MonthlyKpiResult
    {
        private readonly ClientSiteKpiSetting _clientSiteKpiSetting;
        private readonly List<DailyKpiResult> _dailyKpiResults;
        private readonly List<EffortCount> _effortCounts = new List<EffortCount>();
        private readonly string _reportTimeStamp;

        public MonthlyKpiResult(ClientSiteKpiSetting clientSiteKpiSetting,
            List<DailyKpiResult> dailyKpiResults)
        {
            _clientSiteKpiSetting = clientSiteKpiSetting;
            _dailyKpiResults = dailyKpiResults;

            _reportTimeStamp = $"{DateTime.Now:dd MMM yyyy @ HH:mm} hrs";
            CalculateEfforCounts();
        }

        public string ReportTimeStamp
        {
            get { return _reportTimeStamp; }
        }

        public int IrCountTotal
        {
            get
            {
                var data = _dailyKpiResults.Where(z => z.IncidentCount.GetValueOrDefault() > 0);
                if (data.Any())
                    return data.Sum(z => z.IncidentCount.Value);
                return 0;
            }
        }

        public int AlarmCountTotal
        {
            get
            {
                var data = _dailyKpiResults.Where(z => z.HasFireOrAlarm.Equals("Yes"));
                if (data.Any())
                    return data.Count();
                return 0;
            }
        }

        public int NotInAcceptableLogFreqCount
        {
            get
            {
                return _dailyKpiResults.Select(z => z.IsAcceptableLogFreq).Count(z => z.HasValue && !z.Value);
            }
        }

        public decimal ImageCountAverage
        {
            get
            {
                var data = _dailyKpiResults.Where(z => z.ImageCountPerHr.GetValueOrDefault() > 0);
                if (data.Any())
                    return data.Average(y => y.ImageCountPerHr.Value);
                return decimal.Zero;
            }
        }

        public decimal WandScanAverage
        {
            get
            {
                var data = _dailyKpiResults.Where(z => z.WandScanCountPerHr.GetValueOrDefault() > 0);
                if (data.Any())
                    return data.Average(y => y.WandScanCountPerHr.Value);
                return decimal.Zero;
            }
        }

        public decimal WandPatrolsAverage
        {
            get
            {
                var data = _dailyKpiResults.Where(z => z.WandPatrolsRatio.GetValueOrDefault() > 0);
                if (data.Any())
                    return data.Average(y => y.WandPatrolsRatio.Value);
                return decimal.Zero;
            }
        }

        public decimal WandPatrolsPercentage
        {
            get
            {
                var data = _clientSiteKpiSetting.ClientSiteDayKpiSettings.Where(z => z.NoOfPatrols.GetValueOrDefault() > 0);
                if (data.Any())
                    return (WandPatrolsAverage / (decimal)data.Average(y => y.NoOfPatrols.Value)) * 100;
                return decimal.Zero;
            }
        }


        public decimal? ImageCountPercentage
        {
            get
            {
                if (_dailyKpiResults.All(z => z.ImageCount.GetValueOrDefault() == 0))
                {
                    return null;
                }

                var data = _clientSiteKpiSetting.ClientSiteDayKpiSettings.Where(z => z.ImagesTarget.GetValueOrDefault() > 0);
                if (data.Any())
                {
                    var target = data.Average(y => y.ImagesTarget.Value);
                    if (target > 0)
                        return (ImageCountAverage / target) * 100;
                }

                return decimal.Zero;
            }
        }

        public decimal? WandScanPercentage
        {
            get
            {
                if (_dailyKpiResults.All(z => z.WandScanCount.GetValueOrDefault() == 0))
                {
                    return null;
                }

                var data = _clientSiteKpiSetting.ClientSiteDayKpiSettings.Where(z => z.WandScansTarget.GetValueOrDefault() > 0);
                if (data.Any())
                {
                    var target = data.Average(y => y.WandScansTarget.Value);
                    if (target > 0)
                        return (WandScanAverage / target) * 100;
                }

                return decimal.Zero;
            }
        }

        public decimal? SiteScorePercentage
        {
            get
            {
                if (ImageCountPercentage.HasValue && ImageCountPercentage.HasValue)
                    return (ImageCountPercentage + WandScanPercentage) / 2;

                return null;
            }
        }

        public decimal? ShiftFilledVsRosterPercentage
        {
            get
            {
                var data = _dailyKpiResults.Where(z => z.EffectiveEmployeeHours.HasValue && z.EffectiveEmployeeHours.Value > 0);
                //var ImageCount = data.Sum(z => z.ImageCount);
                //var WandScanCount = data.Sum(z => z.WandScanCount);

                //if(ImageCount == 0 && WandScanCount == 0)
                //    return null;

                //if (ImageCount > 0 && WandScanCount > 0)
                //    return 100M;

                //if (ImageCount > 0 || WandScanCount > 0)
                //    return 50M;

                if (data.All(z => z.ImageCount.GetValueOrDefault() == 0 && z.WandScanCount.GetValueOrDefault() == 0))
                    return null;

                if (data.All(z => z.ImageCount.GetValueOrDefault() > 0 && z.WandScanCount.GetValueOrDefault() > 0))
                    return 100M;

                if (data.All(z => z.ImageCount.GetValueOrDefault() > 0 || z.WandScanCount.GetValueOrDefault() > 0))
                    return 50M;

                return decimal.Zero;
            }
        }

        public decimal? LogReportsVsRosterPercentage
        {
            get
            {
                if (_dailyKpiResults.Select(z => z.IsAcceptableLogFreq).All(z => !z.HasValue))
                    return null;

                var dailyAcceptableLogFreq = _dailyKpiResults.Select(z => z.IsAcceptableLogFreq).Where(z => z.HasValue);
                var totalCount = dailyAcceptableLogFreq.Count();
                if (totalCount > 0)                    
                    return ((decimal)dailyAcceptableLogFreq.Count(z => z.Value) / totalCount) * 100;

                return decimal.Zero;
            }
        }

        public decimal GuardCompetency
        {
            get
            {
                return decimal.Zero;
            }
        }

        public List<EffortCount> EffortCounts
        {
            get
            {
                return _effortCounts;
            }
        }

        public List<DailyKpiResult> DailyKpiResults
        {
            get
            {
                return _dailyKpiResults;
            }
        }

        public ClientSiteKpiSetting ClientSiteKpiSetting
        {
            get
            {
                return _clientSiteKpiSetting;
            }
        }

        private void CalculateEfforCounts()
        {
            int sumImage = 0;
            int sumWand = 0;
            int weekCount = 0;
            int dayCount = 1;
            foreach (var kpi in _dailyKpiResults.OrderBy(z => z.Date))
            {
                sumImage += kpi.ImageCount.GetValueOrDefault();
                sumWand += kpi.WandScanCount.GetValueOrDefault();

                if (dayCount % 7 == 0)
                {

                    //Change 02052024 dileep the old codes show error with  multiple values 
                    _dailyKpiResults.FirstOrDefault(x => x.Date == kpi.Date).EffortCounterImage = sumImage > 0 ? sumImage : null;
                    _dailyKpiResults.FirstOrDefault(x => x.Date == kpi.Date).EffortCounterWand = sumWand > 0 ? sumWand : null;
                    //Old Code Start
                    //_dailyKpiResults.Single(x => x.Date == kpi.Date).EffortCounterImage = sumImage > 0 ? sumImage : null;
                    //_dailyKpiResults.Single(x => x.Date == kpi.Date).EffortCounterWand = sumWand > 0 ? sumWand : null;
                    //Old Code end
                  

                    ++weekCount;
                    _effortCounts.Add(new EffortCount()
                    {
                        WeekNumber = weekCount,
                        Flir = sumImage,
                        Wand = sumWand
                    });

                    sumImage = 0;
                    sumWand = 0;
                }
                dayCount++;
            }
        }
    }
}
