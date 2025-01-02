using CityWatch.Data.Models;
using Dropbox.Api.Users;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1.BC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static Dropbox.Api.TeamLog.EventCategory;

namespace CityWatch.Data.Providers
{
    public interface IConfigDataProvider
    {
        List<FeedbackTemplate> GetFeedbackTemplates();
        List<FeedbackType> GetFeedbackTypes();
        int GetFeedbackTypesId(string Name);
        void SaveFeedbackTemplate(FeedbackTemplate template);
        void DeleteFeedbackTemplate(int id);
        ReportTemplate GetReportTemplate();
        void SaveReportTemplate(DateTime dateTimeUpdated);
        List<StaffDocument> GetStaffDocuments();
        void SaveStaffDocument(StaffDocument staffdocument);
        void DeleteStaffDocument(int id);
        List<State> GetStates();
        List<IncidentReportField> GetReportFields();
        List<IncidentReportField> GetReportFieldsByType(ReportFieldType type);
        void SaveReportField(IncidentReportField incidentReportField);
        void DeleteReportField(int id);
        List<IncidentReportPosition> GetPositions();
        List<IncidentReportPSPF> GetPSPF();
        int GetLastValue();
        int OnGetMaxIdIR();
        void SavePostion(IncidentReportPosition incidentReportPosition);
        void SavePSPF(IncidentReportPSPF incidentReportPSPF);
        void DeletePSPF(int id);
        string GetPSPFName(string name);
        string GetPSPFNameFromId(int pspfid);
        void UpdateDefault();
        void DeletePosition(int id);
        void CrPrimaryLogoUpload(DateTime dateTimeUploaded, string primaryLogoPath);
        List<IncidentReportsPlatesLoaded> GetPlatesLoaded(int LogId);
        List<StaffDocument> GetStaffDocumentsUsingType(int type);
        //to get functions for settings in radio check-start
        List<RadioCheckStatusColor> GetRadioCheckStatusColorCode(string name);
        List<RadioCheckStatus> GetRadioCheckStatusWithOutcome();
        int GetRadioCheckStatusCount();
        List<SelectListItem> GetRadioCheckStatusForDropDown(bool withoutSelect = false);

        //to get functions for settings in radio check-end
        //broadcast banner live events-start
        List<BroadcastBannerLiveEvents> GetBroadcastLiveEvents();
        List<BroadcastBannerLiveEvents> GetBroadcastLiveEventsByDate();
        //broadcast banner live events-end
        //broadcast banner calendar events-start
        int GetCalendarEventsCount();
        List<BroadcastBannerCalendarEvents> GetBroadcastCalendarEvents();
        List<BroadcastBannerCalendarEvents> GetBroadcastCalendarEventsByDate();
        string GetBroadcastLiveEventsNotExpired();
        string GetUrlsInsideBroadcastLiveEventsNotExpired();
        string GetBroadcastLiveEventsWeblink();

        void SaveDefaultEmail(string DefaultEmail);
        //broadcast banner calendar events-end
        //broadcast banner calendar events-end
        //SW Channels-start
        public List<SWChannels> GetSWChannels();
        //SW Channels-end
        //General Feeds-start
        List<GeneralFeeds> GetGeneralFeeds();
        //General Feeds-end
        public List<SmsChannel> GetSmsChannels();
        public IncidentReportPosition GetIsLogbookData(string Name);
        List<HrSettings> GetHRSettings();
        List<LicenseTypes> GetLicensesTypes();
        KeyVehcileLogField GetKVLogField();
        List<KeyVehicleLogVisitorPersonalDetail> GetProviderList(int ID);

        //p1-191 hr files task 3-end
        List<SelectListItem> GetClientSitesUsingLoginUserId(int guardId, string type = "");
        List<SelectListItem> GetDescList(int HRGroupId);
        void SaveCriticalDoc(CriticalDocuments CriticalDoc, bool updateClientSites = false);
        List<CriticalDocuments> GetCriticalDocs();
        public CriticalDocuments GetCriticalDocById(int CriticalID);
        public CriticalDocuments GetCriticalDocByIdandGuardId(int CriticalID, int GuardId);
        void DeleteCriticalDoc(int id);
        public HrSettings GetHrSettingById(int CriticalID);
        void RemoveCriticalDownSelect(CriticalDocuments CriticalDoc);

        public void UpdateStaffDocumentModuleType(StaffDocument staffdocument);

        public List<StaffDocument> GetStaffDocumentModuleDetails();

        public List<StaffDocument> GetStaffDocumentSOPDocDetails(int clientSiteId);

        public int SaveSubDomain(SubDomain subDomain);

        public SubDomain GetSubDomainDetails(string domain);

        List<HrSettings> GetHRSettingsWithHRLockEnable();

        List<HrSettings> GetHRSettingsUsingGroupId(int hrgroupId, string searchKeyNo);

        string GetClientTypeNameById(int id);

        List<string> GetCompanyDetailsUsingFilter(int[] clientSiteIds, string searchKeyNo);
        List<TrainingCourses> GetCourseDocsUsingSettingsId(int type);
        List<TrainingTQNumbers> GetTQNumbers();

    }

    public class ConfigDataProvider : IConfigDataProvider
    {
        private readonly CityWatchDbContext _context;

        public ConfigDataProvider(CityWatchDbContext context)
        {
            _context = context;
        }

        public List<FeedbackTemplate> GetFeedbackTemplates()
        {
            return _context.FeedbackTemplates.Where(x => x.DeleteStatus == 0).OrderBy(x => x.Name).ToList();
        }
        //to retrieve the feedback type-start
        public List<FeedbackType> GetFeedbackTypes()
        {
            return _context.FeedbackType.OrderBy(x => x.Id).ToList();
        }
        //to retrieve the feedback type-end
        //to retrieve the feedback type id -start
        public int GetFeedbackTypesId(string Name)
        {
            return _context.FeedbackType.Where(x => x.Name == Name).Select(x => x.Id).FirstOrDefault();
        }
        //to retrieve the feedback type id -end

        public void SaveFeedbackTemplate(FeedbackTemplate template)
        {
            if (template == null)
                throw new ArgumentNullException();

            if (template.Id == 0)
            {
                _context.FeedbackTemplates.Add(new FeedbackTemplate()
                {
                    Name = template.Name,
                    Text = template.Text,
                    Type = template.Type,
                    BackgroundColour = template.BackgroundColour,
                    TextColor = template.TextColor,
                    DeleteStatus = 0,
                    SendtoRC= template.SendtoRC
                });
            }
            else
            {
                var templateToUpdate = _context.FeedbackTemplates.SingleOrDefault(x => x.Id == template.Id && x.DeleteStatus == 0);
                if (templateToUpdate == null)
                    throw new InvalidOperationException();

                templateToUpdate.Text = template.Text;
                templateToUpdate.Type = template.Type;
                templateToUpdate.BackgroundColour = template.BackgroundColour;
                templateToUpdate.TextColor = template.TextColor;
                templateToUpdate.DeleteStatus = 0;
                templateToUpdate.SendtoRC = template.SendtoRC;
            }
            _context.SaveChanges();
        }

        public void DeleteFeedbackTemplate(int id)
        {
            if (id == -1)
                return;

            var templateToDelete = _context.FeedbackTemplates.SingleOrDefault(x => x.Id == id && x.DeleteStatus == 0);
            if (templateToDelete == null)
                throw new InvalidOperationException();
            //Not Delete data just change Status 0 to 1 
            templateToDelete.DeleteStatus = 1;
            _context.SaveChanges();
            //_context.FeedbackTemplates.Remove(templateToDelete);
            //_context.SaveChanges();
        }

        public ReportTemplate GetReportTemplate()
        {
            return _context.ReportTemplates.Single();
        }

        public void SaveReportTemplate(DateTime dateTimeUpdated)
        {
            var templateToUpdate = _context.ReportTemplates.Single();
            templateToUpdate.LastUpdated = dateTimeUpdated;
            _context.SaveChanges();
        }
        //To save the DefaultEmail
        public void SaveDefaultEmail(string DefaultEmail)
        {
            var templateToUpdate = _context.ReportTemplates.Single();
            templateToUpdate.DefaultEmail = DefaultEmail;
            _context.SaveChanges();
        }


        public List<State> GetStates()
        {
            return new List<State>()
            {
                new State() { Name = "ACT" },
                new State() { Name = "NSW" },
                new State() { Name = "NT" },
                new State() { Name = "QLD" },
                new State() { Name = "SA" },
                new State() { Name = "TAS" },
                new State() { Name = "VIC" },
                new State() { Name = "WA" }
            }
            .OrderBy(x => x.Name)
            .ToList();
        }

        public List<StaffDocument> GetStaffDocuments()
        {
            return _context.StaffDocuments.OrderBy(x => x.FileName).ToList();
        }

        public List<StaffDocument> GetStaffDocumentsUsingType(int type)
        {
            // Retrieve documents of the specified type
            var staffDocList = _context.StaffDocuments
                .Where(x => x.DocumentType == type)
                .ToList();
            // Check if any of the documents have a ClientSite assigned.
            bool hasClientSite = staffDocList.Any(doc => doc.ClientSite.HasValue);

            // If there are documents with ClientSite, retrieve their names and client types.
            if (hasClientSite)
            {
                foreach (var doc in staffDocList)
                {
                    if (doc.ClientSite.HasValue)
                    {
                        // Fetch the ClientSite along with ClientType using Include
                        var clientSite = _context.ClientSites
                            .Where(x => x.Id == doc.ClientSite)
                            .Include(x => x.ClientType)
                            .FirstOrDefault();

                        // Set the properties if ClientSite is found
                        if (clientSite != null)
                        {
                            doc.ClientSiteName = clientSite.Name ?? "Unknown";
                            doc.ClientTypeName = clientSite.ClientType?.Name ?? "Unknown";
                        }
                    }
                }

                // Sort the staff document list by ClientSite and then by ClientTypeName
                staffDocList = staffDocList
                    .OrderBy(x => x.ClientTypeName)
                    .ThenBy(x => x.ClientSiteName)
                    .ToList();
            }
            else
            {
                // If no ClientSite, just order by FileName
                staffDocList = staffDocList.OrderBy(x => x.FileName).ToList();
            }

            return staffDocList;
        }

        public void SaveStaffDocument(StaffDocument staffdocument)
        {
            if (staffdocument.Id == 0)
            {
                _context.StaffDocuments.Add(staffdocument);
            }
            else
            {
                var documentToUpdate = _context.StaffDocuments.SingleOrDefault(x => x.Id == staffdocument.Id);
                if (documentToUpdate != null)
                {
                    documentToUpdate.FileName = staffdocument.FileName;
                    documentToUpdate.LastUpdated = staffdocument.LastUpdated;
                    documentToUpdate.SOP = staffdocument.SOP;
                    documentToUpdate.ClientSite = staffdocument.ClientSite;
                }
            }
            _context.SaveChanges();
        }


        public int SaveSubDomain(SubDomain subDomain)
        {
            var status = 0;
            var subDomainToUpdate = _context.SubDomain.SingleOrDefault(x => x.Id == subDomain.Id);

            if (subDomainToUpdate != null)
            {
                // Check if another subdomain with the same domain exists
                var ifAlreadyExist = _context.SubDomain.Any(x => x.Domain == subDomain.Domain && x.Id != subDomain.Id);

                if (!ifAlreadyExist)
                {
                    // Update only if no conflicting domain exists
                    subDomainToUpdate.Domain = subDomain.Domain;
                    subDomainToUpdate.Logo = subDomain.Logo;
                    subDomainToUpdate.Enabled = subDomain.Enabled;
                    subDomainToUpdate.TypeId = subDomain.TypeId;
                    _context.SubDomain.Update(subDomainToUpdate);
                    status = 1;
                }
            }
            else
            {
                // Check if the domain already exists before adding a new subdomain
                var ifAlreadyExist = _context.SubDomain.Any(x => x.Domain == subDomain.Domain);

                if (!ifAlreadyExist)
                {
                    _context.SubDomain.Add(subDomain);
                    status = 1;
                }
            }

            _context.SaveChanges();
            return status;
        }

        public void UpdateStaffDocumentModuleType(StaffDocument staffdocument)
        {
            if (staffdocument.Id != 0)
            {
                var documentToUpdate = _context.StaffDocuments.SingleOrDefault(x => x.Id == staffdocument.Id);
                if (documentToUpdate != null)
                {
                    /*Only update module typle field*/
                    documentToUpdate.LastUpdated = staffdocument.LastUpdated;
                    documentToUpdate.DocumentModuleName = staffdocument.DocumentModuleName;
                }
            }

            _context.SaveChanges();
        }

        public void DeleteStaffDocument(int id)
        {
            var docToDelete = _context.StaffDocuments.SingleOrDefault(x => x.Id == id);
            if (docToDelete == null)
                throw new InvalidOperationException();

            _context.StaffDocuments.Remove(docToDelete);
            _context.SaveChanges();
        }

        public List<IncidentReportField> GetReportFields()
        {
            return _context.IncidentReportFields.OrderBy(x => x.TypeId).ThenBy(x => x.Name).ToList();
        }
        public List<KeyVehicleLogVisitorPersonalDetail> GetProviderList(int ID)
        {
            return _context.KeyVehicleLogVisitorPersonalDetails.Where(x => x.PersonType == ID).OrderBy(x => x.CompanyName).ToList();
        }
        public KeyVehcileLogField GetKVLogField()
        {
            return _context.KeyVehcileLogFields.Where(x => x.Name == "CRM (Supplier/Partner)").FirstOrDefault();
        }

        public List<IncidentReportField> GetReportFieldsByType(ReportFieldType type)
        {
            return GetReportFields().Where(x => x.TypeId == type).OrderBy(x => x.Name).ToList();
        }

        public void DeleteReportField(int id)
        {
            if (id == -1)
                return;

            var reportFieldToDelete = _context.IncidentReportFields.SingleOrDefault(x => x.Id == id);
            if (reportFieldToDelete == null)
                throw new InvalidOperationException();

            _context.IncidentReportFields.Remove(reportFieldToDelete);
            _context.SaveChanges();
        }

        public void SaveReportField(IncidentReportField incidentReportField)
        {
            if (incidentReportField == null)
                throw new ArgumentNullException();
            if (incidentReportField.Id == -1)
            {
                _context.IncidentReportFields.Add(new IncidentReportField()
                {
                    Name = incidentReportField.Name,
                    TypeId = incidentReportField.TypeId,
                    EmailTo = incidentReportField.EmailTo,
                    ClientSiteIds = incidentReportField.ClientSiteIds,
                    ClientTypeIds = incidentReportField.ClientTypeIds,
                    StampRcLogbook = incidentReportField.StampRcLogbook
                });
            }
            else
            {
                var reportFieldToUpdate = _context.IncidentReportFields.SingleOrDefault(x => x.Id == incidentReportField.Id);
                if (reportFieldToUpdate == null)
                    throw new InvalidOperationException();
                reportFieldToUpdate.Name = incidentReportField.Name;
                reportFieldToUpdate.TypeId = incidentReportField.TypeId;
                reportFieldToUpdate.EmailTo = incidentReportField.EmailTo;
                reportFieldToUpdate.ClientSiteIds = incidentReportField.ClientSiteIds;
                reportFieldToUpdate.ClientTypeIds = incidentReportField.ClientTypeIds;
                reportFieldToUpdate.StampRcLogbook = incidentReportField.StampRcLogbook;

            }
            _context.SaveChanges();
        }
        //code added For PSPF Report start
        public void UpdateDefault()
        {
            var pspfDefaultval = _context.IncidentReportPSPF.Where(z => z.IsDefault == true).Select(z => z.Id).FirstOrDefault();
            if (pspfDefaultval != 0)
            {
                var PSPFToUpdate = _context.IncidentReportPSPF.SingleOrDefault(x => x.Id == pspfDefaultval);
                PSPFToUpdate.IsDefault = false;
            }
            _context.SaveChanges();
        }
        public string GetPSPFName(string name)
        {
            return _context.IncidentReportPSPF.Where(x => x.Name == name).Select(x => x.Name).FirstOrDefault();

        }
        public string GetPSPFNameFromId(int id)
        {
            return _context.IncidentReportPSPF.Where(x => x.Id == id).Select(x => x.Name).FirstOrDefault();

        }
        public List<IncidentReportPSPF> GetPSPF()
        {
            return _context.IncidentReportPSPF.OrderBy(z => z.ReferenceNo).ToList();
        }
        public int GetLastValue()
        {
            return _context.IncidentReportPSPF.Count();
        }

        public int OnGetMaxIdIR()
        {
            var incidentReportid = _context.IncidentReportFields.Max(x => (int?)x.Id);
            return Convert.ToInt32(incidentReportid);

        }
        public void SavePSPF(IncidentReportPSPF incidentReportPSPF)
        {
            if (incidentReportPSPF.Id == -1)
            {
                _context.IncidentReportPSPF.Add(new IncidentReportPSPF()
                {
                    ReferenceNo = incidentReportPSPF.ReferenceNo,
                    Name = incidentReportPSPF.Name,
                    IsDefault = incidentReportPSPF.IsDefault
                });
            }
            else
            {
                var PSPFToUpdate = _context.IncidentReportPSPF.SingleOrDefault(x => x.Id == incidentReportPSPF.Id);
                if (PSPFToUpdate != null)
                {
                    PSPFToUpdate.ReferenceNo = incidentReportPSPF.ReferenceNo;
                    PSPFToUpdate.Name = incidentReportPSPF.Name;
                    PSPFToUpdate.IsDefault = incidentReportPSPF.IsDefault;
                }
            }
            _context.SaveChanges();
        }
        public void DeletePSPF(int id)
        {
            if (id == -1)
                return;

            var PSPFToDelete = _context.IncidentReportPSPF.SingleOrDefault(x => x.Id == id);
            if (PSPFToDelete == null)
                throw new InvalidOperationException();

            _context.IncidentReportPSPF.Remove(PSPFToDelete);
            _context.SaveChanges();
        }
        //code added For PSPF Report stop
        public List<IncidentReportPosition> GetPositions()
        {
            return _context.IncidentReportPositions.OrderBy(z => z.Name).ToList();
        }
        //To get the Logbbok Data-p6-101 start
        public IncidentReportPosition GetIsLogbookData(string Name)
        {
            return _context.IncidentReportPositions.Where(x => x.Name == Name).FirstOrDefault();
        }
        //To get the Logbbok Data-p6-101 stop
        public void SavePostion(IncidentReportPosition incidentReportPosition)
        {
            var ClientSiteName = _context.ClientSites.Where(x => x.Id == incidentReportPosition.ClientsiteId).FirstOrDefault();

            if (incidentReportPosition.Id == -1)
            {
                if (ClientSiteName != null)
                {
                    _context.IncidentReportPositions.Add(new IncidentReportPosition()
                    {
                        Name = incidentReportPosition.Name,
                        EmailTo = incidentReportPosition.EmailTo,
                        IsPatrolCar = incidentReportPosition.IsPatrolCar,
                        DropboxDir = incidentReportPosition.DropboxDir,
                        IsLogbook = incidentReportPosition.IsLogbook,
                        ClientsiteId = incidentReportPosition.ClientsiteId,
                        ClientsiteName = ClientSiteName.Name,
                        IsSmartwandbypass = incidentReportPosition.IsSmartwandbypass
                    });
                }
                else
                {
                    _context.IncidentReportPositions.Add(new IncidentReportPosition()
                    {
                        Name = incidentReportPosition.Name,
                        EmailTo = incidentReportPosition.EmailTo,
                        IsPatrolCar = incidentReportPosition.IsPatrolCar,
                        DropboxDir = incidentReportPosition.DropboxDir,
                        IsLogbook = incidentReportPosition.IsLogbook,
                        ClientsiteId = incidentReportPosition.ClientsiteId,
                        ClientsiteName = null,
                        IsSmartwandbypass = incidentReportPosition.IsSmartwandbypass
                    });

                }
            }
            else
            {
                var positionToUpdate = _context.IncidentReportPositions.SingleOrDefault(x => x.Id == incidentReportPosition.Id);
                if (positionToUpdate != null)
                {
                    positionToUpdate.Name = incidentReportPosition.Name;
                    positionToUpdate.EmailTo = incidentReportPosition.EmailTo;
                    positionToUpdate.IsPatrolCar = incidentReportPosition.IsPatrolCar;
                    positionToUpdate.DropboxDir = incidentReportPosition.DropboxDir;
                    positionToUpdate.IsLogbook = incidentReportPosition.IsLogbook;
                    positionToUpdate.ClientsiteId = incidentReportPosition.ClientsiteId;
                    if (ClientSiteName != null)
                    {
                        positionToUpdate.ClientsiteName = ClientSiteName.Name;
                    }
                    else
                    {
                        positionToUpdate.ClientsiteName = null;
                    }
                    positionToUpdate.IsSmartwandbypass = incidentReportPosition.IsSmartwandbypass;

                }
            }
            _context.SaveChanges();
        }

        public void DeletePosition(int id)
        {
            if (id == -1)
                return;

            var positionToDelete = _context.IncidentReportPositions.SingleOrDefault(x => x.Id == id);
            if (positionToDelete == null)
                throw new InvalidOperationException();

            _context.IncidentReportPositions.Remove(positionToDelete);
            _context.SaveChanges();
        }

        public void CrPrimaryLogoUpload(DateTime dateTimeUploaded, string primaryLogoPath)
        {
            var templateToUpdate = _context.CompanyDetails.Single();
            templateToUpdate.PrimaryLogoUploadedOn = dateTimeUploaded;
            templateToUpdate.PrimaryLogoPath = primaryLogoPath;
            _context.SaveChanges();
        }
        public List<IncidentReportsPlatesLoaded> GetPlatesLoaded(int LogId)
        {
            return _context.IncidentReportsPlatesLoaded.Where(z => z.LogId == LogId).OrderBy(z => z.Id).ToList();
        }
        //to get functions for settings in radio check-start
        public List<RadioCheckStatusColor> GetRadioCheckStatusColorCode(string name)
        {
            //To get the Name in order
            var filteredRecords = _context.RadioCheckStatusColor
                .OrderBy(x => x.Name)
                .ToList();

            var redRecords = filteredRecords
                .Where(x => x.Name.StartsWith("Red", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var greenRecords = filteredRecords
                .Where(x => x.Name.StartsWith("Green", StringComparison.OrdinalIgnoreCase))
                .ToList();


            var orderedRecords = new List<RadioCheckStatusColor>();

            // Add blank record from DB (if it has an empty or specific Name like "")
            var blankRecord = filteredRecords.FirstOrDefault(x => string.IsNullOrEmpty(x.Name) || x.Name == "");
            if (blankRecord != null)
            {
                orderedRecords.Add(blankRecord);
            }

            orderedRecords.AddRange(redRecords);
            orderedRecords.AddRange(greenRecords);

            return orderedRecords;
        }
        public List<RadioCheckStatus> GetRadioCheckStatusWithOutcome()
        {
            var radiocheckstatus = _context.RadioCheckStatus.ToList();
            foreach (var item in radiocheckstatus)
            {
                var radioCheckStatusColor = _context.RadioCheckStatusColor.Where(x => x.Id == item.RadioCheckStatusColorId).ToList();
                foreach (var item1 in radioCheckStatusColor)
                {
                    item.RadioCheckStatusColor.Name = item1.Name;
                }

            }
            // return _context.RadioCheckStatus.ToList();
            return radiocheckstatus.OrderBy(x => Convert.ToInt32(x.ReferenceNo)).ToList();
        }
        public int GetRadioCheckStatusCount()
        {
            return _context.RadioCheckStatus.Count();
        }
        public List<SelectListItem> GetRadioCheckStatusForDropDown(bool withoutSelect = true)
        {
            var radioCheckStatuses = GetRadioCheckStatusWithOutcome();
            var items = new List<SelectListItem>();

            if (!withoutSelect)
            {
                items.Add(new SelectListItem("Select", "", true));
            }

            foreach (var item in radioCheckStatuses)
            {
                //items.Add(new SelectListItem(item.Name, item.Id.ToString()));
                items.Add(new SelectListItem(item.Name, item.Id.ToString()));
            }

            return items;
        }

        //to get functions for settings in radio check-end
        //broadcast banner live events-start
        public List<BroadcastBannerLiveEvents> GetBroadcastLiveEvents()
        {
            return _context.BroadcastBannerLiveEvents.ToList();
        }
        public List<BroadcastBannerLiveEvents> GetBroadcastLiveEventsByDate()
        {
            return _context.BroadcastBannerLiveEvents.Where(x => x.ExpiryDate >= DateTime.Now.Date).ToList();
        }
        public string GetBroadcastLiveEventsNotExpired()
        {
            var lv = _context.BroadcastBannerLiveEvents.Where(x => x.ExpiryDate >= DateTime.Now.Date).ToList();
            var LiveEventstxtmsg = string.Empty;
            if (lv != null)
            {
                foreach (var fileName in lv)
                {
                    LiveEventstxtmsg = LiveEventstxtmsg + fileName.TextMessage.Replace("\n", "\t") + '\t' + '\t';
                }
            }
            return LiveEventstxtmsg;
        }
        public string GetBroadcastLiveEventsWeblink()
        {
            var lv = _context.BroadcastBannerLiveEvents.Where(x => x.ExpiryDate >= DateTime.Now.Date).ToList();
            var LiveEventstxtmsg = string.Empty;
            if (lv != null)
            {
                foreach (var fileName in lv)
                {
                    LiveEventstxtmsg = fileName.Weblink;
                }
            }
            return LiveEventstxtmsg;
        }
        public string GetUrlsInsideBroadcastLiveEventsNotExpired()
        {
            string urls = string.Empty;
            var lv = _context.BroadcastBannerLiveEvents.Where(x => x.ExpiryDate >= DateTime.Now.Date).ToList();
            var LiveEventstxtmsg = string.Empty;
            if (lv != null)
            {
                foreach (var fileName in lv)
                {
                    LiveEventstxtmsg = LiveEventstxtmsg + fileName.TextMessage.Replace("\n", "\t") + '\t' + '\t';
                }

                var lvsplit = LiveEventstxtmsg.Split(" ");

                foreach (var line in lvsplit)
                {
                    if (line.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                        line.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        urls = string.Concat(urls, line, "|");
                }
            }

            return urls;

        }

        public List<BroadcastBannerCalendarEvents> GetBroadcastCalendarEventsByDate()
        {
            return _context.BroadcastBannerCalendarEvents.Where(x => DateTime.Now.Date >= x.StartDate && DateTime.Now.Date <= x.ExpiryDate).ToList();
        }
        //broadcast banner live events-end
        //broadcast banner calendar events-start
        public int GetCalendarEventsCount()
        {
            return _context.BroadcastBannerCalendarEvents.Count();
        }
        public List<BroadcastBannerCalendarEvents> GetBroadcastCalendarEvents()
        {
            return _context.BroadcastBannerCalendarEvents.OrderBy(x => Convert.ToInt32(x.ReferenceNo)).ToList();
        }
        //broadcast banner calendar events-end
        //SW Channels-start
        public List<SWChannels> GetSWChannels()
        {
            return _context.SWChannel.OrderBy(x => x.Id).ToList();
        }
        //SW Channels-end
        //General Feeds-start
        public List<GeneralFeeds> GetGeneralFeeds()
        {
            return _context.GeneralFeeds.OrderBy(x => x.Id).ToList();
        }
        //General Feeds-end
        public List<SmsChannel> GetSmsChannels()
        {
            return _context.SmsChannel.OrderBy(x => x.Id).ToList();
        }
        //p1-191 hr files task 3-start
        public List<HrSettings> GetHRSettings()
        {
            return _context.HrSettings.Include(z => z.HRGroups)
                .Include(z => z.ReferenceNoNumbers)
                .Include(z => z.ReferenceNoAlphabets)
                .Include(z => z.hrSettingsClientStates)
                .Include(z => z.hrSettingsClientSites)
                .ThenInclude(y => y.ClientSite)
              .ThenInclude(y => y.ClientType)
                .OrderBy(x => x.HRGroups.Name).ThenBy(x => x.ReferenceNoNumbers.Name).
                ThenBy(x => x.ReferenceNoAlphabets.Name).ToList();
        }
        public List<HrSettings> GetHRSettingsUsingGroupId(int hrgroupId,string searchKeyNo)
        {



            var result = _context.HrSettings
       .Where(x => x.HRGroupId == hrgroupId)
       .Include(z => z.HRGroups)
       .Include(x => x.ReferenceNoNumbers)
       .Include(x => x.ReferenceNoAlphabets)
       .OrderBy(x => x.Description)
        .OrderBy(x => x.HRGroups.Name).ThenBy(x => x.ReferenceNoNumbers.Name).
        ThenBy(x => x.ReferenceNoAlphabets.Name).ToList();

            //.Where(z => string.IsNullOrEmpty(searchKeyNo) || z.KeyNo.Contains(searchKeyNo, StringComparison.OrdinalIgnoreCase))
            return result.Where(x => (string.IsNullOrEmpty(searchKeyNo) ||
                 x.Description.Contains(searchKeyNo, StringComparison.OrdinalIgnoreCase) ||
                 x.ReferenceNo.Contains(searchKeyNo, StringComparison.OrdinalIgnoreCase))).ToList(); // Adding search by ReferenceNoAlphabets

        }


        public List<string> GetCompanyDetailsUsingFilter(int[] clientSiteIds, string searchKeyNo)
        {
            // Initialize the base query
            var query = _context.KeyVehicleLogs.AsQueryable();
            query = query.Where(z => z.ClientSiteLogBook.Type == LogBookType.VehicleAndKeyLog);

            // Apply the search key filter if provided
            if (!string.IsNullOrEmpty(searchKeyNo))
            {

                query = _context.KeyVehicleLogs.Where(k => string.IsNullOrEmpty(searchKeyNo) ||
                k.CompanyName.ToLower().Contains(searchKeyNo.ToLower()));
                //query = query.Where(z => z.CompanyName.Contains(searchKeyNo, StringComparison.OrdinalIgnoreCase));
            }

            // Apply the client site and log book type filters if client site IDs are provided
            if (clientSiteIds?.Length > 0)
            {
                query = query.Where(z => clientSiteIds.Contains(z.ClientSiteLogBook.ClientSiteId)
                                      && z.ClientSiteLogBook.Type == LogBookType.VehicleAndKeyLog);
            }

            // Execute the query and return the results
            // Select distinct company names and return as a list
            return query
                .Select(z => z.CompanyName) // Project only the company name
                .Distinct()
                 .OrderBy(name => name)  // Get distinct company names
                .ToList();
        }

        public List<HrSettings> GetHRSettingsWithHRLockEnable()
        {
            return _context.HrSettings.Where(x => x.HRLock == true).ToList();
        }
        public List<LicenseTypes> GetLicensesTypes()
        {
            return _context.LicenseTypes.Where(x => x.IsDeleted == false)
                .OrderBy(x => x.Id).ToList();
        }
        //p1-191 hr files task 3-end
        //p1-213 critical documents start
        public List<SelectListItem> GetClientSitesUsingLoginUserId(int guardId, string type = "")
        {
            if (guardId == 0)
            {
                var sites = new List<SelectListItem>();


                var mapping = _context.UserClientSiteAccess
               .Where(x => x.ClientSite.ClientType.Name.Trim() == type.Trim() && x.ClientSite.IsActive == true)
               .Include(x => x.ClientSite)
               .Include(x => x.ClientSite.ClientType)
               .Select(x => new { x.ClientSiteId, x.ClientSite.Name })
            .Distinct().
             OrderBy(x => x.Name).ToList();


                foreach (var item in mapping)
                {
                    sites.Add(new SelectListItem(item.Name, item.ClientSiteId.ToString()));
                }
                return sites;

            }
            else
            {
                var sites = new List<SelectListItem>();

                var mapping = _context.GuardLogins
                .Where(z => z.GuardId == guardId)
                    .Include(z => z.ClientSite)
                    .OrderBy(x => x.ClientSite.Name)
                    .ToList();


                foreach (var item in mapping)
                {
                    if (!sites.Any(cus => cus.Text == item.ClientSite.Name))
                    {
                        sites.Add(new SelectListItem(item.ClientSite.Name, item.ClientSite.Id.ToString()));
                    }
                }
                return sites;

            }
        }

        public List<SelectListItem> GetDescList(int HRGroupId)
        {
            var Group = new List<SelectListItem>();
            var descriptions = _context.HrSettings.Include(z => z.HRGroups)
       .Include(z => z.ReferenceNoNumbers)
       .Include(z => z.ReferenceNoAlphabets)
       .OrderBy(x => x.HRGroups.Name).ThenBy(x => x.ReferenceNoNumbers.Name).
       ThenBy(x => x.ReferenceNoAlphabets.Name).Where(z => z.HRGroups.Id == HRGroupId).ToList();
            var mapping = _context.HrSettings.Where(x => x.HRGroupId == HRGroupId).ToList();
            foreach (var item in descriptions)
            {
                Group.Add(new SelectListItem(item.ReferenceNo + "&nbsp;&nbsp;&nbsp;&nbsp;" + item.Description, item.Id.ToString()));

            }
            return Group;
        }
        public void RemoveCriticalDownSelect(CriticalDocuments CriticalDoc)
        {
            var Document = _context.KpiSendSchedules.FirstOrDefault(z => z.CriticalGroupNameID == CriticalDoc.Id.ToString());
            if (Document != null)
            {
                Document.IsCriticalDocumentDownselect = false;
                Document.CriticalGroupNameID = null;
                _context.SaveChanges();
            }
        }
        public void SaveCriticalDoc(CriticalDocuments CriticalDoc, bool updateClientSites1 = false)
        {
            var DesriptionDoc = _context.CriticalDocuments.Include(z => z.CriticalDocumentDescriptions).SingleOrDefault(z => z.Id == CriticalDoc.Id);
            var Document = _context.CriticalDocuments.Include(z => z.CriticalDocumentsClientSites)
                .Include(z => z.CriticalDocumentDescriptions)
                .SingleOrDefault(z => z.Id == CriticalDoc.Id);
            if (Document == null)
                _context.Add(CriticalDoc);
            else
            {
                if (updateClientSites1)
                {
                    _context.CriticalDocumentsClientSites.RemoveRange(Document.CriticalDocumentsClientSites);
                    _context.CriticalDocumentDescriptions.RemoveRange(DesriptionDoc.CriticalDocumentDescriptions);
                    _context.SaveChanges();
                }

                Document.HRGroupID = CriticalDoc.HRGroupID;
                Document.ClientTypeId = CriticalDoc.ClientTypeId;
                Document.GroupName = CriticalDoc.GroupName;
                Document.IsCriticalDocumentDownselect = CriticalDoc.IsCriticalDocumentDownselect;

                if (updateClientSites1)
                    Document.CriticalDocumentsClientSites = CriticalDoc.CriticalDocumentsClientSites;
                DesriptionDoc.CriticalDocumentDescriptions = CriticalDoc.CriticalDocumentDescriptions;
            }
            _context.SaveChanges();

        }
        public List<CriticalDocuments> GetCriticalDocs()
        {


            var document1 = _context.CriticalDocuments
    .Include(z => z.CriticalDocumentsClientSites)
        .ThenInclude(y => y.ClientSite)
            .ThenInclude(cs => cs.ClientType)
    .Include(z => z.CriticalDocumentDescriptions)
            .ThenInclude(y => y.HRSettings)
     .ThenInclude(z => z.HRGroups)
     .Include(z => z.CriticalDocumentDescriptions)
    .ThenInclude(y => y.HRSettings)
        .ThenInclude(z => z.ReferenceNoNumbers)
         .Include(z => z.CriticalDocumentDescriptions)
    .ThenInclude(y => y.HRSettings)
     .ThenInclude(z => z.ReferenceNoAlphabets)
     .Select(d => new
     {
         CriticalDocument = d,
         SortedDescriptions = d.CriticalDocumentDescriptions
            .Where(desc => desc.HRSettings != null && desc.HRSettings.ReferenceNoNumbers != null && desc.HRSettings.ReferenceNoAlphabets != null)
            .OrderBy(desc => desc.HRSettings.ReferenceNoNumbers)
            .ThenBy(d => d.HRSettings.ReferenceNoAlphabets)
            .ToList()
     })
    .ToList();
            var sortedDocuments = document1.Select(doc =>
            {
                var criticalDocument = doc.CriticalDocument;
                criticalDocument.CriticalDocumentDescriptions = doc.SortedDescriptions;
                return criticalDocument;
            }).ToList();

            return sortedDocuments;


        }
        public CriticalDocuments GetCriticalDocById(int CriticalID)
        {
            var sss1 = _context.CriticalDocuments
   .Include(z => z.CriticalDocumentsClientSites)
       .ThenInclude(y => y.ClientSite)
           .ThenInclude(cs => cs.ClientType)
   .Include(z => z.CriticalDocumentDescriptions)
           .ThenInclude(y => y.HRSettings)
    .ThenInclude(z => z.HRGroups)
    .Include(z => z.CriticalDocumentDescriptions)
   .ThenInclude(y => y.HRSettings)
       .ThenInclude(z => z.ReferenceNoNumbers)
        .Include(z => z.CriticalDocumentDescriptions)
   .ThenInclude(y => y.HRSettings)
    .ThenInclude(z => z.ReferenceNoAlphabets)
  .SingleOrDefault(x => x.Id == CriticalID);

            var document = _context.CriticalDocuments
   .Include(z => z.CriticalDocumentsClientSites)
       .ThenInclude(y => y.ClientSite)
           .ThenInclude(cs => cs.ClientType)
   .Include(z => z.CriticalDocumentDescriptions)
           .ThenInclude(y => y.HRSettings)
    .ThenInclude(z => z.HRGroups)
    .Include(z => z.CriticalDocumentDescriptions)
   .ThenInclude(y => y.HRSettings)
       .ThenInclude(z => z.ReferenceNoNumbers)
        .Include(z => z.CriticalDocumentDescriptions)
   .ThenInclude(y => y.HRSettings)
    .ThenInclude(z => z.ReferenceNoAlphabets)
  .Where(x => x.Id == CriticalID)
    .Select(d => new
    {
        CriticalDocument = d,
        SortedDescriptions = d.CriticalDocumentDescriptions
            .Where(desc => desc.HRSettings != null && desc.HRSettings.ReferenceNoNumbers != null && desc.HRSettings.ReferenceNoAlphabets != null)
            .OrderBy(desc => desc.HRSettings.ReferenceNoNumbers)
            .ThenBy(d => d.HRSettings.ReferenceNoAlphabets)
            .ToList()
    })
    .SingleOrDefault();

            if (document != null)
            {
                document.CriticalDocument.CriticalDocumentDescriptions = document.SortedDescriptions;
                return document.CriticalDocument;
            }

            return null;
        }


        public HrSettings GetHrSettingById(int CriticalID)
        {


            return _context.HrSettings.Include(z => z.HRGroups)
                .Include(z => z.ReferenceNoNumbers)
                .Include(z => z.ReferenceNoAlphabets)
                .Include(z => z.hrSettingsClientStates)
                .Include(z => z.hrSettingsClientSites)
                .ThenInclude(y => y.ClientSite)
              .ThenInclude(y => y.ClientType)
                .OrderBy(z => z.HRGroups.Name).ThenBy(z => z.ReferenceNoNumbers.Name).
                ThenBy(z => z.ReferenceNoAlphabets.Name).SingleOrDefault(z => z.Id == CriticalID);


        }
        public CriticalDocuments GetCriticalDocByIdandGuardId(int CriticalID, int GuardId)
        {
            var distinctClientSiteIds = _context.GuardLogins
          .Where(z => z.GuardId == GuardId)
          .Select(z => z.ClientSite.Id)
          .Distinct()
          .ToList();
            var KpiSendSchedule = _context.CriticalDocuments
              .Include(z => z.CriticalDocumentsClientSites)
              .ThenInclude(y => y.ClientSite)
              .ThenInclude(y => y.ClientType)
              .SingleOrDefault(x => x.Id == CriticalID);
            foreach (var li in KpiSendSchedule.CriticalDocumentsClientSites)
            {
                if (!distinctClientSiteIds.Contains(li.ClientSiteId))
                {
                    KpiSendSchedule.CriticalDocumentsClientSites.Remove(li);

                }

            }
            return KpiSendSchedule;
        }
        public void DeleteCriticalDoc(int id)
        {
            var recordToDelete = _context.CriticalDocuments.SingleOrDefault(x => x.Id == id);
            if (recordToDelete == null)
                throw new InvalidOperationException();

            _context.CriticalDocuments.Remove(recordToDelete);
            _context.SaveChanges();
        }


        public List<StaffDocument> GetStaffDocumentModuleDetails()
        {

            return _context.StaffDocuments.Where(x => x.DocumentModuleName.Trim() != string.Empty).ToList();



        }

        public List<StaffDocument> GetStaffDocumentSOPDocDetails(int clientSiteId)
        {

            return _context.StaffDocuments.Where(x => x.DocumentType == 4 && x.ClientSite == clientSiteId && x.SOP != string.Empty).ToList();



        }


        public SubDomain GetSubDomainDetails(string domain)
        {

            return _context.SubDomain.Where(x => x.Domain.Trim() == domain.Trim()).FirstOrDefault();



        }
        //p1-213 critical documents stop
        public string GetClientTypeNameById(int id)
        {

            return _context.ClientTypes.Where(x => x.Id == id).FirstOrDefault().Name;



        }
        public List<TrainingCourses> GetCourseDocsUsingSettingsId(int type)
        {
            // Retrieve documents of the specified type
            var courseDocList = _context.TrainingCourses
                .Where(x => x.HRSettingsId == type)
                .ToList();
            

            return courseDocList;
        }
        public List<TrainingTQNumbers> GetTQNumbers()
        {
            return _context.TrainingTQNumbers.OrderBy(x => x.Id).ToList();
        }

    }
}
