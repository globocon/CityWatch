using CityWatch.Data;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Kpi.Helpers;
using CityWatch.Kpi.Models;
using Dropbox.Api;
using Dropbox.Api.Common;
using Dropbox.Api.Files;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CityWatch.Kpi.Services
{
    public class ImportJobResult
    {
        public ImportJobResult()
        { }

        public ImportJobResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public interface IImportDataService
    {
        Task Run(int jobId);
    }

    public class ImportDataService : IImportDataService
    {
        private readonly Settings _settings;
        private readonly CityWatchDbContext _dbContext;
        private readonly IImportJobDataProvider _importJobDataProvider;
        private readonly IClientDataProvider _clientDataProvider;

        public ImportDataService(IOptions<Settings> settings,
            CityWatchDbContext dbContext,
            IImportJobDataProvider importJobDataProvider,
            IClientDataProvider clientDataProvider)
        {
            _settings = settings.Value;
            _dbContext = dbContext;
            _importJobDataProvider = importJobDataProvider;
            _clientDataProvider = clientDataProvider;
        }

        /// <summary>
        /// Process a single import job
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        public async Task Run(int jobId)
        {
            var item = _importJobDataProvider.GetKpiDataImportJobById(jobId);
            if (item != null)
            {
                ImportJobResult result;
                try
                {
                    result = await RunJob(item);
                }
                catch (Exception ex)
                {
                    result = new ImportJobResult(false, $"[Exception] Message: {ex.Message} Trace: {ex.StackTrace}");
                }
                UpdateJobStatus(item, result);
            }
        }

        /// <summary>
        /// Import four KPI data for a single client - report month
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private async Task<ImportJobResult> RunJob(KpiDataImportJob item)
        {
            var runLog = new StringBuilder();

            // Days in report month for which KPI data has to be created or updated
            var datesInReport = GetDatesToProcess(item.ReportDate);
            var datesToProcess = datesInReport.ToList();
            if (!datesToProcess.Any())
                return new ImportJobResult(false, "No dates to process");

            var clientSiteKpiSetting = _clientDataProvider.GetClientSiteKpiSetting(item.ClientSiteId);
            if (clientSiteKpiSetting == null)
                return new ImportJobResult(false, "ClientSiteKpiSettings Missing");

            runLog.AppendFormat(GetFormattedLogMessage($"Job (id={item.Id}) run started"));

            // Get IR count
            var irCounts = await GetIrCount(datesToProcess, item.ClientSiteId);
            runLog.AppendFormat(GetFormattedLogMessage("IR count collected"));

            // Get Log in Acceptable range            
            var dailyLogTimers = await GetDailyLogTimer(datesToProcess, item.ClientSiteId);
            runLog.AppendFormat(GetFormattedLogMessage("Daily log data collected"));

            // Get Dropbox Image Count
            var imageCounts = await GetImageCount(clientSiteKpiSetting, item.ReportDate, datesToProcess);
            runLog.AppendFormat(GetFormattedLogMessage("Images count collected"));

            // Get WAND Scan count
            var wandScanCounts = await GetWandScansCount(clientSiteKpiSetting, datesToProcess);
            runLog.AppendFormat(GetFormattedLogMessage("Wand scans count collected"));

            // Get employee hours
            var employeeHours = GetEmployeeHours(clientSiteKpiSetting, datesToProcess);
            runLog.AppendFormat(GetFormattedLogMessage("Employee hours collected"));

            // Create and save daily KPI data
            var dailyKpis = new List<DailyClientSiteKpi>();
            foreach (var date in datesInReport)
            {
                var pastDate = date <= DateTime.Today;
                var irCount = irCounts.ContainsKey(date) && irCounts[date] != null ? irCounts[date].Count : 0;
                var fireOrAlarmCount = irCounts.ContainsKey(date) && irCounts[date] != null ? irCounts[date].FireOrAlarmCount : 0;
                var imageCount = imageCounts.GetValueOrDefault(date, 0);
                var wandScanCount = wandScanCounts.GetValueOrDefault(date, 0);
                var employeeHour = employeeHours.GetValueOrDefault(date, 0);
                var acceptableLogFreq = dailyLogTimers.ContainsKey(date) && dailyLogTimers[date] != null ? dailyLogTimers[date].IsAcceptable : null;

                var kpi = new DailyClientSiteKpi()
                {
                    Date = date,
                    ClientSiteId = item.ClientSiteId,
                    IncidentCount = pastDate ? irCount : null,
                    FireOrAlarmCount = pastDate ? fireOrAlarmCount : null,
                    ImageCount = pastDate ? imageCount : null,
                    WandScanCount = pastDate ? wandScanCount : null,
                    EmployeeHours = pastDate ? employeeHour : null,
                    IsAcceptableLogFreq = acceptableLogFreq,
                };
                dailyKpis.Add(kpi);
            }
            SaveDailyKpiData(item, dailyKpis);
            runLog.AppendFormat(GetFormattedLogMessage("DailyKpiData saved"));

            runLog.AppendFormat(GetFormattedLogMessage("Completed"));
            return new ImportJobResult(true, runLog.ToString());
        }

        private string GetFormattedLogMessage(string message)
        {
            return $"{message} at {DateTime.Now.ToString("dd-MM-yy HH:mm")} {Environment.NewLine}";
        }

        private void UpdateJobStatus(KpiDataImportJob item, ImportJobResult importJobResult)
        {
            item.Success = importJobResult?.Success;
            item.StatusMessage = importJobResult?.Message;
            item.CompletedDate = DateTime.Now;
            _importJobDataProvider.SaveKpiDataImportJob(item);
        }

        private void SaveDailyKpiData(KpiDataImportJob item, List<DailyClientSiteKpi> kpis)
        {
            var kpiDates = kpis.Select(x => x.Date).ToList();
            var kpisToUpdate = _dbContext.DailyClientSiteKpis.Where(x => x.ClientSiteId == item.ClientSiteId && kpiDates.Contains(x.Date));
            foreach (var kpi in kpis)
            {
                var existingDateKpi = kpisToUpdate.SingleOrDefault(x => x.Date == kpi.Date);
                if (existingDateKpi != null)
                {
                    existingDateKpi.EmployeeHours = kpi.EmployeeHours;
                    existingDateKpi.ImageCount = kpi.ImageCount;
                    existingDateKpi.IncidentCount = kpi.IncidentCount;
                    existingDateKpi.WandScanCount = kpi.WandScanCount;
                    existingDateKpi.FireOrAlarmCount = kpi.FireOrAlarmCount;
                    existingDateKpi.IsAcceptableLogFreq = kpi.IsAcceptableLogFreq;
                }
                else
                {
                    _dbContext.DailyClientSiteKpis.Add(kpi);
                }
            }
            _dbContext.SaveChanges();
        }

        private async Task<Dictionary<DateTime, DailyIrCount>> GetIrCount(List<DateTime> kpiDates, int clientSiteId)
        {
            var irCounts = new Dictionary<DateTime, DailyIrCount>();
            var results = new List<DailyIrCount>();
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(_settings.IrApiUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var url = $"api/incidentreport?dateFrom={kpiDates.Min().ToString("yyyy-MM-dd")}&dateTo={kpiDates.Max().ToString("yyyy-MM-dd")}&siteId={clientSiteId}";
                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var resultString = await response.Content.ReadAsStringAsync();
                    results = JsonSerializer.Deserialize<List<DailyIrCount>>(resultString);
                }
            }

            foreach (var date in kpiDates)
            {
                var irResult = results.SingleOrDefault(x => x.Date == date);
                irCounts.Add(date, irResult);
            }

            return irCounts;
        }

        private async Task<Dictionary<DateTime, DailyLogTimer>> GetDailyLogTimer(List<DateTime> kpiDates, int clientSiteId)
        {
            var isAcceptableLogFreq = new Dictionary<DateTime, DailyLogTimer>();
            var results = new List<DailyLogTimer>();
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(_settings.IrApiUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var url = $"api/incidentreport/DailyLogTimer?dateFrom={kpiDates.Min():yyyy-MM-dd}&dateTo={kpiDates.Max():yyyy-MM-dd}&siteId={clientSiteId}";
                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var resultString = await response.Content.ReadAsStringAsync();
                    results = JsonSerializer.Deserialize<List<DailyLogTimer>>(resultString);
                }
            }

            foreach (var date in kpiDates)
            {
                var dailyLogFreqResult = results.SingleOrDefault(x => x.Date == date);
                isAcceptableLogFreq.Add(date, dailyLogFreqResult);
            }
            return isAcceptableLogFreq;
        }
    
        private async Task<Dictionary<DateTime, int>> GetImageCount(ClientSiteKpiSetting clientSiteKpiSetting, DateTime reportDate, List<DateTime> kpiDates)
        {
            var dbxItemCount = new Dictionary<string, int>();
            var imageCount = new Dictionary<DateTime, int>();
            
            if (string.IsNullOrEmpty(clientSiteKpiSetting.DropboxImagesDir))
                return imageCount;

            try
            {
                using (var dbxTeam = new DropboxTeamClient(_settings.DropboxAccessToken, _settings.DropboxRefreshToken, _settings.DropboxAppKey, _settings.DropboxAppSecret, new DropboxClientConfig()))
                {
                    try
                    {
                        var team = dbxTeam.Team.MembersListAsync().Result;
                        if (team.Members.Count > 0)
                        {
                            var cwsMember = team.Members.SingleOrDefault(z => z.Profile.Email == _settings.DropboxUserEmail);
                            if (cwsMember != null)
                            {
                                var dbx = dbxTeam.AsMember(cwsMember.Profile.TeamMemberId);
                                var account = await dbx.Users.GetCurrentAccountAsync();
                                var nsId = new PathRoot.NamespaceId(account.RootInfo.RootNamespaceId);

                                var siteBasePathRoot = clientSiteKpiSetting.DropboxImagesDir.Remove(0, 1).Split('/').First();
                                var siteBasePath = clientSiteKpiSetting.DropboxImagesDir;
                                var monthBasePath = $"{siteBasePath}/FLIR - Wand Recordings - IRs - Daily Logs/{reportDate.Year}/{reportDate.ToString("yyyyMM")} - {reportDate.ToString("MMMM").ToUpper()} DATA/";
                                try
                                {
                                    await ReadMonthlyFolder(clientSiteKpiSetting, dbx, nsId, monthBasePath, dbxItemCount);
                                }
                                catch (ApiException<ListFolderError>)
                                {

                                }                                

                                siteBasePath = clientSiteKpiSetting.DropboxImagesDir.Replace(siteBasePathRoot, $"{siteBasePathRoot}-Archive");
                                monthBasePath = $"{siteBasePath}/FLIR - Wand Recordings - IRs - Daily Logs/{reportDate.Year}/{reportDate.ToString("yyyyMM")} - {reportDate.ToString("MMMM").ToUpper()} DATA/";
                                try
                                {
                                    await ReadMonthlyFolder(clientSiteKpiSetting, dbx, nsId, monthBasePath, dbxItemCount);
                                }
                                catch (ApiException<ListFolderError>)
                                {

                                }                                
                            }
                        }
                    }
                    catch (ApiException<ListFolderError>)
                    {
                        // handle ListFolder-specific error
                    }
                    catch (DropboxException ex)
                    {
                        // inspect and handle ex as desired
                        if (ex is AuthException)
                        {
                            // handle AuthException, which can happen on any call
                            if (((AuthException)ex).ErrorResponse.IsInvalidAccessToken)
                            {
                                // handle invalid access token case
                            }
                        }
                        else if (ex is HttpException)
                        {
                            // handle HttpException, which can happen on any call
                        }
                    }
                }
            }
            catch (Exception)
            {

            }

            foreach (var date in kpiDates)
            {
                var folderName = clientSiteKpiSetting.IsWeekendOnlySite ? $"{date.ToString("yyyyMMdd")} - {date.ToString("ddd").ToUpper()}" : date.ToString("yyyyMMdd");
                imageCount.Add(date, dbxItemCount.GetValueOrDefault(folderName, 0));
            }   

            return imageCount;
        }

        private static async Task ReadMonthlyFolder(ClientSiteKpiSetting clientSiteKpiSetting, DropboxClient dbx, PathRoot.NamespaceId nsId, string basePath, Dictionary<string, int> dbxItemCount)
        {
            var basePathResult = await dbx.WithPathRoot(nsId).Files.ListFolderAsync(basePath);
            var hasMoreFiles = basePathResult.HasMore;
            foreach (var result in basePathResult.Entries.Where(e => e.IsFolder))
            {
                await ReadDailyFolder(clientSiteKpiSetting, dbx, nsId, dbxItemCount, result);
            }

            while (hasMoreFiles)
            {
                basePathResult = await dbx.WithPathRoot(nsId).Files.ListFolderContinueAsync(basePathResult.Cursor);
                hasMoreFiles = basePathResult.HasMore;
                foreach (var result in basePathResult.Entries.Where(e => e.IsFolder))
                {
                    await ReadDailyFolder(clientSiteKpiSetting, dbx, nsId, dbxItemCount, result);
                }
            }
        }

        private static async Task ReadDailyFolder(ClientSiteKpiSetting clientSiteKpiSetting, DropboxClient dbx, PathRoot.NamespaceId nsId, Dictionary<string, int> dbxItemCount, Metadata result)
        {
            var subPathFormat = clientSiteKpiSetting.IsWeekendOnlySite ? "yyyyMMdd - ddd" : "yyyyMMdd";
            if (DateTime.TryParseExact(result.Name.ToUpper(), subPathFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _) &&
                !dbxItemCount.ContainsKey(result.Name))
            {
                var subPath = await GetExactImageFolder(dbx, nsId, result.PathLower);
                try
                {
                    var subPathResult = await dbx.WithPathRoot(nsId).Files.ListFolderAsync(subPath);
                    var fileCount = subPathResult.Entries.Where(e => e.IsFile).Count();
                    var hasMoreFiles = subPathResult.HasMore;
                    while (hasMoreFiles)
                    {
                        subPathResult = await dbx.WithPathRoot(nsId).Files.ListFolderContinueAsync(subPathResult.Cursor);
                        fileCount += subPathResult.Entries.Where(e => e.IsFile).Count();
                        hasMoreFiles = subPathResult.HasMore;
                    }
                    dbxItemCount.Add(result.Name, fileCount);
                }
                catch (ApiException<ListFolderError>)
                {

                }
            }
        }

        private static async Task<string> GetExactImageFolder(DropboxClient dbx, PathRoot.NamespaceId nsId, string imageFolderPath)
        {
            var subPathResult = await dbx.WithPathRoot(nsId).Files.ListFolderAsync(imageFolderPath);
            if (subPathResult.Entries.Any(e => e.IsFolder && e.Name.Equals("Daily Photos", StringComparison.OrdinalIgnoreCase)))
                return imageFolderPath + "/Daily Photos";
            
            return imageFolderPath;
        }

        private async Task<Dictionary<DateTime, int>> GetWandScansCount(ClientSiteKpiSetting clientSiteKpiSetting, List<DateTime> kpiDates)
        {
            var wandScans = new Dictionary<DateTime, int>();
            var results = new List<DailyWandScanCount>();
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(_settings.WandApiUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(_settings.WandApiToken);
                var url = $"reports-api/daily_data/?agency=citywatch&start_date={ kpiDates.Min().ToString("yyyy-MM-dd") }" +
                            $"&end_date={ kpiDates.Max().ToString("yyyy-MM-dd") }" +
                            $"&site_id={clientSiteKpiSetting.KoiosClientSiteId}" + 
                            $"&limit=-1";
                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var resultString = await response.Content.ReadAsStringAsync();
                    results = JsonSerializer.Deserialize<List<DailyWandScanCount>>(resultString);
                }
            }


            foreach (var date in kpiDates)
            {
                var wandScanResult = results.SingleOrDefault(x => x.Date == date)?.Count;
                wandScans.Add(date, wandScanResult.GetValueOrDefault());
            }

            return wandScans;
        }

        private static Dictionary<DateTime, decimal> GetEmployeeHours(ClientSiteKpiSetting clientSiteKpiSetting, List<DateTime> kpiDates)
        {
            var employeeHours = new Dictionary<DateTime, decimal>();
            foreach (var date in kpiDates) 
            {
                var empHrs = clientSiteKpiSetting.ClientSiteDayKpiSettings.SingleOrDefault(z => z.WeekDay == date.DayOfWeek)?.EmpHours ?? 1m;
                employeeHours.Add(date, empHrs);
            }
                
            return employeeHours;
        }

        private static IEnumerable<DateTime> GetDatesToProcess(DateTime reportDate)
        {
            var monthStartDate = new DateTime(reportDate.Year, reportDate.Month, 1);
            var monthEndDate = monthStartDate.AddMonths(1).AddDays(-1);
            var datesInMonth = monthStartDate.Range(monthEndDate);
            return datesInMonth;
        }
    }
}
