using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Models;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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

        /// <summary>
        /// Issues the certificate for one guard on one course, exactly as the RPL run does for each
        /// guard it processes: marks the training/assessment records complete, generates the
        /// certificate PDF, stores the GuardComplianceAndLicense record and sends the notification.
        /// Exposed so the Bulk Certificate Release can reuse this logic instead of re-implementing it.
        /// </summary>
        void IssueCertificateForGuard(int guardId, int hrSettingsId);

        /// <summary>
        /// As above, but for a course that is set up for RPL: records the assessment against this
        /// guard first - the same TrainingCourseCertificateRPL row the rplDetailsModal writes for a
        /// single guard - and then issues. The row has to exist before the PDF is built, because
        /// CertificateGenerator reads it to decide whether the certificate carries a score card or
        /// the RPL compliance document, and to date the file from the assessment end date.
        /// </summary>
        /// <param name="rplDetails">
        /// The assessment captured once for the whole run. Null issues exactly as the overload above.
        /// </param>
        void IssueCertificateForGuard(int guardId, int hrSettingsId, RplAssessmentDetails rplDetails);

        /// <summary>
        /// The certificate document that decides whether a course is RPL, or null when the course
        /// has none uploaded. Deliberately the same
        /// GetCourseCertificateDocsUsingSettingsId(...).FirstOrDefault() row that the guard's
        /// Training and Assessment grid reads for its RPL button and that CertificateGenerator
        /// reads back when it looks for the guard's assessment - a different row here would record
        /// an RPL assessment the certificate never sees.
        /// </summary>
        TrainingCourseCertificate GetCertificateDocumentForCourse(int hrSettingsId);
    }

    /// <summary>
    /// One RPL assessment, as captured by rplDetailsModal. Carries no guard and no course: the
    /// Bulk Certificate Release collects it once and applies it to every guard in the run, and the
    /// guard/certificate pairing is resolved per guard when the row is written.
    /// </summary>
    public class RplAssessmentDetails
    {
        public int TrainingTheoryLocationId { get; set; }
        public int TrainingPracticalLocationId { get; set; }
        public int TrainingInstructorId { get; set; }
        public DateTime AssessmentStartDate { get; set; }
        public DateTime AssessmentEndDate { get; set; }

        /// <summary>Compliance document, already uploaded into each guard's folder. Optional, as in the single-guard modal.</summary>
        public string FileName { get; set; }
    }

    public class RPLCertificateGeneratorService : IRPLCertificateGeneratorService
    {
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly IConfigDataProvider _configDataProvider;
        private readonly ICertificateGenerator _certificateGenerator;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly ILogger<RPLCertificateGeneratorService> _logger;
        private readonly EmailOptions _EmailOptions;


        public RPLCertificateGeneratorService(IGuardLogDataProvider guardLogDataProvider, IGuardDataProvider guardDataProvider
            , IConfigDataProvider configDataProvider, ICertificateGenerator certificateGenerator, IOptions<EmailOptions> emailOptions, IClientDataProvider clientDataProvider
            , ILogger<RPLCertificateGeneratorService> logger)
        {
            _logger = logger;
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
                /* Each guard is isolated: this scheduled run had no error handling, so a single
                   unconfigured course (missing TrainingTestQuestionSettings, or a missing certificate
                   document row on the line below) aborted the whole run and every remaining guard was
                   silently skipped. Log and carry on instead. */
                try
                {
                    var certificateDocument = _configDataProvider.GetCourseCertificateDocuments()
                        .FirstOrDefault(x => x.Id == item.TrainingCourseCertificateId);

                    if (certificateDocument == null)
                    {
                        _logger.LogError($"RPL certificate run skipped GuardId {item.GuardId}: no course certificate document for TrainingCourseCertificateId {item.TrainingCourseCertificateId}.");
                        continue;
                    }

                    /* Was the two calls inlined here. Moved into IssueCertificateForGuard verbatim and
                       in the same order so the Bulk Certificate Release runs identical logic.
                       The row itself is handed down so it - and only it - is marked as consumed. */
                    IssueCertificateForGuard(item.GuardId, certificateDocument.HRSettingsId, item);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"RPL certificate run failed for GuardId {item.GuardId}, TrainingCourseCertificateId {item.TrainingCourseCertificateId}.");
                }
            }
        }

        /// <inheritdoc />
        public void IssueCertificateForGuard(int guardId, int hrSettingsId)
        {
            // No queue row: the Bulk Certificate Release and the admin release issue on demand and
            // have nothing to mark as consumed.
            IssueCertificateForGuard(guardId, hrSettingsId, (TrainingCourseCertificateRPL)null);
        }

        /// <inheritdoc />
        public TrainingCourseCertificate GetCertificateDocumentForCourse(int hrSettingsId) =>
            _configDataProvider.GetCourseCertificateDocsUsingSettingsId(hrSettingsId).FirstOrDefault();

        /// <inheritdoc />
        public void IssueCertificateForGuard(int guardId, int hrSettingsId, RplAssessmentDetails rplDetails)
        {
            if (rplDetails == null)
            {
                IssueCertificateForGuard(guardId, hrSettingsId);
                return;
            }

            /* Written before the certificate is built, exactly as OnPostSaveRPLDetails does for a
               single guard: CertificateGenerator.GeneratePdf looks this row up and, when it finds
               one, attaches the RPL compliance document instead of a score card and dates the file
               from AssessmentEndDate. Issuing first and recording afterwards would produce a
               score-card certificate dated today for a guard who never sat the test. */
            var assessment = SaveRplAssessment(guardId, hrSettingsId, rplDetails);

            /* Handed to the private overload as the row being drained, so it is marked consumed
               once the certificate exists. The release IS the issuing event, so leaving the row
               pending would hand the same assessment to the nightly api/RPLCertificate run
               tomorrow - a second certificate, a second compliance record and a second
               "New Certificate Issued" email for every guard in the release. */
            IssueCertificateForGuard(guardId, hrSettingsId, assessment);
        }

        /// <summary>
        /// Records the assessment against one guard, updating that guard's existing row for this
        /// certificate rather than adding a second one - the same lookup fetchURPLDeatils performs
        /// before the modal opens, so re-running a release cannot leave a guard with duplicate RPL
        /// records (and cannot confuse GeneratePdf, which takes the last row it finds).
        /// </summary>
        private TrainingCourseCertificateRPL SaveRplAssessment(int guardId, int hrSettingsId, RplAssessmentDetails details)
        {
            var certificateDocument = GetCertificateDocumentForCourse(hrSettingsId);
            if (certificateDocument == null)
            {
                var course = _configDataProvider.GetHRSettings().FirstOrDefault(x => x.Id == hrSettingsId);
                throw new InvalidOperationException(
                    $"No certificate document is uploaded for course '{course?.Description ?? hrSettingsId.ToString()}', so an RPL assessment cannot be recorded.");
            }

            var existing = _configDataProvider.GetCourseCertificateRPLUsingId(certificateDocument.Id)
                .Where(x => x.GuardId == guardId)
                .OrderByDescending(x => x.Id)
                .FirstOrDefault();

            var record = new TrainingCourseCertificateRPL()
            {
                // -1, not 0, is this provider's "insert" sentinel.
                Id = existing?.Id ?? -1,
                GuardId = guardId,
                TrainingCourseCertificateId = certificateDocument.Id,
                TrainingTheoryLocationId = details.TrainingTheoryLocationId,
                TrainingPracticalLocationId = details.TrainingPracticalLocationId,
                TrainingInstructorId = details.TrainingInstructorId,
                AssessmentStartDate = details.AssessmentStartDate,
                AssessmentEndDate = details.AssessmentEndDate,
                FileName = details.FileName,
                isDeleted = false
            };

            _guardLogDataProvider.SaveTrainingCourseCertificateRPL(record);

            return record;
        }

        /// <param name="rplAssessment">
        /// The TrainingCourseCertificateRPL row this issue is draining, or null when the certificate
        /// is being released on demand rather than off the RPL queue.
        /// </param>
        private void IssueCertificateForGuard(int guardId, int hrSettingsId, TrainingCourseCertificateRPL rplAssessment)
        {
            /* A course with no TrainingTestQuestionSettings row cannot be certified: both methods below
               read that row for IsCertificateHoldUntilPracticalTaken / IsCertificateWithQAndADump /
               IsCertificateExpiry and dereference it unguarded, which threw a bare
               NullReferenceException that told the operator nothing (e.g. "Martha Cove - Alarm Faults"
               has no settings row). Fail fast here, before any training record is touched, with a
               message the Bulk Certificate Release can show against the guard and course. Both private
               methods are only reachable through here, so this one check covers them. */
            var certificateSettings = _configDataProvider.GetTQSettings(hrSettingsId).FirstOrDefault();
            if (certificateSettings == null)
            {
                var course = _configDataProvider.GetHRSettings().FirstOrDefault(x => x.Id == hrSettingsId);
                throw new InvalidOperationException(
                    $"No Training/Test Question settings configured for course '{course?.Description ?? hrSettingsId.ToString()}', so a certificate cannot be issued.");
            }

            GuardCertificateAndfeedBackStatus(guardId, hrSettingsId);
            GuardCertificate(guardId, hrSettingsId, rplAssessment);
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
        private void GuardCertificate(int guardId, int hrSettingsId, TrainingCourseCertificateRPL rplAssessment)
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
            var hrSettings = _configDataProvider.GetHRSettings().Where(x => x.Id == hrSettingsId).FirstOrDefault();
            if (hrSettings == null)
                throw new InvalidOperationException($"Course {hrSettingsId} was not found, so a certificate cannot be issued.");
            /* Was hrSettings.Description alone, which recorded "Thermal Camera (FLIR Ti)" against
               the guard while every single-guard flow recorded "03e Thermal Camera (FLIR Ti)" - the
               reference number was simply missing from this copy of the expression. The generated
               file was always named correctly (GeneratePdf builds its name from the same three
               parts), so only the GuardComplianceAndLicense record was short. One shared definition
               now; see HrSettings.CertificateRecordName. */
            var hrdesription = hrSettings.CertificateRecordName;
            var hrgroupid = hrSettings.HRGroupId;
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
            /* Drain the queue row this run is actually processing, and nothing else.

               GenerateRPLCertificate selects TrainingCourseCertificateRPL rows with isDeleted = 0 and
               marks them done here, so this line is what stops a row coming back tomorrow. It used to
               throw the row away and re-derive a certificate document from the course
               (GetCourseCertificateDocsUsingSettingsId(...).FirstOrDefault()), then only mark anything
               if THAT document had isRPLEnabled set. Two ways it missed, both seen in live data:
               a course whose first document has the flag turned off ("Thermal Camera (FLIR Ti)"), and
               rows pointing at a different document of the same course ("DashCAM - Martha Cove"). Either
               way isDeleted stayed 0 and the daily scheduler re-issued the same certificate every day -
               regenerating the PDF, re-uploading it to Dropbox, inserting another compliance record and
               emailing "New Certificate Issued" again, indefinitely.

               A null row means this is an on-demand release (Bulk Certificate Release or admin), which
               has no queue entry to consume. */
            if (rplAssessment != null)
            {
                _guardLogDataProvider.SaveTrainingCourseCertificateRPL(new TrainingCourseCertificateRPL()
                {
                    Id = rplAssessment.Id,
                    GuardId = rplAssessment.GuardId,
                    TrainingCourseCertificateId = rplAssessment.TrainingCourseCertificateId,
                    AssessmentStartDate = rplAssessment.AssessmentStartDate,
                    AssessmentEndDate = rplAssessment.AssessmentEndDate,
                    TrainingPracticalLocationId = rplAssessment.TrainingPracticalLocationId,
                    TrainingTheoryLocationId = rplAssessment.TrainingTheoryLocationId,
                    TrainingInstructorId = rplAssessment.TrainingInstructorId,
                    FileName = rplAssessment.FileName,
                    isDeleted = true
                });
            }

            /* The certificate has been generated and the compliance record saved by this point, so the
               release has succeeded. A mail server problem must not be reported back as a failed
               certificate - the Bulk Certificate Release would show the guard as failed for a
               certificate that actually exists. Log it and move on. */
            try
            {
                var emailBody = GiveGuardCourseCompletedNotification(guardId, hrdesription);
                SendEmailNew(emailBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Certificate issued for GuardId {guardId}, course '{hrdesription}', but the course-completed notification could not be sent.");
            }

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
            if (guardDetails == null)
                throw new InvalidOperationException($"Guard {guardId} was not found, so the course-completed notification cannot be built.");
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
