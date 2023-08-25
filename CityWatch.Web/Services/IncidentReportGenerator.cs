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
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Text;
using IO = System.IO;

namespace CityWatch.Web.Services
{
    public enum AttachmentType
    {
        Unknown = 0,
        Image = 1,
        Pdf = 2,
    }

    public interface IIncidentReportGenerator
    {
        string GeneratePdf(IncidentRequest incidentReport, ClientSite clientSite);

    }

    public class IncidentReportGenerator : IIncidentReportGenerator
    {
        private IncidentRequest _IncidentReport;
        private ClientSite _clientSite;        

        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfigDataProvider _configDataProvider;
        private readonly IClientDataProvider _clientDataProvider;
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
        
        public IncidentReportGenerator(IWebHostEnvironment webHostEnvironment,
            IConfigDataProvider configDataProvider,
            IClientDataProvider clientDataProvider,
            IOptions<Settings> settings,
            IConfiguration configuration,
            ILogger<IncidentReportGenerator> logger)
        {
            _configDataProvider = configDataProvider;
            _clientDataProvider = clientDataProvider;
            _webHostEnvironment = webHostEnvironment;
            _settings = settings.Value;
            _configuration = configuration;
            _logger = logger;

            _ReportRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "Pdf");
            _GpsMapRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "GpsImage");
            
            // report output directory webroot\Pdf\Output
            if (!IO.Directory.Exists(IO.Path.Combine(_ReportRootDir, REPORT_DIR)))
                IO.Directory.CreateDirectory(IO.Path.Combine(_ReportRootDir, REPORT_DIR));

            // pdf template directory webroot\Pdf\Template\IR_Form_Template.pdf
            _TemplatePdf = IO.Path.Combine(_ReportRootDir, TEMPLATE_DIR, TEMPLATE_FILE_NAME);
            if (!IO.File.Exists(_TemplatePdf))
                throw new IO.FileNotFoundException("Template file not found");
        }

        public string GeneratePdf(IncidentRequest incidentReport, ClientSite clientSite)
        {
            _IncidentReport = incidentReport;
            _clientSite = clientSite;
            _UploadRootDir = IO.Path.Combine(_webHostEnvironment.WebRootPath, "Uploads", incidentReport.ReportReference);

            var editableFields = PdfFormHelper.GetPdfFormFields();
            var eventType = string.Empty;
            foreach (var field in editableFields)
            {
                if (field.PropName.StartsWith("EventType."))
                {
                    var propValue = GetPopertyValue(field);
                    if (propValue == "Yes")
                    {
                        eventType = field.Name;
                        break;
                    }
                }
            }
            string reportFileName = GetReportFileName(eventType);
            var reportPdf = IO.Path.Combine(_ReportRootDir, REPORT_DIR, reportFileName);
            PdfDocument pdfDocument = new PdfDocument(new PdfReader(_TemplatePdf), new PdfWriter(reportPdf));

            PdfAcroForm acroForm = PdfAcroForm.GetAcroForm(pdfDocument, false);

            acroForm.GetField("IR-NO9").SetValue("", false);
            acroForm.GetField("IR-NO").SetValue("", false);
            acroForm.GetField("IR-NO-BC").SetValue("", false);



            foreach (var field in editableFields)
            {
                var acroField = acroForm.GetField(field.Name);
                if (acroField == null)
                    continue;

                var propValue = GetPopertyValue(field);

                if (!string.IsNullOrEmpty(propValue))
                {
                    if (field.PropType == typeof(bool))
                    {
                        if (field.Name == "IR-YES-KV")
                        {
                            acroField.SetValue(propValue, true);
                            acroForm.GetField("IR-NO-KV").SetValue("", false);
                        }
                        else
                            acroField.SetValue(propValue, false);
                    }

                    else
                        acroField.SetValue(propValue);
                }

                acroForm.PartialFormFlattening(field.Name);
            }

            acroForm.FlattenFields();

            var attachLiveGps = (_IncidentReport.WandScannedYes3b || _IncidentReport.DateLocation.ShowIncidentLocationAddress) && !string.IsNullOrEmpty(_IncidentReport.DateLocation.ClientSiteLiveGps);

            var attachGpsMap = _clientSite != null && !string.IsNullOrEmpty(_clientSite.Gps);

            if (attachLiveGps || attachGpsMap || _IncidentReport.Attachments != null)
            {
                var doc = new Document(pdfDocument);
                var index = 1;

                var imageFile = string.Empty;
                if (attachLiveGps)
                    imageFile = GetLiveGpsImageFilePath(_IncidentReport.DateLocation.ClientSiteLiveGps);
                else if (attachGpsMap)
                    imageFile = IO.Path.Combine(_GpsMapRootDir, $"Client_{_clientSite.Id}.jpg");

                if (!string.IsNullOrEmpty(imageFile) && IO.File.Exists(imageFile))
                {
                    var image = AttachImageToPdf(pdfDocument, ++index, imageFile);
                    doc.Add(image);
                }

                if (_IncidentReport.Attachments != null)
                {
                    foreach (var fileName in _IncidentReport.Attachments)
                    {
                        var paraName = new Paragraph($"File Name: {fileName}").SetFontColor(WebColors.GetRGBColor(FONT_COLOR_BLACK));
                        if (GetAttachmentType(IO.Path.GetExtension(fileName)) == AttachmentType.Image)
                        {
                            var image = AttachImageToPdf(pdfDocument, ++index, IO.Path.Combine(_UploadRootDir, fileName));
                            paraName.SetFixedPosition(index, 5, 0, 400);
                            doc.Add(image).Add(paraName);
                        }
                        else if (GetAttachmentType(IO.Path.GetExtension(fileName)) == AttachmentType.Pdf)
                        {
                            var uploadPdfName = IO.Path.Combine(_UploadRootDir, fileName);
                            var uploadDoc = new PdfDocument(new PdfReader(uploadPdfName));
                            uploadDoc.CopyPagesTo(1, uploadDoc.GetNumberOfPages(), pdfDocument, pdfDocument.GetNumberOfPages());
                            for (int i = 0, pageIndex = pdfDocument.GetNumberOfPages() - 1; i < uploadDoc.GetNumberOfPages(); i++, pageIndex--)
                            {
                                paraName.SetFixedPosition(pageIndex, 5, 0, 400);
                                doc.Add(paraName);
                            }
                            uploadDoc.Close();
                        }
                    }
                }
            }

            try
            {
                if (_clientSite != null)
                {
                    var siteImageUrl = GetSiteImage(_clientSite.Id);
                    AttachClientSiteImage(pdfDocument, siteImageUrl);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.StackTrace);
            }
            try
            {
                //if (_clientSite != null)
                //{
                //    var siteImageUrl = GetSiteImage(_clientSite.Id);
                //AttachClientSiteImage(pdfDocument, siteImageUrl);
                AttachKvlDetails(pdfDocument, "3");
                //}
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.StackTrace);
            }
            pdfDocument.Close();

            return reportFileName;
        }

        private string GetLiveGpsImageFilePath(string gpsCoordinates)
        {
            string gpsImageDir = IO.Path.Combine(_webHostEnvironment.WebRootPath, "GpsImageLive");
            var mapSettings = _configuration.GetSection("GoogleMap").Get(typeof(GoogleMapSettings)) as GoogleMapSettings;
            try
            {
                return GoogleMapHelper.DownloadGpsImage(gpsImageDir, gpsCoordinates, mapSettings);
            }
            catch
            {

            }
            return null;
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

        private string GetReportFileName(string eventType)
        {
            var fileName = new StringBuilder();
            fileName.Append(_IncidentReport.DateLocation.ReportDate.ToString("yyyyMMdd"));
            fileName.Append(" - IR Report - ");
            fileName.Append(FileNameHelper.GetSanitizedFileNamePart(_IncidentReport.DateLocation.ClientSite));
            if (TryGetFileNameAreaPart(out var areaPart))
                fileName.Append(areaPart);
            if (TryGetFileNameCallSignPart(out var callSignPart))
                fileName.Append(callSignPart);
            fileName.Append(" - ");
            fileName.Append(eventType);
            if (TryGetSiteColourCodePart(out var siteColourCodePart))
                fileName.Append(siteColourCodePart);
            fileName.Append(" - ");
            fileName.Append(GetSerialNumberPart());
            fileName.Append(" - ");
            fileName.Append("v1.0");
            fileName.Append(".pdf");

            return fileName.ToString();
        }

        private string GetSerialNumberPart()
        {
            var serialNoPrefix = "SN";

            if (_IncidentReport.PatrolType == PatrolType.General)
                return $"{serialNoPrefix} {_IncidentReport.OccurrenceNo} {_IncidentReport.SerialNumber}";

            return $"{serialNoPrefix} {_IncidentReport.SerialNumber}";
        }

        private bool TryGetSiteColourCodePart(out string part)
        {
            part = string.Empty;
            var hasValue = false;

            var siteColourCode = _IncidentReport.SiteColourCode;
            if (_IncidentReport.EventType.SiteColour && 
                !string.IsNullOrEmpty(siteColourCode))
            {
                part = $" - {siteColourCode}";
                hasValue = true;
            }
            return hasValue;
        }           
        
        private bool TryGetFileNameAreaPart(out string part)
        {
            part = string.Empty;
            if (!string.IsNullOrEmpty(_IncidentReport.DateLocation.ClientArea))
            {
                part = $" - {FileNameHelper.GetSanitizedFileNamePart(_IncidentReport.DateLocation.ClientArea)}";
                return true;
            }
            return false;
        }

        private bool TryGetFileNameCallSignPart(out string part)
        {
            part = string.Empty;
            var hasValue = false;
            var callSign = _configDataProvider.GetReportFieldsByType(ReportFieldType.CallSign).SingleOrDefault(x => x.Name == _IncidentReport.Officer.CallSign);
            var siteColorHasValue = TryGetSiteColourCodePart(out _);
            if (callSign != null && !siteColorHasValue)
            {
                part = $" - {callSign.Name}";
                hasValue = true;
            }
            return hasValue;
        }

        private AttachmentType GetAttachmentType(string extn)
        {
            if (".jpg,.jpeg,.png,.bmp".IndexOf(extn.ToLower()) >= 0)
                return AttachmentType.Image;

            if (".pdf".IndexOf(extn.ToLower()) >= 0)
                return AttachmentType.Pdf;

            return AttachmentType.Unknown;
        }

        private string GetPopertyValue(FormField field)
        {
            string propValue = string.Empty;
            if (field.PropType == typeof(DateTime?))
            {
                if (field.Name.IndexOf("Date") >= 0)
                {
                    propValue = _IncidentReport.GetPropValue<DateTime?>(field.PropName)?.ToString("dd MMM yyyy");
                }
                else if (field.Name.IndexOf("Time") >= 0)
                {
                    propValue = _IncidentReport.GetPropValue<DateTime?>(field.PropName)?.ToString("HH:mm");
                }
                propValue = string.IsNullOrEmpty(propValue) ? "n/a" : propValue;
            }
            else if (field.PropType == typeof(DateTime))
            {
                if (field.Name.IndexOf("Date") >= 0)
                {
                    propValue = _IncidentReport.GetPropValue<DateTime>(field.PropName).ToString("dd MMM yyyy");
                }
                else if (field.Name.IndexOf("Time") >= 0)
                {
                    propValue = _IncidentReport.GetPropValue<DateTime>(field.PropName).ToString("HH:mm");
                }
            }
            else if (field.PropType == typeof(bool))
            {
                if (
                    field.Name.IndexOf("IR-NO") >= 0 ||
                    field.Name.IndexOf("PTL-EX") >= 0 ||
                    field.Name.IndexOf("PTL-IN") >= 0 ||
                    //field.Name.IndexOf("IR-NO-KV") >= 0 ||
                    field.Name == "IR-YES-BC")
                {
                    propValue = _IncidentReport.GetPropValue<bool>(field.PropName) ? "No" : string.Empty;
                }
                else
                {
                    propValue = _IncidentReport.GetPropValue<bool>(field.PropName) ? "Yes" : string.Empty;
                }
            }
            else if (field.PropType == typeof(int?))
            {
                propValue = _IncidentReport.GetPropValue<int?>(field.PropName).ToString();
            }
            else
            {
                propValue = _IncidentReport.GetPropValue<string>(field.PropName);
            }

            return propValue;
        }

        private string GetSiteImage(int clientSiteId)
        {
            var clientSiteSetting = _clientDataProvider.GetClientSiteKpiSetting(clientSiteId);
            if (clientSiteSetting != null && !string.IsNullOrEmpty(clientSiteSetting.SiteImage))
                return $"{new Uri(_settings.KpiWebUrl)}{clientSiteSetting.SiteImage}";

            return string.Empty;
        }

        private void AttachClientSiteImage(PdfDocument pdfDocument, string siteImageUrl)
        {
            const string IMG_REF_PAGE_1 = "/Im5";
            const string IMG_REF_PAGE_2 = "/Im2";

            if (string.IsNullOrEmpty(siteImageUrl))
                return;

            SetImageObject(pdfDocument.GetFirstPage().GetPdfObject(), IMG_REF_PAGE_1, siteImageUrl);
            SetImageObject(pdfDocument.GetLastPage().GetPdfObject(), IMG_REF_PAGE_2, siteImageUrl);
        }
        
          private void AttachKvlDetails(PdfDocument pdfDocument, string data)
        {
            

            if (string.IsNullOrEmpty(data))
                return;
            var index = 3;
            var pageSize = new PageSize(pdfDocument.GetFirstPage().GetPageSize());
            pdfDocument.AddNewPage(index, pageSize);
            
        }
        private static void SetImageObject(PdfDictionary pagePdfDict, string imgReference, string siteImageUrl)
        {
            PdfDictionary resources = pagePdfDict.GetAsDictionary(PdfName.Resources);
            PdfDictionary xobjects = resources.GetAsDictionary(PdfName.XObject);
            PdfName imgRef = null;
            foreach (var xobject in xobjects.KeySet())
            {
                var image = xobject.ToString();
                if (image == imgReference)
                {
                    imgRef = xobject;
                    break;
                }
            }

            if (imgRef != null)
                xobjects.Put(imgRef, new Image(ImageDataFactory.Create(siteImageUrl)).GetXObject().GetPdfObject());
        }
    }
}
