using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using Dropbox.Api.Team;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static MailKit.Net.Imap.ImapEvent;

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
        public readonly IClientDataProvider _clientDataProvider;

        public GuardReminderService(IGuardDataProvider guardDataProvider,
            IOptions<EmailOptions> emailOptions,
            IClientDataProvider clientDataProvider)
        {
            _guardDataProvider = guardDataProvider;
            _emailOptions = emailOptions.Value;
            _clientDataProvider = clientDataProvider;
        }

        public void Process()
        {
            var guardLicenses = _guardDataProvider.GetAllGuardLicensesAndCompliances().Where(z => z.ExpiryDate.HasValue).ToList();
            var guardcompliances = _guardDataProvider.GetAllGuardCompliances().Where(z => z.ExpiryDate.HasValue).ToList();

            var messages = new List<KeyValuePair<DateTime, string>>();
            messages.AddRange(GetLicenseMessagesAndCompliance(guardLicenses));
            //messages.AddRange(GetComplianceMessages(guardcompliances));

            if (messages.Any())
            {
                var mailBodyHtml = new StringBuilder();
                mailBodyHtml.Append("Hi, <br/><br/>Following guard documents are expiring soon. <br/><br/>");
                mailBodyHtml.Append("<table border=\"1px solid black\">");
                mailBodyHtml.Append("<thead><th>Document Type</th><th>Guard Name</th><th>Expiry Date</th><th>Description</th></thead>");
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
            //to avoid duplicate emails sending-start
            //New Email Message To Address Start
            
            var Emails = _clientDataProvider.GetGlobalComplianceAlertEmail().ToList();
            var emailAddresses = string.Join(",", Emails.Select(email => email.Email));
          
            //New Email Message To Address end
            bool flag = false;
            if (!flag)
            {
                //to avoid duplicate emails sending-end
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromAddress[1], fromAddress[0]));

                // message.To.Add(new MailboxAddress(toAddress[1], toAddress[0]));
                if (emailAddresses != null && emailAddresses!="" )
                {
                   var toAddressNew = emailAddresses.Split(',');
                    foreach (var address in GetToEmailAddressList(toAddressNew))
                        message.To.Add(address);
                }
                //p1-191 hr files task 8-start
                //to get the emails of the guard-start
                var guardIdsforlicenses = _guardDataProvider.GetAllGuardLicenses().Where(z => z.ExpiryDate.HasValue).ToList();
                var guardIdsNewforLicenses = GetLicenseMessagesId(guardIdsforlicenses).Select(x=>x.GuardId).ToList();
                var guardIdsforcompliances = _guardDataProvider.GetAllGuardCompliances().Where(z => z.ExpiryDate.HasValue && !guardIdsNewforLicenses.Contains(z.GuardId)).ToList();
                var guardIdsNewforCompliances = GetComplianceMessagesId(guardIdsforcompliances).Select(x => x.GuardId).ToList();
                var guardlicenseemail = _guardDataProvider.GetGuards().Where(z => guardIdsNewforLicenses.Contains(z.Id)).ToList();
                var guardcomplianceemail = _guardDataProvider.GetGuards().Where(z => guardIdsNewforCompliances.Contains(z.Id)).ToList();
                var guardEmailAddress = string.Empty;
                if (guardlicenseemail.Count>0 && guardcomplianceemail.Count>0)
                {
                    guardEmailAddress = string.Join(",", guardlicenseemail.Select(email => email.Email)) + ',' + string.Join(",", guardcomplianceemail.Select(email => email.Email));

                }
                else if (guardlicenseemail.Count == 0 && guardcomplianceemail.Count > 0)
                {
                    guardEmailAddress =  string.Join(",", guardcomplianceemail.Select(email => email.Email));

                }
                else if(guardlicenseemail.Count > 0 && guardcomplianceemail.Count == 0)
                {
                    guardEmailAddress = string.Join(",", guardlicenseemail.Select(email => email.Email));

                }
                else
                {
                    guardEmailAddress = string.Empty;
                }
                if (guardEmailAddress != null && guardEmailAddress!="")
                {
                    var toAddressNew = guardEmailAddress.Split(',');
                    foreach (var address in GetToEmailAddressList(toAddressNew))
                        if (!message.To.Contains(address))
                        {
                            message.To.Add(address);
                        }
                }

                //to get the emails of the guard - end
                //to get the emails of the provider -start
                var guardlicenseprovider = _guardDataProvider.GetGuards().Where(z => guardIdsNewforLicenses.Contains(z.Id)).Select(x=>x.Provider).ToList();
                var guardcomplianceprovider = _guardDataProvider.GetGuards().Where(z => guardIdsNewforCompliances.Contains(z.Id) && !guardlicenseprovider.Contains(z.Provider)).Select(x => x.Provider).ToList();
                var guardlicenseprovideremail = _clientDataProvider.GetKeyVehiclogWithProviders(guardlicenseprovider.ToArray());
                var guardcomplianceprovideremail = _clientDataProvider.GetKeyVehiclogWithProviders(guardcomplianceprovider.ToArray());

                var providerEmailAddress = string.Empty;
                if (guardlicenseprovideremail.Count > 0 && guardcomplianceprovideremail.Count > 0)
                {
                    providerEmailAddress = string.Join(",", guardlicenseprovideremail.Select(email => email.Email)) + ',' + string.Join(",", guardcomplianceprovideremail.Select(email => email.Email));

                }
                else if (guardlicenseprovideremail.Count == 0 && guardcomplianceprovideremail.Count > 0)
                {
                    providerEmailAddress = string.Join(",", guardcomplianceprovideremail.Select(email => email.Email));

                }
                else if (guardlicenseprovideremail.Count > 0 && guardcomplianceprovideremail.Count == 0)
                {
                    providerEmailAddress = string.Join(",", guardlicenseprovideremail.Select(email => email.Email));

                }
                else
                {
                    providerEmailAddress = string.Empty;
                }


                if (providerEmailAddress != null && providerEmailAddress!="")
                {
                    var toAddressNew = providerEmailAddress.Split(',');
                    foreach (var address in GetToEmailAddressList(toAddressNew))
                        
                            if (!message.To.Contains(address))
                            {
                                message.To.Add(address);
                            }
                        
                }

                //to get the emails of the provider -end
                //p1-191 hr files task 8-end
                /* Mail Id added Bcc globoconsoftware for checking Ir Mail not getting Issue Start(date 11,01,2024) */
                message.Bcc.Add(new MailboxAddress("globoconsoftware", "globoconsoftware@gmail.com"));
               // message.Bcc.Add(new MailboxAddress("globoconsoftware2", "jishakallani@gmail.com"));
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
                //to avoid duplicate emails sending-start
                flag = true;
                //to avoid duplicate emails sending-end
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

        private static IEnumerable<KeyValuePair<DateTime, string>> GetComplianceMessages(List<GuardCompliance> guardCompliances)
        {
            foreach (var compliance in guardCompliances)
            {
                if ((DateTime.Today.AddDays(compliance.Reminder1.GetValueOrDefault()) == compliance.ExpiryDate) ||
                    (DateTime.Today.AddDays(compliance.Reminder2.GetValueOrDefault()) == compliance.ExpiryDate))
                {
                    var message = $"<tr><td>Compliance</td><td>{compliance.ReferenceNo}</td><td>{compliance.Guard.Name}</td><td>{compliance.ExpiryDate?.ToString("dd-MMM-yyyy")}</td><td>{compliance.Description}</td>";
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
        private static IEnumerable<KeyValuePair<DateTime, string>> GetLicenseMessagesAndCompliance(List<GuardComplianceAndLicense> guardComplianceAndLicense)
        {
            foreach (var license in guardComplianceAndLicense)
            {
                
                    var message = $"<tr><td>Compliance</td><td><td>{license.Guard.Name}</td><td>{license.ExpiryDate?.ToString("dd-MMM-yyyy")}</td><td>{license.Description}</td>";
                    yield return new KeyValuePair<DateTime, string>(license.ExpiryDate.Value, message);
                
            }
        }
        private List<GuardLicense> GetLicenseMessagesId(List<GuardLicense> guardLicenses)
        {
            var guardListList = new List<GuardLicense>();
            foreach (var license in guardLicenses)
            {
                if ((DateTime.Today.AddDays(license.Reminder1.GetValueOrDefault()) == license.ExpiryDate) ||
                    (DateTime.Today.AddDays(license.Reminder2.GetValueOrDefault()) == license.ExpiryDate))
                {
                    guardListList.Add(license);
                }
            }
            return guardListList;
        }
        private List<GuardCompliance> GetComplianceMessagesId(List<GuardCompliance> guardCompliances)
        {
            var guardListList = new List<GuardCompliance>();
            foreach (var compliance in guardCompliances)
            {
                if ((DateTime.Today.AddDays(compliance.Reminder1.GetValueOrDefault()) == compliance.ExpiryDate) ||
                    (DateTime.Today.AddDays(compliance.Reminder2.GetValueOrDefault()) == compliance.ExpiryDate))
                {
                    guardListList.Add(compliance);
                }
            }
            return guardListList;
        }
        
    }
}
