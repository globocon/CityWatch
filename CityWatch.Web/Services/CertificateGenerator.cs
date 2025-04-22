using CityWatch.Common.Helpers;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Extensions;
using CityWatch.Web.Helpers;
using CityWatch.Web.Models;
using iText.Forms;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Pdf.Annot;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Text;
using IO = System.IO;
using iText.Pdfa;
using iText.Layout.Borders;

using System.Collections.Generic;
using CityWatch.Data;
using iText.Kernel.Pdf.Filespec;
using System.Net.Mail;
using System.IO;
using CityWatch.Data.Enums;
using CityWatch.Data.Services;
using Jering.Javascript.NodeJS;
using System.Reflection;
using System.ComponentModel;
using static Dropbox.Api.TeamLog.SpaceCapsType;
using static Dropbox.Api.TeamLog.LoginMethod;
using static Dropbox.Api.FileProperties.PropertiesSearchMode;
namespace CityWatch.Web.Services
{
    public interface ICertificateGenerator
    {
        string GeneratePdf(int guardId, int hrSettingsId,string hashCode,bool isCertificateHold,bool isCertificatewithQADump,bool isCertificateExpiry);

    }
    public class CertificateGenerator : ICertificateGenerator
    {
        private IncidentRequest _IncidentReport;
        private ClientSite _clientSite;

        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfigDataProvider _configDataProvider;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly Settings _settings;
        private readonly IConfiguration _configuration;
        private readonly ILogger<IncidentReportGenerator> _logger;

        private string _UploadRootDir;
        private readonly string _ReportRootDir;
        private readonly string _GpsMapRootDir;
        private readonly string _TemplatePdf;
        private const string TEMPLATE_DIR = "Template";
        private const string TEMPLATE_FILE_NAME = "IR_Form_Template.pdf";
        private const string REPORT_DIR = "Output";
        private const float MAX_IMAGE_WIDTH = 600;
        private const float MAX_IMAGE_HEIGHT = 800;
        private const float SCALE_FACTOR = 0.92f;
        private const int ROTATION_ANGLE_DEG = 270;
        private const string FONT_COLOR_BLACK = "#000000";
        private const string FONT_COLOR_BLUE = "#0000FF";
        private const float ATTACHMENT_BOX_HEIGHT = 20; // Added by binoy 0n 03-01-2024 under task id p1#160_MultimediaAttachments03012024
        private const float ATTACHMENT_BOX_WIDTH = 20; // Added by binoy 0n 03-01-2024 under task id p1#160_MultimediaAttachments03012024

        private const float CELL_FONT_SIZE = 6f;
        private const float CELL_FONT_SIZE_BIG = 10f;
        private const string COLOR_LIGHT_BLUE = "#d9e2f3";
        private readonly CityWatchDbContext _context;

        private const float PDF_DOC_MARGIN = 15f;

        private const string CELL_BG_GREEN = "#96e3ac";
        private const string CELL_BG_RED = "#ffcccc";
        private const string CELL_BG_YELLOW = "#fcf8d1";
        private const string CELL_BG_BLUE_HEADER = "#bdd7ee";
        private const string CELL_BG_YELLOW_IR_COUNT = "#feff9a";
        private const string CELL_BG_ORANGE_IR_ALARM = "#ffdab3";
        private const string CELL_FONT_GREEN = "#008000";
        private const string CELL_FONT_RED = "#FF0000";

        private readonly string _imageRootDir;
        private readonly string _siteImageRootDir;
        private readonly string _graphImageRootDir;
        private readonly IPatrolDataReportService _irChartDataService;
        private const string COLOR_WHITE = "#ffffff";
        private const string COLOR_GREY = "#666362";
        public CertificateGenerator(IWebHostEnvironment webHostEnvironment,
            IConfigDataProvider configDataProvider,
            IClientDataProvider clientDataProvider,
            IOptions<Settings> settings,
            IConfiguration configuration,
            ILogger<IncidentReportGenerator> logger,
            IPatrolDataReportService irChartDataService,
            CityWatchDbContext context, IGuardDataProvider guardDataProvider)
        {
            _configDataProvider = configDataProvider;
            _clientDataProvider = clientDataProvider;
            _guardDataProvider = guardDataProvider;
            _webHostEnvironment = webHostEnvironment;
            _settings = settings.Value;
            _configuration = configuration;
            _logger = logger;
            _context = context;

            //_ReportRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "Pdf");
            //_GpsMapRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "GpsImage");
            //_imageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "images");
            //// report output directory webroot\Pdf\Output
            //if (!IO.Directory.Exists(IO.Path.Combine(_ReportRootDir, REPORT_DIR)))
            //    IO.Directory.CreateDirectory(IO.Path.Combine(_ReportRootDir, REPORT_DIR));

            // pdf template directory webroot\Pdf\Template\IR_Form_Template.pdf
            _TemplatePdf = IO.Path.Combine(webHostEnvironment.WebRootPath, "TA");
            //if (!IO.File.Exists(_TemplatePdf))
            //    throw new IO.FileNotFoundException("Template file not found");
            //_irChartDataService = irChartDataService;
            //_graphImageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "GraphImage");
        }
        public string GeneratePdf(int guardId, int hrSettingsId,string hashCode, bool isCertificateHold, bool isCertificatewithQADump, bool isCertificateExpiry)

        {
            var guards = _guardDataProvider.GetGuards().Where(z => z.Id == guardId).FirstOrDefault();
            var licenseno= _guardDataProvider.GetGuards().Where(z => z.Id == guardId).FirstOrDefault().SecurityNo;
            var jresult = _configDataProvider.GetHRSettings().Where(x => x.Id == hrSettingsId);
            int trainingCourseId = _configDataProvider.GetTrainingCourses(hrSettingsId, 1).FirstOrDefault().Id;
            var hrreferenceNumber = "HR" + jresult.FirstOrDefault().ReferenceNoNumbers.Name + jresult.FirstOrDefault().ReferenceNoAlphabets.Name;
            var certificateName = _configDataProvider.GetCourseCertificateDocsUsingSettingsId(hrSettingsId).FirstOrDefault().FileName;
            string CertificateTemplatePath = IO.Path.Combine(_TemplatePdf, hrreferenceNumber, "Certificate", certificateName);
            var guardsstarttest = _configDataProvider.GetGuardTrainingStartTest(guardId, trainingCourseId).FirstOrDefault();
            int certificateId = _configDataProvider.GetCourseCertificateDocsUsingSettingsId(hrSettingsId).FirstOrDefault().Id;
            var certificateRPL=_configDataProvider.GetCourseCertificateRPLUsingId( certificateId).Where(x=>x.GuardId==guardId);
            //_IncidentReport = incidentReport;
            //_clientSite = clientSite;
            _UploadRootDir =  IO.Path.Combine(_webHostEnvironment.WebRootPath, "Uploads", "Guards", "License", licenseno);

            if (!IO.Directory.Exists(IO.Path.Combine(_UploadRootDir)))
                IO.Directory.CreateDirectory(IO.Path.Combine(_UploadRootDir));
            //string reportFileName = GetReportFileName(eventType);
            var reportPdf = IO.Path.Combine(_UploadRootDir ,certificateName);
            PdfDocument pdfDocument = new PdfDocument(new PdfReader(CertificateTemplatePath), new PdfWriter(reportPdf));

            PdfAcroForm acroForm = PdfAcroForm.GetAcroForm(pdfDocument, false);

            acroForm.GetField("Student").SetValue(guards.Name, true);
                acroForm.GetField("Location_theory").SetValue(guardsstarttest.TrainingLocation.Location, true);
                acroForm.GetField("DOI_theory").SetValue(guardsstarttest.TestDate.ToString("dd-MMM-yyyy"), true);
            var practicalresult= _configDataProvider.GetGuardTrainingPracticalDetails(guardId, hrSettingsId).FirstOrDefault();
            if (practicalresult == null)
            {
                acroForm.GetField("Location_practical").SetValue("", true);
                acroForm.GetField("DOI_practical").SetValue("", true);
            }
            else
            {
                if (acroForm.GetField("DOI_practical") != null)
                {
                    acroForm.GetField("DOI_practical").SetValue(practicalresult.PracticalDate.ToString("dd-MMM-yyyy"), true);
                }
                if (acroForm.GetField("Location_practical") != null)
                {
                    acroForm.GetField("Location_practical").SetValue(practicalresult.TrainingLocation.Location, true);
                }
            }
            
            if(certificateRPL.Count()>0)
            {
                if (acroForm.GetField("DOI_RPL_start") != null)
                {
                    acroForm.GetField("DOI_RPL_start").SetValue(certificateRPL.FirstOrDefault().AssessmentStartDate.ToString("dd-MMM-yyyy"), true);
                }
                if (acroForm.GetField("DOI_RPL_end") != null)
                {
                    acroForm.GetField("DOI_RPL_end").SetValue(certificateRPL.FirstOrDefault().AssessmentEndDate.ToString("dd-MMM-yyyy"), true);
                }
            }
            if (isCertificateExpiry)
            {
                var expiryyears = _configDataProvider.GetTQSettings(hrSettingsId).Where(x => x.IsCertificateExpiry == true).FirstOrDefault().CertificateExpiryYears.Name;
                string newexpiry = string.Empty;
                if (expiryyears.Contains("year"))
                     newexpiry = expiryyears.Replace("year", "");
                if (expiryyears.Contains("years"))
                    newexpiry = expiryyears.Replace("years", "");
                DateTime currentdate = DateTime.Now;
                DateTime futuredate = currentdate.AddYears(Convert.ToInt32(newexpiry));
                if (acroForm.GetField("DOE") != null)
                {
                    acroForm.GetField("DOE").SetValue(futuredate.ToString("dd-MMM-yyyy"), true);
                }
            }
            if (acroForm.GetField("HASH") != null)
            {
                acroForm.GetField("HASH").SetValue(hashCode, true);
            }
            if (acroForm.GetField("sign_off_name") != null)
            {
                acroForm.GetField("sign_off_name").SetValue("", true);
            }
            if (acroForm.GetField("sign_off_title") != null)
            {
                acroForm.GetField("sign_off_title").SetValue("", true);
            
            }

            acroForm.FlattenFields();
           
            AttachScoreCard(pdfDocument, guardId, hrSettingsId,certificateName);
            if (isCertificatewithQADump)
            {
                AttachQuestionsAndAnswers(pdfDocument,guardId,hrSettingsId, certificateName);
            }

            pdfDocument.Close();
            return certificateName;
        }
        private void AttachScoreCard(PdfDocument pdfDocument, int guardId, int hrSettingsId,string certificateName)
        {
            var reportPdf = IO.Path.Combine(_UploadRootDir, "ScoreCrd.pdf");
            var result = _configDataProvider.GetTrainingCoursesWithHrSettingsId(hrSettingsId);
            var pdfDoc = new PdfDocument(new PdfWriter(reportPdf));
            var pageSize = new PageSize(pdfDocument.GetLastPage().GetPageSize());
            var doc = new Document(pdfDoc, pageSize);
            doc.SetLeftMargin(PDF_DOC_MARGIN);
            doc.SetRightMargin(PDF_DOC_MARGIN);
            doc.Add(new Paragraph("Score Card")
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontSize(20)
                .SetBold()
                .SetMarginBottom(20));

            foreach (var item in result)
            {
                int TotalQuestions = _configDataProvider.GetQuestionCount(hrSettingsId, item.TQNumberId);
                int trainingCourseId = _configDataProvider.GetTrainingCourses(hrSettingsId, item.TQNumberId).FirstOrDefault().Id;
                string CourseName= _configDataProvider.GetTrainingCourses(hrSettingsId, item.TQNumberId).FirstOrDefault().FileName;
                var existingGuardScrore = _configDataProvider.GetGuardScores(guardId, trainingCourseId);


                int guardCorrectQuestionsCount = existingGuardScrore.FirstOrDefault().guardCorrectQuestionsCount;

                string guardScore = existingGuardScrore.FirstOrDefault().guardScore;

                bool IsPass = existingGuardScrore.FirstOrDefault().IsPass;
                doc.Add(CreateCertificateHeaderTable(CourseName, guardScore));


            }
            doc.Close();



            var uploadPdfName = IO.Path.Combine(_UploadRootDir, "ScoreCrd.pdf");
            var uploadDoc = new PdfDocument(new PdfReader(reportPdf));

         
              
            uploadDoc.CopyPagesTo(1, uploadDoc.GetNumberOfPages(), pdfDocument, pdfDocument.GetNumberOfPages()+1);
            uploadDoc.Close();
            FileInfo file = new FileInfo(reportPdf);
            if (file.Exists)//check file exsit or not  
            {
                file.Delete();
            }

        }
        private void AttachQuestionsAndAnswers( PdfDocument pdfDocument, int guardId, int hrSettingsId,string certificateName)
        {
            var reportPdf = IO.Path.Combine(_UploadRootDir, "QuestionBank.pdf");
            var result = _configDataProvider.GetTrainingCoursesWithHrSettingsId(hrSettingsId);
            var pdfDoc = new PdfDocument(new PdfWriter(reportPdf));
            var pageSize = new PageSize(pdfDocument.GetLastPage().GetPageSize());
            var doc = new Document(pdfDoc, pageSize);
            doc.SetLeftMargin(PDF_DOC_MARGIN);
            doc.SetRightMargin(PDF_DOC_MARGIN);
            doc.Add(new Paragraph("Question And Answers")
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontSize(20)
                .SetBold()
                .SetMarginBottom(20));
            //int questionno = 1;
            foreach (var item in result)
            {
                int questionno = 1;
                int trainingCourseId = _configDataProvider.GetTrainingCourses(hrSettingsId, item.TQNumberId).FirstOrDefault().Id;
                if (result.Count() > 1)
                {
                    string courseName = _configDataProvider.GetTrainingCourses(hrSettingsId, item.TQNumberId).FirstOrDefault().FileName;
                    doc.Add(new Paragraph(courseName)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontSize(19)
                .SetBold()
                .SetMarginBottom(20));

                }
                var attendedQuestions = _configDataProvider.GetGuardAttendedQuestionsAndanswers(guardId, trainingCourseId);
                if (attendedQuestions.Count() > 0)
                {
                    //int questionno = 1;
                    foreach (var attendedquestion in attendedQuestions)
                    {
                        int numberOfDigits = questionno / 10 + 1;
                        string Qno=string.Empty;
                        if (numberOfDigits == 1)
                        {
                         
                            Qno = "0" + questionno.ToString();
                        }
                        var question= new Paragraph("Q." + Qno +  "  " + attendedquestion.TrainingTestQuestions.Question).SetFontColor(WebColors.GetRGBColor(FONT_COLOR_BLACK)).SetFontSize(16)
                        .SetBold();
                        //question.SetFixedPosition(index, 5, pageSize.GetTop() - 40, x - 10);
                        doc.Add(question);
                        var choices = _configDataProvider.GetTrainingQuestionsAnswers(attendedquestion.TrainingTestQuestionsId);
                        List bulletList = new List()
                        .SetSymbolIndent(12)
                        .SetListSymbol("\u2022") // Unicode bullet point
                        .SetMarginLeft(20);
                        foreach (var choice in choices)
                        {
                            //doc.Add(new Paragraph(choice.Options)
                            //    .SetMarginLeft(20)
                            //    .SetFontSize(12));

                            bulletList.Add(new ListItem(choice.Options)).SetMarginLeft(20)
                               .SetFontSize(12);

                        }
                        doc.Add(bulletList);

                        // Add list items
                        
                    //    doc.Add(new Paragraph("Actual Answer")
                    //.SetTextAlignment(TextAlignment.LEFT)
                    //.SetFontSize(14)
                    //.SetBold()
                    //.SetMarginTop(30));
                        var actualanswer = _configDataProvider.GetTrainingQuestionsAnswers(attendedquestion.TrainingTestQuestionsId).Where(x=>x.IsAnswer==true).FirstOrDefault().Options;
                        //doc.Add(new Paragraph(actualanswer)
                        //        .SetMarginLeft(20)
                        //        .SetFontSize(12));
                        //                    doc.Add(new Paragraph("Actual Answer: " )
                        //.SetTextAlignment(TextAlignment.LEFT)
                        //.SetFontSize(14)
                        //.SetBold()
                        //.SetMarginTop(30)
                        ////.Add("\n") // Line break before the actual answer
                        //.Add(new Text(actualanswer).SetFontSize(12)));
                        doc.Add(new Paragraph()
                        .Add(new Text("Actual Answer: ").SetBold().SetFontSize(14)) // Bold only for the label
                        .Add(new Text(actualanswer).SetFontSize(12)) // Normal text for the answer
                        .SetTextAlignment(TextAlignment.LEFT)
                        .SetMarginTop(30));

                        var answer = attendedquestion.TrainingTestQuestionsAnswers.Options;

                        doc.Add(new Paragraph()
                       .Add(new Text("Student Answer: ").SetBold().SetFontSize(14)) // Bold only for the label
                       .Add(new Text(answer).SetFontSize(12)) // Normal text for the answer
                       .SetTextAlignment(TextAlignment.LEFT)
                       .SetMarginTop(30));
                        //    doc.Add(new Paragraph("Student Answer")
                        //.SetTextAlignment(TextAlignment.LEFT)
                        //.SetFontSize(14)
                        //.SetBold()
                        //.SetMarginTop(30));

                        //question.SetFixedPosition(index, 5, pageSize.GetTop() - 40, x - 10);
                        //doc.Add(new Paragraph(answer)
                        //         .SetMarginLeft(20)
                        //         .SetFontSize(12));

                        questionno++;
                    }
                }
                

            }
            doc.Close();



            var uploadPdfName = IO.Path.Combine(_UploadRootDir, "QuestionBank.pdf");
            var uploadDoc = new PdfDocument(new PdfReader(reportPdf));



            uploadDoc.CopyPagesTo(1, uploadDoc.GetNumberOfPages(), pdfDocument, pdfDocument.GetNumberOfPages() + 1);
            uploadDoc.Close();
            FileInfo file = new FileInfo(reportPdf);
            if (file.Exists)//check file exsit or not  
            {
                file.Delete();
            }






        }
        private static Table CreateCertificateHeaderTable(string CourseName,string guardScore)
        {
            var siteDataTable = new Table(UnitValue.CreatePercentArray(new float[] {  10, 23 })).UseAllAvailableWidth();

            siteDataTable.AddCell(GetCertificateHeaderCell("Course Name:"));
            siteDataTable.AddCell(GetCertificateValueCell(CourseName));

            siteDataTable.AddCell(GetCertificateHeaderCell("Score Obtained:"));
            siteDataTable.AddCell(GetCertificateValueCell(guardScore));


            siteDataTable.SetMarginBottom(20);


            return siteDataTable;
        }
        private static Cell GetCertificateValueCell(string text)
        {
            return new Cell()
               .Add(new Paragraph().Add(new Text(text)))
               .SetFont(PdfHelper.GetPdfFont())
               .SetFontSize(12)
               .SetTextAlignment(TextAlignment.CENTER)
               .SetHorizontalAlignment(HorizontalAlignment.CENTER)
               .SetVerticalAlignment(VerticalAlignment.MIDDLE);
        }
        private static Cell GetCertificateHeaderCell(string text)
        {
            return new Cell()
                    .Add(new Paragraph().Add(new Text(text)))
                    .SetFont(PdfHelper.GetPdfFont())
                    .SetFontSize(14)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE).SetBold();
                   // .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE));
        }
        private static Cell GetSerialNoValueCell(string text)
        {
            return new Cell()
               .Add(new Paragraph().Add(new Text(text)))
               .SetFont(PdfHelper.GetPdfFont())
               .SetFontSize(CELL_FONT_SIZE_BIG)
               .SetFontColor(WebColors.GetRGBColor("#FF323A"))
               .SetHorizontalAlignment(HorizontalAlignment.CENTER)
               .SetVerticalAlignment(VerticalAlignment.MIDDLE);
        }



    }
}
