﻿using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Kpi.API;
using CityWatch.Kpi.Helpers;
using Dropbox.Api.Common;
using Dropbox.Api.Files;
using Dropbox.Api;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CityWatch.Data.Services;
using CityWatch.Common.Helpers;
using System.Security.Policy;
using System.Globalization;
using Dropbox.Api.Users;

namespace CityWatch.Kpi.Services
{
    public interface ISendScheduleService
    {
        Task<string> ProcessSchedule(KpiSendSchedule schedule, DateTime reportStartDate, bool ignoreRecipients, bool upload);
        byte[] ProcessDownload(KpiSendSchedule schedule, DateTime reportStartDate, bool ignoreRecipients, bool upload);
        byte[] ProcessDownloadTimeSheet(KpiSendTimesheetSchedules schedule, DateTime reportStartDate, bool ignoreRecipients, bool upload);

        Task<string> ProcessTimeSheetSchedule(KpiSendTimesheetSchedules schedule, DateTime reportStartDate, bool ignoreRecipients, bool upload);
    }

    public class SendScheduleService : ISendScheduleService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly EmailOptions _emailOptions;
        private readonly IImportJobDataProvider _importJobDataProvider;
        private readonly IImportDataService _importDataService;
        private readonly IReportGenerator _kpiReportGenerator;
        private readonly IKpiSchedulesDataProvider _kpiSchedulesDataProvider;
        private readonly IViewDataService _viewDataService;
        public readonly IPatrolDataReportService _patrolDataReportService;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly ILogger<SendScheduleService> _logger;
        private readonly Settings _settings;
        private readonly ITimesheetGenerator _kpiTimesheetReportGenerator;

        public SendScheduleService(IWebHostEnvironment webHostEnvironment,
            IOptions<EmailOptions> emailOptions,
            IImportJobDataProvider importJobDataProvider,
            IImportDataService importDataService,
            IReportGenerator kpiReportGenerator,
            IKpiSchedulesDataProvider kpiSchedulesDataProvider,
            IPatrolDataReportService patrolDataReportService,
            IViewDataService viewDataService,
            IClientDataProvider clientDataProvider,
            ILogger<SendScheduleService> logger,            
            IOptions<Settings> settings,
            ITimesheetGenerator kpiTimesheetReportGenerator)
        {
            _webHostEnvironment = webHostEnvironment;
            _emailOptions = emailOptions.Value;
            _importJobDataProvider = importJobDataProvider;
            _importDataService = importDataService;
            _kpiReportGenerator = kpiReportGenerator;
            _kpiSchedulesDataProvider = kpiSchedulesDataProvider;
            _viewDataService = viewDataService;
            _patrolDataReportService = patrolDataReportService;
            _clientDataProvider = clientDataProvider;
            _logger = logger;
            _settings = settings.Value;
            _kpiTimesheetReportGenerator = kpiTimesheetReportGenerator;
        }

        public async Task<string> ProcessSchedule(KpiSendSchedule schedule, DateTime reportStartDate, bool ignoreRecipients, bool upload)
        {
            var statusLog = new StringBuilder();
            try
            {
                statusLog.AppendFormat("Schedule {0} - Starting. ", schedule.Id);
                var siteIds = schedule.KpiSendScheduleClientSites.Select(z => z.ClientSiteId);
                var reportEndDate = reportStartDate.AddMonths(1).AddDays(-1);

                var siteReportFileNames = new List<string>();
                foreach (var siteId in siteIds)
                {
                    // Import Job
                    var serviceLog = new KpiDataImportJob()
                    {
                        ClientSiteId = siteId,
                        ReportDate = reportStartDate,
                        CreatedDate = DateTime.Now,
                    };
                    var jobId = _importJobDataProvider.SaveKpiDataImportJob(serviceLog);
                    await _importDataService.Run(jobId);

                    // Create Pdf Report
                    var fileName = _kpiReportGenerator.GeneratePdfReport(siteId, reportStartDate, reportEndDate, schedule.IsHrTimerPaused);
                    if (string.IsNullOrEmpty(fileName))
                    {
                        statusLog.AppendFormat("Site {0} - Error creating pdf. ", siteId);
                        continue;
                    }

                    siteReportFileNames.Add(Path.Combine(_webHostEnvironment.WebRootPath, "Pdf", "Output", fileName));
                    statusLog.AppendFormat("Site {0} - Completed. ", siteId);
                }

                if (siteReportFileNames.Any())
                {
                    schedule.ProjectName = GetSchduleIdentifier(schedule);

                    // Create summary page
                    var summaryFileName = CreateSummaryReport(schedule, reportStartDate, reportEndDate);

                    // Combine reports to a single pdf                    
                    var reportFileName = $"{FileNameHelper.GetSanitizedFileNamePart(schedule.ProjectName)} - Daily KPI Reports - {reportStartDate:MMM} {reportStartDate.Year}.pdf";
                    reportFileName = Path.Combine(_webHostEnvironment.WebRootPath, "Pdf", "Output", reportFileName);
                    PdfHelper.CombinePdfReports(reportFileName, siteReportFileNames, summaryFileName);

                    // Send Email
                    SendEmail(reportFileName, schedule, reportStartDate, ignoreRecipients);

                    if (upload)
                    {
                        schedule.NextRunOn = KpiSendScheduleRunOnCalculator.GetNextRunOn(schedule);
                        _kpiSchedulesDataProvider.SaveSendSchedule(schedule);

                        if (!_webHostEnvironment.IsDevelopment())
                            UploadReport(reportFileName, schedule, reportStartDate);
                    }

                    // Cleanup files
                    foreach (var fileName in siteReportFileNames)
                    {
                        if (File.Exists(fileName))
                            File.Delete(fileName);
                    }

                    if (File.Exists(reportFileName))
                        File.Delete(reportFileName);

                    if (File.Exists(summaryFileName))
                        File.Delete(summaryFileName);
                }

                statusLog.AppendFormat("Schedule {0} - Completed. ", schedule.Id);
            }
            catch (Exception ex)
            {
                statusLog.AppendFormat("Schedule {0} - Exception - {1}", schedule.Id, ex.Message);
            }

            return statusLog.ToString();
        }

        public async Task<string> ProcessTimeSheetSchedule(KpiSendTimesheetSchedules schedule, DateTime reportStartDate, bool ignoreRecipients, bool upload)
        {
            var statusLog = new StringBuilder();
            try
            {
                statusLog.AppendFormat("Schedule {0} - Starting. ", schedule.Id);
                var siteIds = schedule.KpiSendTimesheetClientSites.Select(z => z.ClientSiteId).ToArray();
                var reportEndDate = reportStartDate.AddMonths(1).AddDays(-1);

                var siteReportFileNames = new List<string>();
                
                    string StartDate = reportStartDate.ToString("MM/dd/yyyy");
                    string EndDate = "";
                    if (reportEndDate != null)
                    {
                        EndDate = reportEndDate.ToString("MM/dd/yyyy");
                    }
                    // Create Pdf Report
                    var fileName = "";
                    var clientSiteDetails = _clientDataProvider.GetGuardDetailsAllTimesheetList(siteIds, reportStartDate, reportEndDate);
                if (clientSiteDetails != null)
                {
                    int[] guardIds = clientSiteDetails.Select(x => x.GuardId).ToArray();
                    fileName = _kpiTimesheetReportGenerator.GeneratePdfTimesheetReportList(reportStartDate, reportEndDate, guardIds);


                }

                else
                {
                        int GuardID = 0;
                        fileName = _kpiTimesheetReportGenerator.GeneratePdfTimesheetReport(reportStartDate, reportEndDate, GuardID);
                    }
                  
                    if (string.IsNullOrEmpty(fileName))
                    {
                        statusLog.AppendFormat("Site {0} - Error creating pdf. ", siteIds);
                        //continue;
                    }

                    siteReportFileNames.Add(Path.Combine(_webHostEnvironment.WebRootPath, "Pdf", "Output", fileName));
                    statusLog.AppendFormat("Site {0} - Completed. ", siteIds);
                


                if (siteReportFileNames.Any())
                {
                    schedule.ProjectName = GetSchduleIdentifierTimesheet(schedule);

                    // Create summary page
                    var summaryFileName = CreateSummaryReportTimesheet(schedule, reportStartDate, reportEndDate);

                    // Combine reports to a single pdf                    
                    var reportFileName = $"{FileNameHelper.GetSanitizedFileNamePart(schedule.ProjectName)} - Daily TimeSheet Reports - {reportStartDate:MMM} {reportStartDate.Year}.pdf";
                    reportFileName = Path.Combine(_webHostEnvironment.WebRootPath, "Pdf", "Output", reportFileName);
                    PdfHelper.CombinePdfReportsTimesheet(reportFileName, siteReportFileNames, summaryFileName);

                    // Send Email
                    SendEmailTimesheet(reportFileName, schedule, reportStartDate, ignoreRecipients);

                    if (upload)
                    {
                        schedule.NextRunOn = KpiTimesheetScheduleRunOnCalculator.GetNextRunOn(schedule);
                        _kpiSchedulesDataProvider.SaveTimesheetSchedule(schedule);

                        if (!_webHostEnvironment.IsDevelopment())
                            UploadReportTime(reportFileName, schedule, reportStartDate);
                    }

                    // Cleanup files
                    foreach (var fileName1 in siteReportFileNames)
                    {
                        if (File.Exists(fileName1))
                            File.Delete(fileName1);
                    }

                    if (File.Exists(reportFileName))
                        File.Delete(reportFileName);

                    if (File.Exists(summaryFileName))
                        File.Delete(summaryFileName);
                }

                statusLog.AppendFormat("Schedule {0} - Completed. ", schedule.Id);
            }
            catch (Exception ex)
            {
                statusLog.AppendFormat("Schedule {0} - Exception - {1}", schedule.Id, ex.Message);
            }

            return statusLog.ToString();
        }

        private bool UploadReport(string reportFileName, KpiSendSchedule schedule, DateTime reportDate)
        {
            var clientSiteIds = schedule.KpiSendScheduleClientSites.Select(z => z.ClientSiteId).ToArray();
            var ClientSiteKpiSettings  = _clientDataProvider.GetClientSiteKpiSetting(clientSiteIds);

            var success = false;
            foreach (var settings in ClientSiteKpiSettings)
            {
                //This is active only upload new folder 27112024
                if (settings.DropboxScheduleisActive)
                {
                    if (settings != null && !string.IsNullOrEmpty(settings.DropboxImagesDir))
                    {
                        try
                        {
                            var dbxFilePath = $"{settings.DropboxImagesDir}/FLIR - Wand Recordings - IRs - Daily Logs/{reportDate.Date.Year}/{reportDate.Date:yyyyMM} - {reportDate.Date.ToString("MMMM").ToUpper()} DATA/x - Site KPI Telematics & Statistics/" + Path.GetFileName(reportFileName);
                            success = Task.Run(() => UploadDailyLogToDropbox(reportFileName, dbxFilePath)).Result;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"KPI Report Upload | Failed | Schedule Id: {schedule.Id} Client Site Id: {settings.ClientSiteId}. Error: {ex.Message}");
                            _logger.LogError(ex.StackTrace);
                        }
                    }
                }
            }

            return success;
        }
        private bool UploadReportTime(string reportFileName, KpiSendTimesheetSchedules schedule, DateTime reportDate)
        {
            var clientSiteIds = schedule.KpiSendTimesheetClientSites.Select(z => z.ClientSiteId).ToArray();
            var ClientSiteKpiSettings = _clientDataProvider.GetClientSiteKpiSetting(clientSiteIds);

            var success = false;
            foreach (var settings in ClientSiteKpiSettings)
            {
                if (settings.DropboxScheduleisActive)
                {
                    if (settings != null && !string.IsNullOrEmpty(settings.DropboxImagesDir))
                    {
                        try
                        {
                            var dbxFilePath = $"{settings.DropboxImagesDir}/FLIR - Wand Recordings - IRs - Daily Logs/{reportDate.Date.Year}/{reportDate.Date:yyyyMM} - {reportDate.Date.ToString("MMMM").ToUpper()} DATA/x - Site KPI Telematics & Statistics/" + Path.GetFileName(reportFileName);
                            success = Task.Run(() => UploadDailyLogToDropbox(reportFileName, dbxFilePath)).Result;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"KPI Report Upload | Failed | Schedule Id: {schedule.Id} Client Site Id: {settings.ClientSiteId}. Error: {ex.Message}");
                            _logger.LogError(ex.StackTrace);
                        }
                    }

                }
            }

            return success;
        }
        private async Task<bool> UploadDailyLogToDropbox(string fileToUpload, string dbxFilePath)
        {
            using var dbxTeam = new DropboxTeamClient(_settings.DropboxAccessToken, _settings.DropboxRefreshToken, _settings.DropboxAppKey, _settings.DropboxAppSecret, new DropboxClientConfig());
            var team = dbxTeam.Team.MembersListAsync().Result;
            if (team.Members.Count > 0)
            {
                var cwsMember = team.Members.SingleOrDefault(z => z.Profile.Email == _settings.DropboxUserEmail);
                if (cwsMember != null)
                {
                    var dbx = dbxTeam.AsMember(cwsMember.Profile.TeamMemberId);
                    var account = await dbx.Users.GetCurrentAccountAsync();
                    var nsId = new PathRoot.NamespaceId(account.RootInfo.RootNamespaceId);

                    await ChunkUpload(dbx, nsId, fileToUpload, dbxFilePath);
                }
            }
            return true;
        }

        private async Task<bool> ChunkUpload(DropboxClient client, PathRoot.NamespaceId nsId, string srcFilePath, string destFilePath)
        {
            int chunkSize = 128 * 1024;       // Chunk size is 128KB.
            if (new FileInfo(srcFilePath).Length < chunkSize)
                chunkSize = 16 * 1024;

            using var stream = new MemoryStream(File.ReadAllBytes(srcFilePath));
            int numChunks = (int)Math.Ceiling((double)stream.Length / chunkSize);

            byte[] buffer = new byte[chunkSize];
            string sessionId = null;
            try
            {
                for (var index = 0; index < numChunks; index++)
                {
                    var byteRead = stream.Read(buffer, 0, chunkSize);

                    using var memStream = new MemoryStream(buffer, 0, byteRead);
                    if (index == 0)
                    {
                        var result = await client.WithPathRoot(nsId).Files.UploadSessionStartAsync(body: memStream);
                        sessionId = result.SessionId;
                    }
                    else
                    {
                        var cursor = new UploadSessionCursor(sessionId, (ulong)(chunkSize * index));

                        if (index == numChunks - 1)
                            await client.WithPathRoot(nsId).Files.UploadSessionFinishAsync(cursor, new CommitInfo(destFilePath, mode: WriteMode.Overwrite.Instance), body: memStream);
                        else
                            await client.WithPathRoot(nsId).Files.UploadSessionAppendV2Async(cursor, body: memStream);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"KPI Report Upload | Failed | Src File: {srcFilePath} Dest File: {destFilePath}. Error: {ex.Message}");
                throw;
            }

            return true;
        }

        private void SendEmail(string fileName, KpiSendSchedule schedule, DateTime reportDate, bool ignoreRecipients)
        {
            var fromAddress = _emailOptions.FromAddress.Split('|');
            //To get the Default Email start Old Code
            //var ToAddreddAppset = _emailOptions.ToAddress.Split('|');
            //var toAddressData = _clientDataProvider.GetDefaultEmailAddress() + '|'+ ToAddreddAppset[1];
            //var toAddress = toAddressData.Split('|');
            //var ToAddressFirststr = _clientDataProvider.GetDefaultEmailAddress();
            //if (ToAddressFirststr==null)
            //{
            //    toAddress = _emailOptions.ToAddress.Split('|');
            //}

            //To get the Default Email stop end Old Code

           

            var subject = _emailOptions.Subject;
            var messageHtml = _emailOptions.Message;
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromAddress[1], fromAddress[0]));
            /*Default to adresss for kpi Schedule Start*/
            var Emails = _clientDataProvider.GetKPIScheduleDeafultMailbox().ToList();
            var emailAddresses = string.Join(",", Emails.Select(email => email.KPIMail));
            if (emailAddresses != null && emailAddresses != "")
            {
                var toAddressNew = emailAddresses.Split(',');
                foreach (var address in GetToEmailAddressList(toAddressNew))
                    message.To.Add(address);
            }
            /*Default to adresss for kpi Schedule end*/
            /* Mail Id added Bcc globoconsoftware for checking KPI Mail not getting Issue Start(date 17,01,2024) */

            message.Bcc.Add(new MailboxAddress("globoconsoftware", "globoconsoftware@gmail.com"));
           // message.Bcc.Add(new MailboxAddress("globoconsoftware2", "jishakallani@gmail.com"));
            /* Mail Id added Bcc globoconsoftware end */
            if (!ignoreRecipients)
            {
                if (!string.IsNullOrEmpty(schedule.EmailTo))
                {
                    foreach (var email in schedule.EmailTo.Split(","))
                    {
                        if (CommonHelper.IsValidEmail(email))
                            message.Cc.Add(new MailboxAddress(string.Empty, email.Trim()));
                    }
                }

                if (!string.IsNullOrEmpty(schedule.EmailBcc))
                {
                    foreach (var email in schedule.EmailBcc.Split(","))
                    {
                        if (CommonHelper.IsValidEmail(email))
                            message.Bcc.Add(new MailboxAddress(string.Empty, email.Trim()));
                    }
                }
            }
            message.Subject = $"{subject} - {schedule.ProjectName} - {reportDate:MMM yyyy}";

            var builder = new BodyBuilder()
            {
                HtmlBody = messageHtml
            };
            builder.Attachments.Add(fileName);
            message.Body = builder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                client.Connect(_emailOptions.SmtpServer, _emailOptions.SmtpPort, MailKit.Security.SecureSocketOptions.None);
                if (!string.IsNullOrEmpty(_emailOptions.SmtpUserName) &&
                    !string.IsNullOrEmpty(_emailOptions.SmtpPassword))
                    client.Authenticate(_emailOptions.SmtpUserName, _emailOptions.SmtpPassword);
                client.Send(message);
                client.Disconnect(true);
            }
        }

        private void SendEmailTimesheet(string fileName, KpiSendTimesheetSchedules schedule, DateTime reportDate, bool ignoreRecipients)
        {
            var fromAddress = _emailOptions.FromAddress.Split('|');


            var subject = "Monthly TimeSheet Report";
            var messageHtml = "Dear Citywatch Security Client; <br><br>Please find attached Timesheet Records.</a>";
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromAddress[1], fromAddress[0]));
            /*Default to adresss for kpi Schedule Start*/
            //var Emails = _clientDataProvider.GetKPIScheduleDeafultMailbox().ToList();
            //var emailAddresses = string.Join(",", Emails.Select(email => email.TimesheetsMail));
            //if (emailAddresses != null && emailAddresses != "")
            //{
            //    var toAddressNew = emailAddresses.Split(',');
            //    foreach (var address in GetToEmailAddressList(toAddressNew))
            //        message.From.Add(address);
            //}
            /*Default to adresss for kpi Schedule end*/
            /* Mail Id added Bcc globoconsoftware for checking KPI Mail not getting Issue Start(date 17,01,2024) */

            message.Bcc.Add(new MailboxAddress("globoconsoftware", "globoconsoftware@gmail.com"));
            // message.Bcc.Add(new MailboxAddress("globoconsoftware2", "jishakallani@gmail.com"));
            /* Mail Id added Bcc globoconsoftware end */
            if (!ignoreRecipients)
            {
                if (!string.IsNullOrEmpty(schedule.EmailTo))
                {
                    foreach (var email in schedule.EmailTo.Split(","))
                    {
                        if (CommonHelper.IsValidEmail(email))
                            message.Cc.Add(new MailboxAddress(string.Empty, email.Trim()));
                    }
                }

                if (!string.IsNullOrEmpty(schedule.EmailBcc))
                {
                    foreach (var email in schedule.EmailBcc.Split(","))
                    {
                        if (CommonHelper.IsValidEmail(email))
                            message.Bcc.Add(new MailboxAddress(string.Empty, email.Trim()));
                    }
                }
            }
            message.Subject = $"{subject} - {schedule.ProjectName} - {reportDate:MMM yyyy}";

            var builder = new BodyBuilder()
            {
                HtmlBody = messageHtml
            };
            builder.Attachments.Add(fileName);
            message.Body = builder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                client.Connect(_emailOptions.SmtpServer, _emailOptions.SmtpPort, MailKit.Security.SecureSocketOptions.None);
                if (!string.IsNullOrEmpty(_emailOptions.SmtpUserName) &&
                    !string.IsNullOrEmpty(_emailOptions.SmtpPassword))
                    client.Authenticate(_emailOptions.SmtpUserName, _emailOptions.SmtpPassword);
                client.Send(message);
                client.Disconnect(true);
            }
        }
        private List<MailboxAddress> GetToEmailAddressList(string[] toAddress)
        {
            var emailAddressList = new List<MailboxAddress>();
            foreach (var item in toAddress)
            {
                emailAddressList.Add(new MailboxAddress(string.Empty, item));
            }
            return emailAddressList;
        }

        private string GetSchduleIdentifier(KpiSendSchedule schedule)
        {
            if (!string.IsNullOrEmpty(schedule.ProjectName))
                return schedule.ProjectName;

            if (schedule.KpiSendScheduleClientSites.Count == 1)
                return schedule.KpiSendScheduleClientSites.Single().ClientSite.Name;

            return string.Join(", ", schedule.KpiSendScheduleClientSites.Select(z => z.ClientSite.ClientType.Name).Distinct());
        }

        private string CreateSummaryReport(KpiSendSchedule schedule, DateTime reportStartDate, DateTime reportEndDate)
        {
            var coverSheetType = schedule.CoverSheetType;
            if (reportStartDate.Month != DateTime.Today.Month)
                coverSheetType = CoverSheetType.Monthly;

            ISummaryReportGenerator summaryReportGenerator = coverSheetType == CoverSheetType.Weekly ?
                new WeeklySummaryReportGenerator(_webHostEnvironment, _viewDataService, _patrolDataReportService) :
                new MonthlySummaryReportGenerator(_webHostEnvironment, _viewDataService, _patrolDataReportService);
            var summaryReportFromDate = coverSheetType == CoverSheetType.Weekly ? DateTime.Today.AddDays(-6) : reportStartDate;
            var summaryReportToDate = coverSheetType == CoverSheetType.Weekly ? DateTime.Today : reportEndDate;
            var summaryFileName = summaryReportGenerator.GeneratePdfReport(schedule, summaryReportFromDate, summaryReportToDate);
            summaryFileName = Path.Combine(_webHostEnvironment.WebRootPath, "Pdf", "Output", summaryFileName);
            return summaryFileName;
        }
        private string GetSchduleIdentifierTimesheet(KpiSendTimesheetSchedules schedule)
        {
            if (!string.IsNullOrEmpty(schedule.ProjectName))
                return schedule.ProjectName;

            if (schedule.KpiSendTimesheetClientSites.Count == 1)
                return schedule.KpiSendTimesheetClientSites.Single().ClientSite.Name;

            return string.Join(", ", schedule.KpiSendTimesheetClientSites.Select(z => z.ClientSite.ClientType.Name).Distinct());
        }
        private string CreateSummaryReportTimesheetNew(KpiSendTimesheetSchedules schedule, DateTime reportStartDate, DateTime reportEndDate,string FileName)
        {
            var summaryFileName = FileName;
            summaryFileName = Path.Combine(_webHostEnvironment.WebRootPath, "Pdf", "Output", summaryFileName);
            return summaryFileName;
        }
            private string CreateSummaryReportTimesheet(KpiSendTimesheetSchedules schedule, DateTime reportStartDate, DateTime reportEndDate)
        {

            var coverSheetType = CoverSheetType.Monthly;
            if (reportStartDate.Month != DateTime.Today.Month)
                coverSheetType = CoverSheetType.Monthly;

            ISummaryReportGenerator summaryReportGenerator = coverSheetType == CoverSheetType.Weekly ?
                new WeeklySummaryReportGenerator(_webHostEnvironment, _viewDataService, _patrolDataReportService) :
                new MonthlySummaryReportGenerator(_webHostEnvironment, _viewDataService, _patrolDataReportService);
            var summaryReportFromDate = coverSheetType == CoverSheetType.Weekly ? DateTime.Today.AddDays(-6) : reportStartDate;
            var summaryReportToDate = coverSheetType == CoverSheetType.Weekly ? DateTime.Today : reportEndDate;
            string StartDate = reportStartDate.ToString("MM/dd/yyyy");
            string EndDate = reportEndDate.ToString("MM/dd/yyyy");
            int[] ClientsiteIds = schedule.KpiSendTimesheetClientSites.Select(x => x.ClientSiteId).ToArray();

            var clientSiteDetails = _clientDataProvider.GetGuardDetailsAllTimesheetList(ClientsiteIds, reportStartDate, reportEndDate);
            var summaryFileName = "";
            if (clientSiteDetails != null)
            {
                int[] guardIds = clientSiteDetails.Select(x => x.GuardId).ToArray();
                summaryFileName = _kpiTimesheetReportGenerator.GeneratePdfTimesheetReportList(reportStartDate, reportEndDate, guardIds);


            }
            else
            {
                var GuardId = 0;
                 summaryFileName = _kpiTimesheetReportGenerator.GeneratePdfTimesheetReport(reportStartDate, reportEndDate, GuardId);
                //fileName = $"{DateTime.Now.ToString("yyyyMMdd")} - Time Sheet -_{new Random().Next()}.pdf";
            }

           
            summaryFileName = Path.Combine(_webHostEnvironment.WebRootPath, "Pdf", "Output", summaryFileName);
            return summaryFileName;
        }
        public byte[] ProcessDownload(KpiSendSchedule schedule, DateTime reportStartDate, bool ignoreRecipients, bool upload)
        {
            var statusLog = new StringBuilder();
            byte[] fileBytes = null;
            try
            {
                statusLog.AppendFormat("Schedule {0} - Starting. ", schedule.Id);
                var siteIds = schedule.KpiSendScheduleClientSites.Select(z => z.ClientSiteId);
                var reportEndDate = reportStartDate.AddMonths(1).AddDays(-1);

                var siteReportFileNames = new List<string>();
                foreach (var siteId in siteIds)
                {
                    // Import Job
                    var serviceLog = new KpiDataImportJob()
                    {
                        ClientSiteId = siteId,
                        ReportDate = reportStartDate,
                        CreatedDate = DateTime.Now,
                    };
                    var jobId = _importJobDataProvider.SaveKpiDataImportJob(serviceLog);
                    //await _importDataService.Run(jobId);

                    // Create Pdf Report
                    var IsDownselect = schedule.IsCriticalDocumentDownselect;
                    int CriticalDocumentID = Convert.ToInt32(schedule.CriticalGroupNameID);
                    var fileName = _kpiReportGenerator.GeneratePdfReport(siteId, reportStartDate, reportEndDate, schedule.IsHrTimerPaused, IsDownselect, CriticalDocumentID);
                    if (string.IsNullOrEmpty(fileName))
                    {
                        statusLog.AppendFormat("Site {0} - Error creating pdf. ", siteId);
                        continue;
                    }

                    siteReportFileNames.Add(Path.Combine(_webHostEnvironment.WebRootPath, "Pdf", "Output", fileName));
                    statusLog.AppendFormat("Site {0} - Completed. ", siteId);
                }

                if (siteReportFileNames.Any())
                {
                    schedule.ProjectName = GetSchduleIdentifier(schedule);

                    // Create summary page
                    var summaryFileName = CreateSummaryReport(schedule, reportStartDate, reportEndDate);

                    // Combine reports to a single pdf                    
                    var reportFileName = $"{FileNameHelper.GetSanitizedFileNamePart(schedule.ProjectName)} - Daily KPI Reports - {reportStartDate:MMM} {reportStartDate.Year}.pdf";
                    reportFileName = Path.Combine(_webHostEnvironment.WebRootPath, "Pdf", "Output", reportFileName);
                    PdfHelper.CombinePdfReports(reportFileName, siteReportFileNames, summaryFileName);


                    // Send Email
                    //SendEmail(reportFileName, schedule, reportStartDate, ignoreRecipients);



                    if (upload)
                    {
                        schedule.NextRunOn = KpiSendScheduleRunOnCalculator.GetNextRunOn(schedule);
                        _kpiSchedulesDataProvider.SaveSendSchedule(schedule);

                        if (!_webHostEnvironment.IsDevelopment())
                            UploadReport(reportFileName, schedule, reportStartDate);
                    }

                    fileBytes = System.IO.File.ReadAllBytes(reportFileName);
                    //// Cleanup files
                    foreach (var fileName in siteReportFileNames)
                    {
                        if (File.Exists(fileName))
                            File.Delete(fileName);
                    }

                    if (File.Exists(reportFileName))
                        File.Delete(reportFileName);

                    if (File.Exists(summaryFileName))
                        File.Delete(summaryFileName);
                }

                statusLog.AppendFormat("Schedule {0} - Completed. ", schedule.Id);
            }
            catch (Exception ex)
            {
                statusLog.AppendFormat("Schedule {0} - Exception - {1}", schedule.Id, ex.Message);
            }

            return fileBytes;
        }

        public byte[] ProcessDownloadTimeSheet(KpiSendTimesheetSchedules schedule, DateTime reportStartDate, bool ignoreRecipients, bool upload)
        {
            var statusLog = new StringBuilder();
            byte[] fileBytes = null;
            try
            {
                statusLog.AppendFormat("Schedule {0} - Starting. ", schedule.Id);
                int[] siteIds = schedule.KpiSendTimesheetClientSites.Select(z => z.ClientSiteId).ToArray();
                var reportEndDate = reportStartDate.AddMonths(1).AddDays(-1);

                var siteReportFileNames = new List<string>();
               
                    string StartDate = reportStartDate.ToString("MM/dd/yyyy");
                    string EndDate = "";
                    if (reportEndDate!=null)
                    {
                         EndDate = reportEndDate.ToString("MM/dd/yyyy");
                    }
                var fileName = "";
                // Create Pdf Report
                //DateTime reportEndDate = DateTime.ParseExact("your_input_here", "MM/dd/yyyy", CultureInfo.InvariantCulture);
                var clientSiteDetails = _clientDataProvider.GetGuardDetailsAllTimesheetList(siteIds, reportStartDate, reportEndDate);
                    
                    if (clientSiteDetails != null)
                    {
                        int[] guardIds = clientSiteDetails.Select(x => x.GuardId).ToArray();
                            fileName = _kpiTimesheetReportGenerator.GeneratePdfTimesheetReportList(reportStartDate, reportEndDate, guardIds);
                        

                    }
                    else
                    {
                        var GuardId = 0;
                        fileName= _kpiTimesheetReportGenerator.GeneratePdfTimesheetReport(reportStartDate, reportEndDate, GuardId);
                        //fileName = $"{DateTime.Now.ToString("yyyyMMdd")} - Time Sheet -_{new Random().Next()}.pdf";
                    }
                    
                    if (string.IsNullOrEmpty(fileName))
                    {
                        statusLog.AppendFormat("Site {0} - Error creating pdf. ", siteIds);
                        //continue;
                    }

                    siteReportFileNames.Add(Path.Combine(_webHostEnvironment.WebRootPath, "Pdf", "Output", fileName));
                    statusLog.AppendFormat("Site {0} - Completed. ", siteIds);
                

                if (siteReportFileNames.Any())
                {
                    schedule.ProjectName = GetSchduleIdentifierTimesheet(schedule);

                    // Create summary page
                    var summaryFileName = CreateSummaryReportTimesheetNew(schedule, reportStartDate, reportEndDate, fileName);

                    // Combine reports to a single pdf                    
                    var reportFileName = $"{FileNameHelper.GetSanitizedFileNamePart(schedule.ProjectName)} - Daily TimeSheet Reports - {reportStartDate:MMM} {reportStartDate.Year}.pdf";
                    reportFileName = Path.Combine(_webHostEnvironment.WebRootPath, "Pdf", "Output", reportFileName);
                    PdfHelper.CombinePdfReportsTimesheet(reportFileName, siteReportFileNames, summaryFileName);


                   


                    if (upload)
                    {
                        schedule.NextRunOn = KpiTimesheetScheduleRunOnCalculator.GetNextRunOn(schedule);
                       // _kpiSchedulesDataProvider.SaveSendSchedule(schedule);

                        if (!_webHostEnvironment.IsDevelopment())
                            UploadReportTime(reportFileName, schedule, reportStartDate);
                    }

                    fileBytes = System.IO.File.ReadAllBytes(reportFileName);
                    //// Cleanup files
                    foreach (var fileName1 in siteReportFileNames)
                    {
                        if (File.Exists(fileName1))
                            File.Delete(fileName1);
                    }

                    if (File.Exists(reportFileName))
                        File.Delete(reportFileName);

                    if (File.Exists(summaryFileName))
                        File.Delete(summaryFileName);
                }

                statusLog.AppendFormat("Schedule {0} - Completed. ", schedule.Id);
            }
            catch (Exception ex)
            {
                statusLog.AppendFormat("Schedule {0} - Exception - {1}", schedule.Id, ex.Message);
            }

            return fileBytes;
        }

    }
}
