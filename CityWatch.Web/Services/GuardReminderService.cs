using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CityWatch.Web.Services
{
    public interface IGuardReminderService
    {
        void Process();
    }

    public class GuardReminderService : IGuardReminderService
    {
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly EmailOptions _emailOptions;

        public GuardReminderService(IGuardDataProvider guardDataProvider,
            IOptions<EmailOptions> emailOptions)
        {
            _guardDataProvider = guardDataProvider;
            _emailOptions = emailOptions.Value;
        }

        public void Process()
        {
            var guardLicenses = _guardDataProvider.GetAllGuardLicenses().Where(z => z.ExpiryDate.HasValue).ToList();
            var guardcompliances = _guardDataProvider.GetAllGuardCompliances().Where(z => z.ExpiryDate.HasValue).ToList();
            
            var messages = new List<KeyValuePair<DateTime, string>>();
            messages.AddRange(GetLicenseMessages(guardLicenses));
            messages.AddRange(GetComplianceMessages(guardcompliances));

            if (messages.Any())
            {
                var mailBodyHtml = new StringBuilder();
                mailBodyHtml.Append("Hi, <br/><br/>Following guard documents are expiring soon. <br/><br/>");
                mailBodyHtml.Append("<table border=\"1px solid black\">");
                mailBodyHtml.Append("<thead><th>Document Type</th><th>Document No</th><th>Guard Name</th><th>Expiry Date</th></thead>");
                mailBodyHtml.Append("<tbody>");
                foreach (var item in messages.OrderBy(z => z.Key))
                {
                    mailBodyHtml.Append(item.Value);
                }
                mailBodyHtml.Append("</tbody>");
                mailBodyHtml.Append("</table>");

                SendEmail(mailBodyHtml.ToString());
            }
        }

        private void SendEmail(string mailBodyHtml)
        {
            var fromAddress = _emailOptions.FromAddress.Split('|');
    
            //To get the Default Email start
            var ToAddreddAppset = _emailOptions.ToAddress.Split('|');
            var toAddressData = _guardDataProvider.GetDefaultEmailAddress() + '|' + ToAddreddAppset[1];
            var toAddress = toAddressData.Split('|');
            var ToAddressFirststr = _guardDataProvider.GetDefaultEmailAddress();
            if (ToAddressFirststr == null)
            {
                toAddress = _emailOptions.ToAddress.Split('|');
            }

            //To get the Default Email stop

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromAddress[1], fromAddress[0]));
            message.To.Add(new MailboxAddress(toAddress[1], toAddress[0]));
            /* Mail Id added Bcc globoconsoftware for checking Ir Mail not getting Issue Start(date 11,01,2024) */
            message.Bcc.Add(new MailboxAddress("globoconsoftware", "globoconsoftware@gmail.com"));
            message.Bcc.Add(new MailboxAddress("globoconsoftware2", "jishakallani@gmail.com"));
            /* Mail Id added Bcc globoconsoftware end */
            message.Subject = "Reminder - Guard Documents Expiring";
            var builder = new BodyBuilder()
            {
                HtmlBody = mailBodyHtml
            };
            message.Body = builder.ToMessageBody();
            using var client = new SmtpClient();
            client.Connect(_emailOptions.SmtpServer, _emailOptions.SmtpPort, MailKit.Security.SecureSocketOptions.None);
            if (!string.IsNullOrEmpty(_emailOptions.SmtpUserName) &&
                !string.IsNullOrEmpty(_emailOptions.SmtpPassword))
                client.Authenticate(_emailOptions.SmtpUserName, _emailOptions.SmtpPassword);
            client.Send(message);
            client.Disconnect(true);
        }

        private static IEnumerable<KeyValuePair<DateTime, string>> GetComplianceMessages(List<GuardCompliance> guardCompliances)
        {
            foreach (var compliance in guardCompliances)
            {
                if ((DateTime.Today.AddDays(compliance.Reminder1.GetValueOrDefault()) == compliance.ExpiryDate) || 
                    (DateTime.Today.AddDays(compliance.Reminder2.GetValueOrDefault()) == compliance.ExpiryDate))
                {
                    var message = $"<tr><td>Compliance</td><td>{compliance.ReferenceNo}</td><td>{compliance.Guard.Name}</td><td>{compliance.ExpiryDate?.ToString("dd-MMM-yyyy")}</td>";
                    yield return new KeyValuePair<DateTime, string>(compliance.ExpiryDate.Value, message);
                }
            }
        }

        private static IEnumerable<KeyValuePair<DateTime, string>> GetLicenseMessages(List<GuardLicense> guardLicenses)
        {
            foreach (var license in guardLicenses)
            {
                if ((DateTime.Today.AddDays(license.Reminder1.GetValueOrDefault()) == license.ExpiryDate) ||
                    (DateTime.Today.AddDays(license.Reminder2.GetValueOrDefault()) == license.ExpiryDate))
                {
                    var message = $"<tr><td>License</td><td>{license.LicenseNo}</td><td>{license.Guard.Name}</td><td>{license.ExpiryDate?.ToString("dd-MMM-yyyy")}</td>";
                    yield return new KeyValuePair<DateTime, string>(license.ExpiryDate.Value, message);
                }                
            }
        }
    }
}
