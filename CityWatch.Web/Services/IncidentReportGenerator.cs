using CityWatch.Common.Helpers;
using CityWatch.Data;
using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using CityWatch.Web.Extensions;
using CityWatch.Web.Helpers;
using CityWatch.Web.Models;
//using DocumentFormat.OpenXml.Drawing.Charts;
using iText.Forms;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Annot;
using iText.Kernel.Pdf.Filespec;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Pdfa;
using Jering.Javascript.NodeJS;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto.Macs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Image = iText.Layout.Element.Image;
using IO = System.IO;
using Path = System.IO.Path;
using Rectangle = iText.Kernel.Geom.Rectangle;


namespace CityWatch.Web.Services
{
    public enum AttachmentType
    {
        Unknown = 0,
        Image = 1,
        Pdf = 2,
        Multimedia = 3,  // Added by binoy 0n 03-01-2024 under task id p1#160_MultimediaAttachments03012024
        Excel = 4, // Added by binoy on 03-06-2024 P1 #215
    }
    public enum ChartType
    {
        Pie = 1,

        Bar
    }

    public interface IIncidentReportGenerator
    {
        string GeneratePdf(IncidentRequest incidentReport, ClientSite clientSite, string Templete);
        string GeneratePdfReport(PatrolRequest patrolRequest);

    }

    public class IncidentReportGenerator : IIncidentReportGenerator
    {
        private IncidentRequest _IncidentReport;
        private ClientSite _clientSite;

        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfigDataProvider _configDataProvider;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
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
        public IncidentReportGenerator(IWebHostEnvironment webHostEnvironment,
            IConfigDataProvider configDataProvider,
            IClientDataProvider clientDataProvider,
            IGuardLogDataProvider guardLogDataProvider,
            IOptions<Settings> settings,
            IConfiguration configuration,
            ILogger<IncidentReportGenerator> logger,
            IPatrolDataReportService irChartDataService,
            CityWatchDbContext context)
        {
            _configDataProvider = configDataProvider;
            _clientDataProvider = clientDataProvider;
            _guardLogDataProvider = guardLogDataProvider;
            _webHostEnvironment = webHostEnvironment;
            _settings = settings.Value;
            _configuration = configuration;
            _logger = logger;
            _context = context;

            _ReportRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "Pdf");
            _GpsMapRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "GpsImage");
            _imageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "images");
            // report output directory webroot\Pdf\Output
            if (!IO.Directory.Exists(IO.Path.Combine(_ReportRootDir, REPORT_DIR)))
                IO.Directory.CreateDirectory(IO.Path.Combine(_ReportRootDir, REPORT_DIR));

            // pdf template directory webroot\Pdf\Template\IR_Form_Template.pdf
            _TemplatePdf = IO.Path.Combine(_ReportRootDir, TEMPLATE_DIR, TEMPLATE_FILE_NAME);
            if (!IO.File.Exists(_TemplatePdf))
                throw new IO.FileNotFoundException("Template file not found");
            _irChartDataService = irChartDataService;
            _graphImageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "GraphImage");
        }

        public string GeneratePdf(IncidentRequest incidentReport, ClientSite clientSite, string Templete)
        {
            if (incidentReport.DateLocation.IsUnknownGpsLocationAddress)
            {
                incidentReport.DateLocation.ClientAddress = "Unknown, GPS used instead";
            }

            //dynamic template based on the domain 
            var IRPdfTemplete = IO.Path.Combine(_ReportRootDir, TEMPLATE_DIR, Templete);
            if (IRPdfTemplete == string.Empty)
            {
                IRPdfTemplete = _TemplatePdf;

            }
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
            PdfDocument pdfDocument = new PdfDocument(new PdfReader(IRPdfTemplete), new PdfWriter(reportPdf));

            PdfAcroForm acroForm = PdfAcroForm.GetAcroForm(pdfDocument, false);

            //acroForm.GetField("IR-NO9").SetValue("", false);
            //acroForm.GetField("IR-NO").SetValue("", false);
            //acroForm.GetField("IR-NO-BC").SetValue("", false);

            var field1 = acroForm.GetField("IR-NO9");
            if (field1 != null)
            {
                field1.SetValue("", false);
            }

            var field2 = acroForm.GetField("IR-NO");
            if (field2 != null)
            {
                field2.SetValue("", false);
            }

            var field3 = acroForm.GetField("IR-NO-BC");
            if (field3 != null)
            {
                field3.SetValue("", false);
            }

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

                    if (field.Name == "CC-List")
                    {
                        var colorcode = _context.FeedbackTemplates.SingleOrDefault(x => x.Id == _IncidentReport.SiteColourCodeId && x.DeleteStatus == 0);
                        if (colorcode != null)
                        {
                            var bgcolor = colorcode.BackgroundColour;
                            var txtcolor = colorcode.TextColor;
                            acroField.SetBackgroundColor(WebColors.GetRGBColor(bgcolor));
                            acroField.SetColor(WebColors.GetRGBColor(txtcolor));
                        }
                    }
                }

                acroForm.PartialFormFlattening(field.Name);
            }

            acroForm.FlattenFields();

            var attachLiveGps = (_IncidentReport.WandScannedYes3b || _IncidentReport.DateLocation.ShowIncidentLocationAddress || _IncidentReport.DateLocation.IsUnknownGpsLocationAddress) && !string.IsNullOrEmpty(_IncidentReport.DateLocation.ClientSiteLiveGps);

            var attachGpsMap = _clientSite != null && !string.IsNullOrEmpty(_clientSite.Gps);

            if (attachLiveGps || attachGpsMap || _IncidentReport.Attachments != null)
            {
                var doc = new Document(pdfDocument);
                var index = 1;
                var closePageIndex = 2;

                var imageFile = string.Empty;
                if (attachLiveGps)
                    imageFile = GetLiveGpsImageFilePath(_IncidentReport.DateLocation.ClientSiteLiveGps);
                //if (attachLiveGps)
                //    imageFile = GetGpsWithWeatherImage(_IncidentReport.DateLocation.ClientSiteLiveGps).Result;

                else if (attachGpsMap)
                {
                    //p1-274 gps stuck if gps image is not available then create  -start
                    if (!IO.File.Exists(IO.Path.Combine(_GpsMapRootDir, $"Client_{_clientSite.Id}.jpg")))
                    {
                        CreateGpsImage(_clientSite);
                    }
                    //p1-274 gps stuck if gps image is not available then create  -end
                    imageFile = IO.Path.Combine(_GpsMapRootDir, $"Client_{_clientSite.Id}.jpg");
                }
                if (!string.IsNullOrEmpty(imageFile) && IO.File.Exists(imageFile))
                {
                    //p1-341-wather in ir-created by jisha-start
                    var uvdate = _IncidentReport.DateLocation.IncidentDate.HasValue ? _IncidentReport.DateLocation.IncidentDate.Value.Date : _IncidentReport.DateLocation.ReportDate.Date;
                    var newimageFile = GetGpsWithWeatherImage(_IncidentReport.DateLocation.ClientSiteLiveGps, imageFile, uvdate).Result;// to create an image indicating wweatheer in corresponding place

                    var image = AttachMapImageToPdf(pdfDocument, ++index, imageFile, newimageFile);// merge the weater image to gps image and display tp pdf
                    //p1-341-wather in ir-created by jisha-end
                    doc.Add(image);
                    ++closePageIndex;
                }

                var pdfAttachmentCount = 0;

                if (_IncidentReport.Attachments != null)
                {
                    foreach (var fileName in _IncidentReport.Attachments)
                    {
                        var paraName = new Paragraph($"File Name: {fileName}").SetFontColor(WebColors.GetRGBColor(FONT_COLOR_BLACK));
                        if (GetAttachmentType(IO.Path.GetExtension(fileName)) == AttachmentType.Pdf)
                        {
                            var uploadPdfName = IO.Path.Combine(_UploadRootDir, fileName);
                            var uploadDoc = new PdfDocument(new PdfReader(uploadPdfName));
                            uploadDoc.CopyPagesTo(1, uploadDoc.GetNumberOfPages(), pdfDocument, pdfDocument.GetNumberOfPages());
                            for (int i = 0, pageIndex = pdfDocument.GetNumberOfPages() - 1; i < uploadDoc.GetNumberOfPages(); i++, pageIndex--)
                            {
                                paraName.SetFixedPosition(pageIndex, 5, 0, 400);
                                doc.Add(paraName);
                            }
                            pdfAttachmentCount += uploadDoc.GetNumberOfPages();
                            closePageIndex += uploadDoc.GetNumberOfPages();
                            uploadDoc.Close();
                        }

                    }
                }



                // Reset index to before close page index
                index = closePageIndex - 1;

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
                            ++closePageIndex;
                        }
                    }
                }

                // Reset index to before close page index
                index = closePageIndex - 1;

                if (_IncidentReport.Attachments != null)
                {
                    // p1#160_MultimediaAttachments03012024 done by Binoy - Start 

                    float currentX = 20;
                    float currentY = 700;
                    float x = 0;
                    bool newPageRequired = true;
                    foreach (var fileName in _IncidentReport.Attachments)
                    {

                        string videoPath = IO.Path.Combine(_UploadRootDir, fileName);
                        string embeddedFileDescription = fileName;
                        var embeddedFileExtn = GetAttachmentType(IO.Path.GetExtension(fileName));

                        if (embeddedFileExtn == AttachmentType.Multimedia || embeddedFileExtn == AttachmentType.Excel)
                        {
                            if (newPageRequired)
                            {
                                var pageSize = new PageSize(pdfDocument.GetFirstPage().GetPageSize());
                                pdfDocument.AddNewPage(++index, pageSize);
                                ++pdfAttachmentCount;
                                ++closePageIndex;
                                x = pdfDocument.GetFirstPage().GetPageSize().GetWidth();
                                var paraName = new Paragraph("NOTE: Multimedia Attachments & Spreadsheets require Adobe Reader to be opened or extracted. Web browser viewing generally can’t access the embedded file.")
                                    .SetFontColor(WebColors.GetRGBColor(FONT_COLOR_BLUE));
                                //.SetBold();                           
                                var centr = (x / 2) - 10;
                                paraName.SetFixedPosition(index, 5, pageSize.GetTop() - 40, x - 10);
                                doc.Add(paraName);
                                newPageRequired = false;
                            }

                            byte[] attachmentfileByteArray = ConvertToByteArrayChunked(videoPath);

                            Rectangle rect = new Rectangle(currentX, currentY, ATTACHMENT_BOX_WIDTH, ATTACHMENT_BOX_HEIGHT);
                            PdfFileSpec fs = PdfFileSpec.CreateEmbeddedFileSpec(pdfDocument, attachmentfileByteArray, embeddedFileDescription, fileName, null, null);
                            try
                            {
                                PdfString title = new PdfString(fileName);
                                PdfAnnotation attachment = new PdfFileAttachmentAnnotation(rect, fs)
                                    .SetContents(string.Format("Double click me to open file: {0}", fileName))
                                    .SetTitle(title)
                                    .SetColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE));
                                PdfPage page = pdfDocument.GetPage(index);
                                page.AddAnnotation(attachment);
                                attachment.Flush();
                                fs.Flush();
                                fs = null;
                                attachment = null;
                            }
                            finally
                            {
                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                            }


                            currentX += 50;
                            if (currentX + ATTACHMENT_BOX_WIDTH > x)
                            {
                                currentY -= 100;
                                currentX = 20;
                            }

                            if (currentY - ATTACHMENT_BOX_HEIGHT < 20)
                            {
                                currentY = 700;
                                currentX = 20;
                                newPageRequired = true;
                            }

                        }

                    }

                    // p1#160_MultimediaAttachments03012024 done by Binoy - End
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
                if (incidentReport.PlateLoadedYes == true)
                {


                    AttachKvlDetails(attachLiveGps, attachGpsMap, pdfDocument, "3", incidentReport);

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.StackTrace);
            }
            pdfDocument.Close();

            return reportFileName;
        }
        private void CreateGpsImage(ClientSite clientSite)
        {
            string gpsImageDir = System.IO.Path.Combine(_webHostEnvironment.WebRootPath, "GpsImage");
            var mapSettings = _configuration.GetSection("GoogleMap").Get(typeof(GoogleMapSettings)) as GoogleMapSettings;
            try
            {
                GoogleMapHelper.DownloadGpsImage(gpsImageDir, clientSite, mapSettings);
            }
            catch
            {

            }
        }
        // p1#160_MultimediaAttachments03012024 done by Binoy - Start
        public static byte[] ConvertToByteArrayChunked(string filePath)
        {
            const int MaxChunkSizeInBytes = 2048;
            var totalBytes = 0;
            byte[] fileByteArray;
            var fileByteArrayChunk = new byte[MaxChunkSizeInBytes];


            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                int bytesRead;

                while ((bytesRead = stream.Read(fileByteArrayChunk, 0, fileByteArrayChunk.Length)) > 0)
                {
                    totalBytes += bytesRead;
                }

                fileByteArray = new byte[totalBytes];
                stream.Seek(0, SeekOrigin.Begin);
                stream.Read(fileByteArray, 0, totalBytes);
            }

            return fileByteArray;
        }

        // p1#160_MultimediaAttachments03012024 done by Binoy - End

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
        private Image AttachMapImageToPdf(PdfDocument pdfDocument, int index, string imagePath, string weatherImagePath = null)
        {
            var pageSize = new PageSize(pdfDocument.GetFirstPage().GetPageSize());
            pdfDocument.AddNewPage(index, pageSize);

            string finalImagePath = imagePath;

            // ---------- COMBINE MAP + WEATHER ----------
            if (!string.IsNullOrEmpty(weatherImagePath) && File.Exists(weatherImagePath))
            {
                string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".jpg");

                using var mapOriginal = System.Drawing.Image.FromFile(imagePath);
                using var weather = System.Drawing.Image.FromFile(weatherImagePath);

                // Rotate map if landscape
                if (mapOriginal.Width > mapOriginal.Height)
                {
                    mapOriginal.RotateFlip(RotateFlipType.Rotate90FlipNone);
                }

                int targetWidth = 1200;

                int mapHeight = (int)((double)mapOriginal.Height / mapOriginal.Width * targetWidth);
                int weatherHeight = (int)((double)weather.Height / weather.Width * targetWidth);

                using var resizedMap = new Bitmap(mapOriginal, new Size(targetWidth, mapHeight));
                using var resizedWeather = new Bitmap(weather, new Size(targetWidth, weatherHeight));

                int finalHeight = mapHeight + weatherHeight;

                using var finalBitmap = new Bitmap(targetWidth, finalHeight);
                using var g = Graphics.FromImage(finalBitmap);

                g.Clear(System.Drawing.Color.White);
                g.DrawImage(resizedMap, 0, 0);
                g.DrawImage(resizedWeather, 0, mapHeight);

                finalBitmap.Save(tempFile, System.Drawing.Imaging.ImageFormat.Jpeg);

                finalImagePath = tempFile;
            }

            // ---------- LOAD INTO PDF ----------
            var imageData = ImageDataFactory.Create(finalImagePath);
            var image = new Image(imageData);

            bool rotateImage = image.GetImageWidth() > image.GetImageHeight();
            bool scaleImage = image.GetImageWidth() > MAX_IMAGE_WIDTH ||
                              image.GetImageHeight() > MAX_IMAGE_HEIGHT;

            if (rotateImage)
            {
                image.SetRotationAngle(ROTATION_ANGLE_DEG * (Math.PI / 180));

                if (scaleImage)
                    image.ScaleToFit(PageSize.A4.GetHeight() * SCALE_FACTOR,
                                     PageSize.A4.GetWidth() * SCALE_FACTOR);
            }
            else
            {
                if (scaleImage)
                    image.ScaleToFit(PageSize.A4.GetWidth() * SCALE_FACTOR,
                                     PageSize.A4.GetHeight() * SCALE_FACTOR);
            }

            var bottom = rotateImage
                ? pageSize.GetTop()
                : pageSize.GetTop() - image.GetImageScaledHeight();

            image.SetFixedPosition(index, 0, bottom);
            if (File.Exists(weatherImagePath))
            {
                File.Delete(weatherImagePath);
            }

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
            extn = extn.ToLowerInvariant();

            string[] imageExts = { ".jpg", ".jpeg", ".png", ".bmp" };
            if (imageExts.Contains(extn))
                return AttachmentType.Image;

            if (extn == ".pdf")
                return AttachmentType.Pdf;

            // Added by binoy 0n 03-01-2024 under task id p1#160_MultimediaAttachments03012024
            string[] multimediaExts = { ".mp4", ".avi", ".mp3" };
            if (multimediaExts.Contains(extn))
                return AttachmentType.Multimedia;

            // Added by binoy 0n 03-06-2024 under task P1 #215
            if (extn == ".xlsx")
                return AttachmentType.Excel;

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

        private void AttachKvlDetails(bool attachLiveGps, bool attachGpsMap, PdfDocument pdfDocument, string data, IncidentRequest incidentReport)
        {
            //Pdf file New Code by Dileep 22042024
            var incidentreportdetails = _clientDataProvider.GetIncidentDetailsKvlReport(AuthUserHelper.LoggedInUserId.GetValueOrDefault());
            var plateIds = incidentreportdetails.Select(x => x.PlateId).ToArray();
            var truckNos = incidentreportdetails.Select(x => x.TruckNo).ToArray();
            var keyVehicleLog = _clientDataProvider.GetKeyVehiclogWithPlateIdAndTruckNoByLogId(plateIds, truckNos, AuthUserHelper.LoggedInUserId.GetValueOrDefault());

            if (keyVehicleLog != null && keyVehicleLog.Count != 0)
            {
                var reportFileName = $"{DateTime.Now:yyyyMMdd}-Kvltemp" + DateTime.Now.Ticks + ".pdf";
                var reportPdf = IO.Path.Combine(_ReportRootDir, REPORT_DIR, reportFileName);
                var pdfDoc = new PdfDocument(new PdfWriter(reportPdf));
                var pageSize = new PageSize(pdfDocument.GetLastPage().GetPageSize());
                var doc = new Document(pdfDoc, pageSize);
                doc.SetLeftMargin(PDF_DOC_MARGIN);
                doc.SetRightMargin(PDF_DOC_MARGIN);
                doc.Add(CreateSiteDetailsTable(keyVehicleLog));
                //doc.SetMargins(5f, 15f, 5f, 5f);
                doc.Add(CreateReportDetailsTable(_clientDataProvider, keyVehicleLog));
                doc.Close();



                var uploadPdfName = IO.Path.Combine(_ReportRootDir, reportFileName);
                var uploadDoc = new PdfDocument(new PdfReader(reportPdf));

                var check = pdfDocument.GetNumberOfPages();
                if (attachLiveGps || attachGpsMap)
                {
                    uploadDoc.CopyPagesTo(1, uploadDoc.GetNumberOfPages(), pdfDocument, 3);
                }
                else
                {
                    uploadDoc.CopyPagesTo(1, uploadDoc.GetNumberOfPages(), pdfDocument, 2);
                }
                // uploadDoc.CopyPagesTo(1, uploadDoc.GetNumberOfPages(), pdfDocument, pdfDocument.GetNumberOfPages());
                uploadDoc.Close();
                pdfDocument.Close();


                FileInfo file = new FileInfo(reportPdf);
                if (file.Exists)//check file exsit or not  
                {
                    file.Delete();
                }

            }

        }

        private static Table CreateSiteDetailsTable(List<KeyVehicleLog> keyVehicleLog)
        {
            var siteDataTable = new Table(UnitValue.CreatePercentArray(new float[] { 5, 38, 10, 23 })).UseAllAvailableWidth().SetMarginTop(10);

            siteDataTable.AddCell(GetSiteHeaderCell("Site:"));
            var siteName = new Cell()
                .Add(new Paragraph().Add(new Text(keyVehicleLog[0].ClientSiteLogBook.ClientSite.Name)
                .SetFont(PdfHelper.GetPdfFont())))
                .Add(new Paragraph().Add(new Text(keyVehicleLog[0].ClientSiteLogBook.ClientSite.Address ?? string.Empty)))
                .SetFontSize(CELL_FONT_SIZE)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER);
            siteDataTable.AddCell(siteName);

            siteDataTable.AddCell(GetSiteHeaderCell("Date of Log:"));
            siteDataTable.AddCell(GetSiteValueCell(keyVehicleLog[0].ClientSiteLogBook.Date.ToString("yyyy-MMM-dd-dddd")));

            //siteDataTable.AddCell(GetSiteHeaderCell("Guard Intials"));
            //siteDataTable.AddCell(GetSiteValueCell(keyVehicleLog.GuardLogin.Guard.Initial ?? string.Empty));

            //siteDataTable.AddCell(GetSiteHeaderCell("S/No:"));
            //siteDataTable.AddCell(GetSerialNoValueCell(keyVehicleLog.DocketSerialNo ?? string.Empty));

            return siteDataTable;
        }
        private static Cell GetSiteValueCell(string text)
        {
            return new Cell()
               .Add(new Paragraph().Add(new Text(text)))
               .SetFont(PdfHelper.GetPdfFont())
               .SetFontSize(CELL_FONT_SIZE)
               .SetTextAlignment(TextAlignment.CENTER)
               .SetHorizontalAlignment(HorizontalAlignment.CENTER)
               .SetVerticalAlignment(VerticalAlignment.MIDDLE);
        }
        private static Cell GetSiteHeaderCell(string text)
        {
            return new Cell()
                    .Add(new Paragraph().Add(new Text(text)))
                    .SetFont(PdfHelper.GetPdfFont())
                    .SetFontSize(CELL_FONT_SIZE)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE));
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
        private Table CreateReportDetailsTable(IClientDataProvider _clientDataProvider, List<KeyVehicleLog> keyVehicleLogViewModel)
        {

            //var outerTable = new Table(UnitValue.CreatePercentArray(new float[] { 78, 22 })).UseAllAvailableWidth().SetMarginTop(10);
            var outerTable = new Table(1).UseAllAvailableWidth().SetMarginTop(10);

            //var innerTable1 = new Table(1).UseAllAvailableWidth();
            //var cellClockDetails = new Cell()
            //                        .SetPaddingLeft(0)
            //                        .SetPaddingTop(0)
            //                        .SetBorder(Border.NO_BORDER)
            //                        .Add(GetClockDetailsTable(_clientDataProvider, keyVehicleLogViewModel));
            ////innerTable1.AddCell(cellClockDetails);
            //outerTable.AddCell(cellClockDetails);

            //var kvlFields = _guardLogDataProvider.GetKeyVehicleLogFields();
            //var kvLogViewModel = new KeyVehicleLogViewModel(keyVehicleLogViewModel, kvlFields);
            //var cellVehicleTrailerDetails = new Cell()
            //                            .SetPaddingLeft(0)
            //                            .SetPaddingTop(0)
            //                            .SetBorder(Border.NO_BORDER)
            //                            .Add(GetVehicleTrailerDetailsTable(kvLogViewModel));
            //outerTable.AddCell(cellVehicleTrailerDetails).SetPaddingBottom(1);

            foreach (var kv in keyVehicleLogViewModel)
            {
                var cellClockDetails = new Cell()
                                    .SetPaddingLeft(0)
                                    .SetPaddingTop(0)
                                    .SetPaddingBottom(0)
                                    .SetBorder(Border.NO_BORDER)
                                    .Add(GetClockDetailsTable(_clientDataProvider, kv));
                //innerTable1.AddCell(cellClockDetails);
                outerTable.AddCell(cellClockDetails);

                var kvlFields = _guardLogDataProvider.GetKeyVehicleLogFields();
                var kvLogViewModel = new KeyVehicleLogViewModel(kv, kvlFields);
                var cellVehicleTrailerDetails = new Cell()
                                            .SetPaddingLeft(0)
                                            .SetPaddingTop(0)
                                            .SetPaddingBottom(10) // gap only between kv records
                                            .SetBorder(Border.NO_BORDER)
                                            .Add(GetVehicleTrailerDetailsTable(kvLogViewModel));
                outerTable.AddCell(cellVehicleTrailerDetails);
            }

            return outerTable;
        }
        private static Table GetClockDetailsTable(IClientDataProvider _clientDataProvider, List<KeyVehicleLog> keyVehicleLogViewModel)
        {
            //var clockDetails = new Table(UnitValue.CreatePercentArray(new float[] { 14, 14, 14, 14, 14, 14, 14, 12, 14, 8, 8, 8, 8, 14, 30, 25, 14, 14, 20, 14, 14, 14, 14, 14, 35 })).UseAllAvailableWidth();
            var clockDetails = new Table(UnitValue.CreatePercentArray(new float[] { 14, 14, 14, 14, 14, 14, 14, 12, 14, 14, 30, 25, 14, 14, 20, 14, 14, 14, 14, 14, 35 })).UseAllAvailableWidth();

            clockDetails.AddCell(GetHeaderCell("Clocks", 1, 5));
            clockDetails.AddCell(GetHeaderCell("ID No / Vehicle Rego", 2));
            clockDetails.AddCell(GetHeaderCell("ID /Plate", 2));
            clockDetails.AddCell(GetHeaderCell("Vehicle Description", 1, 2));
            // clockDetails.AddCell(GetHeaderCell("Trailers Rego or ISO", 1, 4));
            clockDetails.AddCell(GetHeaderCell("Key /card scan", 2));
            clockDetails.AddCell(GetHeaderCell("Company Name", 2));
            clockDetails.AddCell(GetHeaderCell("Individual", 1, 3));
            clockDetails.AddCell(GetHeaderCell("Site POC", 2));
            clockDetails.AddCell(GetHeaderCell("Site Location", 2));
            clockDetails.AddCell(GetHeaderCell("Purpose of Entry", 2));
            clockDetails.AddCell(GetHeaderCell("Weight", 1, 3));
            clockDetails.AddCell(GetHeaderCell("Notes", 2));

            clockDetails.AddCell(GetHeaderCell("Intial Call"));
            clockDetails.AddCell(GetHeaderCell("Entry Time"));
            clockDetails.AddCell(GetHeaderCell("Sent In Time"));
            clockDetails.AddCell(GetHeaderCell("Exit Time"));

            clockDetails.AddCell(GetHeaderCell("Time Slot No"));




            clockDetails.AddCell(GetHeaderCell("Truck Config"));
            clockDetails.AddCell(GetHeaderCell("Trailer Type"));

            //clockDetails.AddCell(GetHeaderCell("1"));
            //clockDetails.AddCell(GetHeaderCell("2"));
            //clockDetails.AddCell(GetHeaderCell("3"));
            //clockDetails.AddCell(GetHeaderCell("4"));


            clockDetails.AddCell(GetHeaderCell("Name"));
            clockDetails.AddCell(GetHeaderCell("Mobile No"));
            clockDetails.AddCell(GetHeaderCell("Type"));


            clockDetails.AddCell(GetHeaderCell("In Gross"));
            clockDetails.AddCell(GetHeaderCell("Out Net"));
            clockDetails.AddCell(GetHeaderCell("Tare"));

            for (int i = 0; i < keyVehicleLogViewModel.Count; i++)
            {
                var headerTimeSlotNo = keyVehicleLogViewModel[i].IsTimeSlotNo ? "Time Slot No." : "T-No. (Load)";

                //clockDetails.AddCell(GetHeaderCell(headerTimeSlotNo));

                clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel[i].InitialCallTime?.ToString("HH:mm")).SetMaxWidth(15).SetMinWidth(15));
                clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel[i].EntryTime?.ToString("HH:mm")).SetMaxWidth(15).SetMinWidth(15));
                clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel[i].SentInTime?.ToString("HH:mm")).SetMaxWidth(15).SetMinWidth(15));
                clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel[i].ExitTime?.ToString("HH:mm")).SetMaxWidth(15).SetMinWidth(15));
                clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel[i].TimeSlotNo).SetMaxWidth(15).SetMinWidth(15));
                clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel[i].VehicleRego).SetMaxWidth(15).SetMinWidth(15));
                var kvlFields = _clientDataProvider.GetKeyVehicleLogFields();
                var plates = kvlFields.Where(z => z.Id == keyVehicleLogViewModel[i].PlateId).ToList();
                clockDetails.AddCell(GetDataCell(plates[0].Name).SetMaxWidth(14).SetMaxWidth(14));

                if (keyVehicleLogViewModel[i].TruckConfig == null)
                {
                    clockDetails.AddCell(GetDataCell(null).SetMaxWidth(14).SetMinWidth(14));
                }
                else
                {
                    var TruckConfigText = kvlFields.Where(z => z.Id == keyVehicleLogViewModel[i].TruckConfig).ToList();
                    clockDetails.AddCell(GetDataCell(TruckConfigText[0].Name).SetMaxWidth(12).SetMinWidth(12));
                }
                if (keyVehicleLogViewModel[i].TrailerType == null)
                {
                    clockDetails.AddCell(GetDataCell(null).SetMaxWidth(14).SetMinWidth(14));
                }
                else
                {
                    var TrailerTypeText = kvlFields.Where(z => z.Id == keyVehicleLogViewModel[i].TrailerType).ToList();
                    clockDetails.AddCell(GetDataCell(TrailerTypeText[0].Name).SetMaxWidth(14).SetMinWidth(14));
                }







                //clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel[i].Trailer1Rego).SetMaxWidth(18).SetMinWidth(18));
                //clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel[i].Trailer2Rego).SetMaxWidth(18).SetMinWidth(18));
                //clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel[i].Trailer3Rego).SetMaxWidth(18).SetMinWidth(18));
                //clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel[i].Trailer4Rego).SetMaxWidth(18).SetMinWidth(18));
                // clockDetails.AddCell(GetDataCell(GetKeyDetailsCommaSeparated(keyVehicleLogViewModel[0].Detail), textAlignment: TextAlignment.LEFT));
                clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel[i].KeyNo).SetMaxWidth(14).SetMinWidth(14));
                clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel[i].CompanyName).SetMaxWidth(30).SetMinWidth(30));
                clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel[i].PersonName).SetMaxWidth(25).SetMinWidth(25));
                clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel[i].MobileNumber).SetMaxWidth(14).SetMinWidth(14));

                var PersonTypeText = kvlFields.Where(z => z.Id == keyVehicleLogViewModel[i].PersonType).ToList();
                clockDetails.AddCell(GetDataCell(PersonTypeText[0].Name).SetMaxWidth(15).SetMinWidth(15));

                if (keyVehicleLogViewModel[i].ClientSitePocId == null)
                {
                    clockDetails.AddCell(GetDataCell(null).SetMaxWidth(20).SetMinWidth(20));
                }
                else
                {

                    var clientsitepocdetails = _clientDataProvider.GetClientSitePocs();
                    var clientsitepoc = clientsitepocdetails.Where(z => z.Id == keyVehicleLogViewModel[i].ClientSitePocId).ToList();
                    clockDetails.AddCell(GetDataCell(clientsitepoc[0].Name).SetMaxWidth(20).SetMinWidth(20));
                }
                if (keyVehicleLogViewModel[i].ClientSiteLocationId == null)
                {
                    clockDetails.AddCell(GetDataCell(null).SetMaxWidth(19).SetMinWidth(19));
                }
                else
                {
                    var clientsitelocdetails = _clientDataProvider.GetClientSiteLocations();
                    var clientsiteloc = clientsitelocdetails.Where(z => z.Id == keyVehicleLogViewModel[i].ClientSiteLocationId).ToList();
                    clockDetails.AddCell(GetDataCell(clientsiteloc[0].Name).SetMaxWidth(19).SetMinWidth(19));
                }
                if (keyVehicleLogViewModel[i].EntryReason == null)
                {
                    clockDetails.AddCell(GetDataCell(null).SetMaxWidth(14).SetMinWidth(14));
                }
                else
                {
                    var EntryReason = kvlFields.Where(z => z.Id == keyVehicleLogViewModel[i].EntryReason).ToList();
                    clockDetails.AddCell(GetDataCell(EntryReason[0].Name).SetMaxWidth(14).SetMinWidth(14));

                }

                clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel[i].InWeight.ToString()).SetMaxWidth(14).SetMinWidth(14));
                clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel[i].OutWeight.ToString()).SetMaxWidth(14).SetMinWidth(14));
                clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel[i].TareWeight?.ToString()).SetMaxWidth(14).SetMinWidth(14));

                clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel[i].Notes).SetMaxWidth(35).SetMinWidth(35));
            }
            //clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel[0][0].InWeight.ToString()));
            //clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel[0][0].InWeight.ToString()));
            //clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel[0][0].InWeight.ToString()));
            //clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel[0][0].Notes.ToString()));
            return clockDetails;
        }

        private static Table GetClockDetailsTable(IClientDataProvider _clientDataProvider, KeyVehicleLog keyVehicleLogViewModel)
        {            
            var clockDetails = new Table(UnitValue.CreatePercentArray(new float[] { 14, 14, 14, 14, 14, 14, 14, 12, 14, 14, 30, 25, 14, 14, 20, 14, 14, 14, 14, 14, 35 })).UseAllAvailableWidth();

            clockDetails.AddCell(GetHeaderCell("Clocks", 1, 5));
            clockDetails.AddCell(GetHeaderCell("ID No / Vehicle Rego", 2));
            clockDetails.AddCell(GetHeaderCell("ID /Plate", 2));
            clockDetails.AddCell(GetHeaderCell("Vehicle Description", 1, 2));
            clockDetails.AddCell(GetHeaderCell("Key /card scan", 2));
            clockDetails.AddCell(GetHeaderCell("Company Name", 2));
            clockDetails.AddCell(GetHeaderCell("Individual", 1, 3));
            clockDetails.AddCell(GetHeaderCell("Site POC", 2));
            clockDetails.AddCell(GetHeaderCell("Site Location", 2));
            clockDetails.AddCell(GetHeaderCell("Purpose of Entry", 2));
            clockDetails.AddCell(GetHeaderCell("Weight", 1, 3));
            clockDetails.AddCell(GetHeaderCell("Notes", 2));

            clockDetails.AddCell(GetHeaderCell("Intial Call"));
            clockDetails.AddCell(GetHeaderCell("Entry Time"));
            clockDetails.AddCell(GetHeaderCell("Sent In Time"));
            clockDetails.AddCell(GetHeaderCell("Exit Time"));

            clockDetails.AddCell(GetHeaderCell("Time Slot No"));

            clockDetails.AddCell(GetHeaderCell("Truck Config"));
            clockDetails.AddCell(GetHeaderCell("Trailer Type"));                       

            clockDetails.AddCell(GetHeaderCell("Name"));
            clockDetails.AddCell(GetHeaderCell("Mobile No"));
            clockDetails.AddCell(GetHeaderCell("Type"));


            clockDetails.AddCell(GetHeaderCell("In Gross"));
            clockDetails.AddCell(GetHeaderCell("Out Net"));
            clockDetails.AddCell(GetHeaderCell("Tare"));

            var headerTimeSlotNo = keyVehicleLogViewModel.IsTimeSlotNo ? "Time Slot No." : "T-No. (Load)";

            clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel.InitialCallTime?.ToString("HH:mm")).SetMaxWidth(15).SetMinWidth(15));
            clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel.EntryTime?.ToString("HH:mm")).SetMaxWidth(15).SetMinWidth(15));
            clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel.SentInTime?.ToString("HH:mm")).SetMaxWidth(15).SetMinWidth(15));
            clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel.ExitTime?.ToString("HH:mm")).SetMaxWidth(15).SetMinWidth(15));
            clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel.TimeSlotNo).SetMaxWidth(15).SetMinWidth(15));
            clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel.VehicleRego).SetMaxWidth(15).SetMinWidth(15));
            var kvlFields = _clientDataProvider.GetKeyVehicleLogFields();
            var plates = kvlFields.Where(z => z.Id == keyVehicleLogViewModel.PlateId).ToList();
            clockDetails.AddCell(GetDataCell(plates[0].Name).SetMaxWidth(14).SetMaxWidth(14));

            if (keyVehicleLogViewModel.TruckConfig == null)
            {
                clockDetails.AddCell(GetDataCell(null).SetMaxWidth(14).SetMinWidth(14));
            }
            else
            {
                var TruckConfigText = kvlFields.Where(z => z.Id == keyVehicleLogViewModel.TruckConfig).ToList();
                clockDetails.AddCell(GetDataCell(TruckConfigText[0].Name).SetMaxWidth(12).SetMinWidth(12));
            }
            if (keyVehicleLogViewModel.TrailerType == null)
            {
                clockDetails.AddCell(GetDataCell(null).SetMaxWidth(14).SetMinWidth(14));
            }
            else
            {
                var TrailerTypeText = kvlFields.Where(z => z.Id == keyVehicleLogViewModel.TrailerType).ToList();
                clockDetails.AddCell(GetDataCell(TrailerTypeText[0].Name).SetMaxWidth(14).SetMinWidth(14));
            }

            clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel.KeyNo).SetMaxWidth(14).SetMinWidth(14));
            clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel.CompanyName).SetMaxWidth(30).SetMinWidth(30));
            clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel.PersonName).SetMaxWidth(25).SetMinWidth(25));
            clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel.MobileNumber).SetMaxWidth(14).SetMinWidth(14));

            var PersonTypeText = kvlFields.Where(z => z.Id == keyVehicleLogViewModel.PersonType).ToList();
            clockDetails.AddCell(GetDataCell(PersonTypeText[0].Name).SetMaxWidth(15).SetMinWidth(15));

            if (keyVehicleLogViewModel.ClientSitePocId == null)
            {
                clockDetails.AddCell(GetDataCell(null).SetMaxWidth(20).SetMinWidth(20));
            }
            else
            {

                var clientsitepocdetails = _clientDataProvider.GetClientSitePocs();
                var clientsitepoc = clientsitepocdetails.Where(z => z.Id == keyVehicleLogViewModel.ClientSitePocId).ToList();
                clockDetails.AddCell(GetDataCell(clientsitepoc[0].Name).SetMaxWidth(20).SetMinWidth(20));
            }
            if (keyVehicleLogViewModel.ClientSiteLocationId == null)
            {
                clockDetails.AddCell(GetDataCell(null).SetMaxWidth(19).SetMinWidth(19));
            }
            else
            {
                var clientsitelocdetails = _clientDataProvider.GetClientSiteLocations();
                var clientsiteloc = clientsitelocdetails.Where(z => z.Id == keyVehicleLogViewModel.ClientSiteLocationId).ToList();
                clockDetails.AddCell(GetDataCell(clientsiteloc[0].Name).SetMaxWidth(19).SetMinWidth(19));
            }
            if (keyVehicleLogViewModel.EntryReason == null)
            {
                clockDetails.AddCell(GetDataCell(null).SetMaxWidth(14).SetMinWidth(14));
            }
            else
            {
                var EntryReason = kvlFields.Where(z => z.Id == keyVehicleLogViewModel.EntryReason).ToList();
                clockDetails.AddCell(GetDataCell(EntryReason[0].Name).SetMaxWidth(14).SetMinWidth(14));

            }

            clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel.InWeight.ToString()).SetMaxWidth(14).SetMinWidth(14));
            clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel.OutWeight.ToString()).SetMaxWidth(14).SetMinWidth(14));
            clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel.TareWeight?.ToString()).SetMaxWidth(14).SetMinWidth(14));

            clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel.Notes).SetMaxWidth(35).SetMinWidth(35));

            return clockDetails;
        }

        private string GetKeyDetailsCommaSeparated(KeyVehicleLog keyVehicleLog)
        {
            var clientSiteKeys = _clientDataProvider.GetClientSiteKeys(keyVehicleLog.ClientSiteLogBook.ClientSiteId);

            if (string.IsNullOrEmpty(keyVehicleLog.KeyNo))
                return string.Empty;

            var listKeys = new List<string>();
            var keys = keyVehicleLog.KeyNo.Split(';').Select(z => z.Trim());
            foreach (var key in keys)
            {
                var description = clientSiteKeys.SingleOrDefault(z => z.KeyNo == key)?.Description ?? string.Empty;
                listKeys.Add(key + " - " + description);
            }
            return string.Join("; ", listKeys);
        }

        private static Cell GetHeaderCell(string text, int rowSpan = 1, int colSpan = 1, TextAlignment textAlignment = TextAlignment.CENTER)
        {
            return new Cell(rowSpan, colSpan)
                .Add(new Paragraph().Add(new Text(text)))
                .SetFont(PdfHelper.GetPdfFont())
                .SetFontSize(CELL_FONT_SIZE)
                .SetTextAlignment(textAlignment)
                .SetBackgroundColor(WebColors.GetRGBColor(COLOR_LIGHT_BLUE));
        }
        private static Cell GetDataCell(string text, TextAlignment textAlignment = TextAlignment.CENTER, float minHeight = 15, float cellFontSize = CELL_FONT_SIZE)
        {
            return new Cell(1, 1)
                .Add(new Paragraph().SetFontSize(cellFontSize)
                .Add(new Text(text ?? string.Empty)))
                .SetTextAlignment(textAlignment)
                .SetMinHeight(minHeight);
        }

        private static Table GetCompanyDetailsTable(KeyVehicleLogViewModel keyVehicleLogViewModel)
        {
            var companyDetails = new Table(UnitValue.CreatePercentArray(new float[] { 23, 10, 12, 10, 10, 12, 23 })).UseAllAvailableWidth();

            companyDetails.AddCell(GetHeaderCell("Company Name", 2));

            companyDetails.AddCell(GetHeaderCell("Individual", 1, 3));
            companyDetails.AddCell(GetHeaderCell("Site", 1, 3));

            companyDetails.AddCell(GetHeaderCell("Name"));
            companyDetails.AddCell(GetHeaderCell("Mobile No"));
            companyDetails.AddCell(GetHeaderCell("Type"));
            companyDetails.AddCell(GetHeaderCell("POC"));
            companyDetails.AddCell(GetHeaderCell("Location"));
            companyDetails.AddCell(GetHeaderCell("Purpose Of Entry"));

            companyDetails.AddCell(GetDataCell(keyVehicleLogViewModel.Detail.CompanyName));
            companyDetails.AddCell(GetDataCell(keyVehicleLogViewModel.Detail.PersonName));
            companyDetails.AddCell(GetDataCell(keyVehicleLogViewModel.Detail.MobileNumber));
            companyDetails.AddCell(GetDataCell(keyVehicleLogViewModel.PersonTypeText));
            companyDetails.AddCell(GetDataCell(keyVehicleLogViewModel.Detail.ClientSitePoc?.Name));
            companyDetails.AddCell(GetDataCell(keyVehicleLogViewModel.Detail.ClientSiteLocation?.Name));
            companyDetails.AddCell(GetDataCell(keyVehicleLogViewModel.PurposeOfEntry));

            return companyDetails;
        }
        private static Table GetNotesTable(KeyVehicleLogViewModel keyVehicleLogViewModel, string blankNoteOnOrOff)
        {
            var notesTable = new Table(1).UseAllAvailableWidth();

            notesTable.AddCell(GetHeaderCell("Notes", textAlignment: TextAlignment.LEFT));
            if (blankNoteOnOrOff == "true")
            {
                notesTable.AddCell(GetDataCell("", textAlignment: TextAlignment.LEFT, minHeight: 82));

            }
            else
            {
                notesTable.AddCell(GetDataCell(keyVehicleLogViewModel.Detail.Notes, textAlignment: TextAlignment.LEFT, minHeight: 82));
            }
            return notesTable;
        }

        private static Table GetVehicleDetailsTable(KeyVehicleLogViewModel keyVehicleLogViewModel)
        {
            var vehicleDetailsTable = new Table(UnitValue.CreatePercentArray(new float[] { 10, 10, 20, 20, 10, 10, 10, 10 })).UseAllAvailableWidth().SetMarginTop(10);
            vehicleDetailsTable.AddCell(GetHeaderCell("ID No / Vehicle Rego", 2));
            vehicleDetailsTable.AddCell(GetHeaderCell("Plate", 2));
            vehicleDetailsTable.AddCell(GetHeaderCell("Vehicle Description", 1, 2));
            vehicleDetailsTable.AddCell(GetHeaderCell("Trailer Rego or ISO + Seals", 1, 4));

            vehicleDetailsTable.AddCell(GetHeaderCell("Truck Config"));
            vehicleDetailsTable.AddCell(GetHeaderCell("Trailer Type"));
            vehicleDetailsTable.AddCell(GetHeaderCell("1"));
            vehicleDetailsTable.AddCell(GetHeaderCell("2"));
            vehicleDetailsTable.AddCell(GetHeaderCell("3"));
            vehicleDetailsTable.AddCell(GetHeaderCell("4"));

            vehicleDetailsTable.AddCell(GetDataCell(keyVehicleLogViewModel.Detail.VehicleRego));
            vehicleDetailsTable.AddCell(GetDataCell(keyVehicleLogViewModel.Plate));
            vehicleDetailsTable.AddCell(GetDataCell(keyVehicleLogViewModel.TruckConfigText));
            vehicleDetailsTable.AddCell(GetDataCell(keyVehicleLogViewModel.TrailerTypeText));
            vehicleDetailsTable.AddCell(GetDataCell(keyVehicleLogViewModel.Detail.Trailer1Rego));
            vehicleDetailsTable.AddCell(GetDataCell(keyVehicleLogViewModel.Detail.Trailer2Rego));
            vehicleDetailsTable.AddCell(GetDataCell(keyVehicleLogViewModel.Detail.Trailer3Rego));
            vehicleDetailsTable.AddCell(GetDataCell(keyVehicleLogViewModel.Detail.Trailer4Rego));

            return vehicleDetailsTable;
        }

        private static Table GetVehicleTrailerDetailsTable(KeyVehicleLogViewModel keyVehicleLogViewModel)
        {
            var vehicleTrailerDetailsTable = new Table(UnitValue.CreatePercentArray(new float[] { 10, 10, 10, 10, 10, 10, 10, 10 })).UseAllAvailableWidth();

            //var vehicleRegoColumnName = keyVehicleLogViewModel.Detail.
            vehicleTrailerDetailsTable.AddCell(GetHeaderCell(keyVehicleLogViewModel.VehicleRegoHeading, 1, 8));
            vehicleTrailerDetailsTable.AddCell(GetHeaderCell("1"));
            vehicleTrailerDetailsTable.AddCell(GetHeaderCell("2"));
            vehicleTrailerDetailsTable.AddCell(GetHeaderCell("3"));
            vehicleTrailerDetailsTable.AddCell(GetHeaderCell("4"));
            vehicleTrailerDetailsTable.AddCell(GetHeaderCell("5"));
            vehicleTrailerDetailsTable.AddCell(GetHeaderCell("6"));
            vehicleTrailerDetailsTable.AddCell(GetHeaderCell("7"));
            vehicleTrailerDetailsTable.AddCell(GetHeaderCell("8"));

            vehicleTrailerDetailsTable.AddCell(GetDataCell(keyVehicleLogViewModel.Plate1 + "\n" + keyVehicleLogViewModel.Detail.Trailer1Rego));
            vehicleTrailerDetailsTable.AddCell(GetDataCell(keyVehicleLogViewModel.Plate2 + "\n" + keyVehicleLogViewModel.Detail.Trailer2Rego));
            vehicleTrailerDetailsTable.AddCell(GetDataCell(keyVehicleLogViewModel.Plate3 + "\n" + keyVehicleLogViewModel.Detail.Trailer3Rego));
            vehicleTrailerDetailsTable.AddCell(GetDataCell(keyVehicleLogViewModel.Plate4 + "\n" + keyVehicleLogViewModel.Detail.Trailer4Rego));
            vehicleTrailerDetailsTable.AddCell(GetDataCell(keyVehicleLogViewModel.Plate5 + "\n" + keyVehicleLogViewModel.Detail.Trailer5Rego));
            vehicleTrailerDetailsTable.AddCell(GetDataCell(keyVehicleLogViewModel.Plate6 + "\n" + keyVehicleLogViewModel.Detail.Trailer6Rego));
            vehicleTrailerDetailsTable.AddCell(GetDataCell(keyVehicleLogViewModel.Plate7 + "\n" + keyVehicleLogViewModel.Detail.Trailer7Rego));
            vehicleTrailerDetailsTable.AddCell(GetDataCell(keyVehicleLogViewModel.Plate8 + "\n" + keyVehicleLogViewModel.Detail.Trailer8Rego));



            return vehicleTrailerDetailsTable;
        }

        public string GeneratePdfReport(PatrolRequest patrolRequest)
        {

            var reportFileName = $"{DateTime.Now.ToString("yyyyMMdd")} -  - IR Statistics Report - {patrolRequest.FromDate.ToString("MMM")} {patrolRequest.FromDate.Year}_{new Random().Next()}.pdf";
            var reportPdf = IO.Path.Combine(_ReportRootDir, REPORT_DIR, reportFileName);

            var pdfDoc = new PdfDocument(new PdfWriter(reportPdf));
            pdfDoc.SetDefaultPageSize(PageSize.A4.Rotate());
            var doc = new Document(pdfDoc);
            doc.SetMargins(PDF_DOC_MARGIN, PDF_DOC_MARGIN, PDF_DOC_MARGIN, PDF_DOC_MARGIN);

            var headerTable = CreateReportHeader(patrolRequest);
            doc.Add(headerTable);

            //NEWLY ADDED-START
            var patrolDataReport = _irChartDataService.GetDailyPatrolData(patrolRequest);


            if (patrolDataReport.ResultsCount > 0)
            {
                //doc.Add(new AreaBreak());
                //doc.Add(tableReportHeader);
                var graphsTable = CreateGraphsTables(patrolDataReport);
                doc.Add(graphsTable);
            }

            doc.Close();
            pdfDoc.Close();

            return IO.Path.GetFileName(reportFileName);
        }
        private Table CreateGraphsTables(PatrolDataReport patrolDataReport)
        {
            var graphTable = new Table(UnitValue.CreatePercentArray(1)).UseAllAvailableWidth()
                .SetMarginTop(5)
                .SetKeepTogether(true);
            graphTable.AddCell(new Cell()
                .SetPadding(0)
                .SetBorder(Border.NO_BORDER)
                .Add(CreateGraphsTable1(patrolDataReport)));
            graphTable.AddCell(new Cell()
                .SetPadding(0)
                .SetBorder(Border.NO_BORDER)
                .Add(CreateGraphsTable2(patrolDataReport)));
            return graphTable;
        }

        private Table CreateGraphsTable1(PatrolDataReport patrolDataReport)
        {
            var chartDataTable = new Table(UnitValue.CreatePercentArray(new float[] { 33, 1, 32, 1, 33 })).UseAllAvailableWidth().SetMarginBottom(5);

            chartDataTable.AddCell(GetChartHeaderCell("IR RECORDS PERCENTAGE BY SITE", "\nTotal Site Count: " + patrolDataReport.SitePercentage.Count));

            // row 1 blank cell
            chartDataTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));

            chartDataTable.AddCell(GetChartHeaderCell("IR RECORDS PERCENTAGE BY AREA/WARD", "\nTotal Area/Ward Count: " + patrolDataReport.AreaWardPercentage.Count));

            // row 1 blank cell
            chartDataTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));

            chartDataTable.AddCell(GetChartHeaderCell("IR RECORDS PERCENTAGE BY COLOUR CODE", "\nTotal Color Code Count: " + patrolDataReport.ColorCodePercentage.Count));

            var sitesPieChartImage = GetChartImage(patrolDataReport.SitePercentage.OrderByDescending(z => z.Value).ToArray());
            chartDataTable.AddCell(GetChartImageCell(sitesPieChartImage));

            // row 2 blank cell
            chartDataTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));

            var areaPieChartImage = GetChartImage(patrolDataReport.AreaWardPercentage.OrderByDescending(z => z.Value).ToArray());
            chartDataTable.AddCell(GetChartImageCell(areaPieChartImage));

            // row 2 blank cell
            chartDataTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));

            var colorCodeChartImage = GetChartImage(patrolDataReport.ColorCodePercentage.OrderByDescending(z => z.Value).ToArray());
            chartDataTable.AddCell(GetChartImageCell(colorCodeChartImage));

            return chartDataTable;
        }

        private Table CreateGraphsTable2(PatrolDataReport patrolDataReport)
        {
            var chartDataTable = new Table(UnitValue.CreatePercentArray(new float[] { 70, 30 })).UseAllAvailableWidth().SetMarginTop(5);

            var eventTypeCount = patrolDataReport.ResultsCount;
            chartDataTable.AddCell(GetChartHeaderCell("IR EVENT TYPE QUANTITY", "Total IR Count: " + eventTypeCount, 2));

            var eventTypePieChartImage = GetChartImage(patrolDataReport.EventTypePercentage.OrderBy(z => z.Key).ToArray(), chartWidth: 615);
            chartDataTable.AddCell(GetChartImageCell(eventTypePieChartImage).SetBorderRight(Border.NO_BORDER));

            var eventTypeBarChartImage = GetChartImage(patrolDataReport.EventTypeQuantity.OrderBy(z => z.Key).ToArray(), ChartType.Bar);
            chartDataTable.AddCell(GetChartImageCell(eventTypeBarChartImage).SetBorderLeft(Border.NO_BORDER));

            return chartDataTable;
        }
        private Cell GetChartHeaderCell(string leftText, string rightText, int colspan = 1)
        {
            var cell = new Cell(1, colspan)
               .SetFont(PdfHelper.GetPdfFont())
               .SetFontSize(CELL_FONT_SIZE)
               .SetFontColor(WebColors.GetRGBColor(COLOR_WHITE))
               .SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY))
               .SetVerticalAlignment(VerticalAlignment.MIDDLE);

            var p = new Paragraph(leftText);
            p.Add(new Tab());
            p.AddTabStops(new TabStop(1000, TabAlignment.RIGHT));
            p.Add(new Text(rightText).SetFontSize(4.5f));
            cell.Add(p);

            return cell;
        }

        private Cell GetChartImageCell(Image chartImage)
        {
            var imageCell = new Cell();
            if (chartImage != null)
                imageCell.Add(chartImage).SetVerticalAlignment(VerticalAlignment.MIDDLE);

            return imageCell;
        }
        private Image GetChartImage(KeyValuePair<string, double>[] data, ChartType chartType = ChartType.Pie, int? chartWidth = null)
        {
            if (data.All(z => z.Value == 0))
                return null;

            try
            {
                var graphFileName = IO.Path.Combine(_graphImageRootDir, $"{DateTime.Now: ddMMyyyy_HHmmss}.png");
                var options = new { type = chartType, fileName = graphFileName, width = chartWidth };

                var task = StaticNodeJSService.InvokeFromFileAsync<string>("Scripts/ir-chart.js", "drawChart", args: new object[] { options, data });
                var success = task.Result == "OK";

                if (!success)
                    throw new ApplicationException("Create graph failed");

                if (success && !IO.File.Exists(graphFileName))
                    throw new ApplicationException($"Graph image not found. File Name: {graphFileName}");

                var graphImage = new Image(ImageDataFactory.Create(graphFileName)).SetHeight(90);

                IO.File.Delete(graphFileName);

                return graphImage;
            }
            catch
            {
                // no ops
            }
            return null;
        }
        private Table CreateReportHeader(PatrolRequest patrolRequest)
        {
            var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 20, 60, 20 })).UseAllAvailableWidth();

            var cellSiteImage = new Cell().SetBorder(Border.NO_BORDER);
            headerTable.AddCell(cellSiteImage);
            if (patrolRequest.ClientSites != null)
            {
                var cellReportTitle = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.CENTER)
                .Add(new Paragraph("IR Statistics Report\n").SetFont(PdfHelper.GetPdfFont()).SetFontSize(CELL_FONT_SIZE * 1.2f))


                .Add(new Paragraph(patrolRequest.FromDate.ToString("dd-MMM-yyyy") + "  to  " + patrolRequest.ToDate.ToString("dd-MMM-yyyy"))).SetFontSize(CELL_FONT_SIZE)

                .Add(new Paragraph(patrolRequest.ClientSites[0])).SetFontSize(CELL_FONT_SIZE);

                headerTable.AddCell(cellReportTitle);
            }
            else
            {
                var cellReportTitle = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.CENTER)
                .Add(new Paragraph("IR Statistics Report\n").SetFont(PdfHelper.GetPdfFont()).SetFontSize(CELL_FONT_SIZE * 1.2f))


                .Add(new Paragraph(patrolRequest.FromDate.ToString("dd-MMM-yyyy") + "  to  " + patrolRequest.ToDate.ToString("dd-MMM-yyyy"))).SetFontSize(CELL_FONT_SIZE);



                headerTable.AddCell(cellReportTitle);
            }

            var image = new Image(ImageDataFactory.Create(IO.Path.Combine(_imageRootDir, "CWSLogoPdf.png")))
                .SetHeight(50)
                .SetHorizontalAlignment(HorizontalAlignment.RIGHT);
            var cellLogoImage = new Cell()
                .Add(image)
                .SetBorder(Border.NO_BORDER)
                .SetHorizontalAlignment(HorizontalAlignment.RIGHT);
            headerTable.AddCell(cellLogoImage);

            headerTable.AddCell(new Cell(1, 3).SetPadding(3).SetBorder(Border.NO_BORDER));
            return headerTable;
        }

        //p1-341-wather in ir-created by jisha-start
        private async Task<string> GetGpsWithWeatherImage(string gpsCoordinates, string mapPath, DateTime uvdate)
        {
            try
            {
                // 1. Get map image
                //var mapPath = GetLiveGpsImageFilePath(gpsCoordinates);

                if (string.IsNullOrEmpty(mapPath) || !File.Exists(mapPath))
                    return mapPath;

                // 2. Parse coordinates
                var parts = gpsCoordinates.Split(',');
                double lat = Convert.ToDouble(parts[0]);
                double lng = Convert.ToDouble(parts[1]);

                // 3. Get weather
                var weather = await GetWeatherAsync(lat, lng, uvdate);

                // 4. Create weather panel image
                //        string uvChartPath = Path.Combine(_webHostEnvironment.WebRootPath,
                //"weather", "uvchart.png");
                // 3. Generate UV chart dynamically
                //string uvChartPath = GenerateUVChart(weather.UVIndex, weather.HourlyUV);
                string uvChartPath = GenerateUVChart(weather.HourlyUV);

                var weatherImage = CreateWeatherImageExact(weather, uvChartPath);

                // 5. Combine
                //var finalImage = CombineImages(mapPath, weatherImage);

                //return finalImage;
                return weatherImage;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetGpsWithWeatherImage: " + ex.Message);
                return "";
            }

        }
        private async Task<WeatherInfo> GetWeatherAsync(double lat, double lon, DateTime uvdate)
        {
            try
            {
                string url =
                $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}" +
                $"&current=temperature_2m,uv_index,weathercode" +
                $"&hourly=precipitation_probability,precipitation,uv_index" +
                $"&daily=uv_index_max,temperature_2m_max,temperature_2m_min" +
                $"&timezone=Australia%2FSydney&past_days=3";

                using var client = new HttpClient();
                var json = await client.GetStringAsync(url);

                var obj = Newtonsoft.Json.Linq.JObject.Parse(json);

                //// Temperature
                //double minTemp = obj["daily"]?["temperature_2m_min"]?[0]?.Value<double>() ?? 0;
                //double maxTemp = obj["daily"]?["temperature_2m_max"]?[0]?.Value<double>() ?? 0;

                //// UV Index
                //double uvIndex = obj["current"]?["uv_index"]?.Value<double>() ?? 0;

                // Temperature
                double minTemp = 0;
                double maxTemp = 0;
                // UV Index
                double uvIndex = 0;


                if (obj["daily"] != null)
                {
                    WeatherModel weatherModel = obj["daily"].ToObject<WeatherModel>();
                    var tmpmaxList = new List<double>();
                    var tmpminList = new List<double>();
                    var uvmaxList = new List<double>();
                    if (weatherModel != null)
                    {
                        for (int i = 0; i < weatherModel.date.Length; i++)
                        {
                            if (weatherModel.date[i].Date == uvdate.Date)
                            {
                                maxTemp = weatherModel.maxTmp[i];
                                minTemp = weatherModel.minTmp[i];
                                uvIndex = weatherModel.maxUvIndex[i];
                                break;
                            }
                        }
                    }
                }


                // UV Data for the day
                HourlyUvModel hourlyModel = null;
                HourlyUvModel filteredHourlyModel = null;
                var rainProbArray = new List<double>();
                var rainArray = new List<double>();

                if (obj["hourly"] != null)
                {
                    hourlyModel = obj["hourly"].ToObject<HourlyUvModel>();

                    var timeList = new List<DateTime>();
                    var uvList = new List<double>();


                    for (int i = 0; i < hourlyModel.Time.Length; i++)
                    {
                        if (hourlyModel.Time[i].Date == uvdate.Date)
                        {
                            timeList.Add(hourlyModel.Time[i]);
                            uvList.Add(hourlyModel.UvIndex[i]);
                            rainProbArray.Add(hourlyModel.PrecipitationProbability[i]);
                            rainArray.Add(hourlyModel.Precipitation[i]);
                        }
                    }

                    filteredHourlyModel = new HourlyUvModel
                    {
                        Time = timeList.ToArray(),
                        UvIndex = uvList.ToArray()
                    };

                    // uvIndex = uvList?.Max() ?? 0;
                }



                // Rain Chance + Rain MM (next 24 hours)
                //var rainProbArray = obj["hourly"]?["precipitation_probability"]?.ToObject<List<double>>() ?? new();
                //var rainArray = obj["hourly"]?["precipitation"]?.ToObject<List<double>>() ?? new();

                double rainChance = rainProbArray.Count > 0 ? rainProbArray.Max() : 0;
                double rainMm = rainArray.Sum();
                int weatherCode = obj["current"]?["weathercode"]?.Value<int>() ?? 0;
                string condition = GetWeatherCondition(weatherCode);
                return new WeatherInfo
                {
                    MinTemp = minTemp,
                    MaxTemp = maxTemp,
                    RainMm = rainMm,
                    RainChance = Convert.ToInt32(rainChance),
                    UVIndex = uvIndex,
                    Condition = condition,
                    HourlyUV = filteredHourlyModel
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching weather data: " + ex.Message);
                return new WeatherInfo
                {
                    MinTemp = 0,
                    MaxTemp = 0,
                    RainMm = 0,
                    RainChance = 0,
                    UVIndex = 0,
                    Condition = "",
                    HourlyUV = new HourlyUvModel
                    {
                        Time = new DateTime[0],
                        UvIndex = new double[0]
                    }
                };
                //throw;
            }

        }


        private string CreateWeatherImageExact(WeatherInfo weather, string uvChartPath = null)
        {
            string folder = Path.Combine(_webHostEnvironment.WebRootPath, "GpsImage", "Temp");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            //    string path = Path.Combine(folder, $"weather_{Guid.NewGuid()}.JPG");
            string filePath = Path.Combine(folder, $"weather_{Guid.NewGuid()}.jpg");

            int width = 1200;
            int height = 300;

            using var bmp = new Bitmap(width, height);
            using var g = Graphics.FromImage(bmp);

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(System.Drawing.Color.White);

            // ===== HEADER =====
            using var headerBrush = new SolidBrush(System.Drawing.Color.FromArgb(0, 102, 153));
            g.FillRectangle(headerBrush, 0, 0, width, 45);

            using var headerFont = new Font("Arial", 18, FontStyle.Bold);
            g.DrawString("Weather Updates / Alerts", headerFont, Brushes.White, 20, 8);

            // ===== DATE =====
            using var dateFont = new Font("Arial", 14, FontStyle.Regular);
            g.DrawString(DateTime.Now.ToString("dddd d MMMM"), dateFont, Brushes.Black, 20, 60);

            // ===== WEATHER ICON =====
            string iconPath = iconPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "weather", weather.Condition + ".png");
            if (File.Exists(iconPath))
            {
                using var icon = System.Drawing.Image.FromFile(iconPath);
                g.DrawImage(icon, 20, 100, 60, 60);
            }

            // ===== TEMP TEXT =====
            using var tempFont = new Font("Arial", 20, FontStyle.Bold);

            g.DrawString("Min", tempFont, Brushes.Black, 100, 110);
            g.DrawString($"{weather.MinTemp}°C", tempFont, Brushes.DodgerBlue, 150, 110);

            g.DrawString("Max", tempFont, Brushes.Black, 280, 110);
            g.DrawString($"{weather.MaxTemp}°C", tempFont, Brushes.Red, 340, 110);

            // ===== DIVIDER =====
            using var pen = new Pen(System.Drawing.Color.LightGray, 1);
            g.DrawLine(pen, 20, 180, 520, 180);

            // ===== RAIN INFO =====
            using var infoFont = new Font("Arial", 14, FontStyle.Regular);

            g.DrawString("Possible rainfall:", infoFont, Brushes.Black, 20, 200);
            g.DrawString($"{weather.RainMm} mm", infoFont, Brushes.DarkGreen, 200, 200);

            g.DrawString("Chance of any rain:", infoFont, Brushes.Black, 20, 230);
            g.DrawString($"{weather.RainChance}%", infoFont, Brushes.DarkGreen, 220, 230);

            // Rain bars
            for (int i = 0; i < 10; i++)
            {
                Brush barBrush = i < (weather.RainChance / 10)
                    ? Brushes.Green
                    : Brushes.LightGreen;

                g.FillRectangle(barBrush, 360 + (i * 18), 235, 14, 14);
            }

            // ===== UV CHART (RIGHT SIDE) =====
            if (!string.IsNullOrEmpty(uvChartPath) && File.Exists(uvChartPath))
            {
                using var uvImg = System.Drawing.Image.FromFile(uvChartPath);
                g.DrawImage(uvImg, 600, 60, 650, 200);

            }
            else
            {
                // Placeholder box
                g.FillRectangle(Brushes.WhiteSmoke, 550, 60, 600, 200);
                g.DrawRectangle(Pens.Gray, 600, 60, 600, 200);

                using var placeholderFont = new Font("Arial", 14);
                g.DrawString("UV Chart", placeholderFont, Brushes.Gray, 800, 150);
            }

            bmp.Save(filePath, System.Drawing.Imaging.ImageFormat.Jpeg);
            if (File.Exists(uvChartPath))
            {
                File.Delete(uvChartPath);
            }
            return filePath;
        }



        private string CombineImages(string mapPath, string weatherPath)
        {
            string folder = Path.Combine(_webHostEnvironment.WebRootPath, "GpsImage", "Temp");

            string output = Path.Combine(folder, $"combined_{Guid.NewGuid()}.JPG");

            using var mapImg = System.Drawing.Image.FromFile(mapPath);
            using var weatherImg = System.Drawing.Image.FromFile(weatherPath);

            int width = Math.Max(mapImg.Width, weatherImg.Width);
            int height = mapImg.Height + weatherImg.Height;

            using Bitmap finalImg = new Bitmap(width, height);
            using Graphics g = Graphics.FromImage(finalImg);

            g.Clear(System.Drawing.Color.White);

            g.DrawImage(mapImg, 0, 0);
            g.DrawImage(weatherImg, 0, mapImg.Height);

            finalImg.Save(output, System.Drawing.Imaging.ImageFormat.Png);

            return output;
        }

        private async Task<double> GetUVIndexAsync(double lat, double lon)
        {
            string url =
                $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current=uv_index";

            using var client = new HttpClient();
            var json = await client.GetStringAsync(url);

            var obj = Newtonsoft.Json.Linq.JObject.Parse(json);

            return obj["current"]?["uv_index"]?.Value<double>() ?? 0;
        }



        private string GenerateUVChart(HourlyUvModel hourlyUvModel)
        {
            try
            {
                int chartwidth = 650;
                int chartheight = 200;

                // Save file
                string folder = Path.Combine(_webHostEnvironment.WebRootPath, "GpsImage", "Temp");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string graphFileName = Path.Combine(folder, Guid.NewGuid() + "_uvChart.png");
                var options = new { fileName = graphFileName, width = chartwidth, height = chartheight };
                var selectedHours = new HashSet<string> { "05:00", "06:00", "09:00", "12:00", "15:00", "18:00", "19:00" };
                var timeList = new List<string>();
                var uvList = new List<double>();

                for (int i = 0; i < hourlyUvModel.Time.Length; i++)
                {
                    string hourMinute = hourlyUvModel.Time[i]
                        .ToString("HH:mm", CultureInfo.InvariantCulture);

                    if (selectedHours.Contains(hourMinute))
                    {
                        timeList.Add(hourMinute);
                        uvList.Add(hourlyUvModel.UvIndex[i]);
                    }
                }

                var timeData = timeList.ToArray();
                var uvData = uvList.ToArray();

                var task = StaticNodeJSService.InvokeFromFileAsync<string>("Scripts/ir-uvchart.js", "drawUvChart", args: new object[] { options, timeData, uvData });
                var success = task.Result == "OK";

                if (!success)
                {
                    graphFileName = "";
                    Console.WriteLine($"Error generating UV chart: {task.Result}");
                    //throw new ApplicationException("Create uv graph failed");
                }


                if (success && !IO.File.Exists(graphFileName))
                    graphFileName = "";
                //throw new ApplicationException($"UV Graph image not found. File Name: {graphFileName}");
                //var graphImage = new Image(ImageDataFactory.Create(graphFileName)).SetHeight(90);
                //IO.File.Delete(graphFileName);

                return graphFileName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating UV chart: {ex.Message}");
                return "";
            }

        }

        private string GenerateUVChart(double uvMax, HourlyUvModel hourlyUvModel)
        {
            int width = 650;
            int height = 300;

            Bitmap bmp = new Bitmap(width, height);
            Graphics g = Graphics.FromImage(bmp);

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(System.Drawing.Color.White);

            int marginLeft = 110;
            int marginRight = 120;
            int marginBottom = 40;
            int marginTop = 20;

            int chartWidth = width - marginLeft - marginRight;
            int chartHeight = height - marginBottom - marginTop;

            int originX = marginLeft;
            int originY = marginTop + chartHeight;

            Font font = new Font("Arial", 9);
            Font zoneFont = new Font("Arial", 9, FontStyle.Bold);

            // Zone height
            int zoneHeight = chartHeight / 5;

            // 🔥 Draw UV Zones
            DrawZone(g, originX, originY, chartWidth, zoneHeight, System.Drawing.Color.LightGreen, "Low", zoneFont);
            DrawZone(g, originX, originY - zoneHeight, chartWidth, zoneHeight, System.Drawing.Color.Yellow, "Moderate", zoneFont);
            DrawZone(g, originX, originY - 2 * zoneHeight, chartWidth, zoneHeight, System.Drawing.Color.Orange, "High", zoneFont);
            DrawZone(g, originX, originY - 3 * zoneHeight, chartWidth, zoneHeight, System.Drawing.Color.Red, "Very High", zoneFont);
            DrawZone(g, originX, originY - 4 * zoneHeight, chartWidth, zoneHeight, System.Drawing.Color.Violet, "Extreme", zoneFont);

            Pen axisPen = new Pen(System.Drawing.Color.Black, 2);

            // ✅ Axis lines
            g.DrawLine(axisPen, originX, originY, originX + chartWidth, originY); // X
            g.DrawLine(axisPen, originX, originY, originX, marginTop);            // Y

            // ✅ Y Axis Numbers (0–12)
            for (int i = 0; i <= 12; i += 3)
            {
                float y = originY - (i / 12f * chartHeight);

                g.DrawLine(Pens.Gray, originX - 5, y, originX, y);
                g.DrawString(i.ToString(), font, Brushes.Black, originX - 30, y - 7);
            }

            // ✅ X Axis Labels (time)
            for (int i = 0; i <= 12; i += 3)
            {
                int hour = 6 + i;
                float x = originX + (i / 12f * chartWidth);

                g.DrawString(hour + ":00", font, Brushes.Black, x - 15, originY + 5);
            }

            //// ✅ X Axis Labels (time)
            //for (int i = 0; i <= hourlyUvModel.Time.Length; i += 3)
            //{
            //    // int hour = 6 + i;
            //    float x = originX + (i / 12f * chartWidth);

            //    g.DrawString(hourlyUvModel.Time[i].ToString("HH:mm"), font, Brushes.Black, x - 15, originY + 5);
            //}

            // ✅ Rotated Y Axis Title (LOWER POSITION)
            // ✅ Y Axis Title (CENTERED PERFECTLY)
            string yTitle = $"Ultraviolet Radiation Level {hourlyUvModel.Time.FirstOrDefault().ToString("dd-MM-yyyy")}";
            Font yFont = new Font("Arial", 9, FontStyle.Bold);

            // Measure text size
            SizeF textSize = g.MeasureString(yTitle, yFont);

            // Move to vertical center of chart
            float centerY = originY - chartHeight / 2;

            // Position slightly left of Y axis numbers
            float xPos = originX - 50;

            // Apply transform for rotation
            g.TranslateTransform(xPos, centerY + textSize.Width / 2);
            g.RotateTransform(-90);

            // Draw text
            g.DrawString(yTitle, yFont, Brushes.Black, 0, 0);

            // Reset transform
            g.ResetTransform();


            // 🔥 UV Curve
            Pen curvePen = new Pen(System.Drawing.Color.Black, 3);

            PointF[] points = new PointF[50];

            for (int i = 0; i < points.Length; i++)
            {
                double ratio = i / (double)(points.Length - 1);
                double uv = Math.Sin(ratio * Math.PI) * uvMax;

                float x = originX + (float)(chartWidth * ratio);
                float y = originY - (float)(uv / 12.0 * chartHeight);

                points[i] = new PointF(x, y);
            }


            g.DrawLines(curvePen, points);

            // Save file
            string folder = Path.Combine(_webHostEnvironment.WebRootPath, "GpsImage", "Temp");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string path = Path.Combine(folder, Guid.NewGuid() + "_uvChart.png");

            bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);

            g.Dispose();
            bmp.Dispose();

            return path;
        }




        private void DrawZone(Graphics g, int x, int y, int width, int height,
                      System.Drawing.Color color, string label, Font font)
        {
            //using (SolidBrush brush = new SolidBrush(color))
            //{
            //    g.FillRectangle(brush, x, y - height, width, height);
            //}
            using (SolidBrush brush = new SolidBrush(System.Drawing.Color.FromArgb(60, color)))
            {
                g.FillRectangle(brush, x, y - height, width, height);
            }
            // Measure text
            SizeF textSize = g.MeasureString(label, font);

            // Position text on RIGHT side inside zone
            float textX = x + width - textSize.Width - 10; // 10px padding from right
            float textY = (y - height) + (height / 2) - (textSize.Height / 2);

            // Better contrast for dark colors
            //Brush textBrush = (color == System.Drawing.Color.Red || color == System.Drawing.Color.Violet)
            //                  ? Brushes.White
            //                  : Brushes.Black;
            Brush textBrush = Brushes.Black;

            g.DrawString(label, font, textBrush, textX, textY);
        }

        private void DrawZoneLabel(Graphics g, string text, Font font, Brush brush,
                           int originX, int yPosition, int chartWidth)
        {
            float x = originX + chartWidth + 10; // right side of chart
            g.DrawString(text, font, brush, x, yPosition - 8);
        }

        private string GetWeatherCondition(int code)
        {
            return code switch
            {
                0 => "Clear Sky",
                1 or 2 => "Partly Cloudy",
                3 => "Cloudy",
                45 or 48 => "Fog",
                51 or 53 or 55 => "Drizzle",
                61 or 63 or 65 => "Rain",
                71 or 73 or 75 => "Snow",
                80 or 81 or 82 => "Rain Showers",
                95 => "Thunderstorm",
                _ => "Unknown"
            };
        }

        //p1-341-wather in ir-created by jisha-end

    }
    public class WeatherInfo
    {
        public double MinTemp { get; set; }
        public double MaxTemp { get; set; }
        public double RainMm { get; set; }
        public int RainChance { get; set; }
        public string Condition { get; set; }
        public double UVIndex { get; set; }
        public HourlyUvModel HourlyUV { get; set; }
    }

    public class HourlyUvModel
    {
        [JsonProperty("time")]
        public DateTime[] Time { get; set; }
        [JsonProperty("uv_index")]
        public double[] UvIndex { get; set; }
        [JsonProperty("precipitation_probability")]
        public double[] PrecipitationProbability { get; set; }
        [JsonProperty("precipitation")]
        public double[] Precipitation { get; set; }
    }


    public class WeatherModel
    {
        [JsonProperty("time")]
        public DateTime[] date { get; set; }
        [JsonProperty("uv_index_max")]
        public double[] maxUvIndex { get; set; }
        [JsonProperty("temperature_2m_max")]
        public double[] maxTmp { get; set; }
        [JsonProperty("temperature_2m_min")]
        public double[] minTmp { get; set; }

    }

}
