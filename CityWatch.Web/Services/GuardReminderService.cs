using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using DocumentFormat.OpenXml.Office2013.Word;
using Dropbox.Api.Files;
using Dropbox.Api.Team;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private readonly IEmailLogDataProvider _emailLogDataProvider;

        public GuardReminderService(IGuardDataProvider guardDataProvider,
            IOptions<EmailOptions> emailOptions,
            IClientDataProvider clientDataProvider, IEmailLogDataProvider emailLogDataProvider)
        {
            _guardDataProvider = guardDataProvider;
            _emailOptions = emailOptions.Value;
            _clientDataProvider = clientDataProvider;
            _emailLogDataProvider = emailLogDataProvider;
        }

        //public void Process()
        //{
        //    var guardLicenses = _guardDataProvider.GetAllGuardLicensesAndCompliances().Where(z => z.ExpiryDate.HasValue).ToList();
        //    var guardcompliances = _guardDataProvider.GetAllGuardCompliances().Where(z => z.ExpiryDate.HasValue).ToList();

        //    var messages = new List<KeyValuePair<DateTime, string>>();
        //    messages.AddRange(GetLicenseMessagesAndCompliance(guardLicenses));
        //    //messages.AddRange(GetComplianceMessages(guardcompliances));

        //    if (messages.Any())
        //    {
        //        var mailBodyHtml = new StringBuilder();
        //        mailBodyHtml.Append("Hi, <br/><br/>Following guard documents are expiring soon. <br/><br/>");
        //        mailBodyHtml.Append("<table border=\"1px solid black\">");
        //        mailBodyHtml.Append("<thead><th>Document Type</th><th>Guard Name</th><th>Expiry Date</th><th>Description</th></thead>");
        //        mailBodyHtml.Append("<tbody>");
        //        foreach (var item in messages.OrderBy(z => z.Key))
        //        {
        //            mailBodyHtml.Append(item.Value);
        //        }
        //        mailBodyHtml.Append("</tbody>");
        //        mailBodyHtml.Append("</table>");

        //        SendEmail(mailBodyHtml.ToString());
        //    }
        //}

        public void Process()
        {
            /* Select only doc has ExpiryDate  and type is ExpiryDate type*/
            var guardLicenses = _guardDataProvider.GetAllGuardLicensesAndCompliances().Where(z => z.ExpiryDate.HasValue && z.DateType == false).ToList();

            var guardDocHaveExpiryToday = guardLicenses.Where(x => x.ExpiryDate == DateTime.Today.AddDays(x.Reminder1) || x.ExpiryDate == DateTime.Today.AddDays(x.Reminder2));
            if (guardDocHaveExpiryToday.Count() != 0 && guardDocHaveExpiryToday != null)
            {
                var distinctGuardId = guardDocHaveExpiryToday.Select(p => p.GuardId).Distinct().ToList();

                if (distinctGuardId.Count() != 0 && distinctGuardId != null)
                {
                    foreach (var guard in distinctGuardId)
                    {


                        var selectedGuardDocuments = guardDocHaveExpiryToday.Where(x => x.GuardId == guard).ToList();

                        if (selectedGuardDocuments.Count != 0 && selectedGuardDocuments != null)
                        {


                            var messageBody = string.Empty;
                            foreach (var doc in selectedGuardDocuments)
                            {
                                messageBody = messageBody+ $" <tr><td style=\"width:10% ;border: 1px solid #000000;\">Compliance</td><td style=\"width:20% ;border: 1px solid #000000;\">{doc.Guard.Name}</td><td style=\"width:10% ;border: 1px solid #000000;\">{doc.ExpiryDate?.ToString("dd-MMM-yyyy")}</td><td style=\"width:60% ;border: 1px solid #000000;\">{doc.Description}</td>";

                            }
                            var mailBodyHtml = new StringBuilder();
                            if (messageBody != string.Empty)
                            {

                                mailBodyHtml.Append("Hi, <br/><br/>Following guard documents are expiring soon. <br/><br/>");
                                mailBodyHtml.Append(" <table width=\"100%\" cellpadding=\"5\" cellspacing=\"5\" border=\"1\" style=\"border:ridge;border-color:#000000;border-width:thin\">");
                                mailBodyHtml.Append(" <tr><td style=\"width:10% ;border: 1px solid #000000; \"><b>Document Type</b></td><td style=\"width:20% ;border: 1px solid #000000;\"><b>Guard Name</b></td><td style=\"width:10% ;border: 1px solid #000000;\"><b>Expiry Date</b></td><td style=\"width:60% ;border: 1px solid #000000;\"><b>Description</b></td></tr>");
                                mailBodyHtml.Append("");
                                mailBodyHtml.Append(messageBody);
                                mailBodyHtml.Append("");
                                mailBodyHtml.Append("</table>");
                            }


                            var guardlicenseemail = _guardDataProvider.GetGuards().Where(z => z.Id == guard).FirstOrDefault().Email;
                            var guardlicenseprovideremail = string.Empty;
                            if (_guardDataProvider.GetGuards().Where(z => z.Id == guard).Select(x => x.Provider) != null)
                            {
                                guardlicenseprovideremail = _clientDataProvider.GetKeyVehiclogWithProviders(_guardDataProvider.GetGuards().Where(z => z.Id == guard).FirstOrDefault().Provider.Trim());

                            }
                            string toAddress = string.Empty;
                            toAddress = guardlicenseemail;
                            if (!string.IsNullOrEmpty(guardlicenseprovideremail))
                            {
                                toAddress = toAddress+ ',' +guardlicenseprovideremail;

                            }
                            

                            SendEmailNew(mailBodyHtml.ToString(), string.Empty, toAddress, string.Empty, string.Empty);

                        }


                    }

                }

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
                if (emailAddresses != null && emailAddresses != "")
                {
                    var toAddressNew = emailAddresses.Split(',');
                    foreach (var address in GetToEmailAddressList(toAddressNew))
                        message.To.Add(address);
                }
                //p1-191 hr files task 8-start
                //to get the emails of the guard-start
                var guardIdsforlicenses = _guardDataProvider.GetAllGuardLicenses().Where(z => z.ExpiryDate.HasValue).ToList();
                var guardIdsNewforLicenses = GetLicenseMessagesId(guardIdsforlicenses).Select(x => x.GuardId).ToList();
                var guardIdsforcompliances = _guardDataProvider.GetAllGuardCompliances().Where(z => z.ExpiryDate.HasValue && !guardIdsNewforLicenses.Contains(z.GuardId)).ToList();
                var guardIdsNewforCompliances = GetComplianceMessagesId(guardIdsforcompliances).Select(x => x.GuardId).ToList();
                var guardlicenseemail = _guardDataProvider.GetGuards().Where(z => guardIdsNewforLicenses.Contains(z.Id)).ToList();
                var guardcomplianceemail = _guardDataProvider.GetGuards().Where(z => guardIdsNewforCompliances.Contains(z.Id)).ToList();
                var guardEmailAddress = string.Empty;
                if (guardlicenseemail.Count > 0 && guardcomplianceemail.Count > 0)
                {
                    guardEmailAddress = string.Join(",", guardlicenseemail.Select(email => email.Email)) + ',' + string.Join(",", guardcomplianceemail.Select(email => email.Email));

                }
                else if (guardlicenseemail.Count == 0 && guardcomplianceemail.Count > 0)
                {
                    guardEmailAddress = string.Join(",", guardcomplianceemail.Select(email => email.Email));

                }
                else if (guardlicenseemail.Count > 0 && guardcomplianceemail.Count == 0)
                {
                    guardEmailAddress = string.Join(",", guardlicenseemail.Select(email => email.Email));

                }
                else
                {
                    guardEmailAddress = string.Empty;
                }
                if (guardEmailAddress != null && guardEmailAddress != "")
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
                var guardlicenseprovider = _guardDataProvider.GetGuards().Where(z => guardIdsNewforLicenses.Contains(z.Id)).Select(x => x.Provider).ToList();
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


                if (providerEmailAddress != null && providerEmailAddress != "")
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

                /* Save log email Start 24072024 manju*/
                string toAddressForSplit = string.Join(", ", message.To.Select(a => a.ToString()));
                string bccAddressForSplit = string.Join(", ", message.Bcc.Select(a => a.ToString()));
                _emailLogDataProvider.SaveEmailLog(
                    new EmailAuditLog()
                    {
                        UserID = 1,
                        GuardID = 1,
                        IPAddress = string.Empty,
                        ToAddress = toAddressForSplit,
                        BCCAddress = bccAddressForSplit,
                        Module = "Document Expiry",
                        Type= "Guard Reminder Service",
                        EmailSubject=message.Subject,
                        AttachmentFileName= string.Empty,
                        SendingDate = DateTime.Now   
                    }
                 ); 
                /* Save log for email end*/
               
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


        private void SendEmailNew(string mailBodyHtml, string fromAdress, string ToAddress, string BCC, string CC)
        {
            var fromAddress = _emailOptions.FromAddress.Split('|');

            var Emails = _clientDataProvider.GetGlobalComplianceAlertEmail().ToList();
            var emailAddresses = string.Join(",", Emails.Select(email => email.Email));



            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromAddress[1], fromAddress[0]));
            if (emailAddresses != null && emailAddresses != "")
            {
                var toAddressNew = emailAddresses.Split(',');
                foreach (var address in GetToEmailAddressList(toAddressNew))
                    message.To.Add(address);
            }
            if (ToAddress != null && ToAddress != "")
            {
                var toAddressNew = ToAddress.Split(',');
                foreach (var address in GetToEmailAddressList(toAddressNew))

                    if (!message.To.Contains(address))
                    {
                        message.To.Add(address);
                    }

            }

            message.Subject = "Reminder - Guard Documents Expiring";
            message.Bcc.Add(new MailboxAddress("globoconsoftware", "globoconsoftware@gmail.com"));
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
                if (license.DateType == false)
                {
                    if ((DateTime.Today.AddDays(license.Reminder1) == license.ExpiryDate) ||
                                        (DateTime.Today.AddDays(license.Reminder2) == license.ExpiryDate))
                    {
                        var message = $"<tr><td>Compliance</td><td>{license.Guard.Name}</td><td>{license.ExpiryDate?.ToString("dd-MMM-yyyy")}</td><td>{license.Description}</td>";
                        yield return new KeyValuePair<DateTime, string>(license.ExpiryDate.Value, message);
                    }
                }

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
