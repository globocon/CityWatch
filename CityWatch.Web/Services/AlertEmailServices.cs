using System;
using CityWatch.Data.Helpers;

using CityWatch.Data.Providers;
using Microsoft.Extensions.Options;
using MailKit.Net.Smtp;
using MimeKit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CityWatch.Data.Models;
using CityWatch.Web.Models;
using Microsoft.Extensions.Logging;

namespace CityWatch.Web.Services
{
    public interface IAlertEmailServices
    {
        Task<bool> SendNewGuardRegisterAlertMail(Guard guard, string LoggedInSite);
        Task<bool> SendShiftCancelledAlertMail(RosterSchedule shift, string guardName, string licenseNo, string siteName, string reason);
        Task<bool> QueueMobileShiftCancellation(RosterSchedule shift, string reason);
        Task<bool> QueueWebShiftCancellation(RosterSchedule shift, string reason, string cancelledByAdminName);
        Task<bool> QueueReliefGuardAssigned(RosterSchedule shift, string assignedByAdminName);
        Task<bool> RemoveFromQueue(RosterSchedule shift);
        Task<bool> SendAggregatedShiftCancelledAlertMail(Guard guard, string licenseNo, string cancelledBy, string source, List<ShiftCancellationEmailQueue> cancellations);
        Task<bool> SendWandStrikeManipulationAlertMail(WandStrikeManipulationAlert alert);
    }
    public class AlertEmailServices : IAlertEmailServices
    {
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        private readonly IConfigDataProvider _configDataProvider;
        private readonly EmailOptions _EmailOptions;
        private readonly CityWatch.Data.CityWatchDbContext _context;
        private readonly ILogger<AlertEmailServices> _logger;
        public AlertEmailServices(IClientDataProvider clientDataProvider,
            IGuardDataProvider guardDataProvider, IGuardLogDataProvider guardLogDataProvider,
            IConfigDataProvider configDataProvider, IOptions<EmailOptions> emailOptions,
            CityWatch.Data.CityWatchDbContext context,
            ILogger<AlertEmailServices> logger = null)
        {
            _logger = logger;
            _clientDataProvider = clientDataProvider;
            _guardDataProvider = guardDataProvider;
            _guardLogDataProvider = guardLogDataProvider;
            _configDataProvider = configDataProvider;
            _EmailOptions = emailOptions.Value;
            _context = context;
        }

        public async Task<bool> SendNewGuardRegisterAlertMail(Guard guard, string LoggedInSite)
        {
            var subject = $"New Guard Registered - {guard.Name}, {guard.Initial}";
            var mailBodyHtml = "<h1>New Guard Registered</h1><p>A new guard has registered with the following details</p>";
            mailBodyHtml += "<p><strong>Name: </strong>" + guard.Name + "</p>";
            mailBodyHtml += "<p><strong>Initial: </strong>" + guard.Initial + "</p>";
            mailBodyHtml += "<p><strong>Security No: </strong>" + guard.SecurityNo + "</p>";
            mailBodyHtml += "<p><strong>State: </strong>" + guard.State + "</p>";
            mailBodyHtml += "<p><strong>Email: </strong>" + guard.Email + "</p>";
            mailBodyHtml += "<p><strong>Mobile: </strong>" + guard.Mobile + "</p>";
            mailBodyHtml += $"<p><strong>Enrolled On: </strong>{(guard.DateEnrolled.HasValue ? guard.DateEnrolled.Value.ToString("dd-MM-yyyy hh:mm tt") : "")}</p>";
            mailBodyHtml += "<p><strong>Logged In Site: </strong>" + LoggedInSite + "</p>";
            mailBodyHtml += "</br>Thankyou";

            var fromAddress = _EmailOptions.FromAddress.Split('|');
            var Emails = _clientDataProvider.GetGlobalComplianceAlertEmail().ToList();
            var emailAddresses = string.Join(",", Emails.Select(email => email.Email));

            var FromAddress = new MailboxAddress(fromAddress[1], fromAddress[0]);
            var toAddressNew = emailAddresses.Split(',');
            var _toAddressList = GetToEmailAddressList(toAddressNew);

            await SendEmail(mailBodyHtml, subject, _toAddressList, FromAddress);
            return true;
        }

        public async Task<bool> SendShiftCancelledAlertMail(RosterSchedule shift, string guardName, string licenseNo, string siteName, string reason)
        {
            var subject = $"Shift Cancelled Alert - {siteName} - {guardName}";
            var mailBodyHtml = $"<h2>Shift Cancelled Alert</h2>" +
                               $"<p>A guard has cancelled a shift with the following details:</p>" +
                               $"<p><strong>Site:</strong> {siteName}</p>" +
                               $"<p><strong>Guard:</strong> {guardName}</p>" +
                               $"<p><strong>License No:</strong> {licenseNo}</p>" +
                               $"<p><strong>Date:</strong> {shift.ShiftStart:dd-MM-yyyy}</p>" +
                               $"<p><strong>Time:</strong> {shift.ShiftStart:HH:mm} - {shift.ShiftEnd:HH:mm}</p>" +
                               $"<p><strong>Reason:</strong> {reason}</p>" +
                               $"</br>Thank you";

            var fromAddress = _EmailOptions.FromAddress.Split('|');
            var FromAddress = new MailboxAddress(fromAddress[1], fromAddress[0]);

            var toAddresses = new List<string>();

            try
            {
                var kpiSetting = _clientDataProvider.GetClientSiteKpiSetting(shift.ClientSiteId);
                if (kpiSetting != null && kpiSetting.KPITelematicsFieldID.HasValue)
                {
                    var manager = _clientDataProvider.GetKPITelematicsDetails(kpiSetting.KPITelematicsFieldID.Value);
                    if (manager != null && !string.IsNullOrEmpty(manager.Email))
                    {
                        toAddresses.Add(manager.Email);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log or ignore if we can't get manager email, we still want to send to default
            }

            var _toAddressList = GetToEmailAddressList(toAddresses.ToArray());

            await SendEmail(mailBodyHtml, subject, _toAddressList, FromAddress);
            return true;
        }

        public async Task<bool> QueueMobileShiftCancellation(RosterSchedule shift, string reason)
        {
            if (shift.GuardId == null) return false;
            
            var queueItem = new ShiftCancellationEmailQueue
            {
                GuardId = shift.GuardId.Value,
                ClientSiteId = shift.ClientSiteId,
                ShiftStart = shift.ShiftStart,
                ShiftEnd = shift.ShiftEnd,
                Reason = reason,
                CancelledBy = "Guard",
                Source = "Mobile",
                CreatedAt = DateTime.Now,
                IsProcessed = false,
                IsReliefAssigned = false
            };
            
            _context.ShiftCancellationEmailQueues.Add(queueItem);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> QueueWebShiftCancellation(RosterSchedule shift, string reason, string cancelledByAdminName)
        {
            if (shift.GuardId == null) return false;
            
            var queueItem = new ShiftCancellationEmailQueue
            {
                GuardId = shift.GuardId.Value,
                ClientSiteId = shift.ClientSiteId,
                ShiftStart = shift.ShiftStart,
                ShiftEnd = shift.ShiftEnd,
                Reason = reason,
                CancelledBy = string.IsNullOrEmpty(cancelledByAdminName) ? "Admin" : cancelledByAdminName,
                Source = "Web",
                CreatedAt = DateTime.Now,
                IsProcessed = false,
                IsReliefAssigned = false
            };
            
            _context.ShiftCancellationEmailQueues.Add(queueItem);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> QueueReliefGuardAssigned(RosterSchedule shift, string assignedByAdminName)
        {
            if (shift.GuardId == null && shift.ReliefGuardId == null) return false;

            // If the shift doesn't have an original guard, we use ReliefGuardId as the GuardId for grouping purposes
            var guardId = shift.GuardId ?? shift.ReliefGuardId.Value;
            var reliefGuardName = shift.ReliefGuard != null ? shift.ReliefGuard.Name : shift.ReliefProviderName;

            var queueItem = new ShiftCancellationEmailQueue
            {
                GuardId = guardId,
                ClientSiteId = shift.ClientSiteId,
                ShiftStart = shift.ShiftStart,
                ShiftEnd = shift.ShiftEnd,
                Reason = "Relief Guard Assigned",
                CancelledBy = string.IsNullOrEmpty(assignedByAdminName) ? "Admin" : assignedByAdminName,
                Source = "Web",
                CreatedAt = DateTime.Now,
                IsProcessed = false,
                IsReliefAssigned = true,
                ReliefGuardId = shift.ReliefGuardId,
                ReliefGuardName = reliefGuardName
            };
            
            _context.ShiftCancellationEmailQueues.Add(queueItem);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveFromQueue(RosterSchedule shift)
        {
            if (shift.GuardId == null) return false;
            
            var pending = _context.ShiftCancellationEmailQueues
                .Where(q => q.GuardId == shift.GuardId.Value 
                         && q.ClientSiteId == shift.ClientSiteId 
                         && q.ShiftStart == shift.ShiftStart 
                         && q.ShiftEnd == shift.ShiftEnd 
                         && !q.IsProcessed)
                .ToList();

            if (pending.Any())
            {
                _context.ShiftCancellationEmailQueues.RemoveRange(pending);
                await _context.SaveChangesAsync();
            }
            return true;
        }

                public async Task<bool> SendAggregatedShiftCancelledAlertMail(Guard guard, string licenseNo, string cancelledBy, string source, List<ShiftCancellationEmailQueue> cancellations)
        {
            if (cancellations == null || !cancellations.Any()) return false;
            
            string guardName = guard?.Name ?? "Unknown Guard";
            var subject = "Shift Roster Update Alert - " + guardName;
            var mailBodyHtml = "<h2>Shift Roster Update Alert</h2>" +
                               "<p>The following shift updates have been recorded for:</p>" +
                               "<p><strong>Guard:</strong> " + guardName + "</p>" +
                               "<p><strong>License No:</strong> " + licenseNo + "</p><br/>";

            var toAddresses = new List<string>();
            var companyDetails = _context.CompanyDetails.FirstOrDefault();
            if (companyDetails != null && !string.IsNullOrEmpty(companyDetails.ROMail))
            {
                var splitRO = companyDetails.ROMail.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var em in splitRO)
                {
                    if (!toAddresses.Contains(em.Trim())) toAddresses.Add(em.Trim());
                }
            }
            
            // We want to fetch RosterSchedule data to find the group and project alert emails
            var siteGrouped = cancellations.GroupBy(c => c.ClientSite);
            foreach (var siteGroup in siteGrouped)
            {
                string siteName = siteGroup.Key?.Name ?? "Unknown Site";
                mailBodyHtml += "<p><strong>Site:</strong> " + siteName + "</p>";
                
                foreach (var item in siteGroup.OrderBy(x => x.ShiftStart))
                {
                    string actionText = item.IsReliefAssigned ? "Assigned as Relief Guard" : "Cancelled";
                    string whoText = item.IsReliefAssigned ? "Admin" : (item.CancelledBy == "Guard" ? "A guard" : (item.CancelledBy != null && item.CancelledBy.StartsWith("Guard|") ? "A guard (" + item.CancelledBy.Substring(6) + ")" : "An Admin (" + item.CancelledBy + ")"));
                    if (item.IsReliefAssigned && !string.IsNullOrEmpty(item.CancelledBy) && item.CancelledBy != "Admin") whoText = "An Admin (" + item.CancelledBy + ")";

                    mailBodyHtml += "<p><strong>Date (s):</strong> " + item.ShiftStart.ToString("dd-MM-yyyy") + "</p>" +
                                    "<p><strong>Time (s) :</strong> " + item.ShiftStart.ToString("HH:mm") + " - " + item.ShiftEnd.ToString("HH:mm") + "</p>" +
                                    "<p><strong>Action:</strong> " + actionText + " (by " + whoText + ")</p>" +
                                    "<p><strong>Reason (s):</strong> " + item.Reason + "</p><br/>";

                    // Add KPI
                    try
                    {
                        var kpiSetting = _clientDataProvider.GetClientSiteKpiSetting(item.ClientSiteId);
                        if (kpiSetting != null && kpiSetting.KPITelematicsFieldID.HasValue)
                        {
                            var manager = _clientDataProvider.GetKPITelematicsDetails(kpiSetting.KPITelematicsFieldID.Value);
                            if (manager != null && !string.IsNullOrEmpty(manager.Email) && !toAddresses.Contains(manager.Email.Trim()))
                            {
                                toAddresses.Add(manager.Email.Trim());
                            }
                        }
                    }
                    catch { }

                    // Look up schedule to find Group/Project configured emails
                    var schedule = _context.RosterSchedules
                        .Where(s => s.ClientSiteId == item.ClientSiteId && s.ShiftStart == item.ShiftStart && s.ShiftEnd == item.ShiftEnd)
                        .FirstOrDefault();
                    if (schedule != null)
                    {
                        var group = _context.RosterGroups.FirstOrDefault(g => g.Id == schedule.RosterGroupId);
                        if (group != null)
                        {
                            bool shouldSendGroup = (item.IsReliefAssigned && group.AlertOnReliefGuard) || (!item.IsReliefAssigned && group.AlertOnRejectedShift);
                            if (shouldSendGroup && !string.IsNullOrEmpty(group.AlertEmailRecipients))
                            {
                                var splits = group.AlertEmailRecipients.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var em in splits)
                                    if (!toAddresses.Contains(em.Trim())) toAddresses.Add(em.Trim());
                            }

                            var projectLinks = _context.RosterBinderProjects.Where(bp => bp.RosterGroupId == group.Id).ToList();
                            foreach (var link in projectLinks)
                            {
                                var project = _context.RosterBinders.FirstOrDefault(b => b.Id == link.RosterBinderId);
                                if (project != null)
                                {
                                    bool shouldSendProj = (item.IsReliefAssigned && project.AlertOnReliefGuard) || (!item.IsReliefAssigned && project.AlertOnRejectedShift);
                                    if (shouldSendProj && !string.IsNullOrEmpty(project.AlertEmailRecipients))
                                    {
                                        var splits = project.AlertEmailRecipients.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                                        foreach (var em in splits)
                                            if (!toAddresses.Contains(em.Trim())) toAddresses.Add(em.Trim());
                                    }
                                }
                            }
                        }
                    }
                }
            }

            var fromAddress = _EmailOptions.FromAddress.Split('|');
            var FromAddress = new MailboxAddress(fromAddress[1], fromAddress[0]);
            var _toAddressList = GetToEmailAddressList(toAddresses.ToArray());

            await SendEmail(mailBodyHtml, subject, _toAddressList, FromAddress);
            return true;
        }



        /// <summary>
        /// P1#375: tells the IR desk that one guard hit the same NFC checkpoint several times in
        /// a row - the pattern you get when a tag has been removed from its post and is being
        /// struck by hand rather than walked to. It is a flag for a human to investigate, not a
        /// finding, so the wording asks for exactly that.
        ///
        /// Goes to the same mailbox as an IR (EmailOptions.ToAddress), so the address stays
        /// configurable per environment instead of being compiled in.
        /// </summary>
        public async Task<bool> SendWandStrikeManipulationAlertMail(WandStrikeManipulationAlert alert)
        {
            if (alert == null)
                return false;

            var subject = $"Potential Wand Strike Manipulation - {alert.ClientSiteName} - {alert.GuardName}";

            var tagLabel = string.IsNullOrWhiteSpace(alert.TagLabelDescription)
                ? "(no label - tag not registered)"
                : alert.TagLabelDescription;

            var offset = string.IsNullOrWhiteSpace(alert.SiteUtcOffset) ? string.Empty : $" (UTC{alert.SiteUtcOffset})";

            var runTimes = alert.RunSiteLocalDateTimes != null && alert.RunSiteLocalDateTimes.Any()
                ? string.Join(", ", alert.RunSiteLocalDateTimes.Select(x => x.ToString("HH:mm:ss")))
                : alert.HitSiteLocalDateTime.ToString("HH:mm:ss");

            var mailBodyHtml =
                "<h2>Potential Wand Strike Manipulation - Please investigate</h2>" +
                $"<p>The same NFC tag was scanned <strong>{alert.ConsecutiveHitCount} times in a row</strong> by one guard, " +
                "with no other checkpoint scanned in between. This can mean the tag is no longer at its post.</p>" +
                "<table style='border-collapse: collapse;'>" +
                $"<tr><td style='padding-right:12px;'><strong>Site:</strong></td><td>{alert.ClientSiteName}</td></tr>" +
                $"<tr><td style='padding-right:12px;'><strong>Guard:</strong></td><td>{alert.GuardName}</td></tr>" +
                $"<tr><td style='padding-right:12px;'><strong>Date:</strong></td><td>{alert.HitSiteLocalDateTime:dd-MM-yyyy}</td></tr>" +
                $"<tr><td style='padding-right:12px;'><strong>Time:</strong></td><td>{alert.HitSiteLocalDateTime:HH:mm:ss} site local{offset}</td></tr>" +
                $"<tr><td style='padding-right:12px;'><strong>Scan times:</strong></td><td>{runTimes}</td></tr>" +
                $"<tr><td style='padding-right:12px;'><strong>Tag label:</strong></td><td>{tagLabel}</td></tr>" +
                $"<tr><td style='padding-right:12px;'><strong>Tag UID:</strong></td><td>{alert.TagUId}</td></tr>" +
                $"<tr><td style='padding-right:12px;'><strong>Scan type:</strong></td><td>NFC</td></tr>" +
                "</table>";

            if (alert.IsOfflineRecord)
            {
                /* The run was found while syncing a backlog, so the times above are when the
                   scans happened, not when this email was raised - say so, or the reader will
                   read them as current. */
                mailBodyHtml += "<p><em>These scans were cached on the handset while it was offline and " +
                                "synced later. The times shown are when the scans were made.</em></p>";
            }

            mailBodyHtml += $"<p>Raised automatically on {DateTime.UtcNow:dd-MM-yyyy HH:mm:ss} UTC.</p>";

            var fromAddress = _EmailOptions.FromAddress.Split('|');
            var FromAddress = new MailboxAddress(fromAddress[1], fromAddress[0]);

            /* Recipients come from Email:WandStrikeAlertToAddress - one or more addresses.
               Blank or absent, the flag falls back to the IR desk (Email:ToAddress), so an
               environment nobody has configured still sends it somewhere sensible rather than
               silently dropping it. */
            var configured = EmailRecipientHelper.ParseAddressList(_EmailOptions.WandStrikeAlertToAddress);
            if (configured.Count == 0)
            {
                var fallback = EmailRecipientHelper.AddressOnly(_EmailOptions.ToAddress);
                if (!string.IsNullOrWhiteSpace(fallback))
                    configured = new List<string> { fallback };
            }

            if (configured.Count == 0)
            {
                _logger?.LogWarning("Wand strike manipulation flag for {Site} has no recipient: " +
                                    "set Email:WandStrikeAlertToAddress or Email:ToAddress.", alert.ClientSiteName);
                return false;
            }

            var _toAddressList = GetToEmailAddressList(configured.ToArray());

            await SendEmail(mailBodyHtml, subject, _toAddressList, FromAddress);
            return true;
        }

        private async Task SendEmail(string mailBodyHtml, string subject, List<MailboxAddress> ToAddress, MailboxAddress FromAddress)
        {
            var message = new MimeMessage();
            message.From.Add(FromAddress);
            if (ToAddress != null && ToAddress.Any())
            {
                foreach (var address in ToAddress)
                    message.To.Add(address);
            }
            else
            {
                return;
            }


            message.Subject = subject;
            message.Bcc.Add(new MailboxAddress("globoconsoftware", "globoconsoftware@gmail.com"));
            var builder = new BodyBuilder()
            {
                HtmlBody = mailBodyHtml
            };
            message.Body = builder.ToMessageBody();

            // No-op unless Email:TestModeRedirectTo is set. Must come after the standing BCC
            // above, or test mode would leave that recipient in place.
            ApplyTestModeRedirect(message);
            using var client = new SmtpClient();
            client.Connect(_EmailOptions.SmtpServer, _EmailOptions.SmtpPort, MailKit.Security.SecureSocketOptions.None);
            if (!string.IsNullOrEmpty(_EmailOptions.SmtpUserName) &&
                !string.IsNullOrEmpty(_EmailOptions.SmtpPassword))
                client.Authenticate(_EmailOptions.SmtpUserName, _EmailOptions.SmtpPassword);
            await client.SendAsync(message);
            client.Disconnect(true);
        }

        /// <summary>
        /// Test-mode safety valve, the same one the Kpi scheduled send uses: when
        /// <c>Email:TestModeRedirectTo</c> is set, every recipient this service assembled is
        /// thrown away and replaced with the configured test addresses, so nothing can reach a
        /// client or the IR desk while a change is being verified. "[TEST]" is prefixed to the
        /// subject so a redirected mail is obvious in the test inbox.
        ///
        /// Deliberately blunt - it clears To, CC *and* BCC, including the standing
        /// globoconsoftware BCC - because a partial redirect is worse than none: it reads as
        /// safe while still delivering. Clearing rather than filtering also means a recipient
        /// added here in future is covered without anyone remembering to update this method.
        ///
        /// It covers every mail this service sends, not just the wand strike flag: a test mode
        /// that let other alerts through to real people would not be a test mode.
        ///
        /// To go back to live sending, set the value to "" in appsettings.json and restart.
        /// </summary>
        private void ApplyTestModeRedirect(MimeMessage message)
        {
            var redirectTo = EmailRecipientHelper.GetTestModeRedirectAddresses(_EmailOptions);
            if (redirectTo.Count == 0)
                return;

            var suppressed = message.To.Count + message.Cc.Count + message.Bcc.Count;

            message.To.Clear();
            message.Cc.Clear();
            message.Bcc.Clear();

            foreach (var address in redirectTo)
                message.To.Add(new MailboxAddress(string.Empty, address));

            message.Subject = "[TEST] " + message.Subject;

            _logger?.LogWarning(
                "Email TEST MODE is active: '{Subject}' redirected to {Redirect}. {Count} real recipient(s) suppressed. " +
                "Clear Email:TestModeRedirectTo in appsettings.json to resume live sending.",
                message.Subject, string.Join(", ", redirectTo), suppressed);
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
    }
}

