using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Kpi.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Kpi.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class KpiReportController : ControllerBase
    {
        private readonly IKpiSchedulesDataProvider _kpiSchedulesDataProvider;
        private readonly ILogger<KpiReportController> _logger;
        private readonly ISendScheduleService _sendScheduleService;
        private readonly IReportUploadService _reportUploadService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public KpiReportController(IKpiSchedulesDataProvider kpiSchedulesDataProvider,
            ILogger<KpiReportController> logger,
            IWebHostEnvironment webHostEnvironment,
            ISendScheduleService sendScheduleService,
            IReportUploadService reportUploadService)
        {
            _kpiSchedulesDataProvider = kpiSchedulesDataProvider;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _sendScheduleService = sendScheduleService;
            _reportUploadService = reportUploadService;
        }

        [Route("[action]", Name = "Send")]
        [HttpGet]
        public async Task<bool> Send()
        {
            var prevJob = _kpiSchedulesDataProvider.GetAllKpiSendScheduleJobs().FirstOrDefault(z => !z.CompletedDate.HasValue);
            if (prevJob != null)
            {
                _logger.LogWarning($"KpiSendJob: Another job ({prevJob.Id}) is in progress.");
                return false;
            }

            var pendingSchedules = _kpiSchedulesDataProvider.GetAllSendSchedules()
                .Where(z => z.NextRunOn < DateTime.Now && !z.IsPaused)
                .ToList();
            if (!pendingSchedules.Any())
            {
                _logger.LogInformation("KpiSendJob: No schedule to process.");
                return true;
            }

            var sendScheduleJob = new KpiSendScheduleJob()
            {
                CreatedDate = DateTime.Now                
            };
            sendScheduleJob.Id = _kpiSchedulesDataProvider.SaveSendScheduleJob(sendScheduleJob);

            var success = true;
            var statusLog = new StringBuilder();
            statusLog.AppendFormat("KpiSendJob: {0} - Starting. ", sendScheduleJob.Id);
            var scheduleResults = new Dictionary<int, string>();
            try
            {
                foreach (var schedule in pendingSchedules)
                {
                    schedule.KpiSendScheduleSummaryNotes = _kpiSchedulesDataProvider.GetKpiSendScheduleSummaryNotes(schedule.Id);
                    var result = await _sendScheduleService.ProcessSchedule(schedule, new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), false, true);
                    statusLog.Append(result);
                }                
            }
            catch (Exception ex)
            {
                success = false;
                statusLog.AppendFormat("KpiSendJob: {0} Exception - {1}. ", sendScheduleJob.Id, ex.Message);
            }
            statusLog.AppendFormat("KpiSendJob: {0} Completed. Status - {1}", sendScheduleJob.Id, success);
            sendScheduleJob.Success = success;
            sendScheduleJob.CompletedDate = DateTime.Now;
            _kpiSchedulesDataProvider.SaveSendScheduleJob(sendScheduleJob);

            _logger.LogInformation(statusLog.ToString());
            return success;
        }

        [Route("[action]", Name = "Upload")]
        [HttpGet]
        public async Task<bool> Upload()
        {
            if (_webHostEnvironment.IsDevelopment())
                throw new NotSupportedException("Dropbox upload not supported in development environment");

            var success = false;

            try
            {
                var reportFromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                success = await _reportUploadService.ProcessUpload(reportFromDate);

                if (DateTime.Today.Day == 1)
                {
                    success = await _reportUploadService.ProcessUpload(reportFromDate.AddMonths(-1));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.StackTrace);
            }
            
            return success;
        }

        [Route("[action]", Name = "SendTimeSheet")]
        [HttpGet]
        public async Task<bool> SendTimeSheet()
        {
            var prevJob = _kpiSchedulesDataProvider.GetAllKpiSendScheduleJobsTimesheet().FirstOrDefault(z => !z.CompletedDate.HasValue);
            if (prevJob != null)
            {
                _logger.LogWarning($"KpiSendJob: Another job ({prevJob.Id}) is in progress.");
                return false;
            }

            var pendingSchedules = _kpiSchedulesDataProvider.GetAllTimesheetSchedules()
                .Where(z => z.NextRunOn < DateTime.Now)
                .ToList();
            if (!pendingSchedules.Any())
            {
                _logger.LogInformation("KpiSendJob: No schedule to process.");
                return true;
            }

            var sendScheduleJob = new KpiSendScheduleJobsTimeSheet()
            {
                CreatedDate = DateTime.Now
            };
            sendScheduleJob.Id = _kpiSchedulesDataProvider.SaveSendScheduleJobTimesheet(sendScheduleJob);

            var success = true;
            var statusLog = new StringBuilder();
            statusLog.AppendFormat("KpiSendJob: {0} - Starting. ", sendScheduleJob.Id);
            var scheduleResults = new Dictionary<int, string>();
            try
            {
                foreach (var schedule in pendingSchedules)
                {
                    //schedule.KpiSendScheduleSummaryNotes = _kpiSchedulesDataProvider.GetKpiSendScheduleSummaryNotes(schedule.Id);
                    var result = await _sendScheduleService.ProcessTimeSheetSchedule(schedule, new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), false, true);
                    statusLog.Append(result);
                }
            }
            catch (Exception ex)
            {
                success = false;
                statusLog.AppendFormat("KpiSendJob: {0} Exception - {1}. ", sendScheduleJob.Id, ex.Message);
            }
            statusLog.AppendFormat("KpiSendJob: {0} Completed. Status - {1}", sendScheduleJob.Id, success);
            sendScheduleJob.Success = success;
            sendScheduleJob.CompletedDate = DateTime.Now;
            _kpiSchedulesDataProvider.SaveSendScheduleJobTimesheet(sendScheduleJob);

            _logger.LogInformation(statusLog.ToString());
            return success;
        }

        [Route("[action]", Name = "UploadTimeSheet")]
        [HttpGet]
        public async Task<bool> UploadTimeSheet()
        {
            if (_webHostEnvironment.IsDevelopment())
                throw new NotSupportedException("Dropbox upload not supported in development environment");

            var success = false;

            try
            {
                var reportFromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                success = await _reportUploadService.ProcessUploadTimesheet(reportFromDate);

                if (DateTime.Today.Day == 1)
                {
                    success = await _reportUploadService.ProcessUploadTimesheet(reportFromDate.AddMonths(-1));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.StackTrace);
            }

            return success;
        }
    }
}
