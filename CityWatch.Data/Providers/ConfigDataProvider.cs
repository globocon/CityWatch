using CityWatch.Data.Models;
using Dropbox.Api.Users;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
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
            return _context.FeedbackTemplates.OrderBy(x => x.Name).ToList();
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
                    Type = template.Type
                });
            }
            else
            {
                var templateToUpdate = _context.FeedbackTemplates.SingleOrDefault(x => x.Id == template.Id);
                if (templateToUpdate == null)
                    throw new InvalidOperationException();

                templateToUpdate.Text = template.Text;
                templateToUpdate.Type = template.Type;
            }
            _context.SaveChanges();
        }

        public void DeleteFeedbackTemplate(int id)
        {
            if (id == -1)
                return;

            var templateToDelete = _context.FeedbackTemplates.SingleOrDefault(x => x.Id == id);
            if (templateToDelete == null)
                throw new InvalidOperationException();

            _context.FeedbackTemplates.Remove(templateToDelete);
            _context.SaveChanges();
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
                    EmailTo = incidentReportField.EmailTo
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

        public void SavePostion(IncidentReportPosition incidentReportPosition)
        {
            if (incidentReportPosition.Id == -1)
            {
                _context.IncidentReportPositions.Add(new IncidentReportPosition()
                {
                    Name = incidentReportPosition.Name,
                    EmailTo = incidentReportPosition.EmailTo,
                    IsPatrolCar = incidentReportPosition.IsPatrolCar,
                    DropboxDir = incidentReportPosition.DropboxDir
                });
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
            return _context.RadioCheckStatusColor.Where(x => String.IsNullOrEmpty(name) || x.Name == name).ToList();
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
            return radiocheckstatus;
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
    }
}
