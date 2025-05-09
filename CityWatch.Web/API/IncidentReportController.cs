using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CityWatch.Web.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class IncidentReportController : ControllerBase
    {
        private readonly IIrDataProvider _irReportDataProvider;
        private readonly IIrUploadService _irUploadService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        private readonly IClientDataProvider _clientDataProvider;

        public IncidentReportController(IIrDataProvider irReportDataProvider,
            IIrUploadService irUploadService,
            IWebHostEnvironment webHostEnvironment,
            IGuardLogDataProvider guardLogDataProvider,
            IClientDataProvider clientDataProvider)
        {
            _irReportDataProvider = irReportDataProvider;
            _irUploadService = irUploadService;
            _webHostEnvironment = webHostEnvironment;
            _guardLogDataProvider = guardLogDataProvider;
            _clientDataProvider = clientDataProvider;
        }

        [HttpGet]
        public JsonResult Get([FromQuery] DateTime dateFrom, [FromQuery] DateTime dateTo, [FromQuery] int siteId)
        {
            var dailyIncidentReports = _irReportDataProvider.GetIncidentReports(dateFrom, dateTo, siteId)
                .GroupBy(x => new { ReportDate = x.CreatedOn.UtcToAest().ToShortDateString() })
                .Select(x => new
                {
                    Date = DateTime.Parse(x.Key.ReportDate),
                    Count = x.Count(),
                    FireOrAlarmCount = x.Count(z => z.IsEventFireOrAlarm)
                })
                .OrderBy(x => x.Date);

            return new JsonResult(dailyIncidentReports);
        }

        [Route("[action]", Name = "Upload")]
        public async Task<JsonResult> Upload()
        {
            //if (_webHostEnvironment.IsDevelopment())
            //    throw new NotSupportedException("Dropbox upload not supported in development environment");

            await _irUploadService.Process();

            return new JsonResult(true);
        }
        //Old Code Commneted for logical issue 24/12/2024 dileep
        //[Route("[action]", Name = "DailyLogTimer")]
        //public JsonResult DailyLogTimer([FromQuery] DateTime dateFrom, [FromQuery] DateTime dateTo, [FromQuery] int siteId)
        //{
        //    var dailyGuardLogFrequency = new Dictionary<DateTime, bool?>();
        //    for (DateTime date = dateFrom; date <= dateTo; date = date.AddDays(1))
        //    {
        //        bool? isAcceptableLogFreq = null;
        //        var logBook = _clientDataProvider.GetClientSiteLogBook(siteId, LogBookType.DailyGuardLog, date);
        //        if (logBook != null)
        //        {
        //            var guardLogs = _guardLogDataProvider.GetGuardLogs(logBook.Id, logBook.Date);
        //            var deltas = CalculateLogTimeDeltas(guardLogs.Select(z => z.EventDateTime).OrderBy(z => z));
        //            isAcceptableLogFreq = deltas.All(z => z.TotalMinutes < 120);
        //        }
        //        //if keyvehicle log -start

        //            var keylogBook = _clientDataProvider.GetClientSiteLogBook(siteId, LogBookType.VehicleAndKeyLog, date);
        //            if (keylogBook != null)
        //            {
        //                var vehicleLogs = _guardLogDataProvider.GetKeyVehicleLogs(keylogBook.Id);
        //                var deltas = CalculateLogTimeDeltas(vehicleLogs.Select(z => Convert.ToDateTime(z.EntryTime)).OrderBy(z => z));
        //                isAcceptableLogFreq = deltas.All(z => z.TotalMinutes < 120);
        //            }


        //        //if keyvehicle log -end
        //        //if incident report log -start

        //        var incidentreportlogBook = _clientDataProvider.GetIncidentReports(date, siteId);
        //        if (incidentreportlogBook.Count() != 0)
        //        {
        //          //  var incidentReportLogs = _guardLogDataProvider.GetKeyVehicleLogs(keylogBook.Id);
        //            var deltas = CalculateLogTimeDeltas(incidentreportlogBook.Select(z => Convert.ToDateTime(z.CreatedOn)).OrderBy(z => z));
        //            isAcceptableLogFreq = deltas.All(z => z.TotalMinutes < 120);
        //        }


        //        //if incident report log -end
        //        dailyGuardLogFrequency.Add(date, isAcceptableLogFreq);
        //    }

        //    return new JsonResult(dailyGuardLogFrequency.Select(z => new { date = z.Key, isAcceptable = z.Value }));
        //}


        [Route("[action]", Name = "DailyLogTimer")]
        public JsonResult DailyLogTimer([FromQuery] DateTime dateFrom, [FromQuery] DateTime dateTo, [FromQuery] int siteId)
        {
            var dailyGuardLogFrequency = new Dictionary<DateTime, bool?>();
            for (DateTime date = dateFrom; date <= dateTo; date = date.AddDays(1))
            {
                bool? isAcceptableLogFreq = null;
                var logBook = _clientDataProvider.GetClientSiteLogBook(siteId, LogBookType.DailyGuardLog, date);
                if (logBook != null)
                {
                    //var guardLogs = _guardLogDataProvider.GetGuardLogs(logBook.Id, logBook.Date);
                    var guardLogs= _guardLogDataProvider.ClientSiteRadioChecksActivityStatus_History(logBook.ClientSiteId, logBook.Date);
                    
                    if (guardLogs.Count!=0)
                    {
                        var result = FindLargeGaps(guardLogs);
                        isAcceptableLogFreq = result.Count ==0;

                    }
                    else
                    {
                        var guardLogsFromLogbook = _guardLogDataProvider.GetGuardLogs(logBook.Id, logBook.Date);
                        var result = FindLargeGaps(guardLogsFromLogbook);
                        isAcceptableLogFreq = result.Count == 0;
                    }
                    //var gaurdCount= guardLogs.Select(log => log.GuardId).Distinct();
                    
                   
                }
                dailyGuardLogFrequency.Add(date, isAcceptableLogFreq);
            }

            return new JsonResult(dailyGuardLogFrequency.Select(z => new { date = z.Key, isAcceptable = z.Value }));
        }



        public static List<TimeDifference> FindLargeGaps(List<ClientSiteRadioChecksActivityStatus_History> eventLogs)
        {
            //var guardLogs = _guardLogDataProvider.GetGuardLogs(logBook.Id, logBook.Date);
            //var logBook = _clientDataProvider.GetClientSiteLogBook(siteId, LogBookType.DailyGuardLog, date);
            //// Sort events by EventTime
            eventLogs = eventLogs.OrderBy(log => log.EventDateTime).ToList();

            var largeGaps = new List<TimeDifference>();

            for (int i = 1; i < eventLogs.Count; i++)
            {
                var previousLog = eventLogs[i - 1];
                var currentLog = eventLogs[i];

                //Skip time difference calculation if the previous event is "Logout"

                if (!string.IsNullOrEmpty(previousLog.Notes))
                {
                    if (previousLog.Notes.Contains("Guard Off Duty", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                }

                // Calculate the time difference
                var timeDifference = (currentLog.EventDateTime - previousLog.EventDateTime).TotalMinutes;

                // Check if the difference is greater than 2 hours (120 minutes)
                if (timeDifference > 120)
                {
                    largeGaps.Add(new TimeDifference
                    {
                        PreviousEventNote = previousLog.Notes,
                        CurrentEventNote = currentLog.Notes,
                        TimeDifferenceBteweenEvents = timeDifference
                    });
                }
            }

            return largeGaps;
        }




        public static List<TimeDifference> FindLargeGaps(List<GuardLog> eventLogs)
        {
            //var guardLogs = _guardLogDataProvider.GetGuardLogs(logBook.Id, logBook.Date);
            //var logBook = _clientDataProvider.GetClientSiteLogBook(siteId, LogBookType.DailyGuardLog, date);
            //// Sort events by EventTime
            eventLogs = eventLogs.OrderBy(log => log.EventDateTime).ToList();

            var largeGaps = new List<TimeDifference>();

            for (int i = 1; i < eventLogs.Count; i++)
            {
                var previousLog = eventLogs[i - 1];
                var currentLog = eventLogs[i];

                // Skip time difference calculation if the previous event is "Logout"
                //if (previousLog.Notes.Contains("Guard Off Duty", StringComparison.OrdinalIgnoreCase))
                //{
                //    continue;
                //}

                // Calculate the time difference
                var timeDifference = (currentLog.EventDateTime - previousLog.EventDateTime).TotalMinutes;

                // Check if the difference is greater than 2 hours (120 minutes)
                if (timeDifference > 120)
                {
                    largeGaps.Add(new TimeDifference
                    {
                        PreviousEventNote = previousLog.Notes,
                        CurrentEventNote = currentLog.Notes,
                        TimeDifferenceBteweenEvents = timeDifference
                    });
                }
            }

            return largeGaps;
        }


        private static IEnumerable<TimeSpan> CalculateLogTimeDeltas(IEnumerable<DateTime> times)
        {
            var time_deltas = new List<TimeSpan>();
            DateTime prev = times.FirstOrDefault();
            foreach (var t in times.Skip(1))
            {
                time_deltas.Add(t - prev);
                prev = t;
            }

            return time_deltas;
        }
    }


    public class TimeDifference
    {
        public string PreviousEventNote { get; set; }
        public string CurrentEventNote { get; set; }
        public double TimeDifferenceBteweenEvents { get; set; } // In minutes
    }


}

