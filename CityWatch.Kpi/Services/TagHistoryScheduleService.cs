using CityWatch.Common.Helpers;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;

namespace CityWatch.Kpi.Services
{
    public class TagHistoryScheduleRunResult
    {
        public bool Success { get; set; }

        public int RecordCount { get; set; }

        public bool EmailSent { get; set; }

        public string Message { get; set; }

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }
    }

    public interface ITagHistoryScheduleService
    {
        /// <summary>
        /// Runs one weekly Tag History schedule: gathers every site's tag changes for the seven
        /// days before <paramref name="runDate"/>, emails them as an HTML report and records the
        /// outcome in KpiSendScheduleJobsTagHistory. A week without changes is recorded and no
        /// email is sent.
        /// </summary>
        TagHistoryScheduleRunResult ProcessSchedule(KpiSendTagHistorySchedules schedule, DateTime runDate, bool isAutoRunFromScheduler);

        /// <summary>The email body for the given rows - also what a manual preview shows.</summary>
        string BuildEmailHtml(List<TagHistoryReportViewModel> rows, DateTime fromDate, DateTime toDate, string projectName);
    }

    public class TagHistoryScheduleService : ITagHistoryScheduleService
    {
        private const string MonitoringBcc = "globoconsoftware@gmail.com";

        private readonly IClientSiteWandDataProvider _clientSiteWandDataProvider;
        private readonly IKpiSchedulesDataProvider _kpiSchedulesDataProvider;
        private readonly EmailOptions _emailOptions;
        private readonly ILogger<TagHistoryScheduleService> _logger;

        public TagHistoryScheduleService(IClientSiteWandDataProvider clientSiteWandDataProvider,
            IKpiSchedulesDataProvider kpiSchedulesDataProvider,
            IOptions<EmailOptions> emailOptions,
            ILogger<TagHistoryScheduleService> logger)
        {
            _clientSiteWandDataProvider = clientSiteWandDataProvider;
            _kpiSchedulesDataProvider = kpiSchedulesDataProvider;
            _emailOptions = emailOptions.Value;
            _logger = logger;
        }

        public TagHistoryScheduleRunResult ProcessSchedule(KpiSendTagHistorySchedules schedule, DateTime runDate, bool isAutoRunFromScheduler)
        {
            var (fromDate, toDate) = KpiTagHistoryScheduleRunOnCalculator.GetReportPeriod(runDate);
            var result = new TagHistoryScheduleRunResult { FromDate = fromDate, ToDate = toDate };
            var statusLog = new StringBuilder();

            var job = new KpiSendScheduleJobsTagHistory
            {
                ScheduleId = schedule.Id,
                CreatedDate = DateTime.Now,
                RunType = isAutoRunFromScheduler ? "Auto" : "Manual",
                ReportFromDate = fromDate,
                ReportToDate = toDate
            };
            job.Id = _kpiSchedulesDataProvider.SaveSendScheduleJobTagHistory(job);

            statusLog.AppendLine($"Tag History schedule {schedule.Id} - Starting. Run Type: {job.RunType}. Period {fromDate:dd-MMM-yyyy} to {toDate:dd-MMM-yyyy}.");

            try
            {
                // No site filter: every site, including any created since the schedule was saved.
                var rows = _clientSiteWandDataProvider.GetTagHistoryReport(new TagHistoryReportRequest
                {
                    LogFromDate = fromDate,
                    LogToDate = toDate,
                    SortBy = TagHistorySortBy.DateTime
                });

                job.RecordCount = result.RecordCount = rows.Count;
                statusLog.AppendLine($"{rows.Count} tag change(s) found across {rows.Select(x => x.ClientSiteId).Distinct().Count()} site(s).");

                if (rows.Count == 0)
                {
                    job.ReportGenerated = false;
                    job.ReportStatus = "No tag changes for the week - report not generated.";
                    job.EmailSent = false;
                    job.EmailStatus = "Email not sent - no data for the week.";
                    statusLog.AppendLine(job.EmailStatus);
                    result.Success = true;
                }
                else
                {
                    var projectName = string.IsNullOrWhiteSpace(schedule.ProjectName) ? "All Client Sites" : schedule.ProjectName;
                    var html = BuildEmailHtml(rows, fromDate, toDate, projectName);
                    job.ReportGenerated = true;
                    job.ReportStatus = $"Report generated with {rows.Count} record(s).";
                    statusLog.AppendLine(job.ReportStatus);

                    try
                    {
                        var subject = $"Tag History Report - {projectName} - {fromDate:dd MMM yyyy} to {toDate:dd MMM yyyy}";
                        var recipients = SendEmail(subject, html, schedule);
                        job.EmailSent = true;
                        job.EmailTo = recipients;
                        job.EmailStatus = $"Email sent to {recipients}.";
                        result.Success = true;
                    }
                    catch (Exception ex)
                    {
                        job.EmailSent = false;
                        job.EmailStatus = $"Email failed - {ex.Message}";
                        result.Success = false;
                        _logger.LogError(ex, "Tag History schedule {ScheduleId}: email failed.", schedule.Id);
                    }
                    statusLog.AppendLine(job.EmailStatus);
                }

                if (isAutoRunFromScheduler)
                {
                    schedule.NextRunOn = KpiTagHistoryScheduleRunOnCalculator.GetNextRunOn(schedule);
                    _kpiSchedulesDataProvider.SaveTagHistorySchedule(schedule);
                    statusLog.AppendLine($"Next run on {schedule.NextRunOn:dd-MMM-yyyy HH:mm}.");
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                job.ReportStatus ??= $"Report failed - {ex.Message}";
                statusLog.AppendLine($"Tag History schedule {schedule.Id} - Exception - {ex}");
                _logger.LogError(ex, "Tag History schedule {ScheduleId} failed.", schedule.Id);
            }

            statusLog.AppendLine($"Tag History schedule {schedule.Id} - Completed. Status - {result.Success}.");

            job.Success = result.Success;
            job.CompletedDate = DateTime.Now;
            job.StatusMessage = statusLog.ToString();
            _kpiSchedulesDataProvider.SaveSendScheduleJobTagHistory(job);

            result.EmailSent = job.EmailSent;
            result.Message = job.EmailStatus ?? job.ReportStatus;
            return result;
        }

        /// <returns>The addresses the message actually went to.</returns>
        private string SendEmail(string subject, string html, KpiSendTagHistorySchedules schedule)
        {
            var fromAddress = _emailOptions.FromAddress.Split('|');
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromAddress.Length > 1 ? fromAddress[1] : string.Empty, fromAddress[0]));

            AddAddresses(message.To, schedule.EmailTo);
            if (message.To.Count == 0)
                throw new InvalidOperationException("No valid email recipients on the schedule.");

            AddAddresses(message.Bcc, schedule.EmailBcc);
            message.Bcc.Add(new MailboxAddress("globoconsoftware", MonitoringBcc));

            message.Subject = subject;

            // Same safety valve as every other Kpi email: no-op unless Email:TestModeRedirectTo is set.
            var redirectTo = SendScheduleService.GetTestModeRedirectAddresses(_emailOptions);
            if (redirectTo.Count > 0)
            {
                message.To.Clear();
                message.Cc.Clear();
                message.Bcc.Clear();
                foreach (var address in redirectTo)
                    message.To.Add(new MailboxAddress(string.Empty, address));
                message.Subject = "[TEST] " + message.Subject;
                _logger.LogWarning("Email TEST MODE is active: '{Subject}' redirected to {Redirect}.", message.Subject, string.Join(", ", redirectTo));
            }

            message.Body = new BodyBuilder { HtmlBody = html }.ToMessageBody();

            using (var client = new SmtpClient())
            {
                client.Connect(_emailOptions.SmtpServer, _emailOptions.SmtpPort, MailKit.Security.SecureSocketOptions.None);
                if (!string.IsNullOrEmpty(_emailOptions.SmtpUserName) &&
                    !string.IsNullOrEmpty(_emailOptions.SmtpPassword))
                    client.Authenticate(_emailOptions.SmtpUserName, _emailOptions.SmtpPassword);
                client.Send(message);
                client.Disconnect(true);
            }

            return string.Join(", ", message.To.Mailboxes.Select(x => x.Address));
        }

        private static void AddAddresses(InternetAddressList list, string addresses)
        {
            if (string.IsNullOrWhiteSpace(addresses))
                return;

            foreach (var email in addresses.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (CommonHelper.IsValidEmail(email.Trim()))
                    list.Add(new MailboxAddress(string.Empty, email.Trim()));
            }
        }

        public string BuildEmailHtml(List<TagHistoryReportViewModel> rows, DateTime fromDate, DateTime toDate, string projectName)
        {
            static string E(string value) => WebUtility.HtmlEncode(value ?? string.Empty);

            var sites = rows.GroupBy(x => x.SiteName ?? "(Unknown site)")
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var actionCounts = rows.GroupBy(x => x.Action ?? "Other")
                .OrderByDescending(g => g.Count())
                .Select(g => (Action: g.Key, Count: g.Count()))
                .ToList();

            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();

            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>Tag History Report</title></head>");
            sb.Append("<body style=\"margin:0;padding:0;background:#f2f4f7;font-family:Segoe UI,Arial,Helvetica,sans-serif;color:#1f2933;\">");
            sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#f2f4f7;padding:24px 0;\"><tr><td align=\"center\">");
            sb.Append("<table role=\"presentation\" width=\"960\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:960px;width:100%;background:#ffffff;border-radius:6px;overflow:hidden;border:1px solid #dde3ea;\">");

            // Header
            sb.Append("<tr><td style=\"background:#0b3d63;padding:20px 28px;\">");
            sb.Append("<div style=\"font-size:12px;letter-spacing:1.5px;text-transform:uppercase;color:#9fc6e6;\">CityWatch Security &middot; SmartWAND</div>");
            sb.Append("<div style=\"font-size:22px;font-weight:600;color:#ffffff;margin-top:4px;\">Weekly Tag History Report</div>");
            sb.Append($"<div style=\"font-size:14px;color:#d6e6f3;margin-top:6px;\">{E(projectName)} &nbsp;|&nbsp; {fromDate:dd MMM yyyy} &ndash; {toDate:dd MMM yyyy}</div>");
            sb.Append("</td></tr>");

            // Intro + summary tiles
            sb.Append("<tr><td style=\"padding:22px 28px 6px 28px;font-size:14px;line-height:20px;\">");
            sb.Append("Dear CityWatch Security Client,<br><br>Below is a summary of every change made to SmartWAND tags across all client sites during the week. ");
            sb.Append("Each entry shows what changed, which tag it was, and who made the change.");
            sb.Append("</td></tr>");

            sb.Append("<tr><td style=\"padding:12px 28px;\"><table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\"><tr>");
            AppendTile(sb, rows.Count.ToString(), "Tag changes");
            AppendTile(sb, sites.Count.ToString(), "Client sites");
            AppendTile(sb, rows.Select(x => x.TagUId).Where(x => !string.IsNullOrEmpty(x)).Distinct().Count().ToString(), "Tags affected");
            sb.Append("</tr></table></td></tr>");

            sb.Append("<tr><td style=\"padding:4px 28px 10px 28px;font-size:13px;color:#52606d;\">");
            sb.Append(string.Join(" &nbsp;&middot;&nbsp; ", actionCounts.Select(a => $"{ActionBadge(a.Action)} {a.Count}")));
            sb.Append("</td></tr>");

            // One section per site
            foreach (var site in sites)
            {
                sb.Append("<tr><td style=\"padding:14px 28px 4px 28px;\">");
                sb.Append($"<div style=\"font-size:16px;font-weight:600;color:#0b3d63;border-bottom:2px solid #27c2f5;padding-bottom:4px;\">Site Name: {E(site.Key)} <span style=\"font-size:12px;font-weight:400;color:#7b8794;\">({site.Count()} change{(site.Count() == 1 ? "" : "s")})</span></div>");
                sb.Append("</td></tr>");

                sb.Append("<tr><td style=\"padding:0 28px 8px 28px;\">");
                sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"border-collapse:collapse;font-size:12px;\">");
                sb.Append("<tr style=\"background:#eef6fb;color:#0b3d63;text-align:left;\">");
                foreach (var header in new[] { "Action Date Time", "Tag Action", "Tag UID", "Tag Type", "User", "Guard", "Changed From" })
                    sb.Append($"<th style=\"padding:7px 8px;border-bottom:1px solid #cfdbe5;font-weight:600;white-space:nowrap;\">{header}</th>");
                sb.Append("</tr>");

                var odd = false;
                foreach (var row in site.OrderBy(x => x.ChangedOn).ThenBy(x => x.Id))
                {
                    odd = !odd;
                    var bg = odd ? "#ffffff" : "#f8fafc";
                    sb.Append($"<tr style=\"background:{bg};vertical-align:top;\">");
                    sb.Append($"<td style=\"padding:7px 8px;border-bottom:1px solid #e4e9ee;white-space:nowrap;\">{E(row.ActionDateTimeText)}</td>");
                    sb.Append($"<td style=\"padding:7px 8px;border-bottom:1px solid #e4e9ee;\">{ActionBadge(row.Action)}<div style=\"margin-top:3px;color:#3e4c59;\">{E(row.Note)}</div></td>");
                    sb.Append($"<td style=\"padding:7px 8px;border-bottom:1px solid #e4e9ee;font-family:Consolas,monospace;\">{E(row.TagUId)}</td>");
                    sb.Append($"<td style=\"padding:7px 8px;border-bottom:1px solid #e4e9ee;\">{E(row.TagType)}</td>");
                    sb.Append($"<td style=\"padding:7px 8px;border-bottom:1px solid #e4e9ee;\">{E(row.UserDisplay)}</td>");
                    sb.Append($"<td style=\"padding:7px 8px;border-bottom:1px solid #e4e9ee;\">{E(row.GuardDisplay)}</td>");
                    sb.Append($"<td style=\"padding:7px 8px;border-bottom:1px solid #e4e9ee;white-space:nowrap;\">{E(row.AppType)}</td>");
                    sb.Append("</tr>");
                }
                sb.Append("</table></td></tr>");
            }

            // Footer
            sb.Append("<tr><td style=\"padding:20px 28px 24px 28px;font-size:12px;line-height:18px;color:#7b8794;border-top:1px solid #e4e9ee;\">");
            sb.Append("Any concerns, please contact your relevant CityWatch Security Account Manager, or email ");
            sb.Append("<a href=\"mailto:control@citywatchsecurity.com.au\" style=\"color:#0b6fb3;\">control@citywatchsecurity.com.au</a>.<br>");
            sb.Append($"This report was generated automatically on {DateTime.Now:dd MMM yyyy} at {DateTime.Now:HH:mm} hrs{(string.IsNullOrEmpty(version) ? "" : $" (v{version})")}.");
            sb.Append("</td></tr>");

            sb.Append("</table></td></tr></table></body></html>");
            return sb.ToString();
        }

        private static void AppendTile(StringBuilder sb, string value, string label)
        {
            sb.Append("<td width=\"33%\" style=\"padding:4px;\">");
            sb.Append("<div style=\"background:#f5f9fc;border:1px solid #dbe7f0;border-radius:4px;padding:12px 14px;\">");
            sb.Append($"<div style=\"font-size:22px;font-weight:600;color:#0b3d63;\">{value}</div>");
            sb.Append($"<div style=\"font-size:12px;color:#7b8794;text-transform:uppercase;letter-spacing:0.8px;\">{label}</div>");
            sb.Append("</div></td>");
        }

        private static string ActionBadge(string action)
        {
            var color = action switch
            {
                "Added" => "#1e8e3e",
                "Deleted" => "#c5221f",
                "Restored" => "#b06000",
                _ => "#0b6fb3"
            };
            return $"<span style=\"display:inline-block;padding:2px 8px;border-radius:10px;background:{color};color:#ffffff;font-size:11px;font-weight:600;\">{WebUtility.HtmlEncode(action ?? "")}</span>";
        }
    }
}
