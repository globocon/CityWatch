using CityWatch.Common.Helpers;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Helpers;
using CityWatch.Web.Models;
using DocumentFormat.OpenXml.Bibliography;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Pdfa;
using iText.StyledXmlParser.Jsoup.Helper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Asn1.Cmp;
using Org.BouncyCastle.Crypto.Paddings;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using static Dropbox.Api.TeamLog.ClassificationType;
using static Dropbox.Api.TeamLog.PaperDownloadFormat;
using IO = System.IO;

namespace CityWatch.Web.Services
{
    public enum ManualDocketReason
    {
        Other,

        NoComms,

        PhysicalRepair,

        POD
    }

    public interface IKeyVehicleLogDocketGenerator
    {
        string GeneratePdfReport(int keyVehicleLogId, string docketReason, string blankNoteOnOrOff, string serialNo);
        string GeneratePdfReportList(int keyVehicleLogId, string docketReason, string blankNoteOnOrOff, string serialNo, List<int> ids, int clientsiteid);
        string GeneratePdfReportListGlobal(int keyVehicleLogId1, string docketReason, string blankNoteOnOrOff, string serialNo, List<int> ids);
        public string GenerateBulkPdfReport(List<int> keyVehicleLogId, string docketReason, string blankNoteOnOrOff, string serialNo);
        public string GenerateBulkIndividualPdfReportWithZip(List<int> keyVehicleLogId, string docketReason, string blankNoteOnOrOff, string serialNo);

    }
    public class KeyVehicleLogDocketGenerator : IKeyVehicleLogDocketGenerator
    {
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        private readonly IGuardSettingsDataProvider _guardSettingsDataProvider;
        private readonly IWebHostEnvironment _WebHostEnvironment;
        private readonly IAppConfigurationProvider _appConfigurationProvider;
        private readonly IViewDataService _viewDataService;
        private readonly string _reportRootDir;
        private readonly string _imageRootDir;
        private readonly string _PersonimageRootDir;
        private readonly string _vehicleimageRootDir;
        private readonly string _SiteimageRootDir;
        private readonly Settings _settings;

        private const float CELL_FONT_SIZE = 6f;
        private const float CELL_FONT_SIZE_BIG = 10f;
        private const string REPORT_DIR = "Output";
        private const string COLOR_LIGHT_BLUE = "#d9e2f3";
        private const string LAST_USED_DOCKET_SEQ_NO_CONFIG_NAME = "LastUsedDocketSn";
        private readonly string _downloadsFolderPath;
        public KeyVehicleLogDocketGenerator(IWebHostEnvironment webHostEnvironment,
           IGuardLogDataProvider guardLogDataProvider,
           IClientDataProvider clientDataProvider,
           IGuardSettingsDataProvider guardSettingsDataProvider,
           IOptions<Settings> settings, IAppConfigurationProvider appConfigurationProvider,
            IViewDataService viewDataService)
        {
            _clientDataProvider = clientDataProvider;
            _guardLogDataProvider = guardLogDataProvider;
            _guardSettingsDataProvider = guardSettingsDataProvider;
            _reportRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "Pdf");
            _imageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "images");
            _PersonimageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "KvlUploads", "Person");
            _vehicleimageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "KvlUploads");
            _SiteimageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "SiteImage");
            _settings = settings.Value;
            _appConfigurationProvider = appConfigurationProvider;
            _downloadsFolderPath = IO.Path.Combine(webHostEnvironment.WebRootPath, "Pdf", "FromDropbox");
            _viewDataService = viewDataService;
            _WebHostEnvironment = webHostEnvironment;
        }
        //To Generate the Pdf In List start
        public string GeneratePdfReportList(int keyVehicleLogId1, string docketReason, string blankNoteOnOrOff, string serialNo, List<int> ids, int clientsiteid)
        {

            var kvlFields = _guardLogDataProvider.GetKeyVehicleLogFields();
            var profiles = _guardLogDataProvider.GetPOIListFromVisitorPersonalDetails();
            var createdLogIds = profiles.Select(z => z.KeyVehicleLogProfile.CreatedLogId).Where(z => z > 0).ToArray();
            var kvls = _guardLogDataProvider.GetKeyVehicleLogByIds(createdLogIds);
            foreach (var profile in profiles)
            {
                profile.KeyVehicleLogProfile.KeyVehicleLog = kvls.SingleOrDefault(z => z.Id == profile.KeyVehicleLogProfile.CreatedLogId);

            }

            var poiList = profiles.Where(z => z.PersonOfInterest != null).Select(z => new KeyVehicleLogProfileViewModel(z, kvlFields)).ToList();
            if (poiList.Count != 0)
            {
                var idsNew = poiList.Where(x => x.ClientSiteId == clientsiteid).Select(x => x.Detail.Id).ToArray();
                var CliSiteDetails = _guardLogDataProvider.GetClientSites(clientsiteid);
                if (CliSiteDetails != null)
                {
                    if (idsNew.Length != 0)
                    {
                        var reportPdfPath1 = IO.Path.Combine(_reportRootDir, REPORT_DIR, $"{DateTime.Today:yyyyMMdd}_KVManualDocket_{CliSiteDetails.FirstOrDefault().Name}_SN{serialNo}.pdf");
                        var pdfDocList = new PdfDocument(new PdfWriter(reportPdfPath1));
                        pdfDocList.SetDefaultPageSize(PageSize.A4);
                        var docList = new Document(pdfDocList);
                        //docList.Add(CreateReportHeaderTableList(keyVehicleLogData.ClientSiteLogBook.ClientSiteId));
                        docList.Add(CreateReportHeaderTableList(clientsiteid));

                        var reportPdfPath = "";

                        poiList = poiList.Where(x => x.ClientSiteId == clientsiteid).ToList();
                        var last = poiList.Last();
                        foreach (var i in poiList)
                        {

                            if (i.Detail.KeyVehicleLogProfile.KeyVehicleLog != null)
                            {
                                //    for (int i = 0; i < idsNew.Length; i++)
                                //{
                                var keyVehicleLogId = i.Detail.KeyVehicleLogProfile.KeyVehicleLog.Id;
                                var keyVehicleLog = _guardLogDataProvider.GetKeyVehicleLogById(keyVehicleLogId);
                                if (keyVehicleLog != null)
                                {
                                    if (keyVehicleLog.ClientSiteLogBook != null)
                                    {



                                        _guardLogDataProvider.SaveDocketSerialNo(keyVehicleLog.Id, serialNo);

                                        if (keyVehicleLog == null)
                                            return string.Empty;


                                        var keyVehicleLogViewModel = new KeyVehicleLogViewModel(keyVehicleLog, kvlFields);
                                        reportPdfPath = IO.Path.Combine(_reportRootDir, REPORT_DIR, $"{DateTime.Today:yyyyMMdd}_KVManualDocket_{CliSiteDetails.FirstOrDefault().Name}_SN{serialNo}.pdf");

                                        if (IO.File.Exists(reportPdfPath))
                                            //IO.File.Delete(reportPdfPath);


                                            docList.SetMargins(15f, 30f, 40f, 30f);

                                        docList.Add(CreateSiteDetailsTable(keyVehicleLog));
                                        docList.Add(CreateReportDetailsTable(keyVehicleLogViewModel, blankNoteOnOrOff));
                                        docList.Add(GetKeyAndOtherDetailsTable(keyVehicleLogViewModel));
                                        docList.Add(GetCustomerRefAndVwiTable(keyVehicleLogViewModel));
                                        docList.Add(CreateWeightandOtherDetailsTablePOI(keyVehicleLogViewModel, docketReason));
                                        docList.Add(CreateImageDetailsTable(keyVehicleLogViewModel, docketReason));

                                        var path = keyVehicleLog.Id + "/ComplianceDocuments";
                                        int countpagebreak = 0;
                                        var keyVehicleLogDetails = _viewDataService.GetKeyVehicleLogAttachments(
                                             IO.Path.Combine(_WebHostEnvironment.WebRootPath, "KvlUploads"), path)
                                             .ToList();
                                        if (keyVehicleLogDetails.Count > 0)
                                        {
                                            for (int j = 0; j < keyVehicleLogDetails.Count; j++)
                                            {
                                                string filename = IO.Path.Combine(IO.Path.Combine(IO.Path.Combine(_WebHostEnvironment.WebRootPath, "KvlUploads"), path), keyVehicleLogDetails[j]);
                                                int pagecount = 0;

                                                if (IO.Path.GetExtension(filename) == ".pdf")
                                                {
                                                    using (StreamReader sr = new StreamReader(File.OpenRead(filename)))
                                                    {
                                                        Regex regex = new Regex(@"/Type\s*/Page[^s]");
                                                        MatchCollection matches = regex.Matches(sr.ReadToEnd());

                                                        pagecount = matches.Count;
                                                    }
                                                    PdfReader reader = new PdfReader(filename);
                                                    PdfDocument docfile = new PdfDocument(reader);
                                                    int pagecountnew = docfile.GetNumberOfPages();
                                                    docfile.CopyPagesTo(1, pagecountnew, pdfDocList);
                                                    for (int countpage = 0; countpage < pagecountnew; countpage++)
                                                    {
                                                        docList.Add(new AreaBreak(AreaBreakType.NEXT_AREA));

                                                    }

                                                }
                                                if (IO.Path.GetExtension(filename) == ".jpg")
                                                {
                                                    //var pdfDocneew = new PdfDocument(new PdfReader(filename));
                                                    //pdfDocneew.SetDefaultPageSize(PageSize.A4);
                                                    //var docnew = new Document(pdfDocneew);

                                                    //docnew.SetMargins(15f, 30f, 40f, 30f);
                                                    //var pageSize = new PageSize(pdfDoc.GetLastPage().GetPageSize());
                                                    //pdfDoc.AddNewPage(1, pageSize);
                                                    //doc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));

                                                    docList.Add(new AreaBreak(AreaBreakType.NEXT_AREA));
                                                    docList.Add(GetImageFileTable(filename, keyVehicleLog.Id));

                                                    //var countpage = pdfDoc.GetNumberOfPages();
                                                    //var page = pdfDoc.GetFirstPage();
                                                    //pdfDoc.MovePage(page, countpage + 1);
                                                    //docnew.Close();
                                                    //pdfDocneew.Close();
                                                    ////PdfReader reader = new PdfReader(pdfDocneew);
                                                    ////PdfDocument docfile = new PdfDocument(reader);
                                                    //int pagecountnew = pdfDocneew.GetNumberOfPages();
                                                    //pdfDocneew.CopyPagesTo(1, pagecountnew, pdfDoc);
                                                }
                                            }
                                        }



                                        if (i != last)
                                        {
                                            docList.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                                        }


                                    }


                                }
                            }
                            //}
                        }

                        docList.Close();
                        pdfDocList.Close();
                        return IO.Path.GetFileName(reportPdfPath1);


                    }
                    else return string.Empty;
                }
                else return string.Empty;
            }
            else return string.Empty;
        }
        //To Generate the Pdf In List stop

        //To generate POI List Globally strat
        public string GeneratePdfReportListGlobal(int keyVehicleLogId1, string docketReason, string blankNoteOnOrOff, string serialNo, List<int> ids)
        {
            var kvlFields = _guardLogDataProvider.GetKeyVehicleLogFields();
            var profiles = _guardLogDataProvider.GetPOIListFromVisitorPersonalDetails();
            var createdLogIds = profiles.Select(z => z.KeyVehicleLogProfile.CreatedLogId).Where(z => z > 0).ToArray();
            var kvls = _guardLogDataProvider.GetKeyVehicleLogByIds(createdLogIds);
            foreach (var profile in profiles)
            {
                profile.KeyVehicleLogProfile.KeyVehicleLog = kvls.SingleOrDefault(z => z.Id == profile.KeyVehicleLogProfile.CreatedLogId);

            }

            var poiList = profiles.Where(z => z.PersonOfInterest != null).Select(z => new KeyVehicleLogProfileViewModel(z, kvlFields)).ToList();
            if (poiList.Count != 0)
            {
                var idsNew = poiList.Select(x => x.Detail.Id).ToArray();
                //var CliSiteDetails = _guardLogDataProvider.GetClientSites(clientsiteid);

                if (idsNew.Length != 0)
                {
                    var reportPdfPath1 = IO.Path.Combine(_reportRootDir, REPORT_DIR, $"{DateTime.Today:yyyyMMdd}_KVManualDocket_{"POI_ListAll"}_SN{serialNo}.pdf");
                    var pdfDocList = new PdfDocument(new PdfWriter(reportPdfPath1));
                    pdfDocList.SetDefaultPageSize(PageSize.A4);
                    var docList = new Document(pdfDocList);
                    //docList.Add(CreateReportHeaderTableList(keyVehicleLogData.ClientSiteLogBook.ClientSiteId));
                    docList.Add(CreateReportHeaderTableList(0));

                    var reportPdfPath = "";

                    var last = poiList.Last();
                    foreach (var i in poiList)
                    {

                        //    for (int i = 0; i < idsNew.Length; i++)
                        //{

                        if (i.Detail.KeyVehicleLogProfile.KeyVehicleLog != null)
                        {
                            var keyVehicleLogId = i.Detail.KeyVehicleLogProfile.KeyVehicleLog.Id;
                            var keyVehicleLog = _guardLogDataProvider.GetKeyVehicleLogById(keyVehicleLogId);
                            if (keyVehicleLog != null)
                            {
                                if (keyVehicleLog.ClientSiteLogBook != null)
                                {



                                    _guardLogDataProvider.SaveDocketSerialNo(keyVehicleLog.Id, serialNo);

                                    if (keyVehicleLog == null)
                                        return string.Empty;


                                    var keyVehicleLogViewModel = new KeyVehicleLogViewModel(keyVehicleLog, kvlFields);
                                    reportPdfPath = IO.Path.Combine(_reportRootDir, REPORT_DIR, $"{DateTime.Today:yyyyMMdd}_KVManualDocket_{"POI_ListAll"}_SN{serialNo}.pdf");

                                    if (IO.File.Exists(reportPdfPath))
                                        //IO.File.Delete(reportPdfPath);


                                        docList.SetMargins(15f, 30f, 40f, 30f);

                                    docList.Add(CreateSiteDetailsTable(keyVehicleLog));
                                    docList.Add(CreateReportDetailsTable(keyVehicleLogViewModel, blankNoteOnOrOff));
                                    docList.Add(GetKeyAndOtherDetailsTable(keyVehicleLogViewModel));
                                    docList.Add(GetCustomerRefAndVwiTable(keyVehicleLogViewModel));
                                    docList.Add(CreateWeightandOtherDetailsTablePOI(keyVehicleLogViewModel, docketReason));
                                    docList.Add(CreateImageDetailsTable(keyVehicleLogViewModel, docketReason));

                                    var path = keyVehicleLog.Id + "/ComplianceDocuments";
                                    int countpagebreak = 0;
                                    var keyVehicleLogDetails = _viewDataService.GetKeyVehicleLogAttachments(
                                         IO.Path.Combine(_WebHostEnvironment.WebRootPath, "KvlUploads"), path)
                                         .ToList();
                                    if (keyVehicleLogDetails.Count > 0)
                                    {
                                        for (int j = 0; j < keyVehicleLogDetails.Count; j++)
                                        {
                                            string filename = IO.Path.Combine(IO.Path.Combine(IO.Path.Combine(_WebHostEnvironment.WebRootPath, "KvlUploads"), path), keyVehicleLogDetails[j]);
                                            int pagecount = 0;

                                            if (IO.Path.GetExtension(filename) == ".pdf")
                                            {
                                                using (StreamReader sr = new StreamReader(File.OpenRead(filename)))
                                                {
                                                    Regex regex = new Regex(@"/Type\s*/Page[^s]");
                                                    MatchCollection matches = regex.Matches(sr.ReadToEnd());

                                                    pagecount = matches.Count;
                                                }
                                                PdfReader reader = new PdfReader(filename);
                                                PdfDocument docfile = new PdfDocument(reader);
                                                int pagecountnew = docfile.GetNumberOfPages();
                                                docfile.CopyPagesTo(1, pagecountnew, pdfDocList);
                                                for (int countpage = 0; countpage < pagecountnew; countpage++)
                                                {
                                                    docList.Add(new AreaBreak(AreaBreakType.NEXT_AREA));

                                                }
                                            }
                                            if (IO.Path.GetExtension(filename) == ".jpg")
                                            {
                                                //var pdfDocneew = new PdfDocument(new PdfReader(filename));
                                                //pdfDocneew.SetDefaultPageSize(PageSize.A4);
                                                //var docnew = new Document(pdfDocneew);

                                                //docnew.SetMargins(15f, 30f, 40f, 30f);
                                                //var pageSize = new PageSize(pdfDoc.GetLastPage().GetPageSize());
                                                //pdfDoc.AddNewPage(1, pageSize);
                                                //doc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                                                int pagecount1 = pdfDocList.GetNumberOfPages();

                                                docList.Add(new AreaBreak(AreaBreakType.NEXT_AREA));

                                                docList.Add(GetImageFileTable(filename, keyVehicleLog.Id));

                                                //var countpage = pdfDoc.GetNumberOfPages();
                                                //var page = pdfDoc.GetFirstPage();
                                                //pdfDoc.MovePage(page, countpage + 1);
                                                //docnew.Close();
                                                //pdfDocneew.Close();
                                                ////PdfReader reader = new PdfReader(pdfDocneew);
                                                ////PdfDocument docfile = new PdfDocument(reader);
                                                //int pagecountnew = pdfDocneew.GetNumberOfPages();
                                                //pdfDocneew.CopyPagesTo(1, pagecountnew, pdfDoc);
                                            }
                                        }
                                    }


                                    if (i != last)
                                    {
                                        docList.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                                    }


                                }


                            }

                        }
                        //}
                    }

                    docList.Close();
                    pdfDocList.Close();
                    return IO.Path.GetFileName(reportPdfPath1);



                }
                else return string.Empty;
            }
            else return string.Empty;

        }
        //To generate POI List Glabally stop
        public string GeneratePdfReport(int keyVehicleLogId, string docketReason, string blankNoteOnOrOff, string serialNo)
        {
            var keyVehicleLog = _guardLogDataProvider.GetKeyVehicleLogById(keyVehicleLogId);
            //To Get the SitePocNames start
            if (keyVehicleLog.ClientSitePocIdsVehicleLog != null)
            {
                var POCIds = keyVehicleLog.ClientSitePocIdsVehicleLog.Split(',');
                var pocNamesList = new List<string>();
                pocNamesList.Clear();
                foreach (var item in POCIds)
                {
                    var PocName = _guardLogDataProvider.GetClientSitePOCName(Convert.ToInt32(item));
                    pocNamesList.Add(PocName.Name);
                }

                var pocNames = string.Join(", ", pocNamesList);
                keyVehicleLog.SitePocNames = pocNames;
            }

            //To Get the SitePocNames stop

            _guardLogDataProvider.SaveDocketSerialNo(keyVehicleLogId, serialNo);

            if (keyVehicleLog == null)
                return string.Empty;

            var kvlFields = _guardLogDataProvider.GetKeyVehicleLogFields();
            var keyVehicleLogViewModel = new KeyVehicleLogViewModel(keyVehicleLog, kvlFields);
            var reportPdfPath = IO.Path.Combine(_reportRootDir, REPORT_DIR, $"{DateTime.Today:yyyyMMdd}_KVManualDocket_{keyVehicleLog.GuardLogin.ClientSite.Name}_SN{serialNo}.pdf");



            if (IO.File.Exists(reportPdfPath))
                IO.File.Delete(reportPdfPath);

            var pdfDoc = new PdfDocument(new PdfWriter(reportPdfPath));
            pdfDoc.SetDefaultPageSize(PageSize.A4);
            var doc = new Document(pdfDoc);

            doc.SetMargins(15f, 30f, 40f, 30f);

            doc.Add(CreateReportHeaderTable(keyVehicleLog.ClientSiteLogBook.ClientSiteId));
            doc.Add(CreateSiteDetailsTable(keyVehicleLog));
            doc.Add(CreateReportDetailsTable(keyVehicleLogViewModel, blankNoteOnOrOff));
            doc.Add(GetKeyAndOtherDetailsTable(keyVehicleLogViewModel));
            doc.Add(GetCustomerRefAndVwiTable(keyVehicleLogViewModel));
            doc.Add(CreateWeightandOtherDetailsTable(keyVehicleLogViewModel, docketReason));
            doc.Add(CreateImageDetailsTable(keyVehicleLogViewModel, docketReason));



            //p7-115 docket output issues-start
            //pdfDocument.AddNewPage();
            var path = keyVehicleLog.Id + "/ComplianceDocuments";
            int countpagebreak = 0;
            var keyVehicleLogDetails = _viewDataService.GetKeyVehicleLogAttachments(
                 IO.Path.Combine(_WebHostEnvironment.WebRootPath, "KvlUploads"), path)
                 .ToList();
            if (keyVehicleLogDetails.Count > 0)
            {
                for (int i = 0; i < keyVehicleLogDetails.Count; i++)
                {
                    string filename = IO.Path.Combine(IO.Path.Combine(IO.Path.Combine(_WebHostEnvironment.WebRootPath, "KvlUploads"), path), keyVehicleLogDetails[i]);
                    int pagecount = 0;

                    if (IO.Path.GetExtension(filename) == ".pdf")
                    {
                        using (StreamReader sr = new StreamReader(File.OpenRead(filename)))
                        {
                            Regex regex = new Regex(@"/Type\s*/Page[^s]");
                            MatchCollection matches = regex.Matches(sr.ReadToEnd());

                            pagecount = matches.Count;
                        }
                        PdfReader reader = new PdfReader(filename);
                        PdfDocument docfile = new PdfDocument(reader);
                        int pagecountnew = docfile.GetNumberOfPages();
                        docfile.CopyPagesTo(1, pagecountnew, pdfDoc);

                    }
                    if (IO.Path.GetExtension(filename) == ".jpg")
                    {
                        //var pdfDocneew = new PdfDocument(new PdfReader(filename));
                        //pdfDocneew.SetDefaultPageSize(PageSize.A4);
                        //var docnew = new Document(pdfDocneew);

                        //docnew.SetMargins(15f, 30f, 40f, 30f);
                        //var pageSize = new PageSize(pdfDoc.GetLastPage().GetPageSize());
                        //pdfDoc.AddNewPage(1, pageSize);
                        //doc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                        int pagecount1 = pdfDoc.GetNumberOfPages();
                        if (countpagebreak == 0)
                        {
                            for (int countpage = 0; countpage < pagecount1; countpage++)
                            {
                                doc.Add(new AreaBreak(AreaBreakType.NEXT_AREA));
                                countpagebreak = countpagebreak + 1;
                            }
                        }
                        else
                        {
                            doc.Add(new AreaBreak(AreaBreakType.NEXT_AREA));
                        }
                        doc.Add(GetImageFileTable(filename, keyVehicleLog.Id));

                        //var countpage = pdfDoc.GetNumberOfPages();
                        //var page = pdfDoc.GetFirstPage();
                        //pdfDoc.MovePage(page, countpage + 1);
                        //docnew.Close();
                        //pdfDocneew.Close();
                        ////PdfReader reader = new PdfReader(pdfDocneew);
                        ////PdfDocument docfile = new PdfDocument(reader);
                        //int pagecountnew = pdfDocneew.GetNumberOfPages();
                        //pdfDocneew.CopyPagesTo(1, pagecountnew, pdfDoc);
                    }
                }
            }

            //p7 - 115 docket output issues - end
            doc.Close();
            pdfDoc.Close();

            return IO.Path.GetFileName(reportPdfPath);
        }
        //p7 - 115 docket output issues - start
        private Table GetImageFileTable(string filename, int id)
        {
            string originalfile = IO.Path.GetFileName(filename);
            var ImageTable = new Table(1).UseAllAvailableWidth();
            //var path = id + "/c";
            var fromFolderPath = IO.Path.Combine(IO.Path.Combine(_WebHostEnvironment.WebRootPath, "KvlUploads"), id.ToString(), "ComplianceDocuments");
            //var toFolderPath= IO.Path.Combine(IO.Path.Combine(_WebHostEnvironment.WebRootPath, "KvlUploads"), path +"/Images");
            //if (!Directory.Exists(toFolderPath))
            //    Directory.CreateDirectory(toFolderPath);

            //    System.IO.File.Copy(IO.Path.Combine(fromFolderPath, originalfile), IO.Path.Combine(toFolderPath, originalfile), true);

            ImageData imagedata = ImageDataFactory.Create(IO.Path.Combine(fromFolderPath, IO.Path.GetFileNameWithoutExtension(filename) + ".jpg"));
            iText.Layout.Element.Image coverImage = new iText.Layout.Element.Image(imagedata);

            var Imagepath = new Image(ImageDataFactory.Create(IO.Path.Combine(fromFolderPath, originalfile)))
             .SetHeight(160);


            ImageTable.AddCell(new Cell().Add(Imagepath).SetBorder(Border.NO_BORDER));





            return ImageTable;
        }
        //p7 - 115 docket output issues - end
        /* bulk docket generate Start*/
        public string GenerateBulkPdfReport(List<int> keyVehicleLogId, string docketReason, string blankNoteOnOrOff, string serialNo)
        {
            var reportPdfPath1 = IO.Path.Combine(_reportRootDir, REPORT_DIR, $"{DateTime.Today:yyyyMMdd}_KVManualDocket_{"Bulk"}_SN{serialNo}.pdf");
            var pdfDocList = new PdfDocument(new PdfWriter(reportPdfPath1));
            pdfDocList.SetDefaultPageSize(PageSize.A4);
            var docList = new Document(pdfDocList);
            //docList.Add(CreateReportHeaderTableList(keyVehicleLogData.ClientSiteLogBook.ClientSiteId));
            docList.Add(CreateReportHeaderTableListforBulkDocket(0));

            var reportPdfPath = "";

            var last = keyVehicleLogId.Last();
            int countpagebreak = 0;
            foreach (var i in keyVehicleLogId)
            {

                var keyVehicleLog = _guardLogDataProvider.GetKeyVehicleLogById(i);
                serialNo = GetNextDocketSequenceNumber(i);
                _guardLogDataProvider.SaveDocketSerialNo(i, serialNo);

                if (keyVehicleLog == null)
                    return string.Empty;

                var kvlFields = _guardLogDataProvider.GetKeyVehicleLogFields();
                var keyVehicleLogViewModel = new KeyVehicleLogViewModel(keyVehicleLog, kvlFields);



                if (keyVehicleLog != null)
                {
                    if (keyVehicleLog.ClientSiteLogBook != null)
                    {



                        //_guardLogDataProvider.SaveDocketSerialNo(keyVehicleLog.Id, serialNo);

                        if (keyVehicleLog == null)
                            return string.Empty;



                        reportPdfPath = IO.Path.Combine(_reportRootDir, REPORT_DIR, $"{DateTime.Today:yyyyMMdd}_KVManualDocket_{"POI_ListAll"}_SN{serialNo}.pdf");

                        if (IO.File.Exists(reportPdfPath))
                            //IO.File.Delete(reportPdfPath);


                            docList.SetMargins(15f, 30f, 40f, 30f);

                        docList.Add(CreateSiteDetailsTable(keyVehicleLog));
                        docList.Add(CreateReportDetailsTable(keyVehicleLogViewModel, blankNoteOnOrOff));
                        docList.Add(GetKeyAndOtherDetailsTable(keyVehicleLogViewModel));
                        docList.Add(GetCustomerRefAndVwiTable(keyVehicleLogViewModel));
                        docList.Add(CreateWeightandOtherDetailsTable(keyVehicleLogViewModel, docketReason));
                        docList.Add(CreateImageDetailsTable(keyVehicleLogViewModel, docketReason));

                        //p7-115 docket output issues-start
                        //pdfDocument.AddNewPage();
                        var path = keyVehicleLog.Id + "/ComplianceDocuments";

                        var keyVehicleLogDetails = _viewDataService.GetKeyVehicleLogAttachments(
                             IO.Path.Combine(_WebHostEnvironment.WebRootPath, "KvlUploads"), path)
                             .ToList();

                        if (keyVehicleLogDetails.Count > 0)
                        {
                            for (int j = 0; j < keyVehicleLogDetails.Count; j++)
                            {
                                string filename = IO.Path.Combine(IO.Path.Combine(IO.Path.Combine(_WebHostEnvironment.WebRootPath, "KvlUploads"), path), keyVehicleLogDetails[j]);
                                int pagecount = 0;
                                if (IO.Path.GetExtension(filename) == ".pdf")
                                {
                                    using (StreamReader sr = new StreamReader(File.OpenRead(filename)))
                                    {
                                        Regex regex = new Regex(@"/Type\s*/Page[^s]");
                                        MatchCollection matches = regex.Matches(sr.ReadToEnd());

                                        pagecount = matches.Count;
                                    }
                                    PdfReader reader = new PdfReader(filename);
                                    PdfDocument docfile = new PdfDocument(reader);
                                    int pagecountnew = docfile.GetNumberOfPages();
                                    docfile.CopyPagesTo(1, pagecountnew, pdfDocList);

                                    for (int countpage = 0; countpage < pagecountnew; countpage++)
                                    {
                                        docList.Add(new AreaBreak(AreaBreakType.NEXT_AREA));
                                        //countpagebreak = countpagebreak + 1;
                                    }

                                }
                                if (IO.Path.GetExtension(filename) == ".jpg")
                                {
                                    //var pdfDocneew = new PdfDocument(new PdfReader(filename));
                                    //pdfDocneew.SetDefaultPageSize(PageSize.A4);
                                    //var docnew = new Document(pdfDocneew);

                                    //docnew.SetMargins(15f, 30f, 40f, 30f);
                                    //var pageSize = new PageSize(pdfDocList.GetLastPage().GetPageSize());
                                    //pdfDocList.AddNewPage(1, pageSize);
                                    //int pagecount1 = pdfDocList.GetNumberOfPages();

                                    docList.Add(new AreaBreak(AreaBreakType.NEXT_AREA));

                                    docList.Add(GetImageFileTable(filename, keyVehicleLog.Id));

                                    //var countpage = pdfDocList.GetNumberOfPages();
                                    //var page = pdfDocList.GetFirstPage();
                                    //pdfDocList.MovePage(page, countpage + 1);
                                    //docnew.Close();
                                    //pdfDocneew.Close();
                                    ////PdfReader reader = new PdfReader(pdfDocneew);
                                    ////PdfDocument docfile = new PdfDocument(reader);
                                    //int pagecountnew = pdfDocneew.GetNumberOfPages();
                                    //pdfDocneew.CopyPagesTo(1, pagecountnew, pdfDoc);
                                }
                            }
                        }

                        //p7 - 115 docket output issues - end


                        if (i != last)
                        {
                            docList.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                        }


                    }




                }

            }

            docList.Close();
            pdfDocList.Close();
            return IO.Path.GetFileName(reportPdfPath1);


            //var keyVehicleLog = _guardLogDataProvider.GetKeyVehicleLogById(keyVehicleLogId);
            //_guardLogDataProvider.SaveDocketSerialNo(keyVehicleLogId, serialNo);

            //if (keyVehicleLog == null)
            //    return string.Empty;

            //var kvlFields = _guardLogDataProvider.GetKeyVehicleLogFields();
            //var keyVehicleLogViewModel = new KeyVehicleLogViewModel(keyVehicleLog, kvlFields);
            //var reportPdfPath = IO.Path.Combine(_reportRootDir, REPORT_DIR, $"{DateTime.Today:yyyyMMdd}_KVManualDocket_{keyVehicleLog.GuardLogin.ClientSite.Name}_SN{serialNo}.pdf");

            //if (IO.File.Exists(reportPdfPath))
            //    IO.File.Delete(reportPdfPath);

            //var pdfDoc = new PdfDocument(new PdfWriter(reportPdfPath));
            //pdfDoc.SetDefaultPageSize(PageSize.A4);
            //var doc = new Document(pdfDoc);

            //doc.SetMargins(15f, 30f, 40f, 30f);

            //doc.Add(CreateReportHeaderTable(keyVehicleLog.ClientSiteLogBook.ClientSiteId));
            //doc.Add(CreateSiteDetailsTable(keyVehicleLog));
            //doc.Add(CreateReportDetailsTable(keyVehicleLogViewModel, blankNoteOnOrOff));
            //doc.Add(GetKeyAndOtherDetailsTable(keyVehicleLogViewModel));
            //doc.Add(GetCustomerRefAndVwiTable(keyVehicleLogViewModel));
            //doc.Add(CreateWeightandOtherDetailsTable(keyVehicleLogViewModel, docketReason));
            //doc.Add(CreateImageDetailsTable(keyVehicleLogViewModel, docketReason));


            //doc.Close();
            //pdfDoc.Close();

            //return IO.Path.GetFileName(reportPdfPath);

        }



        private string GetNextDocketSequenceNumber(int id)
        {
            var lastSequenceNumber = 0;
            var keyVehicleLog = _guardLogDataProvider.GetKeyVehicleLogById(id);
            if (!string.IsNullOrEmpty(keyVehicleLog.DocketSerialNo))
            {
                var serialNo = keyVehicleLog.DocketSerialNo;
                var suffix = Regex.Replace(serialNo, @"[^A-Z]+", string.Empty);
                int index = GetSuffixNumber(suffix);
                var numberSuffix = GetSequenceNumberSuffix(index);
                var serialnumber = string.Join("", serialNo.ToCharArray().Where(Char.IsDigit));
                return $"{serialnumber}-{numberSuffix}";
            }

            var configuration = _appConfigurationProvider.GetConfigurationByName(LAST_USED_DOCKET_SEQ_NO_CONFIG_NAME);
            if (configuration != null)
            {
                lastSequenceNumber = int.Parse(configuration.Value);
                lastSequenceNumber++;
                configuration.Value = lastSequenceNumber.ToString();
                _appConfigurationProvider.SaveConfiguration(configuration);
            }

            return lastSequenceNumber.ToString().PadLeft(6, '0');
        }

        private static int GetSuffixNumber(string suffix)
        {
            int index = 0;
            // Start suffix from B
            string alphabet = string.IsNullOrEmpty(suffix) ? "A" : suffix.ToUpper();
            for (int iChar = alphabet.Length - 1; iChar >= 0; iChar--)
            {
                char colPiece = alphabet[iChar];
                int colNum = colPiece - 64;
                index += colNum * (int)Math.Pow(26, alphabet.Length - (iChar + 1));
            }
            return index;
        }


        private static string GetSequenceNumberSuffix(int index)
        {
            string value = "";
            decimal number = index + 1;
            while (number > 0)
            {
                decimal currentLetterNumber = (number - 1) % 26;
                char currentLetter = (char)(currentLetterNumber + 65);
                value = currentLetter + value;
                number = (number - (currentLetterNumber + 1)) / 26;
            }
            return value;
        }
        /* bulk docket generate end*/
        private Table CreateReportHeaderTableList(int clientSiteId)
        {
            var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 20, 50, 30 })).UseAllAvailableWidth();

            var cwLogo = new Image(ImageDataFactory.Create(IO.Path.Combine(_imageRootDir, "CWSLogoPdf.png")))
                .SetHeight(30);
            headerTable.AddCell(new Cell().Add(cwLogo).SetBorder(Border.NO_BORDER));

            var reportTitle = new Cell()
                .Add(new Paragraph().Add(new Text("POI List")))
                .SetFont(PdfHelper.GetPdfFont())
                .SetFontSize(CELL_FONT_SIZE * 4f)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                .SetBorder(Border.NO_BORDER);
            headerTable.AddCell(reportTitle);

            var cellSiteImage = new Cell().SetBorder(Border.NO_BORDER);
            var imagePath = string.Empty;
            if (clientSiteId != 0)
            {
                imagePath = GetSiteImage(clientSiteId);
                var folderPath = IO.Path.Combine(_SiteimageRootDir, imagePath);
                if (IO.File.Exists(folderPath))
                {
                    var siteImage = new Image(ImageDataFactory.Create(imagePath))
                        .SetHeight(30)
                        .SetHorizontalAlignment(HorizontalAlignment.RIGHT);
                    cellSiteImage.Add(siteImage);
                }
                headerTable.AddCell(cellSiteImage).SetBorder(Border.NO_BORDER);

            }

            return headerTable;
        }


        private Table CreateReportHeaderTableListforBulkDocket(int clientSiteId)
        {
            var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 20, 50, 30 })).UseAllAvailableWidth();

            var cwLogo = new Image(ImageDataFactory.Create(IO.Path.Combine(_imageRootDir, "CWSLogoPdf.png")))
                .SetHeight(30);
            headerTable.AddCell(new Cell().Add(cwLogo).SetBorder(Border.NO_BORDER));

            var reportTitle = new Cell()
                .Add(new Paragraph().Add(new Text("Bulk Docket")))
                .SetFont(PdfHelper.GetPdfFont())
                .SetFontSize(CELL_FONT_SIZE * 4f)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                .SetBorder(Border.NO_BORDER);
            headerTable.AddCell(reportTitle);

            var cellSiteImage = new Cell().SetBorder(Border.NO_BORDER);
            var imagePath = string.Empty;
            if (clientSiteId != 0)
            {
                imagePath = GetSiteImage(clientSiteId);
                var folderPath = IO.Path.Combine(_SiteimageRootDir, imagePath);
                if (IO.File.Exists(folderPath))
                {
                    var siteImage = new Image(ImageDataFactory.Create(imagePath))
                        .SetHeight(30)
                        .SetHorizontalAlignment(HorizontalAlignment.RIGHT);
                    cellSiteImage.Add(siteImage);
                }
                headerTable.AddCell(cellSiteImage).SetBorder(Border.NO_BORDER);

            }

            return headerTable;
        }


        private Table CreateReportHeaderTable(int clientSiteId)
        {
            var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 20, 50, 30 })).UseAllAvailableWidth();

            var cwLogo = new Image(ImageDataFactory.Create(IO.Path.Combine(_imageRootDir, "CWSLogoPdf.png")))
                .SetHeight(30);
            headerTable.AddCell(new Cell().Add(cwLogo).SetBorder(Border.NO_BORDER));

            var reportTitle = new Cell()
                .Add(new Paragraph().Add(new Text("Manual Docket")))
                .SetFont(PdfHelper.GetPdfFont())
                .SetFontSize(CELL_FONT_SIZE * 4f)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                .SetBorder(Border.NO_BORDER);
            headerTable.AddCell(reportTitle);

            var cellSiteImage = new Cell().SetBorder(Border.NO_BORDER);
            var imagePath = GetSiteImage(clientSiteId);
            var folderPath = IO.Path.Combine(_SiteimageRootDir, imagePath);
            if (IO.File.Exists(folderPath))
            {
                var siteImage = new Image(ImageDataFactory.Create(imagePath))
                    .SetHeight(30)
                    .SetHorizontalAlignment(HorizontalAlignment.RIGHT);
                cellSiteImage.Add(siteImage);
            }
            headerTable.AddCell(cellSiteImage).SetBorder(Border.NO_BORDER);


            return headerTable;
        }

        private static Table CreateSiteDetailsTable(KeyVehicleLog keyVehicleLog)
        {
            var siteDataTable = new Table(UnitValue.CreatePercentArray(new float[] { 5, 38, 10, 18, 5, 8, 4, 15 })).UseAllAvailableWidth().SetMarginTop(10);

            siteDataTable.AddCell(GetSiteHeaderCell("Site:"));
            var siteName = new Cell()
                .Add(new Paragraph().Add(new Text(keyVehicleLog.ClientSiteLogBook.ClientSite.Name)
                .SetFont(PdfHelper.GetPdfFont())))
                .Add(new Paragraph().Add(new Text(keyVehicleLog.ClientSiteLogBook.ClientSite.Address ?? string.Empty)))
                .SetFontSize(CELL_FONT_SIZE)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER);
            siteDataTable.AddCell(siteName);
            //docket output issues- start
            string date = keyVehicleLog.ClientSiteLogBook.Date.ToString("yyyy-MMM-dd").ToUpper();
            string day = keyVehicleLog.ClientSiteLogBook.Date.ToString("dddd");

            siteDataTable.AddCell(GetSiteHeaderCell("Date of Log:"));
            siteDataTable.AddCell(GetSiteValueCell(date + "-" + day));
            //siteDataTable.AddCell(GetSiteValueCell(keyVehicleLog.ClientSiteLogBook.Date.ToString("yyyy-MMM-dd-dddd").ToUpper()));
            //docket output issues- end
            siteDataTable.AddCell(GetSiteHeaderCell("Guard Intials"));
            siteDataTable.AddCell(GetSiteValueCell(keyVehicleLog.GuardLogin.Guard.Initial ?? string.Empty));

            siteDataTable.AddCell(GetSiteHeaderCell("S/No:"));
            siteDataTable.AddCell(GetSerialNoValueCell(keyVehicleLog.DocketSerialNo ?? string.Empty));

            return siteDataTable;
        }

        private Table CreateReportDetailsTable(KeyVehicleLogViewModel keyVehicleLogViewModel, string blankNoteOnOrOff)
        {
            var outerTable = new Table(UnitValue.CreatePercentArray(new float[] { 78, 22 })).UseAllAvailableWidth().SetMarginTop(10);

            var innerTable1 = new Table(1).UseAllAvailableWidth();

            var cellClockDetails = new Cell()
                                    .SetPaddingLeft(0)
                                    .SetPaddingTop(0)
                                    .SetBorder(Border.NO_BORDER)
                                    .Add(GetClockDetailsTable(keyVehicleLogViewModel));
            innerTable1.AddCell(cellClockDetails);

            var cellCompanyDetails = new Cell()
                                        .SetPaddingLeft(0)
                                        .SetBorder(Border.NO_BORDER)
                                        .Add(GetCompanyDetailsTable(keyVehicleLogViewModel));
            innerTable1.AddCell(cellCompanyDetails);

            var cellInnerTable1 = new Cell()
                                    .SetPaddingLeft(0)
                                    .SetPaddingTop(0)
                                    .SetBorder(Border.NO_BORDER)
                                    .Add(innerTable1);
            outerTable.AddCell(cellInnerTable1);

            var cellNotesTable = new Cell()
                                    .SetPaddingRight(0)
                                    .SetPaddingTop(0)
                                    .SetBorder(Border.NO_BORDER)
                                    .Add(GetNotesTable(keyVehicleLogViewModel, blankNoteOnOrOff));
            outerTable.AddCell(cellNotesTable);

            var cellVehicleDetails = new Cell(1, 2)
                                        .SetPaddingLeft(0)
                                        .SetPaddingTop(0)
                                        .SetBorder(Border.NO_BORDER)
                                        .Add(GetVehicleDetailsTable(keyVehicleLogViewModel));
            outerTable.AddCell(cellVehicleDetails);

            return outerTable;
        }

        private Table CreateWeightandOtherDetailsTable(KeyVehicleLogViewModel keyVehicleLogViewModel, string docketReason)
        {
            var outerTable2 = new Table(UnitValue.CreatePercentArray(new float[] { 68, 32 })).UseAllAvailableWidth().SetMarginTop(10);

            var innerTable2 = new Table(1).UseAllAvailableWidth();

            var cellWeightDetailsTable = new Cell()
                                            .SetPaddingLeft(0)
                                            .SetPaddingTop(0)
                                            .SetBorder(Border.NO_BORDER)
                                            .Add(GetWeightDetailsTable(keyVehicleLogViewModel));
            innerTable2.AddCell(cellWeightDetailsTable);

            var otherDetailsTable = new Cell()
                                        .SetPaddingLeft(0)
                                        .SetPaddingTop(7)
                                        .SetBorder(Border.NO_BORDER)
                                        //.Add(GetOtherDetailsTable(docketReason));
                                        //p7 - 115 docket output issues-start
                                        .Add(GetOtherDetailsTable(docketReason, keyVehicleLogViewModel));
            //p7 - 115 docket output issues-end
            innerTable2.AddCell(otherDetailsTable);

            var cellInnerTable2 = new Cell()
                                    .SetPaddingLeft(0)
                                    .SetPaddingTop(0)
                                    .SetBorder(Border.NO_BORDER)
                                    .Add(innerTable2);
            outerTable2.AddCell(cellInnerTable2);

            var cellSignDetails = new Cell()
                                    .SetPaddingRight(0)
                                    .SetPaddingTop(0)
                                    .SetBorder(Border.NO_BORDER)
                                    //p7-115 docket output issues-start
                                    //.Add(GetSignDetailsTable());
                                    .Add(GetSignDetailsTable(keyVehicleLogViewModel));
            //p7-115 docket output issues-end
            outerTable2.AddCell(cellSignDetails);

            return outerTable2;
        }
        //Table For POI List
        private Table CreateWeightandOtherDetailsTablePOI(KeyVehicleLogViewModel keyVehicleLogViewModel, string docketReason)
        {
            var outerTable2 = new Table(UnitValue.CreatePercentArray(new float[] { 68, 32 })).UseAllAvailableWidth().SetMarginTop(10);

            var innerTable2 = new Table(1).UseAllAvailableWidth();

            var cellWeightDetailsTable = new Cell()
                                            .SetPaddingLeft(0)
                                            .SetPaddingTop(0)
                                            .SetBorder(Border.NO_BORDER)
                                            .Add(GetWeightDetailsTable(keyVehicleLogViewModel));
            innerTable2.AddCell(cellWeightDetailsTable);

            var otherDetailsTable = new Cell()
                                        .SetPaddingLeft(0)
                                        .SetPaddingTop(7)
                                        .SetBorder(Border.NO_BORDER)
                                        .Add(GetOtherDetailsTablePOList(docketReason));
            innerTable2.AddCell(otherDetailsTable);

            var cellInnerTable2 = new Cell()
                                    .SetPaddingLeft(0)
                                    .SetPaddingTop(0)
                                    .SetBorder(Border.NO_BORDER)
                                    .Add(innerTable2);
            outerTable2.AddCell(cellInnerTable2);

            //p7-115 Docket output issues-start
            //var cellSignDetails = new Cell()
            //                        .SetPaddingRight(0)
            //                        .SetPaddingTop(0)
            //                        .SetBorder(Border.NO_BORDER)
            //                        .Add(GetSignDetailsTable());
            var cellSignDetails = new Cell()
                                    .SetPaddingRight(0)
                                    .SetPaddingTop(0)
                                    .SetBorder(Border.NO_BORDER)
                                    .Add(GetSignDetailsTable(keyVehicleLogViewModel));
            //p7 - 115 Docket output issues - end

            outerTable2.AddCell(cellSignDetails);

            return outerTable2;
        }
        private Table CreateImageDetailsTable(KeyVehicleLogViewModel keyVehicleLogViewModel, string docketReason)
        {
            var outerTable2 = new Table(UnitValue.CreatePercentArray(new float[] { 68, 32 })).UseAllAvailableWidth().SetMarginTop(10);

            var innerTable2 = new Table(1).UseAllAvailableWidth();

            var cellWeightDetailsTable = new Cell()
                                            .SetPaddingLeft(0)
                                            .SetPaddingTop(0)
                                            .SetBorder(Border.NO_BORDER)
                                            .Add(GetImageTable(keyVehicleLogViewModel));
            innerTable2.AddCell(cellWeightDetailsTable);

            //var otherDetailsTable = new Cell()
            //                            .SetPaddingLeft(0)
            //                            .SetPaddingTop(7)
            //                            .SetBorder(Border.NO_BORDER)
            //                            .Add(GetOtherDetailsTable(docketReason));
            //innerTable2.AddCell(otherDetailsTable);

            var cellInnerTable2 = new Cell()
                                    .SetPaddingLeft(0)
                                    .SetPaddingTop(0)
                                    .SetBorder(Border.NO_BORDER)
                                    .Add(innerTable2);
            outerTable2.AddCell(cellInnerTable2);

            var cellSignDetails = new Cell()
                                   .SetPaddingRight(0)
                                   .SetPaddingTop(0)
                                   .SetBorder(Border.NO_BORDER)
                                   .Add(GetVehicleImageTable(keyVehicleLogViewModel));
            outerTable2.AddCell(cellSignDetails);

            return outerTable2;
        }
        // Table for Image

        private static Table GetClockDetailsTable(KeyVehicleLogViewModel keyVehicleLogViewModel)
        {
            var clockDetails = new Table(UnitValue.CreatePercentArray(new float[] { 15, 15, 15, 15, 40 })).UseAllAvailableWidth();

            clockDetails.AddCell(GetHeaderCell("Clocks", 1, 5));

            clockDetails.AddCell(GetHeaderCell("Intial Call"));
            clockDetails.AddCell(GetHeaderCell("Entry Time"));
            clockDetails.AddCell(GetHeaderCell("Sent In Time"));
            clockDetails.AddCell(GetHeaderCell("Exit Time"));
            var headerTimeSlotNo = keyVehicleLogViewModel.Detail.IsTimeSlotNo ? "Time Slot No." : "T-No. (Load)";
            clockDetails.AddCell(GetHeaderCell(headerTimeSlotNo));

            clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel.Detail.InitialCallTime?.ToString("HH:mm")));
            clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel.Detail.EntryTime?.ToString("HH:mm")));
            clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel.Detail.SentInTime?.ToString("HH:mm")));
            clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel.Detail.ExitTime?.ToString("HH:mm")));
            clockDetails.AddCell(GetDataCell(keyVehicleLogViewModel.Detail.TimeSlotNo));

            return clockDetails;
        }

        private Table GetCompanyDetailsTable(KeyVehicleLogViewModel keyVehicleLogViewModel)
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
            companyDetails.AddCell(GetSitePocNameDetails(keyVehicleLogViewModel));

            companyDetails.AddCell(GetDataCell(keyVehicleLogViewModel.Detail.ClientSiteLocation?.Name));
            companyDetails.AddCell(GetDataCell(keyVehicleLogViewModel.PurposeOfEntry));

            return companyDetails;
        }

        private Table GetSitePocNameDetails(KeyVehicleLogViewModel keyVehicleLogViewModel)
        {
            var deductionDetailsTable = new Table(UnitValue.CreatePercentArray(new float[] { 45, 25, 35 })).UseAllAvailableWidth();

            var PocName = keyVehicleLogViewModel.Detail.SitePocNames;
            if (PocName != null)
            {
                var sitePocNamesArray = PocName.Split(',');

                var iconChecked = new Image(ImageDataFactory.Create(IO.Path.Combine(_imageRootDir, "icons", "checked.png"))).SetHeight(8);

                foreach (var sitePocName in sitePocNamesArray)
                {
                    deductionDetailsTable.AddCell(GetDataCell(sitePocName, textAlignment: TextAlignment.RIGHT, 0).SetBorder(Border.NO_BORDER));

                    deductionDetailsTable.AddCell(new Cell().Add(iconChecked).SetBorder(Border.NO_BORDER));
                    deductionDetailsTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));
                }

            }

            return deductionDetailsTable;
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

          
           

            vehicleDetailsTable.AddCell(GetDataCell(keyVehicleLogViewModel.Plate1+"\n"+ keyVehicleLogViewModel.Detail.Trailer1Rego));
            vehicleDetailsTable.AddCell(GetDataCell(keyVehicleLogViewModel.Plate2 + "\n" + keyVehicleLogViewModel.Detail.Trailer2Rego));
            vehicleDetailsTable.AddCell(GetDataCell(keyVehicleLogViewModel.Plate3 + "\n" + keyVehicleLogViewModel.Detail.Trailer3Rego));
            vehicleDetailsTable.AddCell(GetDataCell(keyVehicleLogViewModel.Plate4 + "\n" + keyVehicleLogViewModel.Detail.Trailer4Rego));

            return vehicleDetailsTable;
        }

        private Table GetKeyAndOtherDetailsTable(KeyVehicleLogViewModel keyVehicleLogViewModel)
        {
            var outerTable = new Table(UnitValue.CreatePercentArray(new float[] { 41, 41, 18 })).UseAllAvailableWidth().SetMarginTop(10);

            var senderDetails = new Cell()
                                    .SetPaddingLeft(0)
                                    .SetPaddingTop(0)
                                    .SetBorder(Border.NO_BORDER)
                                    .Add(GetSenderDetailsTable(keyVehicleLogViewModel));
            outerTable.AddCell(senderDetails);

            var cellKeyDetails = new Cell()
                                    .SetPaddingLeft(0)
                                    .SetPaddingRight(0)
                                    .SetPaddingTop(0)
                                    .SetBorder(Border.NO_BORDER)
                                    .Add(GetKeyDetailsTable(keyVehicleLogViewModel));
            outerTable.AddCell(cellKeyDetails);

            var cellReelsDetails = new Cell()
                                    .SetPaddingRight(0)
                                    .SetPaddingTop(0)
                                    .SetBorder(Border.NO_BORDER)
                                    .Add(GetReelsDetailsTable(keyVehicleLogViewModel));
            outerTable.AddCell(cellReelsDetails);

            return outerTable;
        }

        private static Table GetSenderDetailsTable(KeyVehicleLogViewModel keyVehicleLogViewModel)
        {
            var senderDetailsTable = new Table(1).UseAllAvailableWidth();
            var headerSender = keyVehicleLogViewModel.Detail.IsSender ? "Sender" : "Receiver";

            senderDetailsTable.AddCell(GetHeaderCell(headerSender, textAlignment: TextAlignment.LEFT));
            senderDetailsTable.AddCell(GetDataCell(keyVehicleLogViewModel.Detail.Sender, textAlignment: TextAlignment.LEFT));

            return senderDetailsTable;
        }

        private Table GetKeyDetailsTable(KeyVehicleLogViewModel keyVehicleLogViewModel)
        {
            var keyDetailsTable = new Table(1).UseAllAvailableWidth();

            keyDetailsTable.AddCell(GetHeaderCell("Key / Card Scan", textAlignment: TextAlignment.LEFT));

            keyDetailsTable.AddCell(GetDataCell(GetKeyDetailsCommaSeparated(keyVehicleLogViewModel.Detail), textAlignment: TextAlignment.LEFT));

            return keyDetailsTable;
        }

        private static Table GetReelsDetailsTable(KeyVehicleLogViewModel keyVehicleLogViewModel)
        {
            var reelsDetailsTable = new Table(1).UseAllAvailableWidth();
            //manifest options-start
            var headerReels = keyVehicleLogViewModel.Detail.IsReels ? "Reels" : "QTY";
            reelsDetailsTable.AddCell(GetHeaderCell(headerReels, textAlignment: TextAlignment.LEFT));
            //manifest options-end
            // reelsDetailsTable.AddCell(GetHeaderCell("Reels", textAlignment: TextAlignment.LEFT));

            reelsDetailsTable.AddCell(GetDataCell(keyVehicleLogViewModel.Detail.Reels.ToString(), textAlignment: TextAlignment.LEFT));

            return reelsDetailsTable;
        }

        private static Table GetCustomerRefAndVwiTable(KeyVehicleLogViewModel keyVehicleLogViewModel)
        {
            var outerTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 })).UseAllAvailableWidth().SetMarginTop(10);

            var cellCustomerRef = new Cell()
                                    .SetPaddingLeft(0)
                                    .SetPaddingTop(0)
                                    .SetBorder(Border.NO_BORDER)
                                    .Add(GetCustomerRefTable(keyVehicleLogViewModel));
            outerTable.AddCell(cellCustomerRef);

            var cellVwiDetails = new Cell()
                                    .SetPaddingRight(0)
                                    .SetPaddingTop(0)
                                    .SetBorder(Border.NO_BORDER)
                                    .Add(GetVwiDetailsTable(keyVehicleLogViewModel));
            outerTable.AddCell(cellVwiDetails);

            return outerTable;
        }

        private static Table GetCustomerRefTable(KeyVehicleLogViewModel keyVehicleLogViewModel)
        {
            var customerRefTable = new Table(1).UseAllAvailableWidth();

            customerRefTable.AddCell(GetHeaderCell("Customer Ref", textAlignment: TextAlignment.LEFT));

            customerRefTable.AddCell(GetDataCell(keyVehicleLogViewModel.Detail.CustomerRef, textAlignment: TextAlignment.LEFT));

            return customerRefTable;
        }

        private static Table GetVwiDetailsTable(KeyVehicleLogViewModel keyVehicleLogViewModel)
        {
            var vwiDetailsTable = new Table(1).UseAllAvailableWidth();
            //manifest options -start
            var headerVWI = keyVehicleLogViewModel.Detail.IsVWI ? "VWI" : "Manifest";
            vwiDetailsTable.AddCell(GetHeaderCell(headerVWI, textAlignment: TextAlignment.LEFT));
            //manifest options -end
            //vwiDetailsTable.AddCell(GetHeaderCell("VWI", textAlignment: TextAlignment.LEFT));

            vwiDetailsTable.AddCell(GetDataCell(keyVehicleLogViewModel.Detail.Vwi, textAlignment: TextAlignment.LEFT));

            return vwiDetailsTable;
        }

        private Table GetWeightDetailsTable(KeyVehicleLogViewModel keyVehicleLogViewModel)
        {
            var weightDetailsTable = new Table(UnitValue.CreatePercentArray(new float[] { 15, 15, 15, 40, 15 })).UseAllAvailableWidth();

            weightDetailsTable.AddCell(GetHeaderCell("Weight", 1, 5));

            weightDetailsTable.AddCell(GetHeaderCell("In Gross (t)"));
            weightDetailsTable.AddCell(GetHeaderCell("Out Net (t)"));
            weightDetailsTable.AddCell(GetHeaderCell("Tare (t)"));
            weightDetailsTable.AddCell(GetHeaderCell("Contamination Deduction?"));
            weightDetailsTable.AddCell(GetHeaderCell("Max (t)"));

            weightDetailsTable.AddCell(GetDataCell(keyVehicleLogViewModel.Detail.InWeight?.ToString(), cellFontSize: CELL_FONT_SIZE_BIG)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));
            weightDetailsTable.AddCell(GetDataCell(keyVehicleLogViewModel.Detail.OutWeight?.ToString(), cellFontSize: CELL_FONT_SIZE_BIG)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));
            weightDetailsTable.AddCell(GetDataCell(keyVehicleLogViewModel.Detail.TareWeight?.ToString(), cellFontSize: CELL_FONT_SIZE_BIG)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));
            weightDetailsTable.AddCell(GetDeductionDetailsTable(keyVehicleLogViewModel));
            weightDetailsTable.AddCell(GetDataCell(keyVehicleLogViewModel.Detail.MaxWeight?.ToString(), cellFontSize: CELL_FONT_SIZE_BIG)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));

            return weightDetailsTable;
        }

        private Table GetDeductionDetailsTable(KeyVehicleLogViewModel keyVehicleLogViewModel)
        {
            var deductionDetailsTable = new Table(UnitValue.CreatePercentArray(new float[] { 40, 25, 35 })).UseAllAvailableWidth();

            deductionDetailsTable.AddCell(GetDataCell("Moisture", textAlignment: TextAlignment.RIGHT, 0).SetBorder(Border.NO_BORDER));

            var iconChecked = new Image(ImageDataFactory.Create(IO.Path.Combine(_imageRootDir, "icons", "checked.png"))).SetHeight(8);
            var iconUnChecked = new Image(ImageDataFactory.Create(IO.Path.Combine(_imageRootDir, "icons", "unchecked.png"))).SetHeight(8);

            deductionDetailsTable.AddCell(new Cell().Add(keyVehicleLogViewModel.Detail.MoistureDeduction ? iconChecked : iconUnChecked).SetBorder(Border.NO_BORDER));

            var cellDeductionPercentage = new Cell(2, 1)
                .Add(new Paragraph().SetFontSize(CELL_FONT_SIZE)
                .Add(new Text($"{keyVehicleLogViewModel.Detail.DeductionPercentage} %")))
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetBorder(Border.NO_BORDER);
            deductionDetailsTable.AddCell(cellDeductionPercentage);

            deductionDetailsTable.AddCell(GetDataCell("Rubbish", textAlignment: TextAlignment.RIGHT, 0).SetBorder(Border.NO_BORDER));

            deductionDetailsTable.AddCell(new Cell().Add(keyVehicleLogViewModel.Detail.RubbishDeduction ? iconChecked : iconUnChecked).SetBorder(Border.NO_BORDER));

            return deductionDetailsTable;
        }

        private static Table GetOtherDetailsTable(string docketReason)
        {
            var otherDetailsTable = new Table(UnitValue.CreatePercentArray(new float[] { 40, 60 })).UseAllAvailableWidth().SetMarginTop(12);
            //p7-115 docket output issues-start
            //otherDetailsTable.AddCell(GetHeaderCell("STMS Form \n(Outbound / pickup only)", textAlignment: TextAlignment.LEFT));
            otherDetailsTable.AddCell(GetHeaderCell("Compliance Documents \n(NHVR HML / STMS / HACCP / etc)", textAlignment: TextAlignment.LEFT));
            //p7 - 115 docket output issues - end
            otherDetailsTable.AddCell(GetHeaderCell("Why was MANUAL Docket Created?", textAlignment: TextAlignment.LEFT));
            //p7-115 docket output issues-start
            //otherDetailsTable.AddCell(GetDataCell("Y / NA", textAlignment: TextAlignment.CENTER, cellFontSize: CELL_FONT_SIZE_BIG)
            otherDetailsTable.AddCell(GetDataCell("NA", textAlignment: TextAlignment.CENTER, cellFontSize: CELL_FONT_SIZE_BIG)
           //p7 - 115 docket output issues - end
           .SetHorizontalAlignment(HorizontalAlignment.CENTER)
            .SetVerticalAlignment(VerticalAlignment.MIDDLE));
            otherDetailsTable.AddCell(GetDataCell(docketReason, textAlignment: TextAlignment.LEFT)
                 .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetHeight(10));

            return otherDetailsTable;
        }
        //p7-115 docket output issues-start
        private static Table GetOtherDetailsTable(string docketReason, KeyVehicleLogViewModel keyVehicleLogViewModel)
        {
            var otherDetailsTable = new Table(UnitValue.CreatePercentArray(new float[] { 40, 60 })).UseAllAvailableWidth().SetMarginTop(12);

            //otherDetailsTable.AddCell(GetHeaderCell("STMS Form \n(Outbound / pickup only)", textAlignment: TextAlignment.LEFT));
            otherDetailsTable.AddCell(GetHeaderCell("Compliance Documents \n(NHVR HML / STMS / HACCP / etc)", textAlignment: TextAlignment.LEFT));

            otherDetailsTable.AddCell(GetHeaderCell("Why was MANUAL Docket Created?", textAlignment: TextAlignment.LEFT));
            //p7-115 docket output issues-start
            if (keyVehicleLogViewModel.Detail.IsDocketNo == true)
            {
                otherDetailsTable.AddCell(GetDataCell("Y", textAlignment: TextAlignment.CENTER, cellFontSize: CELL_FONT_SIZE_BIG)
               //otherDetailsTable.AddCell(GetDataCell("NA", textAlignment: TextAlignment.CENTER, cellFontSize: CELL_FONT_SIZE_BIG)
               //p7 - 115 docket output issues - end
               .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));
                otherDetailsTable.AddCell(GetDataCell(docketReason, textAlignment: TextAlignment.LEFT)
                     .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .SetHeight(10));
            }
            else
            {
                otherDetailsTable.AddCell(GetDataCell("NA", textAlignment: TextAlignment.CENTER, cellFontSize: CELL_FONT_SIZE_BIG)
               //otherDetailsTable.AddCell(GetDataCell("NA", textAlignment: TextAlignment.CENTER, cellFontSize: CELL_FONT_SIZE_BIG)
               //p7 - 115 docket output issues - end
               .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));
                otherDetailsTable.AddCell(GetDataCell(docketReason, textAlignment: TextAlignment.LEFT)
                     .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .SetHeight(10));
            }
            return otherDetailsTable;
        }
        //p7 - 115 docket output issues - end
        //PO List 
        private static Table GetOtherDetailsTablePOList(string docketReason)
        {
            var otherDetailsTable = new Table(UnitValue.CreatePercentArray(new float[] { 40, 60 })).UseAllAvailableWidth().SetMarginTop(12);

            otherDetailsTable.AddCell(GetHeaderCell("STMS Form \n(Outbound / pickup only)", textAlignment: TextAlignment.LEFT));
            otherDetailsTable.AddCell(GetHeaderCell("Why was MANUAL Docket Created?", textAlignment: TextAlignment.LEFT));

            otherDetailsTable.AddCell(GetDataCell("Y / NA", textAlignment: TextAlignment.CENTER, cellFontSize: CELL_FONT_SIZE_BIG)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));
            otherDetailsTable.AddCell(GetDataCell("POI List", textAlignment: TextAlignment.LEFT)
                 .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetHeight(10));

            return otherDetailsTable;
        }
        //p7-115 docket output issues-start 
        //private static Table GetSignDetailsTable()
        //{
        //    var signDetailsTable = new Table(UnitValue.CreatePercentArray(new float[] { 25, 75 })).UseAllAvailableWidth();

        //    signDetailsTable.AddCell(GetHeaderCell("Loader"));
        //    signDetailsTable.AddCell(GetDataCell("Name: \n\nSign:\n\n", textAlignment: TextAlignment.LEFT));

        //    signDetailsTable.AddCell(GetHeaderCell("Dispatch"));
        //    signDetailsTable.AddCell(GetDataCell("Name: \n\nSign:\n\n", textAlignment: TextAlignment.LEFT));

        //    signDetailsTable.AddCell(GetHeaderCell("Driver"));
        //    signDetailsTable.AddCell(GetDataCell("Name: \n\nSign:\n\n", textAlignment: TextAlignment.LEFT));

        //    return signDetailsTable;
        //}
        private static Table GetSignDetailsTable(KeyVehicleLogViewModel keyVehicleLogViewModel)
        {
            var signDetailsTable = new Table(UnitValue.CreatePercentArray(new float[] { 25, 75 })).UseAllAvailableWidth();

            signDetailsTable.AddCell(GetHeaderCell("Loader"));
            signDetailsTable.AddCell(GetDataCell("Name: " + keyVehicleLogViewModel.Detail.LoaderName + "\n\nSign:\n\n", textAlignment: TextAlignment.LEFT));

            signDetailsTable.AddCell(GetHeaderCell("Dispatch"));
            signDetailsTable.AddCell(GetDataCell("Name: " + keyVehicleLogViewModel.Detail.DispatchName + "\n\nSign:\n\n", textAlignment: TextAlignment.LEFT));

            signDetailsTable.AddCell(GetHeaderCell("Driver"));
            //p7-115 docket output issues-start
            signDetailsTable.AddCell(GetDataCell("Name: " + keyVehicleLogViewModel.Detail.PersonName + "\n\nSign:\n\n", textAlignment: TextAlignment.LEFT));
            //signDetailsTable.AddCell(GetDataCell("Name:\n\nSign:\n\n", textAlignment: TextAlignment.LEFT));
            //p7 - 115 docket output issues - end
            return signDetailsTable;
        }

        //p7-115 docket output issues-end
        // added to display the image
        private Table GetImageTable(KeyVehicleLogViewModel keyVehicleLogViewModel)
        {

            var ImageTable = new Table(1).UseAllAvailableWidth();


            int personType = Convert.ToInt32(keyVehicleLogViewModel.Detail.PersonType);
            
            var IndividulaName = _guardLogDataProvider.GetIndividualType(personType);
            //var Image1 = keyVehicleLogViewModel.Detail.CompanyName + "-" + IndividulaName.Name + "-" + keyVehicleLogViewModel.Detail.PersonName + ".jpg";
            if (!string.IsNullOrEmpty(keyVehicleLogViewModel.Detail.PersonName))
                
            {
                var Image = keyVehicleLogViewModel.Detail.CompanyName + "-" + IndividulaName.Name + "-" + keyVehicleLogViewModel.Detail.PersonName + ".jpg";

                var folderPath = IO.Path.Combine(_PersonimageRootDir, Image);
                if (IO.File.Exists(folderPath))
                {
                    var Imagepath = new Image(ImageDataFactory.Create(IO.Path.Combine(_PersonimageRootDir, Image)))
                                  .SetHeight(160);
                    ImageTable.AddCell(new Cell().Add(Imagepath).SetBorder(Border.NO_BORDER));
                }
            }


            return ImageTable;
        }
        private Table GetVehicleImageTable(KeyVehicleLogViewModel keyVehicleLogViewModel)
        {

            var ImageTable = new Table(1).UseAllAvailableWidth();
            if (!string.IsNullOrEmpty(keyVehicleLogViewModel.Detail.VehicleRego))
            {
                var Image = keyVehicleLogViewModel.Detail.VehicleRego + ".jpg";
                var folderPath = IO.Path.Combine(_vehicleimageRootDir, keyVehicleLogViewModel.Detail.VehicleRego, Image);
                if (IO.File.Exists(folderPath))
                {
                    var Imagepath = new Image(ImageDataFactory.Create(IO.Path.Combine(_vehicleimageRootDir, keyVehicleLogViewModel.Detail.VehicleRego, Image)))
                   .SetHeight(160);

                    ImageTable.AddCell(new Cell().Add(Imagepath).SetBorder(Border.NO_BORDER));
                }
            }


            return ImageTable;
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

        private static Cell GetSerialNoValueCell(string text)
        {
            return new Cell()
               .Add(new Paragraph(text)
               .SetFont(PdfHelper.GetPdfFont(StandardFonts.COURIER_BOLD))
               .SetFontSize(CELL_FONT_SIZE_BIG * 1.2f)
               .SetFontColor(WebColors.GetRGBColor("#FF323A"))
               .SetTextAlignment(TextAlignment.CENTER))
               .SetVerticalAlignment(VerticalAlignment.MIDDLE);
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

        private string GetSiteImage(int clientSiteId)
        {
            var clientSiteSetting = _clientDataProvider.GetClientSiteKpiSetting(clientSiteId);
            if (clientSiteSetting != null && !string.IsNullOrEmpty(clientSiteSetting.SiteImage))
                return $"{new Uri(_settings.KpiWebUrl)}{clientSiteSetting.SiteImage}";
            return string.Empty;
        }

       
        private string GetKeyDetailsCommaSeparated(KeyVehicleLog keyVehicleLog)
        {
            var clientSiteKeys = _guardSettingsDataProvider.GetClientSiteKeys(keyVehicleLog.ClientSiteLogBook.ClientSiteId);

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
        //generate bulk dock with individual pdfs with zip file-start
        public string GenerateBulkIndividualPdfReportWithZip(List<int> keyVehicleLogId, string docketReason, string blankNoteOnOrOff, string serialNo)
        {
            var zipfilepath = GetZipFolderPath();
            var fileNamePart = $"{DateTime.Today:yyyyMMdd}_KVManualDocket_{"Bulk"}_SN{serialNo}.zip";
            foreach (var i in keyVehicleLogId)
            {
                var keyVehicleLog = _guardLogDataProvider.GetKeyVehicleLogById(i);
                //To Get the SitePocNames start
                if (keyVehicleLog.ClientSitePocIdsVehicleLog != null)
                {
                    var POCIds = keyVehicleLog.ClientSitePocIdsVehicleLog.Split(',');
                    var pocNamesList = new List<string>();
                    pocNamesList.Clear();
                    foreach (var item in POCIds)
                    {
                        var PocName = _guardLogDataProvider.GetClientSitePOCName(Convert.ToInt32(item));
                        pocNamesList.Add(PocName.Name);
                    }

                    var pocNames = string.Join(", ", pocNamesList);
                    keyVehicleLog.SitePocNames = pocNames;
                }

                //To Get the SitePocNames stop
                var reportPdfPath1 = IO.Path.Combine(_reportRootDir, REPORT_DIR, $"{DateTime.Today:yyyyMMdd}_KVManualDocket_{keyVehicleLog.GuardLogin.ClientSite.Name}_SN{serialNo}.pdf");

                var pdfDocList = new PdfDocument(new PdfWriter(reportPdfPath1));
                pdfDocList.SetDefaultPageSize(PageSize.A4);
                var docList = new Document(pdfDocList);
                docList.Add(CreateReportHeaderTable(keyVehicleLog.ClientSiteLogBook.ClientSiteId));
                //docList.Add(CreateReportHeaderTableListforBulkDocket(0));

                var reportPdfPath = "";

                var last = keyVehicleLogId.Last();



                serialNo = GetNextDocketSequenceNumber(i);
                _guardLogDataProvider.SaveDocketSerialNo(i, serialNo);

                if (keyVehicleLog == null)
                    return string.Empty;

                var kvlFields = _guardLogDataProvider.GetKeyVehicleLogFields();
                var keyVehicleLogViewModel = new KeyVehicleLogViewModel(keyVehicleLog, kvlFields);



                if (keyVehicleLog != null)
                {
                    if (keyVehicleLog.ClientSiteLogBook != null)
                    {



                        //_guardLogDataProvider.SaveDocketSerialNo(keyVehicleLog.Id, serialNo);

                        if (keyVehicleLog == null)
                            return string.Empty;



                        reportPdfPath = IO.Path.Combine(_reportRootDir, REPORT_DIR, $"{DateTime.Today:yyyyMMdd}_KVManualDocket_{"POI_ListAll"}_SN{serialNo}.pdf");

                        if (IO.File.Exists(reportPdfPath))
                            //IO.File.Delete(reportPdfPath);


                            docList.SetMargins(15f, 30f, 40f, 30f);

                        docList.Add(CreateSiteDetailsTable(keyVehicleLog));
                        docList.Add(CreateReportDetailsTable(keyVehicleLogViewModel, blankNoteOnOrOff));
                        docList.Add(GetKeyAndOtherDetailsTable(keyVehicleLogViewModel));
                        docList.Add(GetCustomerRefAndVwiTable(keyVehicleLogViewModel));
                        docList.Add(CreateWeightandOtherDetailsTable(keyVehicleLogViewModel, docketReason));
                        docList.Add(CreateImageDetailsTable(keyVehicleLogViewModel, docketReason));

                        //p7-115 docket output issues-start
                        //pdfDocument.AddNewPage();
                        var path = keyVehicleLog.Id + "/ComplianceDocuments";

                        var keyVehicleLogDetails = _viewDataService.GetKeyVehicleLogAttachments(
                             IO.Path.Combine(_WebHostEnvironment.WebRootPath, "KvlUploads"), path)
                             .ToList();
                        int countpagebreak = 0;
                        if (keyVehicleLogDetails.Count > 0)
                        {
                            for (int j = 0; j < keyVehicleLogDetails.Count; j++)
                            {
                                string filename = IO.Path.Combine(IO.Path.Combine(IO.Path.Combine(_WebHostEnvironment.WebRootPath, "KvlUploads"), path), keyVehicleLogDetails[j]);
                                int pagecount = 0;
                                if (IO.Path.GetExtension(filename) == ".pdf")
                                {
                                    using (StreamReader sr = new StreamReader(File.OpenRead(filename)))
                                    {
                                        Regex regex = new Regex(@"/Type\s*/Page[^s]");
                                        MatchCollection matches = regex.Matches(sr.ReadToEnd());

                                        pagecount = matches.Count;
                                    }
                                    PdfReader reader = new PdfReader(filename);
                                    PdfDocument docfile = new PdfDocument(reader);
                                    int pagecountnew = docfile.GetNumberOfPages();
                                    docfile.CopyPagesTo(1, pagecountnew, pdfDocList);

                                }
                                if (IO.Path.GetExtension(filename) == ".jpg")
                                {
                                    //var pdfDocneew = new PdfDocument(new PdfReader(filename));
                                    //pdfDocneew.SetDefaultPageSize(PageSize.A4);
                                    //var docnew = new Document(pdfDocneew);

                                    //docnew.SetMargins(15f, 30f, 40f, 30f);
                                    //var pageSize = new PageSize(pdfDocList.GetLastPage().GetPageSize());
                                    //pdfDocList.AddNewPage(1, pageSize);
                                    int pagecount1 = pdfDocList.GetNumberOfPages();
                                    if (countpagebreak == 0)
                                    {
                                        for (int countpage = 0; countpage < pagecount1; countpage++)
                                        {
                                            docList.Add(new AreaBreak(AreaBreakType.NEXT_AREA));
                                            countpagebreak = countpagebreak + 1;
                                        }
                                    }
                                    else
                                    {
                                        docList.Add(new AreaBreak(AreaBreakType.NEXT_AREA));
                                    }



                                    docList.Add(GetImageFileTable(filename, keyVehicleLog.Id));

                                    //var countpage = pdfDocList.GetNumberOfPages();
                                    //var page = pdfDocList.GetFirstPage();
                                    //pdfDocList.MovePage(page, countpage + 1);
                                    ////docnew.Close();
                                    //pdfDocneew.Close();
                                    ////PdfReader reader = new PdfReader(pdfDocneew);
                                    ////PdfDocument docfile = new PdfDocument(reader);
                                    //int pagecountnew = pdfDocneew.GetNumberOfPages();
                                    //pdfDocneew.CopyPagesTo(1, pagecountnew, pdfDoc);
                                }
                            }
                        }

                        //p7 - 115 docket output issues - end


                        //if (i != last)
                        //{
                        //    docList.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                        //}


                    }




                }
                docList.Close();
                pdfDocList.Close();
                var reportFilePath = IO.Path.Combine(_reportRootDir, "Pdf", "Output", $"{DateTime.Today:yyyyMMdd}_KVManualDocket_{keyVehicleLog.GuardLogin.ClientSite.Name}_SN{serialNo}.pdf");
                File.Copy(reportPdfPath1, IO.Path.Combine(zipfilepath, $"{DateTime.Today:yyyyMMdd}_KVManualDocket_{keyVehicleLog.GuardLogin.ClientSite.Name}_SN{serialNo}.pdf"));
                //File.Delete(reportFilePath);
            }




            return GetZipFileName(zipfilepath, DateTime.Today, DateTime.Today, fileNamePart);

        }
        private string GetZipFolderPath()
        {
            var zipFolderPath = IO.Path.Combine(_downloadsFolderPath, Guid.NewGuid().ToString());
            if (!Directory.Exists(zipFolderPath))
                Directory.CreateDirectory(zipFolderPath);
            return zipFolderPath;
        }
        private string GetZipFileName(string zipFolderPath, DateTime logFromDate, DateTime logToDate, string fileNamePart)
        {
            var zipFileName = $"{FileNameHelper.GetSanitizedFileNamePart(fileNamePart)}";


            if (File.Exists(IO.Path.Combine(_downloadsFolderPath, zipFileName)))
                File.Delete(IO.Path.Combine(_downloadsFolderPath, zipFileName));
            ZipFile.CreateFromDirectory(zipFolderPath, IO.Path.Combine(_downloadsFolderPath, zipFileName), CompressionLevel.Optimal, false);
            if (Directory.Exists(zipFolderPath))
                Directory.Delete(zipFolderPath, true);
            return zipFileName;
        }

    }
}
