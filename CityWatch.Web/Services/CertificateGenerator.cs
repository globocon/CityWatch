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
using static Dropbox.Api.Sharing.ListFileMembersIndividualResult;
using DocumentFormat.OpenXml.Bibliography;
using static Dropbox.Api.TeamLog.PaperDownloadFormat;
using System.Text.RegularExpressions;
using CityWatch.Common.Models;
using System.Threading.Tasks;
using CityWatch.Common.Services;
using static Dropbox.Api.Sharing.MemberSelector;
namespace CityWatch.Web.Services
{
    public interface ICertificateGenerator
    {
        string GeneratePdf(int guardId, int hrSettingsId,string hashCode,bool isCertificateHold,bool isCertificatewithQADump,bool isCertificateExpiry);
        string GenerateGuardFeedbackPdf(int guardId, int hrSettingsId);

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
        private readonly IDropboxService _dropboxUploadService;
        public CertificateGenerator(IWebHostEnvironment webHostEnvironment,
            IConfigDataProvider configDataProvider,
            IClientDataProvider clientDataProvider,
            IOptions<Settings> settings,
            IConfiguration configuration,
            ILogger<IncidentReportGenerator> logger,
            IPatrolDataReportService irChartDataService,
            CityWatchDbContext context, IGuardDataProvider guardDataProvider, IDropboxService dropboxUploadService)
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
            _dropboxUploadService = dropboxUploadService;
        }
        public string GeneratePdf(int guardId, int hrSettingsId,string hashCode, bool isCertificateHold, bool isCertificatewithQADump, bool isCertificateExpiry)

        {
            /* Every lookup below was dereferenced straight off FirstOrDefault(). Any gap in the course
               setup therefore surfaced as a bare "Object reference not set to an instance of an object",
               which tells the operator nothing about which piece is missing - and the Bulk Certificate
               Release shows that message verbatim against the guard. Same lookups in the same order,
               but each one now says what is not configured. */
            var guards = _guardDataProvider.GetGuards().Where(z => z.Id == guardId).FirstOrDefault();
            if (guards == null)
                throw new InvalidOperationException($"Guard {guardId} was not found, so a certificate cannot be generated.");
            var licenseno = guards.SecurityNo;

            var jresult = _configDataProvider.GetHRSettings().Where(x => x.Id == hrSettingsId);
            var hrSettings = jresult.FirstOrDefault();
            if (hrSettings == null)
                throw new InvalidOperationException($"Course {hrSettingsId} was not found, so a certificate cannot be generated.");

            var firstTrainingCourse = _configDataProvider.GetTrainingCourses(hrSettingsId, 1).FirstOrDefault();
            if (firstTrainingCourse == null)
                throw new InvalidOperationException($"No training course is set up for '{hrSettings.Description}', so a certificate cannot be generated.");
            int trainingCourseId = firstTrainingCourse.Id;

            var hrreferenceNumber = "HR" + hrSettings.ReferenceNoNumbers?.Name + hrSettings.ReferenceNoAlphabets?.Name;

            var certificateDocument = _configDataProvider.GetCourseCertificateDocsUsingSettingsId(hrSettingsId).FirstOrDefault();
            if (certificateDocument == null)
                throw new InvalidOperationException($"No certificate document is uploaded for '{hrSettings.Description}', so a certificate cannot be generated.");
            var certificateName = certificateDocument.FileName;
            var extension = ".pdf";
            
            
            
            string CertificateTemplatePath = IO.Path.Combine(_TemplatePdf, hrreferenceNumber, "Certificate", certificateName);
            var guardsstarttest = _configDataProvider.GetGuardTrainingStartTest(guardId, trainingCourseId).FirstOrDefault();
            int certificateId = certificateDocument.Id;
            var certificateRPL=_configDataProvider.GetCourseCertificateRPLUsingId( certificateId).Where(x=>x.GuardId==guardId);
            //_IncidentReport = incidentReport;
            //_clientSite = clientSite;
            _UploadRootDir =  IO.Path.Combine(_webHostEnvironment.WebRootPath, "Uploads", "Guards", "License", licenseno);

            if (!IO.Directory.Exists(IO.Path.Combine(_UploadRootDir)))
                IO.Directory.CreateDirectory(IO.Path.Combine(_UploadRootDir));
            //string reportFileName = GetReportFileName(eventType);
            // Ensure any existing file with the same name is deleted to avoid caching/replacement issues
            if (File.Exists(IO.Path.Combine(_UploadRootDir, certificateName)))
            {
                File.Delete(IO.Path.Combine(_UploadRootDir, certificateName));
            }
            var reportPdf = IO.Path.Combine(_UploadRootDir, certificateName);
            PdfDocument pdfDocument = new PdfDocument(new PdfReader(CertificateTemplatePath), new PdfWriter(reportPdf));

            // Set NeedAppearances to true to ensure PDF viewers correctly render the form fields
            PdfAcroForm acroForm = PdfAcroForm.GetAcroForm(pdfDocument, true);
            acroForm.SetNeedAppearances(true);

            acroForm.GetField("Student").SetValue(guards.Name, true);
            if (guardsstarttest != null)
            {
                if (acroForm.GetField("Location_theory") != null)
                {
                    acroForm.GetField("Location_theory").SetValue(guardsstarttest.TrainingLocation.Location, true);
                }
                if (acroForm.GetField("DOI_theory") != null)
                {
                    acroForm.GetField("DOI_theory").SetValue(guardsstarttest.TestDate.ToString("dd-MMM-yyyy"), true);
                }
            }
            var practicalresult = _configDataProvider.GetGuardTrainingPracticalDetails(guardId, hrSettingsId).LastOrDefault();
            if (practicalresult == null)
            {
                if (acroForm.GetField("Location_practical") != null)
                {
                    acroForm.GetField("Location_practical").SetValue("", true);
                }
                if (acroForm.GetField("DOI_practical") != null)
                {
                    acroForm.GetField("DOI_practical").SetValue("", true);
                }
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
                if (acroForm.GetField("sign_off_name") != null)
                {
                    acroForm.GetField("sign_off_name").SetValue(practicalresult.TrainingInstructor.Name, true);
                }
                if (acroForm.GetField("sign_off_title") != null)
                {
                    acroForm.GetField("sign_off_title").SetValue(practicalresult.TrainingInstructor.Position, true);
                }
            }

            // Materialize the list to avoid multiple queries and ensure consistent data mapping for RPL
            var certificateRPLList = certificateRPL.ToList();
            if (certificateRPLList.Count > 0)
            {
                var rplRecord = certificateRPLList.LastOrDefault();
                // Map AssessmentStartDate to DOI_RPL_start (existing) and DOI_RPL (new requirement)
                if (acroForm.GetField("DOI_RPL_start") != null)
                {
                    acroForm.GetField("DOI_RPL_start").SetValue(rplRecord.AssessmentStartDate.ToString("dd-MMM-yyyy"), true);
                }
                if (acroForm.GetField("DOI_RPL") != null)
                {
                    acroForm.GetField("DOI_RPL").SetValue(rplRecord.AssessmentStartDate.ToString("dd-MMM-yyyy"), true);
                }
                if (acroForm.GetField("DOI_RPL_end") != null)
                {
                    acroForm.GetField("DOI_RPL_end").SetValue(rplRecord.AssessmentEndDate.ToString("dd-MMM-yyyy"), true);
                }
                if (acroForm.GetField("sign_off_name") != null)
                {
                    acroForm.GetField("sign_off_name").SetValue(rplRecord.TrainingInstructor.Name, true);
                }
                if (acroForm.GetField("sign_off_title") != null)
                {
                    acroForm.GetField("sign_off_title").SetValue(rplRecord.TrainingInstructor.Position, true);
                }
                if (acroForm.GetField("DOI_practical") != null)
                {
                    acroForm.GetField("DOI_practical").SetValue(rplRecord.AssessmentEndDate.ToString("dd-MMM-yyyy"), true);
                }
                if (acroForm.GetField("Location_practical") != null)
                {
                    acroForm.GetField("Location_practical").SetValue(rplRecord.TrainingLocation.Location, true);
                }
                if (acroForm.GetField("Location_theory") != null)
                {
                    acroForm.GetField("Location_theory").SetValue(rplRecord.TrainingTheoryLocation.Location, true);
                }
                if (acroForm.GetField("DOI_theory") != null)
                {
                    acroForm.GetField("DOI_theory").SetValue(rplRecord.AssessmentStartDate.ToString("dd-MMM-yyyy"), true);
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
            

            acroForm.FlattenFields();
            if(isCertificateHold==true)
            {
                if(practicalresult !=null && practicalresult.FileName != null)
                {
                    if (GetAttachmentType(IO.Path.GetExtension(practicalresult.FileName)) == AttachmentType.Pdf)
                    {
                        var uploadPdfName = IO.Path.Combine(_UploadRootDir, "CertificateDocuments", jresult.FirstOrDefault().Description, practicalresult.FileName);
                        var uploadDoc = new PdfDocument(new PdfReader(uploadPdfName));
                        uploadDoc.CopyPagesTo(1, uploadDoc.GetNumberOfPages(), pdfDocument, pdfDocument.GetNumberOfPages() + 1);
                        //var standardPageSize = pdfDocument.GetPage(1).GetPageSize();

                        //for (int i =  1; i <= pdfDocument.GetNumberOfPages(); i++)
                        //{
                        //    var page = pdfDocument.GetPage(i);
                        //    page.SetMediaBox(standardPageSize); // Resize the media box to match
                        //    page.SetCropBox(standardPageSize);  // (Optional) Set crop box if needed
                        //}
                        uploadDoc.Close();
                    }
                    if (GetAttachmentType(IO.Path.GetExtension(practicalresult.FileName)) == AttachmentType.Image)
                    {
                        var doc = new Document(pdfDocument);
                        var image = AttachImageToPdf(pdfDocument, pdfDocument.GetNumberOfPages() + 1, IO.Path.Combine(_UploadRootDir, "CertificateDocuments", jresult.FirstOrDefault().Description, practicalresult.FileName));
                        //paraName.SetFixedPosition(index, 5, 0, 400);
                        doc.Add(image);
                    }
                }

            }
            if (certificateRPL.Count() == 0)
            {
                AttachScoreCard(pdfDocument, guardId, hrSettingsId, certificateName);
                if (isCertificatewithQADump)
                {
                    AttachQuestionsAndAnswers(pdfDocument, guardId, hrSettingsId, certificateName);
                }
            }
            else
            {
                if (certificateRPL.LastOrDefault() != null && certificateRPL.LastOrDefault().FileName != null)
                {
                    if (GetAttachmentType(IO.Path.GetExtension(certificateRPL.LastOrDefault().FileName)) == AttachmentType.Pdf)
                    {
                        var uploadPdfName = IO.Path.Combine(_UploadRootDir, "RPLCertificateDocuments", jresult.FirstOrDefault().Description, certificateRPL.LastOrDefault().FileName);
                        var uploadDoc = new PdfDocument(new PdfReader(uploadPdfName));
                        uploadDoc.CopyPagesTo(1, uploadDoc.GetNumberOfPages(), pdfDocument, pdfDocument.GetNumberOfPages() + 1);
                        //var standardPageSize = pdfDocument.GetPage(1).GetPageSize();

                        //for (int i =  1; i <= pdfDocument.GetNumberOfPages(); i++)
                        //{
                        //    var page = pdfDocument.GetPage(i);
                        //    page.SetMediaBox(standardPageSize); // Resize the media box to match
                        //    page.SetCropBox(standardPageSize);  // (Optional) Set crop box if needed
                        //}
                        uploadDoc.Close();
                    }
                    if (GetAttachmentType(IO.Path.GetExtension(certificateRPL.LastOrDefault().FileName)) == AttachmentType.Image)
                    {
                        var doc = new Document(pdfDocument);
                        var image = AttachImageToPdf(pdfDocument, pdfDocument.GetNumberOfPages() + 1, IO.Path.Combine(_UploadRootDir, "RPLCertificateDocuments", jresult.FirstOrDefault().Description, certificateRPL.LastOrDefault().FileName));
                        //paraName.SetFixedPosition(index, 5, 0, 400);
                        doc.Add(image);
                    }
                }
            }
            pdfDocument.Close();
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
                // DateTime parsedDate = DateTime.Parse(futuredate);
                var formattedDate = futuredate.ToString("dd MMM yy").ToUpper();
                var newFileName = jresult.FirstOrDefault().Description + "-" + "exp " + formattedDate + extension;
                var fileName = jresult.FirstOrDefault().ReferenceNoNumbers.Name + jresult.FirstOrDefault().ReferenceNoAlphabets.Name + "_" + newFileName;
                fileName = GetFilename(fileName);
                reportPdf = IO.Path.Combine(_UploadRootDir, certificateName);
                var destinationfilename= IO.Path.Combine(_UploadRootDir, fileName);
                var DropboxDir = _guardDataProvider.GetDrobox();
                if (!File.Exists(destinationfilename))
                {
                    File.Move(reportPdf, destinationfilename);
                    File.Delete(reportPdf);
                    var dbxFilePath = FileNameHelper.GetSanitizedDropboxFileNamePart($"{GuardHelper.GetGuardDocumentDbxRootFolderNew(guards, DropboxDir.DropboxDir)}/{fileName}");
                    UpoadDocumentToDropbox(destinationfilename, dbxFilePath);
                }
                else
                {
                    File.Delete(destinationfilename);
                    File.Move(reportPdf, destinationfilename);
                    File.Delete(reportPdf);
                    var dbxFilePath = FileNameHelper.GetSanitizedDropboxFileNamePart($"{GuardHelper.GetGuardDocumentDbxRootFolderNew(guards, DropboxDir.DropboxDir)}/{fileName}");
                    UpoadDocumentToDropbox(destinationfilename, dbxFilePath);
                }
                certificateName = fileName;
            }
            else
            {
                DateTime currentdate = DateTime.Now;
                // DateTime parsedDate = DateTime.Parse(futuredate);
                var formattedDate = currentdate.ToString("dd MMM yy").ToUpper();
                var newFileName = string.Empty;
                if (certificateRPL.Count() > 0)
                {

                    newFileName = jresult.FirstOrDefault().Description + "-" + "doi " + certificateRPL.LastOrDefault().AssessmentEndDate.ToString("dd-MMM-yyyy") + extension;
                }
                else
                {
                    newFileName = jresult.FirstOrDefault().Description + "-" + "doi " + formattedDate + extension;
                }
                var fileName = jresult.FirstOrDefault().ReferenceNoNumbers.Name + jresult.FirstOrDefault().ReferenceNoAlphabets.Name + "_" + newFileName;
                fileName = GetFilename(fileName);
                reportPdf = IO.Path.Combine(_UploadRootDir, certificateName);
                var destinationfilename = IO.Path.Combine(_UploadRootDir, fileName);
                if (!File.Exists(destinationfilename))
                {
                    File.Move(reportPdf, destinationfilename);
                    File.Delete(reportPdf);
                    var dbxFilePath = FileNameHelper.GetSanitizedDropboxFileNamePart($"{GuardHelper.GetGuardDocumentDbxRootFolder(guards)}/{fileName}");
                    UpoadDocumentToDropbox(destinationfilename, dbxFilePath);
                }
                else
                {
                    File.Delete(destinationfilename);
                    File.Move(reportPdf, destinationfilename);
                    File.Delete(reportPdf);
                    var dbxFilePath = FileNameHelper.GetSanitizedDropboxFileNamePart($"{GuardHelper.GetGuardDocumentDbxRootFolder(guards)}/{fileName}");
                    UpoadDocumentToDropbox(destinationfilename, dbxFilePath);
                }
                certificateName = fileName;
            }
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
                var trainingCourse = _configDataProvider.GetTrainingCourses(hrSettingsId, item.TQNumberId).FirstOrDefault();
                if (trainingCourse == null)
                    continue;

                int trainingCourseId = trainingCourse.Id;
                string CourseName = trainingCourse.FileName;

                /* A guard can be issued a certificate without ever sitting the online test - that is
                   what the admin release and the Bulk Certificate Release do - and then there is no
                   GuardTrainingAndAssessmentScore row at all. FirstOrDefault() was dereferenced three
                   times unguarded, so the whole certificate blew up here with a NullReferenceException
                   (the failure reported for Bruno Timpano and John Remington on "Thermal Camera (FLIR Ti)").
                   The score card still lists the course; the score is simply left blank.
                   guardCorrectQuestionsCount and IsPass were read but never used - dropped. */
                var existingGuardScrore = _configDataProvider.GetGuardScores(guardId, trainingCourseId).FirstOrDefault();
                string guardScore = existingGuardScrore?.guardScore ?? string.Empty;

                doc.Add(CreateCertificateHeaderTable(CourseName ?? string.Empty, guardScore));
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
                var trainingCourse = _configDataProvider.GetTrainingCourses(hrSettingsId, item.TQNumberId).FirstOrDefault();
                if (trainingCourse == null)
                    continue;
                int trainingCourseId = trainingCourse.Id;
                if (result.Count() > 1)
                {
                    string courseName = trainingCourse.FileName;
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
                        else
                        {
                            Qno = questionno.ToString();
                        }
                        var qnaBlock = new Div();
                        qnaBlock.SetKeepTogether(true); // Keep the entire block on the same page


                        var question = new Paragraph("Q." + Qno +  "  " + attendedquestion.TrainingTestQuestions.Question).SetFontColor(WebColors.GetRGBColor(FONT_COLOR_BLACK)).SetFontSize(16)
                        .SetBold();
                        qnaBlock.Add(question);
                        //question.SetFixedPosition(index, 5, pageSize.GetTop() - 40, x - 10);
                        //doc.Add(question);
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
                        qnaBlock.Add(bulletList);

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
                        qnaBlock.Add(new Paragraph()
                        .Add(new Text("Actual Answer: ").SetBold().SetFontSize(14)) // Bold only for the label
                        .Add(new Text(actualanswer).SetFontSize(12)) // Normal text for the answer
                        .SetTextAlignment(TextAlignment.LEFT)
                        .SetMarginTop(30));

                        var answer = attendedquestion.TrainingTestQuestionsAnswers.Options;

                        qnaBlock.Add(new Paragraph()
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
                        doc.Add(qnaBlock);
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


        public string GenerateGuardFeedbackPdf(int guardId, int hrSettingsId)
        {
            var version = "v" + Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var reportPdf = GetReportPdfFilePath(guardId, hrSettingsId,version);
            var pdfDoc = new PdfDocument(new PdfWriter(reportPdf));

            pdfDoc.SetDefaultPageSize(PageSize.A4.Rotate());
            var doc = new Document(pdfDoc);
            doc.SetMargins(15f, 30f, 40f, 30f);
            doc.SetLeftMargin(PDF_DOC_MARGIN);
            doc.SetRightMargin(PDF_DOC_MARGIN);
            doc.Add(new Paragraph("Feedback Question And Answers")
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontSize(20)
                .SetBold()
                .SetMarginBottom(20));
            //int questionno = 1;
           
                int questionno = 1;
                
                var attendedQuestions = _configDataProvider.GetGuardAttendedFeedBackQuestionsAndanswers(guardId,hrSettingsId);
                if (attendedQuestions.Count() > 0)
                {
                    //int questionno = 1;
                    foreach (var attendedquestion in attendedQuestions)
                    {
                        int numberOfDigits = questionno / 10 + 1;
                        string Qno = string.Empty;
                        if (numberOfDigits == 1)
                        {

                            Qno = "0" + questionno.ToString();
                        }
                        else
                        {
                            Qno = questionno.ToString();
                        }
                        var question = new Paragraph("Q." + Qno + "  " + attendedquestion.TrainingTestFeedbackQuestions.Question).SetFontColor(WebColors.GetRGBColor(FONT_COLOR_BLACK)).SetFontSize(16)
                        .SetBold();
                        //question.SetFixedPosition(index, 5, pageSize.GetTop() - 40, x - 10);
                        doc.Add(question);
                       
                        

                        var answer = attendedquestion.TrainingTestFeedbackQuestionsAnswers.Options;

                        doc.Add(new Paragraph()
                       .Add(new Text("Answer: ").SetBold().SetFontSize(14)) // Bold only for the label
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


            

            //var headerTable = CreateReportHeaderTable( guardId,  hrSettingsId);
            //doc.Add(headerTable);

            //var reportSummaryTable = CreateReportDataTable(keyVehicleLogs);
            //doc.Add(reportSummaryTable);

            //var totalEventCountTable = CreateEventCountTable(keyVehicleLogs.Count());
            //doc.Add(totalEventCountTable);


            doc.Close();
            pdfDoc.Close();

            return IO.Path.GetFileName(reportPdf);
        }
        
        private string GetReportPdfFilePath(int guardId, int hrSettingsId, string version)
        {
            var securitylicense = _guardDataProvider.GetActiveGuards().Where(x => x.Id == guardId).FirstOrDefault().SecurityNo;
            var courseDetails = _configDataProvider.GetHRSettings().Where(x => x.Id == hrSettingsId).FirstOrDefault();
            //if (!Directory.Exists(reportPdfPath))
            //{
            //    Directory.CreateDirectory(reportPdfPath);
            //}
            if (!Directory.Exists(IO.Path.Combine(_webHostEnvironment.WebRootPath, "TA", "Feedback", securitylicense)))
                Directory.CreateDirectory(IO.Path.Combine(_webHostEnvironment.WebRootPath, "TA", "Feedback", securitylicense));
            var reportPdfPath = IO.Path.Combine(_webHostEnvironment.WebRootPath,"TA","Feedback", securitylicense, courseDetails.Description + " - " + DateTime.Now.Date.ToString("dd-MMM-yyy") + ".pdf");

            //if (IO.File.Exists(reportPdfPath))
            //    IO.File.Delete(reportPdfPath);

            return reportPdfPath;
        }
        private Table CreateReportHeaderTable(int guardId, int hrSettingsId)
        {
            var guarddetails = _guardDataProvider.GetActiveGuards().Where(x => x.Id == guardId).FirstOrDefault();
            var courseDetails = _configDataProvider.GetHRSettings().Where(x => x.Id == hrSettingsId).FirstOrDefault();
            var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 5, 10, 10, 25, 10, 25, 10, 5 })).UseAllAvailableWidth();

            

            headerTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));

            var columnName = new Cell()
                .Add(new Paragraph().Add(new Text("Guard Name:")))
                .SetFont(PdfHelper.GetPdfFont())
                .SetFontSize(CELL_FONT_SIZE)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE));
            headerTable.AddCell(columnName);

            var guardName = new Cell()
                .Add(new Paragraph().Add(new Text(guarddetails.Name)
                .SetFont(PdfHelper.GetPdfFont())))
                .SetFontSize(CELL_FONT_SIZE)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER);
            headerTable.AddCell(guardName);

            var columnCourse = new Cell()
                .Add(new Paragraph().Add(new Text("Course:")))
                .SetFont(PdfHelper.GetPdfFont())
                .SetFontSize(CELL_FONT_SIZE)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE));
            headerTable.AddCell(columnCourse);

            var courseDone = new Cell()
                .Add(new Paragraph().Add(new Text(courseDetails.Description)))
                .SetFont(PdfHelper.GetPdfFont())
                .SetFontSize(CELL_FONT_SIZE)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE);
            headerTable.AddCell(courseDone);

            var columnCourseCompletedDate = new Cell()
               .Add(new Paragraph().Add(new Text("Date:")))
               .SetFont(PdfHelper.GetPdfFont())
               .SetFontSize(CELL_FONT_SIZE)
               .SetTextAlignment(TextAlignment.CENTER)
               .SetHorizontalAlignment(HorizontalAlignment.CENTER)
               .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE));
            headerTable.AddCell(columnCourseCompletedDate);

            var courseCompletedDate = new Cell()
                .Add(new Paragraph().Add(new Text(courseDetails.Description)))
                .SetFont(PdfHelper.GetPdfFont())
                .SetFontSize(CELL_FONT_SIZE)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE);
            headerTable.AddCell(courseCompletedDate);





            return headerTable;
        }
        private AttachmentType GetAttachmentType(string extn)
        {
            if (".jpg,.jpeg,.png,.bmp".IndexOf(extn.ToLower()) >= 0)
                return AttachmentType.Image;

            if (".pdf".IndexOf(extn.ToLower()) >= 0)
                return AttachmentType.Pdf;

            // Added by binoy 0n 03-01-2024 under task id p1#160_MultimediaAttachments03012024
            if (".mp4,.avi,.mp3".IndexOf(extn.ToLower()) >= 0)
                return AttachmentType.Multimedia;

            // Added by binoy 0n 03-06-2024 under task P1 #215
            if (".xlsx".IndexOf(extn.ToLower()) >= 0)
                return AttachmentType.Excel;

            return AttachmentType.Unknown;
        }
        private Image AttachImageToPdf(PdfDocument pdfDocument, int index, string imagePath)
        {
            var pageSize = new PageSize(pdfDocument.GetFirstPage().GetPageSize());
            pdfDocument.AddNewPage(index, pageSize);
            var imageData = ImageDataFactory.Create(imagePath);
            var image = new Image(imageData);
            bool rotateImage = image.GetImageWidth() > image.GetImageHeight();
            bool scaleImage = image.GetImageWidth() > MAX_IMAGE_WIDTH || image.GetImageHeight() > MAX_IMAGE_HEIGHT;

            if (rotateImage)
            {
                image.SetRotationAngle(ROTATION_ANGLE_DEG * (Math.PI / 180));
                if (scaleImage)
                    image.ScaleToFit(PageSize.A4.GetHeight() * SCALE_FACTOR, PageSize.A4.GetWidth() * SCALE_FACTOR);
            }
            else
            {
                if (scaleImage)
                    image.ScaleToFit(PageSize.A4.GetWidth() * SCALE_FACTOR, PageSize.A4.GetHeight() * SCALE_FACTOR);
            }

            var bottom = rotateImage ? pageSize.GetTop() : pageSize.GetTop() - image.GetImageScaledHeight();
            image.SetFixedPosition(index, 0, bottom);
            return image;
        }
        public string GetFilename(string filename)
        {
            // Use Regex to replace problematic characters with an underscore
            string newFilename = Regex.Replace(filename, @"[\/\\?%*:|""<>]", "_");
            return newFilename;
        }
        private bool UpoadDocumentToDropbox(string fileToUpload, string dbxFilePath)
        {
            var dropboxSettings = new DropboxSettings(_settings.DropboxAppKey, _settings.DropboxAppSecret, _settings.DropboxAccessToken,
                                                        _settings.DropboxRefreshToken, _settings.DropboxUserEmail);

            bool uploaded = false;
            try
            {
                uploaded = Task.Run(() => _dropboxUploadService.Upload(dropboxSettings, fileToUpload, dbxFilePath)).Result;
                //if (uploaded && System.IO.File.Exists(fileToUpload))
                //    System.IO.File.Delete(fileToUpload);
            }
            catch
            {
            }

            return uploaded;
        }

    }
}
