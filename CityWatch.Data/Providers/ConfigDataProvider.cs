using CityWatch.Data.Models;
using Dropbox.Api.Users;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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
            return _context.FeedbackTemplates.Where(x=>x.DeleteStatus==0).OrderBy(x => x.Name).ToList();
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
                    DeleteStatus=0
                });
            }
            else
            {
                var templateToUpdate = _context.FeedbackTemplates.SingleOrDefault(x => x.Id == template.Id && x.DeleteStatus==0);
                if (templateToUpdate == null)
                    throw new InvalidOperationException();

                templateToUpdate.Text = template.Text;
                templateToUpdate.Type = template.Type;
                templateToUpdate.BackgroundColour = template.BackgroundColour;
                templateToUpdate.TextColor = template.TextColor;
                templateToUpdate.DeleteStatus = 0;
            }
            _context.SaveChanges();
        }

        public void DeleteFeedbackTemplate(int id)
        {
            if (id == -1)
                return;

            var templateToDelete = _context.FeedbackTemplates.SingleOrDefault(x => x.Id == id && x.DeleteStatus==0);
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
            return _context.StaffDocuments.Where(x=>x.DocumentType== type).OrderBy(x => x.FileName).ToList();
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
                }
            }
            _context.SaveChanges();
        }

        public void DeleteStaffDocument(int id)
        {
            var docToDelete = _context.StaffDocuments.SingleOrDefault(x=> x.Id == id);
            if(docToDelete == null)
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
            if(incidentReportField == null)
                throw new ArgumentNullException();
            if (incidentReportField.Id == -1)
            {
                _context.IncidentReportFields.Add(new IncidentReportField()
                {
                    Name = incidentReportField.Name,
                    TypeId = incidentReportField.TypeId,
                    EmailTo = incidentReportField.EmailTo,
                    ClientSiteIds = incidentReportField.ClientSiteIds,
                    ClientTypeIds=incidentReportField.ClientTypeIds
                });
            }
            else
            {
                var reportFieldToUpdate = _context.IncidentReportFields.SingleOrDefault(x => x.Id == incidentReportField.Id);
                if(reportFieldToUpdate == null)
                    throw new InvalidOperationException();
                reportFieldToUpdate.Name = incidentReportField.Name;
                reportFieldToUpdate.TypeId = incidentReportField.TypeId;
                reportFieldToUpdate.EmailTo = incidentReportField.EmailTo;
                reportFieldToUpdate.ClientSiteIds = incidentReportField.ClientSiteIds;
                reportFieldToUpdate.ClientTypeIds = incidentReportField.ClientTypeIds;

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
                        ClientsiteName = ClientSiteName.Name
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
                        ClientsiteName = null
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
                    if (ClientSiteName!=null)
                    {
                        positionToUpdate.ClientsiteName = ClientSiteName.Name;
                    }
                    else
                    {
                        positionToUpdate.ClientsiteName = null;
                    }
                    
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
            templateToUpdate.PrimaryLogoPath= primaryLogoPath;
            _context.SaveChanges();
        }
        public List<IncidentReportsPlatesLoaded> GetPlatesLoaded(int  LogId)
        {
            return _context.IncidentReportsPlatesLoaded.Where(z => z.LogId == LogId).OrderBy(z => z.Id).ToList();
        }
        //to get functions for settings in radio check-start
        public List<RadioCheckStatusColor> GetRadioCheckStatusColorCode(string name)
        {
            //To get the Name in order
             var filteredRecords = _context.RadioCheckStatusColor
                 .Where(x => string.IsNullOrEmpty(name) || x.Name == name)
                 .OrderBy(x => x.Name)
                 .ToList();

            var redRecords = filteredRecords
                .Where(x => x.Name.StartsWith("Red", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var greenRecords = filteredRecords
                .Where(x => x.Name.StartsWith("Green", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var orderedRecords = new List<RadioCheckStatusColor>();
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
            return radiocheckstatus.OrderBy(x=>Convert.ToInt32(x.ReferenceNo)).ToList();
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
                items.Add(new SelectListItem("Select","", true));
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
			return _context.BroadcastBannerLiveEvents.Where(x=> x.ExpiryDate>=DateTime.Now.Date).ToList();
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
                    if(line.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                        line.StartsWith("https://", StringComparison.OrdinalIgnoreCase) )
                        urls = string.Concat(urls, line, "|");
                }
            }

            return urls;

        }

        public List<BroadcastBannerCalendarEvents> GetBroadcastCalendarEventsByDate()
        {
            return _context.BroadcastBannerCalendarEvents.Where(x =>  DateTime.Now.Date >=x.StartDate && DateTime.Now.Date <= x.ExpiryDate).ToList();
        }
        //broadcast banner live events-end
        //broadcast banner calendar events-start
        public int GetCalendarEventsCount()
        {
            return _context.BroadcastBannerCalendarEvents.Count();
        }
        public List<BroadcastBannerCalendarEvents> GetBroadcastCalendarEvents()
        {
            return _context.BroadcastBannerCalendarEvents.OrderBy(x=>Convert.ToInt32(x.ReferenceNo)).ToList();
        }
        //broadcast banner calendar events-end
        //SW Channels-start
        public List<SWChannels> GetSWChannels()
        {
            return _context.SWChannel.OrderBy(x=>x.Id).ToList();
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
            var res= _context.HrSettings.Include(z => z.HRGroups)
                .Include(z => z.ReferenceNoNumbers).Include(z => z.ReferenceNoAlphabets)
                .OrderBy(x => x.HRGroups.Name).ThenBy(x=> x.ReferenceNoNumbers.Name).
                ThenBy(x => x.ReferenceNoAlphabets.Name).ToList();
            return _context.HrSettings.Include(z => z.HRGroups)
                .Include(z => z.ReferenceNoNumbers)
                .Include(z => z.ReferenceNoAlphabets)
                .OrderBy(x => x.HRGroups.Name).ThenBy(x => x.ReferenceNoNumbers.Name).
                ThenBy(x => x.ReferenceNoAlphabets.Name).ToList();
        }
        public List<LicenseTypes> GetLicensesTypes()
        {
            return _context.LicenseTypes.Where(x=>x.IsDeleted==false)
                .OrderBy(x => x.Id).ToList();
        }
        //p1-191 hr files task 3-end
    }
}
