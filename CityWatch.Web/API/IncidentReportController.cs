using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            if (_webHostEnvironment.IsDevelopment())
                throw new NotSupportedException("Dropbox upload not supported in development environment");

            await _irUploadService.Process();

            return new JsonResult(true);
        }

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
                    var guardLogs = _guardLogDataProvider.GetGuardLogs(logBook.Id, logBook.Date);
                    var deltas = CalculateLogTimeDeltas(guardLogs.Select(z => z.EventDateTime).OrderBy(z => z));
                    isAcceptableLogFreq = deltas.All(z => z.TotalMinutes < 120);
                }
                //if keyvehicle log -start
                else
                {
                    var keylogBook = _clientDataProvider.GetClientSiteLogBook(siteId, LogBookType.VehicleAndKeyLog, date);
                    if (keylogBook != null)
                    {
                        var vehicleLogs = _guardLogDataProvider.GetKeyVehicleLogs(keylogBook.Id);
                        var deltas = CalculateLogTimeDeltas(vehicleLogs.Select(z => Convert.ToDateTime(z.EntryTime)).OrderBy(z => z));
                        isAcceptableLogFreq = deltas.All(z => z.TotalMinutes < 120);
                    }
                }
                //if keyvehicle log -end
                dailyGuardLogFrequency.Add(date, isAcceptableLogFreq);
            }

            return new JsonResult(dailyGuardLogFrequency.Select(z => new { date = z.Key, isAcceptable = z.Value }));
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
}
