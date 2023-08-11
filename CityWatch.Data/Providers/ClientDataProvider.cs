using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using iText.StyledXmlParser.Jsoup.Safety;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;

namespace CityWatch.Data.Providers
{
    public enum OfficerPositionFilterForManning
    {
        PatrolOnly = 1,

        SecurityOnly = 2
    }
    public interface IClientDataProvider
    {
        List<ClientType> GetClientTypes();
        void SaveClientType(ClientType clientType);
        void DeleteClientType(int id);
        List<ClientSite> GetClientSites(int? typeId);
        List<ClientSite> GetNewClientSites();
        void SaveClientSite(ClientSite clientSite);
        void SaveCompanyDetails(CompanyDetails companyDetails);
        void DeleteClientSite(int id);
        List<ClientSiteKpiSetting> GetClientSiteKpiSettings();
        ClientSiteKpiSetting GetClientSiteKpiSetting(int clientSiteId);
        List<ClientSiteKpiSetting> GetClientSiteKpiSetting(int[] clientSiteIds);
        void SaveClientSiteKpiSetting(ClientSiteKpiSetting setting);
        int SaveClientSiteManningKpiSetting(ClientSiteKpiSetting setting);
        ClientSiteKpiNote GetClientSiteKpiNote(int id);
        int SaveClientSiteKpiNote(ClientSiteKpiNote note);
        List<ClientSiteLogBook> GetClientSiteLogBooks();
        List<ClientSiteLogBook> GetClientSiteLogBooks(int clientSiteId, LogBookType type, DateTime fromDate, DateTime toDate);
        ClientSiteLogBook GetClientSiteLogBook(int clientSiteId, LogBookType type, DateTime date);
        int SaveClientSiteLogBook(ClientSiteLogBook logBook);
        void MarkClientSiteLogBookAsUploaded(int logBookId, string fileName);
        void SetDataCollectionStatus(int clientSiteId, bool enabled);
    }

    public class ClientDataProvider : IClientDataProvider
    {
        private readonly CityWatchDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;


        public ClientDataProvider(IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration,
            CityWatchDbContext context)
        {
            _context = context;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
        }

        public List<ClientType> GetClientTypes()
        {
            return _context.ClientTypes.OrderBy(x => x.Name).ToList();
        }

        public void SaveClientType(ClientType clientType)
        {
            if (clientType == null)
                throw new ArgumentNullException();

            if (clientType.Id == -1)
            {
                _context.ClientTypes.Add(new ClientType() { Name = clientType.Name });
            }
            else
            {
                var clientTypeToUpdate = _context.ClientTypes.SingleOrDefault(x => x.Id == clientType.Id);
                if (clientTypeToUpdate == null)
                    throw new InvalidOperationException();

                clientTypeToUpdate.Name = clientType.Name;
            }
            _context.SaveChanges();
        }

        public void DeleteClientType(int id)
        {
            if (id == -1)
                return;

            var clientTypeToDelete = _context.ClientTypes.SingleOrDefault(x => x.Id == id);
            if (clientTypeToDelete == null)
                throw new InvalidOperationException();

            _context.ClientTypes.Remove(clientTypeToDelete);
            _context.SaveChanges();
        }

        public List<ClientSite> GetClientSites(int? typeId)
        {
            return _context.ClientSites
                .Where(x => !typeId.HasValue || (typeId.HasValue && x.TypeId == typeId.Value))
                .Include(x => x.ClientType)
                .OrderBy(x => x.ClientType.Name)
                .ThenBy(x => x.Name)
                .ToList();
        }

        public void SaveClientSite(ClientSite clientSite)
        {
            if (clientSite == null)
                throw new ArgumentNullException();

            var gpsHasChanged = false;

            if (clientSite.Id == -1)
            {
                _context.ClientSites.Add(new ClientSite()
                {
                    Name = clientSite.Name,
                    TypeId = clientSite.TypeId,
                    Emails = clientSite.Emails,
                    Address = clientSite.Address,
                    State = clientSite.State,
                    Billing = clientSite.Billing,
                    Gps = clientSite.Gps,
                    Status = clientSite.Status,
                    StatusDate = clientSite.StatusDate,
                    SiteEmail = clientSite.SiteEmail,
                    LandLine = "+61 (3)",
                    DataCollectionEnabled = true
                });

                gpsHasChanged = !string.IsNullOrEmpty(clientSite.Gps);
            }
            else
            {
                var clientSiteToUpdate = _context.ClientSites.SingleOrDefault(x => x.Id == clientSite.Id);
                if (clientSiteToUpdate == null)
                    throw new InvalidOperationException();

                gpsHasChanged = clientSiteToUpdate.Gps != clientSite.Gps;
                clientSiteToUpdate.Name = clientSite.Name;
                clientSiteToUpdate.Emails = clientSite.Emails;
                clientSiteToUpdate.Address = clientSite.Address;
                clientSiteToUpdate.State = clientSite.State;
                clientSiteToUpdate.Billing = clientSite.Billing;
                clientSiteToUpdate.Gps = clientSite.Gps;
                clientSiteToUpdate.Status = clientSite.Status;
                clientSiteToUpdate.StatusDate = clientSite.StatusDate;
                clientSiteToUpdate.SiteEmail = clientSite.SiteEmail;
            }
            _context.SaveChanges();

            if (gpsHasChanged && !string.IsNullOrEmpty(clientSite.Gps))
                CreateGpsImage(clientSite);
        }

        public void DeleteClientSite(int id)
        {
            if (id == -1)
                return;

            var clientSiteToDelete = _context.ClientSites.SingleOrDefault(x => x.Id == id);
            if (clientSiteToDelete == null)
                throw new InvalidOperationException();

            _context.ClientSites.Remove(clientSiteToDelete);
            _context.SaveChanges();
        }

        public List<ClientSiteKpiSetting> GetClientSiteKpiSettings()
        {
            return _context.ClientSiteKpiSettings
                .Include(x => x.ClientSite)
                .Include(x => x.ClientSite.ClientType)
                .Include(x => x.Notes)
                .ToList();
        }

        public ClientSiteKpiSetting GetClientSiteKpiSetting(int clientSiteId)
        {
            var clientSiteKpiSetting = _context.ClientSiteKpiSettings
                .Include(x => x.ClientSite)
                .Include(x => x.ClientSite.ClientType)
                .Include(x => x.ClientSiteDayKpiSettings)
                .Include(x => x.Notes)
                .SingleOrDefault(x => x.ClientSiteId == clientSiteId);
            if (clientSiteKpiSetting != null)
            {
                clientSiteKpiSetting.ClientSiteManningGuardKpiSettings = _context.ClientSiteManningKpiSettings.Where(x => x.SettingsId == clientSiteKpiSetting.Id && x.Type == ((int)OfficerPositionFilterForManning.SecurityOnly).ToString()).OrderBy(x => ((int)x.WeekDay + 6) % 7).ToList();
                clientSiteKpiSetting.ClientSiteManningPatrolCarKpiSettings = _context.ClientSiteManningKpiSettings.Where(x => x.SettingsId == clientSiteKpiSetting.Id && x.Type == ((int)OfficerPositionFilterForManning.PatrolOnly).ToString()).OrderBy(x => ((int)x.WeekDay + 6) % 7 ).ToList();
            }
            return clientSiteKpiSetting;
        }

        public List<ClientSiteKpiSetting> GetClientSiteKpiSetting(int[] clientSiteIds)
        {
            var clientSiteKpiSetting = _context.ClientSiteKpiSettings
                .Include(x => x.ClientSite)
                .Include(x => x.ClientSite.ClientType)
                .Include(x => x.ClientSiteDayKpiSettings)
                .Where(x => clientSiteIds.Contains(x.ClientSiteId))
                .ToList();

            return clientSiteKpiSetting;
        }

        public void SaveClientSiteKpiSetting(ClientSiteKpiSetting setting)
        {
            var entityState = !_context.ClientSiteKpiSettings.Any(x => x.ClientSiteId == setting.ClientSiteId) ? EntityState.Added : EntityState.Modified;
            _context.ClientSiteKpiSettings.Attach(setting);
            _context.Entry(setting).State = entityState;
            _context.SaveChanges();

            if (entityState == EntityState.Modified)
            {
                _context.ClientSiteDayKpiSettings.UpdateRange(setting.ClientSiteDayKpiSettings);
                _context.SaveChanges();
            }
        }

        public ClientSiteKpiNote GetClientSiteKpiNote(int id)
        {
            return _context.ClientSiteKpiNotes.SingleOrDefault(z => z.Id == id);
        }

        public int SaveClientSiteKpiNote(ClientSiteKpiNote note)
        {
            if (note.Id == 0)
            {
                _context.ClientSiteKpiNotes.Add(note);
            }
            else
            {
                var noteToUpdate = _context.ClientSiteKpiNotes.SingleOrDefault(z => z.Id == note.Id);
                if (noteToUpdate != null)
                {
                    noteToUpdate.Notes = note.Notes;
                }
            }
            _context.SaveChanges();
            return note.Id;
        }

        public List<ClientSiteLogBook> GetClientSiteLogBooks()
        {
            return _context.ClientSiteLogBooks
                .Include(x => x.ClientSite)
                .Include(x => x.ClientSite.ClientType)
                .ToList();
        }

        public List<ClientSiteLogBook> GetClientSiteLogBooks(int clientSiteId, LogBookType type, DateTime fromDate, DateTime toDate)
        {
            return _context.ClientSiteLogBooks
                .Where(z => z.ClientSiteId == clientSiteId && z.Type == type && z.Date >= fromDate && z.Date <= toDate)
                .ToList();
        }

        public ClientSiteLogBook GetClientSiteLogBook(int clientSiteId, LogBookType type, DateTime date)
        {
            return _context.ClientSiteLogBooks
                 .SingleOrDefault(z => z.ClientSiteId == clientSiteId && z.Type == type && z.Date == date);
        }

        public int SaveClientSiteLogBook(ClientSiteLogBook logBook)
        {
            if (logBook.Id == 0)
            {
                _context.ClientSiteLogBooks.Add(logBook);
            }
            else
            {
                var logBookToUpdate = _context.ClientSiteLogBooks.SingleOrDefault(z => z.Id == logBook.Id);
                if (logBookToUpdate != null)
                {
                    // nothing to update
                }
            }
            _context.SaveChanges();

            return logBook.Id;
        }

        public void MarkClientSiteLogBookAsUploaded(int logBookId, string fileName)
        {
            var logBookToUpdate = _context.ClientSiteLogBooks.SingleOrDefault(z => z.Id == logBookId);
            if (logBookToUpdate != null)
            {
                logBookToUpdate.DbxUploaded = true;
                logBookToUpdate.FileName = fileName;
                _context.SaveChanges();
            }
        }

        public void SetDataCollectionStatus(int clientSiteId, bool enabled)
        {
            var clientSite = _context.ClientSites.SingleOrDefault(z => z.Id == clientSiteId);
            if (clientSite != null)
            {
                clientSite.DataCollectionEnabled = enabled;
                _context.SaveChanges();
            }
        }

        private void CreateGpsImage(ClientSite clientSite)
        {
            string gpsImageDir = Path.Combine(_webHostEnvironment.WebRootPath, "GpsImage");
            var mapSettings = _configuration.GetSection("GoogleMap").Get(typeof(GoogleMapSettings)) as GoogleMapSettings;
            try
            {
                GoogleMapHelper.DownloadGpsImage(gpsImageDir, clientSite, mapSettings);
            }
            catch
            {

            }
        }


        public void SaveCompanyDetails(CompanyDetails companyDetails)
        {
            if (companyDetails.Id == 0)
            {
                companyDetails.LastUploaded = DateTime.Now;
                companyDetails.PrimaryLogoUploadedOn = DateTime.Now;
                companyDetails.HomePageMessageUploadedOn = DateTime.Now;
                companyDetails.BannerMessageUploadedOn = DateTime.Now;
                companyDetails.EmailMessageUploadedOn = DateTime.Now;

                _context.CompanyDetails.Add(companyDetails);
            }
            else
            {
                var templateToUpdate = _context.CompanyDetails.SingleOrDefault(x => x.Id == companyDetails.Id);
                if (templateToUpdate != null)
                {
                    templateToUpdate.Name = companyDetails.Name;
                    templateToUpdate.Domain = companyDetails.Domain;
                    templateToUpdate.LastUploaded = DateTime.Now;
                    templateToUpdate.PrimaryLogoUploadedOn = DateTime.Now;
                    templateToUpdate.PrimaryLogoPath = companyDetails.PrimaryLogoPath;
                    templateToUpdate.HomePageMessage = companyDetails.HomePageMessage;
                    templateToUpdate.MessageBarColour = companyDetails.MessageBarColour;
                    templateToUpdate.HomePageMessageUploadedOn = DateTime.Now;
                    templateToUpdate.BannerLogoPath = companyDetails.BannerLogoPath;
                    templateToUpdate.BannerMessage = companyDetails.BannerMessage;
                    templateToUpdate.BannerMessageUploadedOn = DateTime.Now;
                    templateToUpdate.Hyperlink = companyDetails.Hyperlink;
                    templateToUpdate.EmailMessage = companyDetails.EmailMessage;
                    templateToUpdate.EmailMessageUploadedOn = DateTime.Now;
                }
            }
            _context.SaveChanges();
        }

        /// <summary>
        /// For save and update ClientSite Manning details
        /// </summary>
        /// <param name="setting"></param>
        /// <returns></returns>
        public int SaveClientSiteManningKpiSetting(ClientSiteKpiSetting setting)
        {
            var success = 0;
            try
            {

                if (setting != null)
                {
                    if (setting.ClientSiteManningGuardKpiSettings.Any() || setting.ClientSiteManningPatrolCarKpiSettings.Any())
                    {
                        var entityStateforGuard = !_context.ClientSiteManningKpiSettings.Any(x => x.SettingsId == setting.Id && x.Type == ((int)OfficerPositionFilterForManning.SecurityOnly).ToString()) ? EntityState.Added : EntityState.Modified;
                        var entityStateforpatrolcar = !_context.ClientSiteManningKpiSettings.Any(x => x.SettingsId == setting.Id && x.Type == ((int)OfficerPositionFilterForManning.PatrolOnly).ToString()) ? EntityState.Added : EntityState.Modified;
                        var positionIdGuard = setting.ClientSiteManningGuardKpiSettings.Where(x => x.PositionId != 0).FirstOrDefault();
                        var positionIdPatrolCar = setting.ClientSiteManningPatrolCarKpiSettings.Where(x => x.PositionId != 0).FirstOrDefault();
                        if (positionIdGuard != null || positionIdPatrolCar != null)
                        {
                            //ClientSiteManningKpi Guard Add and Update
                            if (positionIdGuard != null)
                            {
                                //set the values for SettingsId and PositionId
                                setting.ClientSiteManningGuardKpiSettings.ForEach(x => { x.Type = ((int)OfficerPositionFilterForManning.SecurityOnly).ToString(); x.SettingsId = setting.Id; x.PositionId = positionIdGuard.PositionId; });
                                if (entityStateforGuard == EntityState.Added)
                                {
                                    if (setting.ClientSiteManningGuardKpiSettings.Any() && setting.ClientSiteManningGuardKpiSettings != null)
                                    {

                                        _context.ClientSiteManningKpiSettings.AddRange(setting.ClientSiteManningGuardKpiSettings);
                                        _context.SaveChanges();
                                        success = 1;

                                    }
                                }
                                else
                                {
                                    if (setting.ClientSiteManningGuardKpiSettings.Any() && setting.ClientSiteManningGuardKpiSettings != null)
                                    {
                                        _context.ClientSiteManningKpiSettings.UpdateRange(setting.ClientSiteManningGuardKpiSettings);
                                        _context.SaveChanges();
                                        success = 1;
                                    }
                                }
                            }
                            if (positionIdPatrolCar != null)
                            {   //ManningPatrolCar Add and Update
                                //set the values for SettingsId and PositionId
                                setting.ClientSiteManningPatrolCarKpiSettings.ForEach(x => { x.Type = ((int)OfficerPositionFilterForManning.PatrolOnly).ToString(); x.SettingsId = setting.Id; x.PositionId = positionIdPatrolCar.PositionId; });

                                if (entityStateforpatrolcar == EntityState.Added)
                                {
                                    if (setting.ClientSiteManningPatrolCarKpiSettings.Any())
                                    {
                                        _context.ClientSiteManningKpiSettings.AddRange(setting.ClientSiteManningPatrolCarKpiSettings);
                                        _context.SaveChanges();
                                        success = 1;
                                    }
                                }
                                else
                                {
                                    if (setting.ClientSiteManningPatrolCarKpiSettings.Any() && setting.ClientSiteManningPatrolCarKpiSettings != null)
                                    {
                                        _context.ClientSiteManningKpiSettings.UpdateRange(setting.ClientSiteManningPatrolCarKpiSettings);
                                        _context.SaveChanges();
                                        success = 1;
                                    }
                                }
                            }
                        }
                    }
                }

            }
            catch
            {
                return success;
            }
            return success;
        }
        public List<ClientSite> GetNewClientSites()
        {
            return _context.ClientSites

                .Include(x => x.ClientType)
                .OrderBy(x => x.ClientType.Name)
                .ThenBy(x => x.Name)
                .ToList();

        }
    }
}
