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
    }
}
