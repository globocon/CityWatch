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
        public int GuardId { get; set; }
        public int GuardTrainingAssessmentId { get; set; }
        public GuardTrainingAndAssessment GuardTrainingAndAssessment { get; set; }
        public TrainingTestQuestionSettings TrainingTestQuestionSettings { get; set; }
        public string CourseDocsPath;
        public string Coursefilename;
        public string hrreferencenumber;
       

        public GuardStartTestModel(IViewDataService viewDataService,
            IWebHostEnvironment webHostEnvironment,
            IPatrolDataReportService irChartDataService, IIncidentReportGenerator incidentReportGenerator, IConfigDataProvider configurationProvider, IClientDataProvider clientDataProvider, IOptions<Settings> settings, IGuardDataProvider guardDataProvider,
            IGuardLogDataProvider guardLogDataProvider)
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
        }
        public void OnGet()
        {
             GuardId = Convert.ToInt32(Request.Query["guid"]);
            GuardTrainingAssessmentId = Convert.ToInt32(Request.Query["guardCourseId"]);
            if(GuardTrainingAssessmentId!=0)
            {
                GuardTrainingAndAssessment = _guardDataProvider.GetGuardTrainingAndAssessment(GuardId).Where(x=>x.Id== GuardTrainingAssessmentId).FirstOrDefault();
                TrainingTestQuestionSettings= _configDataProvider.GetTQSettings(GuardTrainingAndAssessment.TrainingCourses.HRSettingsId).FirstOrDefault();
                var jresult = _configDataProvider.GetHRSettings().Where(x=>x.Id== GuardTrainingAndAssessment.TrainingCourses.HRSettingsId);
                var hrreferenceNumber = "HR"+ jresult.FirstOrDefault().ReferenceNoNumbers.Name + jresult.FirstOrDefault().ReferenceNoAlphabets.Name;
                var CourseDocsFolder = Path.Combine("TA", hrreferenceNumber, "Course", GuardTrainingAndAssessment.TrainingCourses.FileName);
                CourseDocsPath = CourseDocsFolder;
                hrreferencenumber = hrreferenceNumber;
                Coursefilename = GuardTrainingAndAssessment.TrainingCourses.FileName;

            }
            
        }
        public JsonResult OnGetGuardQuestions(int hrSettingsId, int tqNumberId)
        {
            var result = _configDataProvider.GetGuardQuestions(hrSettingsId, tqNumberId);

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
        public JsonResult OnGetGuardQuestionCount(int hrSettingsId, int tqNumberId)
        {
            int TotalQuestions = _configDataProvider.GetQuestionCount(hrSettingsId, tqNumberId);
            var questionnumer = _configDataProvider.GetQuestionNumber(hrSettingsId, tqNumberId);
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


            int guardCorrectQuestionsCount = existingGuardScrore.FirstOrDefault().guardCorrectQuestionsCount;

            string guardScore = existingGuardScrore.FirstOrDefault().guardScore;
           
            bool IsPass = existingGuardScrore.FirstOrDefault().IsPass;



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

                });
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }
    }
}
