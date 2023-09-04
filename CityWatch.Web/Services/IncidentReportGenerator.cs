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

        private const float CELL_FONT_SIZE = 6f;
        private const float CELL_FONT_SIZE_BIG = 10f;
        private const string COLOR_LIGHT_BLUE = "#d9e2f3";
        private readonly CityWatchDbContext _context;
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
                if (incidentReport.PlateLoadedYes == true)
                {
                    
                    
                    AttachKvlDetails(attachLiveGps,pdfDocument, "3", incidentReport);
                   
                }
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

        private void AttachKvlDetails(bool attachLiveGps,PdfDocument pdfDocument, string data, IncidentRequest incidentReport)
        {
            
            PdfDocument newpdf = new PdfDocument(new PdfReader(_TemplatePdf), new PdfWriter(data));
            
            //  var keyVehicleLog = _guardLogDataProvider.GetKeyVehicleLogById(keyVehicleLogId);
            var incidentreportdetails = _clientDataProvider.GetIncidentDetailsKvlReport(AuthUserHelper.LoggedInUserId.GetValueOrDefault());
            var plateIds = incidentreportdetails.Select(x => x.PlateId).ToArray();
            var truckNos = incidentreportdetails.Select(x => x.TruckNo).ToArray();
            var kvlFields = _clientDataProvider.GetKeyVehicleLogFields();
            //var plates = kvlFields.Where(z => plateIds.Contains(z.Id)).Select(x => x.Name).ToArray();

            var keyVehicleLog = _clientDataProvider.GetKeyVehiclogWithPlateIdAndTruckNoByLogId(plateIds, truckNos, AuthUserHelper.LoggedInUserId.GetValueOrDefault());
            //var keyVehicleLog = keyVehicleLog1.Where(z => DateTime.Compare(z.ClientSiteLogBook.Date, incidentReport.DateLocation.ReportDate));
            // guards.Select(z => new GuardViewModel(z, guardLogins.Where(y => y.GuardId == z.Id))).ToList();
            
            
            var TruckConfigText= kvlFields.Where(z => keyVehicleLog.Select(x => x.TruckConfig).Contains(z.Id)).Select(x => x.Name).ToArray();
            var TrailerTypeText= kvlFields.Where(z => keyVehicleLog.Select(x => x.TrailerType).Contains(z.Id)).Select(x => x.Name).ToArray();
            
            var clientsitepocdetails = _clientDataProvider.GetClientSitePocs();
            var clientsitepoc = clientsitepocdetails.Where(z => keyVehicleLog.Select(x => x.ClientSitePocId).Contains(z.Id)).Select(x => x.Name).ToArray();
            var clientsitelocdetails = _clientDataProvider.GetClientSiteLocations();
            var clientsiteloc = clientsitelocdetails.Where(z => keyVehicleLog.Select(x => x.ClientSiteLocationId).Contains(z.Id)).Select(x => x.Name).ToArray();
            var EntryReason= kvlFields.Where(z => keyVehicleLog.Select(x => x.EntryReason).Contains(z.Id)).Select(x => x.Name).ToArray();
            var PersonTypeText= kvlFields.Where(z => keyVehicleLog.Select(x => x.PersonType).Contains(z.Id)).Select(x => x.Name).ToArray();
            if (string.IsNullOrEmpty(data))
                return;
           // var index = pdfDocument.GetNumberOfPages() + 1;
           var index = 1;
            var pageSize = new PageSize(pdfDocument.GetLastPage().GetPageSize());
            var doc = new Document(pdfDocument);
            pdfDocument.AddNewPage(index, pageSize);
           // newpdf.AddNewPage(1, pageSize);

            
            // var kvlFields = _clientDataProvider.GetKeyVehicleLogFields();
            var keyVehicleLogViewModel = new KeyVehicleLogViewModel(keyVehicleLog, kvlFields);
           
            doc.SetMargins(15f, 10f, 30f, 10f);
            
            doc.Add(CreateSiteDetailsTable(keyVehicleLog));

            doc.Add(CreateReportDetailsTable(_clientDataProvider,keyVehicleLog));
            
            var countpage = newpdf.GetNumberOfPages();

            //pdfDocument.CopyPagesTo(1, 1, pdfDocument)
            var page = pdfDocument.GetFirstPage();
            if (attachLiveGps == true)
            {
                pdfDocument.MovePage(page, countpage + 2);
            }
            else
            {
                pdfDocument.MovePage(page, countpage + 1);
            }
            //var page1 = pdfDocument.GetFirstPage();

            //pdfDocument.RemovePage(page1);
            doc.Close();
           
            pdfDocument.Close();


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
        private Table CreateReportDetailsTable(IClientDataProvider _clientDataProvider,List<KeyVehicleLog> keyVehicleLogViewModel)
        {
            var outerTable = new Table(UnitValue.CreatePercentArray(new float[] { 78, 22 })).UseAllAvailableWidth().SetMarginTop(10);

            var innerTable1 = new Table(1).UseAllAvailableWidth();

            var cellClockDetails = new Cell()
                                    .SetPaddingLeft(0)
                                    .SetPaddingTop(0)
                                    .SetBorder(Border.NO_BORDER)
                                    .Add(GetClockDetailsTable(_clientDataProvider,keyVehicleLogViewModel));
            innerTable1.AddCell(cellClockDetails);
            outerTable.AddCell(cellClockDetails);
            //var cellCompanyDetails = new Cell()
            //                            .SetPaddingLeft(0)
            //                            .SetBorder(Border.NO_BORDER)
            //                            .Add(GetCompanyDetailsTable(keyVehicleLogViewModel));
            //innerTable1.AddCell(cellCompanyDetails);

            //var cellInnerTable1 = new Cell()
            //                        .SetPaddingLeft(0)
            //                        .SetPaddingTop(0)
            //                        .SetBorder(Border.NO_BORDER)
            //                        .Add(innerTable1);
            //outerTable.AddCell(cellInnerTable1);

            //var cellNotesTable = new Cell()
            //                        .SetPaddingRight(0)
            //                        .SetPaddingTop(0)
            //                        .SetBorder(Border.NO_BORDER)
            //                        .Add(GetNotesTable(keyVehicleLogViewModel,null));
            //outerTable.AddCell(cellNotesTable);

            //var cellVehicleDetails = new Cell(1, 2)
            //                            .SetPaddingLeft(0)
            //                            .SetPaddingTop(0)
            //                            .SetBorder(Border.NO_BORDER)
            //                            .Add(GetVehicleDetailsTable(keyVehicleLogViewModel));
            //outerTable.AddCell(cellVehicleDetails);

            return outerTable;
        }
        private static Table GetClockDetailsTable(IClientDataProvider _clientDataProvider, List<KeyVehicleLog> keyVehicleLogViewModel)
        {
            var clockDetails = new Table(UnitValue.CreatePercentArray(new float[] { 14, 14, 14, 14, 14,14,14,12,14,8,8,8,8,14,30,25,14,14,20,14,14,14,14,14,35})).UseAllAvailableWidth();
            
            clockDetails.AddCell(GetHeaderCell("Clocks", 1, 5));
            clockDetails.AddCell(GetHeaderCell("ID No / Vehicle Rego", 2));
            clockDetails.AddCell(GetHeaderCell("ID /Plate", 2));
            clockDetails.AddCell(GetHeaderCell("Vehicle Description", 1, 2));
            clockDetails.AddCell(GetHeaderCell("Trailers Rego or ISO", 1, 4));
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

            clockDetails.AddCell(GetHeaderCell("1"));
            clockDetails.AddCell(GetHeaderCell("2"));
            clockDetails.AddCell(GetHeaderCell("3"));
            clockDetails.AddCell(GetHeaderCell("4"));


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
                var plates = kvlFields.Where(z =>  z.Id == keyVehicleLogViewModel[i].PlateId).ToList();
                clockDetails.AddCell(GetDataCell(plates[0].Name).SetMaxWidth(14).SetMaxWidth(14));

                var TruckConfigText = kvlFields.Where(z => z.Id == keyVehicleLogViewModel[i].TruckConfig).ToList();
                clockDetails.AddCell(GetDataCell(TruckConfigText[0].Name).SetMaxWidth(12).SetMinWidth(12));
                var TrailerTypeText = kvlFields.Where(z => z.Id == keyVehicleLogViewModel[i].TrailerType).ToList();
                clockDetails.AddCell(GetDataCell(TrailerTypeText[0].Name).SetMaxWidth(14).SetMinWidth(14));

                

                
               
                
               
                
                clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel[i].Trailer1Rego).SetMaxWidth(18).SetMinWidth(18));
                clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel[i].Trailer2Rego).SetMaxWidth(18).SetMinWidth(18));
                clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel[i].Trailer3Rego).SetMaxWidth(18).SetMinWidth(18));
                clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel[i].Trailer4Rego).SetMaxWidth(18).SetMinWidth(18));
                // clockDetails.AddCell(GetDataCell(GetKeyDetailsCommaSeparated(keyVehicleLogViewModel[0].Detail), textAlignment: TextAlignment.LEFT));
                clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel[i].KeyNo).SetMaxWidth(14).SetMinWidth(14));
                clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel[i].CompanyName).SetMaxWidth(30).SetMinWidth(30));
                clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel[i].PersonName).SetMaxWidth(25).SetMinWidth(25));
                clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel[i].MobileNumber).SetMaxWidth(14).SetMinWidth(14));
               
                var PersonTypeText = kvlFields.Where(z => z.Id == keyVehicleLogViewModel[i].PersonType).ToList();
                clockDetails.AddCell(GetDataCell(PersonTypeText[0].Name).SetMaxWidth(15).SetMinWidth(15));
               

                var clientsitepocdetails = _clientDataProvider.GetClientSitePocs();
                var clientsitepoc = clientsitepocdetails.Where(z => z.Id == keyVehicleLogViewModel[i].ClientSitePocId).ToList();
                clockDetails.AddCell(GetDataCell(clientsitepoc[0].Name).SetMaxWidth(20).SetMinWidth(20));

                var clientsitelocdetails = _clientDataProvider.GetClientSiteLocations();
                var clientsiteloc = clientsitelocdetails.Where(z => z.Id == keyVehicleLogViewModel[i].ClientSiteLocationId).ToList();
                clockDetails.AddCell(GetDataCell(clientsiteloc[0].Name).SetMaxWidth(19).SetMinWidth(19));

                var EntryReason = kvlFields.Where(z => z.Id == keyVehicleLogViewModel[i].EntryReason).ToList();
                clockDetails.AddCell(GetDataCell(EntryReason[0].Name).SetMaxWidth(14).SetMinWidth(14));

                
                
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


    }
}
