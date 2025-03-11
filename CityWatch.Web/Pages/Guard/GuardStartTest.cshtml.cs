using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using CityWatch.Web.Helpers;
using CityWatch.Web.Models;
using CityWatch.Web.Services;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DocumentFormat.OpenXml.Presentation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NuGet.Packaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using static Dropbox.Api.Team.GroupSelector;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CityWatch.Web.Pages.Guard
{
    public class GuardStartTestModel : PageModel
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IViewDataService _viewDataService;
        private readonly IPatrolDataReportService _irChartDataService;
        private readonly IIncidentReportGenerator _incidentReportGenerator;
        private readonly IConfigDataProvider _configDataProvider;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly Settings _settings;
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        private readonly ICertificateGenerator _certificateGenerator;
        public int GuardId { get; set; }
        public int GuardTrainingAssessmentId { get; set; }
        public GuardTrainingAndAssessment GuardTrainingAndAssessment { get; set; }
        public List<GuardTrainingAttendedQuestionsAndAnswers> GuardTrainingAttendedQuestionsAndAnswers { get; set; }

        public TrainingTestQuestionSettings TrainingTestQuestionSettings { get; set; }
        public string CourseDocsPath;
        public string Coursefilename;
        public string hrreferencenumber;
        public int totalQuestions;
       

        public GuardStartTestModel(IViewDataService viewDataService,
            IWebHostEnvironment webHostEnvironment,
            IPatrolDataReportService irChartDataService, IIncidentReportGenerator incidentReportGenerator, IConfigDataProvider configurationProvider, IClientDataProvider clientDataProvider, IOptions<Settings> settings, IGuardDataProvider guardDataProvider,
            IGuardLogDataProvider guardLogDataProvider, ICertificateGenerator certificateGenerator)
        {
            _viewDataService = viewDataService;
            _webHostEnvironment = webHostEnvironment;
            _irChartDataService = irChartDataService;
            _incidentReportGenerator = incidentReportGenerator;
            _configDataProvider = configurationProvider;
            _clientDataProvider = clientDataProvider;
            _settings = settings.Value;
            _guardDataProvider = guardDataProvider;
            _guardLogDataProvider = guardLogDataProvider;
            _certificateGenerator = certificateGenerator;
        }
        public void OnGet()
        {
            try
            {
                GuardId = Convert.ToInt32(Request.Query["guid"]);
                GuardTrainingAssessmentId = Convert.ToInt32(Request.Query["guardCourseId"]);
                if (GuardTrainingAssessmentId != 0)
                {
                    GuardTrainingAndAssessment = _guardDataProvider.GetGuardTrainingAndAssessment(GuardId).Where(x => x.Id == GuardTrainingAssessmentId).FirstOrDefault();
                    TrainingTestQuestionSettings = _configDataProvider.GetTQSettings(GuardTrainingAndAssessment.TrainingCourses.HRSettingsId).FirstOrDefault();
                    var jresult = _configDataProvider.GetHRSettings().Where(x => x.Id == GuardTrainingAndAssessment.TrainingCourses.HRSettingsId);
                    var hrreferenceNumber = "HR" + jresult.FirstOrDefault().ReferenceNoNumbers.Name + jresult.FirstOrDefault().ReferenceNoAlphabets.Name;
                    var CourseDocsFolder = Path.Combine("TA", hrreferenceNumber, "Course", GuardTrainingAndAssessment.TrainingCourses.FileName);
                    CourseDocsPath = CourseDocsFolder;
                    hrreferencenumber = hrreferenceNumber;
                    Coursefilename = GuardTrainingAndAssessment.TrainingCourses.FileName;
                    GuardTrainingAttendedQuestionsAndAnswers = _configDataProvider.GetGuardAttendedQuestionsAndanswers(GuardId, GuardTrainingAndAssessment.TrainingCourseId).ToList();
                    totalQuestions = _configDataProvider.GetTrainingQuestionsWithHRSettings(GuardTrainingAndAssessment.TrainingCourses.HRSettingsId).Count;

                }
            }
            catch(Exception ex)
            {

            }
            
        }
        public JsonResult OnGetGuardQuestions(int hrSettingsId, int tqNumberId,int guardId)
        {
            var result = _configDataProvider.GetGuardQuestions(hrSettingsId, tqNumberId, guardId);

            return new JsonResult(result);
        }
        public JsonResult OnGetGuardOptions(int questionId)
        {
            var result = _configDataProvider.GetGuardOptions(questionId);

            return new JsonResult(result);
        }
        public JsonResult OnPostSaveGuardAnswers(GuardTrainingAttendedQuestionsAndAnswers record)
        {


            var success = false;
            var message = string.Empty;
            try
            {




                var isAnswer = _configDataProvider.GetGuardOptions(record.TrainingTestQuestionsId).Where(x=>x.Id==record.TrainingTestQuestionsAnswersId).FirstOrDefault().IsAnswer;


                _configDataProvider.SaveGuardAnswers(new GuardTrainingAttendedQuestionsAndAnswers()
                {
                    Id = record.Id,
                    GuardId = record.GuardId,
                    TrainingCourseId = record.TrainingCourseId,
                    TrainingTestQuestionsId = record.TrainingTestQuestionsId,
                    TrainingTestQuestionsAnswersId = record.TrainingTestQuestionsAnswersId,
                    IsCorrect = isAnswer

                });


                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }
        public JsonResult OnGetGuardQuestionCount(int hrSettingsId, int tqNumberId,int guardId)
        {
            int TotalQuestions = _configDataProvider.GetQuestionCount(hrSettingsId, tqNumberId);
            var questionnumer = _configDataProvider.GetQuestionNumber(hrSettingsId, tqNumberId, guardId);
            var countid = questionnumer.Count();
            string Qno = string.Empty;
            if (countid <= TotalQuestions)
            {
                int numberOfDigits = countid / 10 + 1;
                if(numberOfDigits==1)
                {
                    countid = countid + 1;
                    Qno = "0" + countid.ToString();
                }
            }
            return new JsonResult(new { TotalQuestions, Qno , countid });
        }
        public JsonResult OnPostGuardMarks(int guardId, int hrSettingsId, int tqNumberId,string duration)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                int TotalQuestions = _configDataProvider.GetQuestionCount(hrSettingsId, tqNumberId);
                int trainingCourseId = _configDataProvider.GetTrainingCourses(hrSettingsId, tqNumberId).FirstOrDefault().Id;
                var guardCorrectQuestions = _configDataProvider.GetGuardCorrectQuestions(guardId, trainingCourseId);
                int guardCorrectQuestionsCount = guardCorrectQuestions.Count();
                float quotation = (guardCorrectQuestionsCount / TotalQuestions) * 100;
                double scoreAttainedbyguard = Math.Round(quotation, 2);
                string guardScore = scoreAttainedbyguard.ToString() + "%";
                var settings = _configDataProvider.GetTQSettings(hrSettingsId);
                string PassMarkName = settings.FirstOrDefault().PassMark.Name;
                string modifiedPassMarkName = PassMarkName.Replace("%", "");
                double PassMark = Convert.ToDouble(modifiedPassMarkName);
                bool IsPass = false;
                if (scoreAttainedbyguard >= PassMark)
                {
                    IsPass = true;
                }
                else
                {
                    IsPass = false;
                }

                int id = 0;
                var existingGuardScrore = _configDataProvider.GetGuardScores(guardId, trainingCourseId);
                if (existingGuardScrore.Count() > 0)
                {
                    id = existingGuardScrore.FirstOrDefault().Id;
                }
                _configDataProvider.SaveGuardTestScores(new GuardTrainingAndAssessmentScore()
                {
                    Id = id,
                    GuardId = guardId,
                    TrainingCourseId = trainingCourseId,
                    TotalQuestions = TotalQuestions,
                    guardCorrectQuestionsCount = guardCorrectQuestionsCount,
                    guardScore = guardScore,
                    IsPass = IsPass,
                    duration = duration

                });
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success,message });
        }
        public JsonResult OnGetGuardMarks(int guardId, int hrSettingsId, int tqNumberId)
        {
            int TotalQuestions = _configDataProvider.GetQuestionCount(hrSettingsId, tqNumberId);
            int trainingCourseId = _configDataProvider.GetTrainingCourses(hrSettingsId, tqNumberId).FirstOrDefault().Id;
            var existingGuardScrore = _configDataProvider.GetGuardScores(guardId, trainingCourseId);
            int guardCorrectQuestionsCount = 0;
            string guardScore = "0%";
            bool IsPass = false;
            if (existingGuardScrore.Count() >0)
            {

                guardCorrectQuestionsCount = existingGuardScrore.FirstOrDefault().guardCorrectQuestionsCount;

                 guardScore = existingGuardScrore.FirstOrDefault().guardScore;

                 IsPass = existingGuardScrore.FirstOrDefault().IsPass;
            }


            return new JsonResult(new { TotalQuestions, guardCorrectQuestionsCount, guardScore,IsPass });
        }
        public JsonResult OnPostDeleteGuardAttendedQuestions(int guardId, int hrSettingsId, int tqNumberId)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                int TotalQuestions = _configDataProvider.GetQuestionCount(hrSettingsId, tqNumberId);
                int trainingCourseId = _configDataProvider.GetTrainingCourses(hrSettingsId, tqNumberId).FirstOrDefault().Id;
                


                _configDataProvider.DeleteGuardAttendedQuestions(guardId,trainingCourseId);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }
        public JsonResult OnPostDeleteGuardScores(int guardId, int hrSettingsId, int tqNumberId)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                int TotalQuestions = _configDataProvider.GetQuestionCount(hrSettingsId, tqNumberId);
                int trainingCourseId = _configDataProvider.GetTrainingCourses(hrSettingsId, tqNumberId).FirstOrDefault().Id;


                _configDataProvider.DeleteGuardScores(guardId, trainingCourseId);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }
        public JsonResult OnPostReturnCourseTestStatusTostart(int guardId, int hrSettingsId, int tqNumberId)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                int TotalQuestions = _configDataProvider.GetQuestionCount(hrSettingsId, tqNumberId);
                int trainingCourseId = _configDataProvider.GetTrainingCourses(hrSettingsId, tqNumberId).FirstOrDefault().Id;


                var report=_configDataProvider.ReturnCourseTestStatusTostart(guardId, trainingCourseId);
                _configDataProvider.SaveGuardTrainingAndAssessmentTab(new GuardTrainingAndAssessment()
                {
                    Id = report.Id,
                    GuardId = guardId,
                    TrainingCourseId = trainingCourseId,
                    TrainingCourseStatusId = 1,
                    Description = report.Description,
                    HRGroupId = report.HRGroupId
                    //,
                    //IsCompleted = false

                });
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }
        public JsonResult OnGetGuardCertificate(int guardId, int hrSettingsId, int tqNumberId)
        {
            string input = GenerateFormattedString();
            string hashCode = GenerateHashCode(input);
            var getcertificateSatus = _configDataProvider.GetTQSettings(hrSettingsId).FirstOrDefault();
            var filename = _certificateGenerator.GeneratePdf(guardId, hrSettingsId, hashCode, getcertificateSatus.IsCertificateHoldUntilPracticalTaken, getcertificateSatus.IsCertificateWithQAndADump, getcertificateSatus.IsCertificateExpiry);
            DateTime? expirydate= DateTime.Now;
            if (getcertificateSatus.IsCertificateExpiry == true)
            {

                var expiryyears = _configDataProvider.GetTQSettings(hrSettingsId).Where(x => x.IsCertificateExpiry == true).FirstOrDefault().CertificateExpiryYears.Name;

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
                expirydate = null;
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
                DateType = false,
                Reminder1 = 45,

                Reminder2 = 7

            });
       
           


            //int guardCorrectQuestionsCount = existingGuardScrore.FirstOrDefault().guardCorrectQuestionsCount;

            //string guardScore = existingGuardScrore.FirstOrDefault().guardScore;

            //bool IsPass = existingGuardScrore.FirstOrDefault().IsPass;



            return new JsonResult(new { filename });
        }
        //To Generate Hash Code start
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

        //To Generate Hash Code stop
        public JsonResult OnPostGuardStartTest(int guardId, int hrSettingsId, int tqNumberId, int locationId)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                int trainingCourseId = _configDataProvider.GetTrainingCourses(hrSettingsId, tqNumberId).FirstOrDefault().Id;
                int id = 0;
                var result = _configDataProvider.GetGuardTrainingStartTest(guardId, trainingCourseId);
                if(result.Count()==0)
                {
                    id = 0;
                }
                else
                {
                    id = result.FirstOrDefault().Id;
                }
                _configDataProvider.SaveGuardTrainingStartTest(new GuardTrainingStartTest()
                {
                    Id = id,
                    GuardId = guardId,
                    TrainingCourseId = trainingCourseId,
                    ClassroomLocationId=locationId,
                    TestDate=DateTime.Now

                });
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }
        public JsonResult OnGetGuardCertificateAndfeedBackStatus(int guardId, int hrSettingsId, int tqNumberId)
        {
            string input = GenerateFormattedString();
            string hashCode = GenerateHashCode(input);
            var getcertificateSatus = _configDataProvider.GetTQSettings(hrSettingsId).FirstOrDefault();
            if (getcertificateSatus.IsCertificateHoldUntilPracticalTaken)
            {
                int TrainingCourseId = _configDataProvider.GetTrainingCourses(hrSettingsId, tqNumberId).FirstOrDefault().Id;
                var record = _guardDataProvider.GetGuardTrainingAndAssessment(guardId).Where(x => x.TrainingCourseId == TrainingCourseId).FirstOrDefault();
                _configDataProvider.SaveGuardTrainingAndAssessmentTab(new GuardTrainingAndAssessment()
                {
                    Id = record.Id,
                    GuardId = guardId,
                    TrainingCourseId = TrainingCourseId,
                    TrainingCourseStatusId = 3,
                    Description = record.Description,
                    HRGroupId = record.HRGroupId
                    //,
                    //IsCompleted = false

                });
            }
            else
            {
                int TrainingCourseId = _configDataProvider.GetTrainingCourses(hrSettingsId, tqNumberId).FirstOrDefault().Id;
                var record = _guardDataProvider.GetGuardTrainingAndAssessment(guardId).Where(x => x.TrainingCourseId == TrainingCourseId).FirstOrDefault();
                if (record != null)
                {
                    _configDataProvider.SaveGuardTrainingAndAssessmentTab(new GuardTrainingAndAssessment()
                    {
                        Id = record.Id,
                        GuardId = guardId,
                        TrainingCourseId = TrainingCourseId,
                        TrainingCourseStatusId = 4,
                        Description = record.Description,
                        HRGroupId = record.HRGroupId
                       // IsCompleted = true

                    });
                }
            }
            



            //int guardCorrectQuestionsCount = existingGuardScrore.FirstOrDefault().guardCorrectQuestionsCount;

            //string guardScore = existingGuardScrore.FirstOrDefault().guardScore;

            //bool IsPass = existingGuardScrore.FirstOrDefault().IsPass;



            return new JsonResult(new { getcertificateSatus });
        }
        public JsonResult OnGetGuardFeedbackQuestions(int hrSettingsId,int guardId)
        {
            var result = _configDataProvider.GetGuardFeedbackQuestions(hrSettingsId, guardId);

            return new JsonResult(result);
        }
        public JsonResult OnGetGuardFeedbackOptions(int questionId)
        {
            var result = _configDataProvider.GetGuardFeedbackOptions(questionId);

            return new JsonResult(result);
        }
        public JsonResult OnGetGuardFeedbackQuestionCount(int hrSettingsId,int guardId)
        {
            int TotalQuestions = _configDataProvider.GetFeedbackQuestionCount(hrSettingsId);
            var questionnumer = _configDataProvider.GetFeedbackQuestionNumber(hrSettingsId, guardId);
            var countid = questionnumer.Count();
            string Qno = string.Empty;
            if (countid <= TotalQuestions)
            {
                int numberOfDigits = countid / 10 + 1;
                if (numberOfDigits == 1)
                {
                    countid = countid + 1;
                    Qno = "0" + countid.ToString();
                }
            }
            return new JsonResult(new { TotalQuestions, Qno, countid });
        }
        public JsonResult OnPostSaveGuardFeedbackAnswers(GuardTrainingAttendedFeedbackQuestionsAndAnswers record)
        {


            var success = false;
            var message = string.Empty;
            try
            {





                _configDataProvider.SaveGuardFeedbackAnswers(new GuardTrainingAttendedFeedbackQuestionsAndAnswers()
                {
                    Id = record.Id,
                    GuardId = record.GuardId,
                    HrSettingsId = record.HrSettingsId,
                    TrainingTestFeedbackQuestionsId = record.TrainingTestFeedbackQuestionsId,
                    TrainingTestFeedbackQuestionsAnswersId = record.TrainingTestFeedbackQuestionsAnswersId

                });


                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }
        public JsonResult OnPostUpdateCourseTestStatusToHold(int guardId, int hrSettingsId, int tqNumberId)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                int TotalQuestions = _configDataProvider.GetQuestionCount(hrSettingsId, tqNumberId);
                int trainingCourseId = _configDataProvider.GetTrainingCourses(hrSettingsId, tqNumberId).FirstOrDefault().Id;


                var report = _configDataProvider.ReturnCourseTestStatusTostart(guardId, trainingCourseId);
                _configDataProvider.SaveGuardTrainingAndAssessmentTab(new GuardTrainingAndAssessment()
                {
                    Id = report.Id,
                    GuardId = guardId,
                    TrainingCourseId = trainingCourseId,
                    TrainingCourseStatusId = 3,
                    Description = report.Description,
                    HRGroupId = report.HRGroupId
                    //,
                    //IsCompleted = false

                });
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message,hrSettingsId });
        }

    }

}
