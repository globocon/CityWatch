using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Models;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace CityWatch.Web.Services
{
    public interface IRPLCertificateGeneratorService
    {
        void GenerateRPLCertificate();
    }

    public class RPLCertificateGeneratorService : IRPLCertificateGeneratorService
    {
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly IConfigDataProvider _configDataProvider;
        private readonly ICertificateGenerator _certificateGenerator;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly EmailOptions _EmailOptions;


        public RPLCertificateGeneratorService(IGuardLogDataProvider guardLogDataProvider, IGuardDataProvider guardDataProvider
            , IConfigDataProvider configDataProvider, ICertificateGenerator certificateGenerator, IOptions<EmailOptions> emailOptions, IClientDataProvider clientDataProvider)
        {
            _guardLogDataProvider = guardLogDataProvider;
            _guardDataProvider = guardDataProvider;
            _configDataProvider = configDataProvider;
            _certificateGenerator = certificateGenerator;
            _EmailOptions = emailOptions.Value;
            _clientDataProvider = clientDataProvider;
        }

        public void GenerateRPLCertificate()
        {
            var rplCertificateDetails = _guardDataProvider.GetCourseCertificateRPL().Where(x => x.AssessmentEndDate.Date < DateTime.Now.Date);

            foreach(var item in rplCertificateDetails)
            {
                int hrsettingsid = _configDataProvider.GetCourseCertificateDocuments().Where(x => x.Id == item.TrainingCourseCertificateId).FirstOrDefault().HRSettingsId;
                GuardCertificateAndfeedBackStatus(item.GuardId, hrsettingsid);
                GuardCertificate(item.GuardId, hrsettingsid);
            }
        }
        private void GuardCertificateAndfeedBackStatus(int guardId, int hrSettingsId)
        {
            string input = GenerateFormattedString();
            string hashCode = GenerateHashCode(input);

            var getcertificateSatus = _configDataProvider.GetTQSettings(hrSettingsId).FirstOrDefault();
            //if (getcertificateSatus == null)
            //{
            //    return new JsonResult(new { error = "Certificate status not found." });
            //}

            var tqNumberList = _configDataProvider.GetTrainingCoursesWithHrSettingsId(hrSettingsId)?.ToList();
            //if (tqNumberList == null || !tqNumberList.Any())
            //{
            //    return new JsonResult(new { error = "No training course numbers found." });
            //}

            foreach (var item in tqNumberList)
            {
                int tqNumberId = item.TQNumberId;
                var trainingCourse = _configDataProvider.GetTrainingCourses(hrSettingsId, tqNumberId).FirstOrDefault();
                if (trainingCourse == null) continue;

                int trainingCourseId = trainingCourse.Id;
                var record = _guardDataProvider
                    .GetGuardTrainingAndAssessment(guardId)?
                    .FirstOrDefault(x => x.TrainingCourseId == trainingCourseId);

                if (record != null)
                {
                    _configDataProvider.SaveGuardTrainingAndAssessmentTab(new GuardTrainingAndAssessment()
                    {
                        Id = record.Id,
                        GuardId = guardId,
                        TrainingCourseId = trainingCourseId,
                        TrainingCourseStatusId = getcertificateSatus.IsCertificateHoldUntilPracticalTaken ? 3 : 4,
                        Description = record.Description,
                        HRGroupId = record.HRGroupId
                    });
                }
            }

            //return new JsonResult(new { getcertificateSatus });
        }
        private void GuardCertificate(int guardId, int hrSettingsId)
        {
            string input = GenerateFormattedString();
            string hashCode = GenerateHashCode(input);
            var getcertificateSatus = _configDataProvider.GetTQSettings(hrSettingsId).FirstOrDefault();
            var filename = _certificateGenerator.GeneratePdf(guardId, hrSettingsId, hashCode, getcertificateSatus.IsCertificateHoldUntilPracticalTaken, getcertificateSatus.IsCertificateWithQAndADump, getcertificateSatus.IsCertificateExpiry);
            DateTime? expirydate = DateTime.Now;
            bool IsExpiry = false;
            if (getcertificateSatus.IsCertificateExpiry == true)
            {

                var expiryyears = _configDataProvider.GetTQSettings(hrSettingsId).Where(x => x.IsCertificateExpiry == true).FirstOrDefault().CertificateExpiryYears.Name;
                IsExpiry = false;
                string newexpiry = string.Empty;
                if (expiryyears.Contains("year"))
                    newexpiry = expiryyears.Replace("year", "");
                if (expiryyears.Contains("years"))
                    newexpiry = expiryyears.Replace("years", "");
                DateTime currentdate = DateTime.Now;
                expirydate = currentdate.AddYears(Convert.ToInt32(newexpiry));

            }
            else
            {
                expirydate = DateTime.Now;
                IsExpiry = true;
            }
            var hrdesription = _configDataProvider.GetHRSettings().Where(x => x.Id == hrSettingsId).FirstOrDefault().Description;
            var hrgroupid = _configDataProvider.GetHRSettings().Where(x => x.Id == hrSettingsId).FirstOrDefault().HRGroupId;
            _guardDataProvider.SaveGuardComplianceandlicanse(new GuardComplianceAndLicense()
            {
                Id = 0,
                GuardId = guardId,
                Description = hrdesription,
                CurrentDateTime = DateTime.Now.ToString(),
                FileName = filename,
                HrGroup = (HrGroup?)hrgroupid,
                ExpiryDate = expirydate,
                DateType = IsExpiry,
                Reminder1 = 45,
                Reminder2 = 7
            });
            var IsRPL = _configDataProvider.GetCourseCertificateDocsUsingSettingsId(hrSettingsId).FirstOrDefault(); ;
            if (IsRPL.isRPLEnabled == true)
            {
                var rpldetails = _guardDataProvider.GetCourseCertificateRPL().Where(x => x.TrainingCourseCertificateId == IsRPL.Id && x.GuardId == guardId).FirstOrDefault();
                _guardLogDataProvider.SaveTrainingCourseCertificateRPL(new TrainingCourseCertificateRPL()
                {
                    Id = rpldetails.Id,
                    GuardId = rpldetails.GuardId,
                    TrainingCourseCertificateId = rpldetails.TrainingCourseCertificateId,
                    AssessmentStartDate = rpldetails.AssessmentStartDate,
                    AssessmentEndDate = rpldetails.AssessmentEndDate,
                    TrainingPracticalLocationId = rpldetails.TrainingPracticalLocationId,
                    TrainingInstructorId = rpldetails.TrainingInstructorId,
                    isDeleted = true
                });
            }

            var emailBody = GiveGuardCourseCompletedNotification(guardId, hrdesription);
            SendEmailNew(emailBody);

            //int guardCorrectQuestionsCount = existingGuardScrore.FirstOrDefault().guardCorrectQuestionsCount;

            //string guardScore = existingGuardScrore.FirstOrDefault().guardScore;

            //bool IsPass = existingGuardScrore.FirstOrDefault().IsPass;



            //return new JsonResult(new { filename });
        }
        private string GenerateHashCode(string input)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        private string GenerateFormattedString()
        {
            string[] segments = new string[5];
            Random random = new Random();

            for (int i = 0; i < segments.Length; i++)
            {
                switch (i)
                {
                    case 0:
                        segments[i] = GenerateRandomAlphanumeric(5, random);
                        break;
                    case 1:
                        segments[i] = GenerateRandomAlphanumeric(8, random);
                        break;
                    case 2:
                        segments[i] = GenerateRandomAlphanumeric(7, random);
                        break;
                    case 3:
                        segments[i] = "fjfjfjjfl9999";
                        break;
                    case 4:
                        segments[i] = "3456";
                        break;
                }
            }

            return string.Join("-", segments);
        }
        private string GenerateRandomAlphanumeric(int length, Random random)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public string GiveGuardCourseCompletedNotification(int guardId, string hrdesription)
        {
            var guardDetails = _guardDataProvider.GetGuardDetailsUsingId(guardId).FirstOrDefault();
            var sb = new StringBuilder();

            var messageBody = string.Empty;
            messageBody = $" <tr><td style=\"width:2% ;border: 1px solid #000000;\"><b>Name of Guard</b></td><td style=\"width:5% ;border: 1px solid #000000;\">{guardDetails.Name}</td>";
            messageBody = messageBody + $" <tr><td style=\"width:2% ;border: 1px solid #000000;\"><b>License</b></td><td style=\"width:5% ;border: 1px solid #000000;\">{guardDetails.SecurityNo}</td>";
            messageBody = messageBody + $" <tr><td style=\"width:2% ;border: 1px solid #000000;\"><b>Provider</b></td><td style=\"width:5% ;border: 1px solid #000000;\">{guardDetails.Provider}</td>";
            messageBody = messageBody + $" <tr><td style=\"width:2% ;border: 1px solid #000000;\"><b>Course</b></td><td style=\"width:5% ;border: 1px solid #000000;\">{hrdesription}</td>";

            sb.Append("Hi , <br/><br/>The following guard successfully completed a course <br/><br/>");
            sb.Append(" <table width=\"50%\" cellpadding=\"5\" cellspacing=\"5\" border=\"1\" style=\"border:ridge;border-color:#000000;border-width:thin\">");
            sb.Append(" <tr><td style=\"width:2% ;border: 1px solid #000000;text-align:center \" colspan=\"2\"><b>Guard Details</b></td></tr>");
            sb.Append(messageBody);
            sb.Append("");


            //mailBodyHtml.Append("");
            return sb.ToString();
        }
        private void SendEmailNew(string mailBodyHtml)
        {
            var fromAddress = _EmailOptions.FromAddress.Split('|');
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


            message.Subject = "New Certificate Issued";
            message.Bcc.Add(new MailboxAddress("globoconsoftware", "globoconsoftware@gmail.com"));
            var builder = new BodyBuilder()
            {
                HtmlBody = mailBodyHtml
            };
            message.Body = builder.ToMessageBody();
            using (var client = new SmtpClient())
            {
                client.Connect(_EmailOptions.SmtpServer, _EmailOptions.SmtpPort, MailKit.Security.SecureSocketOptions.None);
                if (!string.IsNullOrEmpty(_EmailOptions.SmtpUserName) &&
                    !string.IsNullOrEmpty(_EmailOptions.SmtpPassword))
                    client.Authenticate(_EmailOptions.SmtpUserName, _EmailOptions.SmtpPassword);
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

    }
}
