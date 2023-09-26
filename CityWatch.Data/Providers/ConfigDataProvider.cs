using CityWatch.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Data.Providers
{
    public interface IConfigDataProvider
    {
        List<FeedbackTemplate> GetFeedbackTemplates();
        List<FeedbackNewType> GetFeedbackTypes();
        void SaveFeedbackTemplate(FeedbackTemplate template);
        void DeleteFeedbackTemplate(int id);
        ReportTemplate GetReportTemplate();        
        void SaveReportTemplate(DateTime dateTimeUpdated);
        List<StaffDocument> GetStaffDocuments();
        void SaveStaffDocument(StaffDocument staffdocument);
        void DeleteStaffDocument(int id);
        List<State> GetStates();
        List <IncidentReportField> GetReportFields();
        List<IncidentReportField> GetReportFieldsByType(ReportFieldType type);
        void SaveReportField(IncidentReportField incidentReportField);
        void DeleteReportField(int id);
        List<IncidentReportPosition> GetPositions();
        void SavePostion(IncidentReportPosition incidentReportPosition);
        void DeletePosition(int id);
        void CrPrimaryLogoUpload(DateTime dateTimeUploaded, string primaryLogoPath);
        List<IncidentReportsPlatesLoaded> GetPlatesLoaded(int LogId);
        List<StaffDocument> GetStaffDocumentsUsingType(int type);

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
        public List<FeedbackNewType> GetFeedbackTypes()
        {
            return _context.FeedbackType.OrderBy(x => x.Id).ToList();
        }
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
    }
}
