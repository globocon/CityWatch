using CityWatch.Data.Helpers;
using CityWatch.Data.Providers;
using Microsoft.Extensions.Options;
using MailKit.Net.Smtp;
using MimeKit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CityWatch.Data.Models;

namespace CityWatch.Web.Services
{
    public interface IAlertEmailServices
    {
        Task<bool> SendNewGuardRegisterAlertMail(Guard guard, string LoggedInSite);
    }
    public class AlertEmailServices : IAlertEmailServices
    {
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        private readonly IConfigDataProvider _configDataProvider;
        private readonly EmailOptions _EmailOptions;
        public AlertEmailServices(IClientDataProvider clientDataProvider,
            IGuardDataProvider guardDataProvider, IGuardLogDataProvider guardLogDataProvider,
            IConfigDataProvider configDataProvider, IOptions<EmailOptions> emailOptions)
        {
            _clientDataProvider = clientDataProvider;
            _guardDataProvider = guardDataProvider;
            _guardLogDataProvider = guardLogDataProvider;
            _configDataProvider = configDataProvider;
            _EmailOptions = emailOptions.Value;
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
            using var client = new SmtpClient();
            client.Connect(_EmailOptions.SmtpServer, _EmailOptions.SmtpPort, MailKit.Security.SecureSocketOptions.None);
            if (!string.IsNullOrEmpty(_EmailOptions.SmtpUserName) &&
                !string.IsNullOrEmpty(_EmailOptions.SmtpPassword))
                client.Authenticate(_EmailOptions.SmtpUserName, _EmailOptions.SmtpPassword);
            await client.SendAsync(message);
            client.Disconnect(true);
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
