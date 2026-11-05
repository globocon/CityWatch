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
        private readonly ITagHistoryScheduleService _tagHistoryScheduleService;

        public KpiReportController(IKpiSchedulesDataProvider kpiSchedulesDataProvider,
            ILogger<KpiReportController> logger,
            IWebHostEnvironment webHostEnvironment,
            ISendScheduleService sendScheduleService,
            IReportUploadService reportUploadService,
            ITagHistoryScheduleService tagHistoryScheduleService)
        {
            _kpiSchedulesDataProvider = kpiSchedulesDataProvider;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _sendScheduleService = sendScheduleService;
            _reportUploadService = reportUploadService;
            _tagHistoryScheduleService = tagHistoryScheduleService;
        }

        [Route("[action]", Name = "Send")]
        [HttpGet]
        public async Task<bool> Send()
        {
            var prevJob = _kpiSchedulesDataProvider.GetAllKpiSendScheduleJobs().FirstOrDefault(z => !z.CompletedDate.HasValue);
            if (prevJob != null)
            {
                _kpiSchedulesDataProvider.RemoveAllKpiSendScheduleJobsOldNotComplete();
                //_logger.LogWarning($"KpiSendJob: Another job ({prevJob.Id}) is in progress.");
                //return false;
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


        [Route("[action]", Name = "SendCustomWand")]
        [HttpGet]
        public async Task<bool> SendCustomWand()
        {
            var prevJob = _kpiSchedulesDataProvider.GetAllKpiSendScheduleJobsCustomWand().FirstOrDefault(z => !z.CompletedDate.HasValue);
            if (prevJob != null)
            {
                _logger.LogWarning($"KpiCustomWandSendJob: Another job ({prevJob.Id}) is in progress.");
                return false;
            }

            var pendingSchedules = _kpiSchedulesDataProvider.GetAllCustomWandSchedules().Where(z => z.NextRunOn < DateTime.Now).ToList();
            if (!pendingSchedules.Any())
            {
                _logger.LogInformation("KpiCustomWandSendJob: No schedule to process.");
                return true;
            }

            var sendScheduleJob = new KpiSendScheduleJobsCustomWand()
            {
                CreatedDate = DateTime.Now
            };
            sendScheduleJob.Id = _kpiSchedulesDataProvider.SaveSendScheduleJobCustomWand(sendScheduleJob);

            var success = true;
            var statusLog = new StringBuilder();
            statusLog.AppendLine($"KpiCustomWandSendJob: {sendScheduleJob.Id} - Starting.");
            //var scheduleResults = new Dictionary<int, string>();
            try
            {
                foreach (var schedule in pendingSchedules)
                {
                    DateTime _reportStartDate = schedule.NextRunOn; //new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    var (result, fileNames) = await _sendScheduleService.ProcessCustomWandSchedule(schedule, _reportStartDate, false, true, true, true, true);
                    statusLog.AppendLine(result);
                }
            }
            catch (Exception ex)
            {
                success = false;
                statusLog.AppendLine($"KpiCustomWandSendJob: {sendScheduleJob.Id} Exception - {ex.Message}.");
            }
            statusLog.AppendLine($"KpiCustomWandSendJob: {sendScheduleJob.Id} Completed. Status - {success}.");
            sendScheduleJob.Success = success;
            sendScheduleJob.CompletedDate = DateTime.Now;
            sendScheduleJob.StatusMessage = statusLog.ToString();
            _kpiSchedulesDataProvider.SaveSendScheduleJobCustomWand(sendScheduleJob);

            _logger.LogInformation(statusLog.ToString());
            return success;
        }

        /// <summary>
        /// Weekly Tag History emails. Call it on the same timer as the other send jobs; each
        /// schedule only runs once its NextRunOn has passed. Every run - including a week with
        /// no changes, which sends nothing - is recorded in KpiSendScheduleJobsTagHistory.
        /// </summary>
        [Route("[action]", Name = "SendTagHistory")]
        [HttpGet]
        public bool SendTagHistory()
        {
            // A job row left open by a crashed run stops blocking after two hours.
            var prevJob = _kpiSchedulesDataProvider.GetTagHistoryJobsInProgress(TimeSpan.FromHours(2)).FirstOrDefault();
            if (prevJob != null)
            {
                _logger.LogWarning($"KpiTagHistorySendJob: Another job ({prevJob.Id}) is in progress.");
                return false;
            }

            var pendingSchedules = _kpiSchedulesDataProvider.GetAllTagHistorySchedules()
                .Where(z => z.NextRunOn < DateTime.Now && !z.IsPaused)
                .ToList();
            if (!pendingSchedules.Any())
            {
                _logger.LogInformation("KpiTagHistorySendJob: No schedule to process.");
                return true;
            }

            var sendScheduleJob = new KpiSendScheduleJobsTagHistory()
            {
                CreatedDate = DateTime.Now,
                RunType = "Auto"
            };
            sendScheduleJob.Id = _kpiSchedulesDataProvider.SaveSendScheduleJobTagHistory(sendScheduleJob);

            var success = true;
            var statusLog = new StringBuilder();
            statusLog.AppendLine($"KpiTagHistorySendJob: {sendScheduleJob.Id} - Starting. {pendingSchedules.Count} schedule(s).");
            foreach (var schedule in pendingSchedules)
            {
                try
                {
                    // The week before the day it was due, even if the timer runs late.
                    var result = _tagHistoryScheduleService.ProcessSchedule(schedule, schedule.NextRunOn, true);
                    statusLog.AppendLine($"Schedule {schedule.Id}: {result.RecordCount} change(s). {result.Message}");
                    success &= result.Success;
                }
                catch (Exception ex)
                {
                    success = false;
                    statusLog.AppendLine($"Schedule {schedule.Id}: Exception - {ex.Message}.");
                }
            }
            statusLog.AppendLine($"KpiTagHistorySendJob: {sendScheduleJob.Id} Completed. Status - {success}.");

            sendScheduleJob.Success = success;
            sendScheduleJob.CompletedDate = DateTime.Now;
            sendScheduleJob.StatusMessage = statusLog.ToString();
            _kpiSchedulesDataProvider.SaveSendScheduleJobTagHistory(sendScheduleJob);

            _logger.LogInformation(statusLog.ToString());
            return success;
        }

        [Route("[action]", Name = "SendKeyVehicleLogs")]
        [HttpGet]
        public async Task<bool> SendKeyVehicleLogs()
        {
            var prevJob = _kpiSchedulesDataProvider.GetAllKpiSendScheduleJobsKV().FirstOrDefault(z => !z.CompletedDate.HasValue);
            if (prevJob != null)
            {
                _logger.LogWarning($"KpiSendJob: Another job ({prevJob.Id}) is in progress.");
                return false;
            }

            var pendingSchedules = _kpiSchedulesDataProvider.GetAllKVSchedules()
                .Where(z => z.NextRunOn < DateTime.Now)
                .ToList();
            if (!pendingSchedules.Any())
            {
                _logger.LogInformation("KpiSendJob: No schedule to process.");
                return true;
            }

            var sendScheduleJob = new KpiSendScheduleJobsKV()
            {
                CreatedDate = DateTime.Now
            };
            sendScheduleJob.Id = _kpiSchedulesDataProvider.SaveSendScheduleJobKV(sendScheduleJob);

            var success = true;
            var statusLog = new StringBuilder();
            statusLog.AppendFormat("KpiSendJob: {0} - Starting. ", sendScheduleJob.Id);
            var scheduleResults = new Dictionary<int, string>();
            try
            {
                foreach (var schedule in pendingSchedules)
                {
                    //schedule.KpiSendScheduleSummaryNotes = _kpiSchedulesDataProvider.GetKpiSendScheduleSummaryNotes(schedule.Id);
                    var result = await _sendScheduleService.ProcessKVSchedule(schedule, new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), false, true);
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
            _kpiSchedulesDataProvider.SaveSendScheduleJobKV(sendScheduleJob);

            _logger.LogInformation(statusLog.ToString());
            return success;
        }

        [Route("[action]", Name = "UploadKV")]
        [HttpGet]
        public async Task<bool> UploadKV()
        {
            if (_webHostEnvironment.IsDevelopment())
                throw new NotSupportedException("Dropbox upload not supported in development environment");

            var success = false;

            try
            {
                var reportFromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                success = await _reportUploadService.ProcessUploadKV(reportFromDate);

                if (DateTime.Today.Day == 1)
                {
                    success = await _reportUploadService.ProcessUploadKV(reportFromDate.AddMonths(-1));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.StackTrace);
            }

            return success;
        }

        [Route("[action]", Name = "SendKV")]
        [HttpGet]
        public async Task<bool> SendKV()
        {
            var prevJob = _kpiSchedulesDataProvider.GetAllKpiSendScheduleJobsKV().FirstOrDefault(z => !z.CompletedDate.HasValue);
            if (prevJob != null)
            {
                _logger.LogWarning($"KpiSendJob: Another job ({prevJob.Id}) is in progress.");
                return false;
            }

            var pendingSchedules = _kpiSchedulesDataProvider.GetAllKVSchedules()
                .Where(z => z.NextRunOn < DateTime.Now)
                .ToList();
            if (!pendingSchedules.Any())
            {
                _logger.LogInformation("KpiSendJob: No schedule to process.");
                return true;
            }

            var sendScheduleJob = new KpiSendScheduleJobsKV()
            {
                CreatedDate = DateTime.Now
            };
            sendScheduleJob.Id = _kpiSchedulesDataProvider.SaveSendScheduleJobKV(sendScheduleJob);

            var success = true;
            var statusLog = new StringBuilder();
            statusLog.AppendFormat("KpiSendJob: {0} - Starting. ", sendScheduleJob.Id);
            var scheduleResults = new Dictionary<int, string>();
            try
            {
                foreach (var schedule in pendingSchedules)
                {
                    //schedule.KpiSendScheduleSummaryNotes = _kpiSchedulesDataProvider.GetKpiSendScheduleSummaryNotes(schedule.Id);
                    var result = await _sendScheduleService.ProcessKVSchedule(schedule, new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), false, true);
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
            _kpiSchedulesDataProvider.SaveSendScheduleJobKV(sendScheduleJob);

            _logger.LogInformation(statusLog.ToString());
            return success;
        }
    }
}
