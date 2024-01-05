using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Helpers;
using CityWatch.Web.Models;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
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

    }
    public class KeyVehicleLogDocketGenerator : IKeyVehicleLogDocketGenerator
    {
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        private readonly IGuardSettingsDataProvider _guardSettingsDataProvider;
        private readonly IWebHostEnvironment _WebHostEnvironment;
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

        public KeyVehicleLogDocketGenerator(IWebHostEnvironment webHostEnvironment,
           IGuardLogDataProvider guardLogDataProvider,
           IClientDataProvider clientDataProvider,
           IGuardSettingsDataProvider guardSettingsDataProvider,
           IOptions<Settings> settings)
        {
            _clientDataProvider = clientDataProvider;
            _guardLogDataProvider = guardLogDataProvider;
            _guardSettingsDataProvider = guardSettingsDataProvider;
            _reportRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "Pdf");
            _imageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "images");
            _PersonimageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "KvlUploads","Person");
            _vehicleimageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "KvlUploads");
            _SiteimageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "SiteImage");
            _settings = settings.Value;
        }
        //To Generate the Pdf In List start
        public string GeneratePdfReportList(int keyVehicleLogId1, string docketReason, string blankNoteOnOrOff, string serialNo, List<int> ids,int clientsiteid)
        {

            List<int> Newids = new List<int>();
            int keyVehicleLogIdArr;


            var keyVehicleLogData = _guardLogDataProvider.GetKeyVehicleLogById(keyVehicleLogId1);
            for (int i = 0; i < ids.Count; i++)
            {
                var keyVehicleLogArrID = _guardLogDataProvider.GetKeyVehicleLogById(ids[i]);
                if (keyVehicleLogArrID.ClientSiteLogBook.ClientSite.Id == clientsiteid)
                {
                     keyVehicleLogIdArr = ids[i];
                  
                    Newids.Add(keyVehicleLogIdArr);
                }
                }
            var reportPdfPath1 = IO.Path.Combine(_reportRootDir, REPORT_DIR, $"{DateTime.Today:yyyyMMdd}_KVManualDocket_{keyVehicleLogData.GuardLogin.ClientSite.Name}_SN{serialNo}.pdf");
            var pdfDocList = new PdfDocument(new PdfWriter(reportPdfPath1));
            pdfDocList.SetDefaultPageSize(PageSize.A4);
            var docList = new Document(pdfDocList);
            docList.Add(CreateReportHeaderTableList(keyVehicleLogData.ClientSiteLogBook.ClientSiteId));
            var reportPdfPath = "";
        
            for (int i = 0; i < Newids.Count; i++)
            {
                var keyVehicleLogId = Newids[i];
                var keyVehicleLog = _guardLogDataProvider.GetKeyVehicleLogById(keyVehicleLogId);
                //if (keyVehicleLog.ClientSiteLogBook.ClientSite.Id== clientsiteid)
                //{

               

                _guardLogDataProvider.SaveDocketSerialNo(keyVehicleLogId, serialNo);

                if (keyVehicleLog == null)
                    return string.Empty;

                var kvlFields = _guardLogDataProvider.GetKeyVehicleLogFields();
                var keyVehicleLogViewModel = new KeyVehicleLogViewModel(keyVehicleLog, kvlFields);
                 reportPdfPath = IO.Path.Combine(_reportRootDir, REPORT_DIR, $"{DateTime.Today:yyyyMMdd}_KVManualDocket_{keyVehicleLog.GuardLogin.ClientSite.Name}_SN{serialNo}.pdf");

                if (IO.File.Exists(reportPdfPath))
                    //IO.File.Delete(reportPdfPath);

                  
                docList.SetMargins(15f, 30f, 40f, 30f);
               
                docList.Add(CreateSiteDetailsTable(keyVehicleLog));
                docList.Add(CreateReportDetailsTable(keyVehicleLogViewModel, blankNoteOnOrOff));
                docList.Add(GetKeyAndOtherDetailsTable(keyVehicleLogViewModel));
                docList.Add(GetCustomerRefAndVwiTable(keyVehicleLogViewModel));
                docList.Add(CreateWeightandOtherDetailsTablePOI(keyVehicleLogViewModel, docketReason));
                docList.Add(CreateImageDetailsTable(keyVehicleLogViewModel, docketReason));
                if (i < Newids.Count - 1)
                {
                    docList.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                }
                

            }

            docList.Close();
            pdfDocList.Close();

            return IO.Path.GetFileName(reportPdfPath1);
        }
        //To Generate the Pdf In List stop

        //To generate POI List Globally strat
        public string GeneratePdfReportListGlobal(int keyVehicleLogId1, string docketReason, string blankNoteOnOrOff, string serialNo, List<int> ids)
        {

            var keyVehicleLogData = _guardLogDataProvider.GetKeyVehicleLogById(keyVehicleLogId1);

            var reportPdfPath1 = IO.Path.Combine(_reportRootDir, REPORT_DIR, $"{DateTime.Today:yyyyMMdd}_KVManualDocket_{keyVehicleLogData.GuardLogin.ClientSite.Name}_SN{serialNo}.pdf");
            var pdfDocList = new PdfDocument(new PdfWriter(reportPdfPath1));
            pdfDocList.SetDefaultPageSize(PageSize.A4);
            var docList = new Document(pdfDocList);

            var reportPdfPath = "";
            docList.Add(CreateReportHeaderTableList(keyVehicleLogData.ClientSiteLogBook.ClientSiteId));
            for (int i = 0; i < ids.Count; i++)
            {
                var keyVehicleLogId = ids[i];
                var keyVehicleLog = _guardLogDataProvider.GetKeyVehicleLogById(keyVehicleLogId);
                //if (keyVehicleLog.ClientSiteLogBook.ClientSite.Id== clientsiteid)
                //{



                _guardLogDataProvider.SaveDocketSerialNo(keyVehicleLogId, serialNo);

                if (keyVehicleLog == null)
                    return string.Empty;

                var kvlFields = _guardLogDataProvider.GetKeyVehicleLogFields();
                var keyVehicleLogViewModel = new KeyVehicleLogViewModel(keyVehicleLog, kvlFields);
                reportPdfPath = IO.Path.Combine(_reportRootDir, REPORT_DIR, $"{DateTime.Today:yyyyMMdd}_KVManualDocket_{keyVehicleLog.GuardLogin.ClientSite.Name}_SN{serialNo}.pdf");

                if (IO.File.Exists(reportPdfPath))
                    //IO.File.Delete(reportPdfPath);


                    docList.SetMargins(15f, 30f, 40f, 30f);

                docList.Add(CreateSiteDetailsTable(keyVehicleLog));
                docList.Add(CreateReportDetailsTable(keyVehicleLogViewModel, blankNoteOnOrOff));
                docList.Add(GetKeyAndOtherDetailsTable(keyVehicleLogViewModel));
                docList.Add(GetCustomerRefAndVwiTable(keyVehicleLogViewModel));
                docList.Add(CreateWeightandOtherDetailsTablePOI(keyVehicleLogViewModel, docketReason));
                docList.Add(CreateImageDetailsTable(keyVehicleLogViewModel, docketReason));
                if (i < ids.Count - 1)
                {
                    docList.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                }

            }

            docList.Close();
            pdfDocList.Close();

            return IO.Path.GetFileName(reportPdfPath1);
        }
        //To generate POI List Glabally stop
        public string GeneratePdfReport(int keyVehicleLogId, string docketReason, string blankNoteOnOrOff, string serialNo)
        {
            var keyVehicleLog = _guardLogDataProvider.GetKeyVehicleLogById(keyVehicleLogId);

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
                
           
            doc.Close();
            pdfDoc.Close();

            return IO.Path.GetFileName(reportPdfPath);
        }
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

            siteDataTable.AddCell(GetSiteHeaderCell("Date of Log:"));
            siteDataTable.AddCell(GetSiteValueCell(keyVehicleLog.ClientSiteLogBook.Date.ToString("yyyy-MMM-dd-dddd")));

            siteDataTable.AddCell(GetSiteHeaderCell("Guard Intials"));
            siteDataTable.AddCell(GetSiteValueCell(keyVehicleLog.GuardLogin.Guard.Initial ?? string.Empty));

            siteDataTable.AddCell(GetSiteHeaderCell("S/No:"));
            siteDataTable.AddCell(GetSerialNoValueCell(keyVehicleLog.DocketSerialNo ?? string.Empty));

            return siteDataTable;
        }

        private Table CreateReportDetailsTable(KeyVehicleLogViewModel keyVehicleLogViewModel,string  blankNoteOnOrOff)
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
                                        .Add(GetOtherDetailsTable(docketReason));
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
                                    .Add(GetSignDetailsTable());
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

            var cellSignDetails = new Cell()
                                    .SetPaddingRight(0)
                                    .SetPaddingTop(0)
                                    .SetBorder(Border.NO_BORDER)
                                    .Add(GetSignDetailsTable());
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
            var clockDetails = new Table(UnitValue.CreatePercentArray(new float[] { 15, 15, 15, 15, 40})).UseAllAvailableWidth();

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

        private static Table GetNotesTable(KeyVehicleLogViewModel keyVehicleLogViewModel,string blankNoteOnOrOff)
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

            reelsDetailsTable.AddCell(GetHeaderCell("Reels", textAlignment: TextAlignment.LEFT));

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

            vwiDetailsTable.AddCell(GetHeaderCell("VWI", textAlignment: TextAlignment.LEFT));

            vwiDetailsTable.AddCell(GetDataCell(keyVehicleLogViewModel.Detail.Vwi, textAlignment: TextAlignment.LEFT));
            
            return vwiDetailsTable;
        }

        private Table GetWeightDetailsTable(KeyVehicleLogViewModel keyVehicleLogViewModel)
        {
            var weightDetailsTable = new Table(UnitValue.CreatePercentArray(new float[] { 15, 15, 15, 40, 15})).UseAllAvailableWidth();

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

            otherDetailsTable.AddCell(GetHeaderCell("STMS Form \n(Outbound / pickup only)", textAlignment: TextAlignment.LEFT));
            otherDetailsTable.AddCell(GetHeaderCell("Why was MANUAL Docket Created?", textAlignment: TextAlignment.LEFT));

            otherDetailsTable.AddCell(GetDataCell("Y / NA", textAlignment: TextAlignment.CENTER, cellFontSize: CELL_FONT_SIZE_BIG)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE));
            otherDetailsTable.AddCell(GetDataCell(docketReason, textAlignment: TextAlignment.LEFT)
                 .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetHeight(10));

            return otherDetailsTable;
        }
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
        private static Table GetSignDetailsTable()
        {
            var signDetailsTable = new Table(UnitValue.CreatePercentArray(new float[] { 25, 75 })).UseAllAvailableWidth();

            signDetailsTable.AddCell(GetHeaderCell("Loader"));
            signDetailsTable.AddCell(GetDataCell("Name: \n\nSign:\n\n", textAlignment: TextAlignment.LEFT));

            signDetailsTable.AddCell(GetHeaderCell("Dispatch"));
            signDetailsTable.AddCell(GetDataCell("Name: \n\nSign:\n\n", textAlignment: TextAlignment.LEFT));

            signDetailsTable.AddCell(GetHeaderCell("Driver"));
            signDetailsTable.AddCell(GetDataCell("Name: \n\nSign:\n\n", textAlignment: TextAlignment.LEFT));

            return signDetailsTable;
        }
       // added to display the image
        private  Table GetImageTable(KeyVehicleLogViewModel keyVehicleLogViewModel)
        {

            var ImageTable = new Table(1).UseAllAvailableWidth();

            int personType = Convert.ToInt32(keyVehicleLogViewModel.Detail.PersonType);
            var IndividulaName = _guardLogDataProvider.GetIndividualType(personType);
            var Image = keyVehicleLogViewModel.Detail.CompanyName + "-" + IndividulaName.Name + "-" + keyVehicleLogViewModel.Detail.PersonName + ".jpg";
            
            var folderPath = IO.Path.Combine(_PersonimageRootDir,Image);
            if (IO.File.Exists(folderPath))
            {
                var Imagepath = new Image(ImageDataFactory.Create(IO.Path.Combine(_PersonimageRootDir, Image)))
                              .SetHeight(160);
                ImageTable.AddCell(new Cell().Add(Imagepath).SetBorder(Border.NO_BORDER));
            }
               
          
            
            return ImageTable;
        }
        private Table GetVehicleImageTable(KeyVehicleLogViewModel keyVehicleLogViewModel)
        {

            var ImageTable = new Table(1).UseAllAvailableWidth();

          
            var Image = keyVehicleLogViewModel.Detail.VehicleRego + ".jpg";
            var folderPath= IO.Path.Combine(_vehicleimageRootDir, keyVehicleLogViewModel.Detail.VehicleRego,Image);
            if (IO.File.Exists(folderPath))
            {
                var Imagepath = new Image(ImageDataFactory.Create(IO.Path.Combine(_vehicleimageRootDir, keyVehicleLogViewModel.Detail.VehicleRego, Image)))
               .SetHeight(160);

                ImageTable.AddCell(new Cell().Add(Imagepath).SetBorder(Border.NO_BORDER));
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
    }
}
