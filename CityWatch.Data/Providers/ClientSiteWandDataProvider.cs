using CityWatch.Data.Models;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Threading.Tasks;

namespace CityWatch.Data.Providers
{
    public interface IClientSiteWandDataProvider
    {
        List<ClientSiteSmartWand> GetClientSiteSmartWands();
        List<ClientSiteSmartWand> GetClientSiteSmartWands(string searchTerms);
        void SaveClientSiteSmartWand(ClientSiteSmartWand clientSiteSmartWand);
        bool UpdateClientSiteSmartWand(ClientSiteSmartWand clientSiteSmartWand);
        bool DeRegisterDeviceWithSmartWand(int SmartWandId);
        void DeleteClientSiteSmartWand(int id);
        List<ClientSiteSmartWand> GetClientSiteAllSmartWands(int[] clientSiteIds);
        List<ClientSiteRadioChecksActivityStatus_History> GetClientSiteAllSmartWandsStrikes(int[] clientSiteIds, DateTime fromDate, DateTime toDate);
        List<ClientSitePatrolCar> GetClientSitePatrolCars(int clientSiteId);
        void SaveClientSitePatrolCar(ClientSitePatrolCar clientSitePatrolCar);
        void DeleteClientSitePatrolCar(int id);
        ClientSiteSmartWand GetClientSiteSmartWandsNo(string PhoneNumber, int id);
        void SaveClientSiteSmartWandTags(ClientSiteSmartWandTags clientSiteSmartWandTag);
        void DeleteClientSiteSmartWandTags(int id);
        List<ClientSiteSmartWandTags> GetClientSiteSmartWandTags();
        List<SmartWandTagsType> GetSmartWandTagsType();
        void SaveSmartWandTagLog(ClientSiteSmartWandTagsHitLog log);

        List<ClientSiteSmartWandTags> GetClientSiteWandTagsForClientSites(int[] clientSiteIds);
        List<ClientSiteSmartWandTags> GetClientSiteWandTagsForClientSitesFromLogs(int[] clientSiteIds);
        List<ClientSiteSmartWandTagsHitLog> GetClientSiteSmartWandTagsHitLogs(int[] clientSiteIds, DateTime fromDate, DateTime toDate);
        ClientSiteSmartWandTagsHitLog GetLastScannedTagDateTime(int siteId, string tagUid);
        List<SiteEquipmentsDetails> GetClientSiteEquipments();
        void SaveClientSiteEquipments(SiteEquipmentsDetails siteEquipmentsDetails);
        void DeleteClientSiteEquipments(int id);
        bool SaveOfflineSmartWandTagHitDataRecordError(ClientSiteSmartWandTagsHitLogCacheOfflineNotSynced _offlineRecordsNotSynced);
        List<ClientSiteSmartWandTags> GetAllClientSitesSmartwandTags();
        public List<ClientSiteSmartWandTags> GetAllSmartwandTags();
        List<IncidentReportPosition> GetPatrolCars();
        List<IncidentReportPosition> GetPatrolCarsForSite(int[] clientsiteid);

    }

    public class ClientSiteWandDataProvider : IClientSiteWandDataProvider
    {
        private readonly CityWatchDbContext _dbContext;
        private readonly IConfigDataProvider _configDataProvider;

        public ClientSiteWandDataProvider(CityWatchDbContext dbContext, IConfigDataProvider configDataProvider)
        {
            _dbContext = dbContext;
            _configDataProvider = configDataProvider;
        }

        public List<ClientSiteSmartWand> GetClientSiteSmartWands()
        {
            return _dbContext.ClientSiteSmartWands
                .Where(x => x.ClientSite.IsActive == true && x.IsDeleted == false)
                .Include(x => x.ClientSite)
                .ToList();
        }
        public ClientSiteSmartWand GetClientSiteSmartWandsNo(string PhoneNumber, int id)
        {
            return _dbContext.ClientSiteSmartWands
                .Where(x => x.ClientSite.IsActive == true && x.IsDeleted == false)
                .Include(x => x.ClientSite)
                .Where(x => x.PhoneNumber == PhoneNumber && x.Id != id)
                .FirstOrDefault();
        }
        public List<ClientSiteSmartWand> GetClientSiteSmartWands(string searchTerms)
        {
            return _dbContext.ClientSiteSmartWands
                .Include(x => x.ClientSite)
                .Where(x => (string.IsNullOrEmpty(searchTerms) || x.PhoneNumber.Contains(searchTerms)) && x.ClientSite.IsActive == true && x.IsDeleted == false)
                .ToList();
        }

        public void SaveClientSiteSmartWand(ClientSiteSmartWand clientSiteSmartWand)
        {
            if (clientSiteSmartWand == null)
                throw new ArgumentNullException();

            if (clientSiteSmartWand.Id == -1)
            {
                clientSiteSmartWand.Id = 0;
                clientSiteSmartWand.IsDeleted = false;
                _dbContext.ClientSiteSmartWands.Add(clientSiteSmartWand);
            }
            else
            {
                var clientSiteSmartWandToUpdate = _dbContext.ClientSiteSmartWands.SingleOrDefault(x => x.Id == clientSiteSmartWand.Id);
                if (clientSiteSmartWandToUpdate != null)
                {
                    clientSiteSmartWandToUpdate.SmartWandId = clientSiteSmartWand.SmartWandId;
                    clientSiteSmartWandToUpdate.PhoneNumber = clientSiteSmartWand.PhoneNumber;
                    clientSiteSmartWandToUpdate.SIMProvider = clientSiteSmartWand.SIMProvider;
                    clientSiteSmartWandToUpdate.IMEI = clientSiteSmartWand.IMEI;
                    clientSiteSmartWandToUpdate.PatrolCarId = clientSiteSmartWand.PatrolCarId;
                }
            }
            _dbContext.SaveChanges();
        }

        public bool UpdateClientSiteSmartWand(ClientSiteSmartWand clientSiteSmartWand)
        {
            if (clientSiteSmartWand == null)
                throw new ArgumentNullException();

            if (clientSiteSmartWand.Id <= 0)            
                throw new Exception("Invalid Smart Wand.") ;
            
            _dbContext.SaveChanges();
            return true;                       
        }

        public bool DeRegisterDeviceWithSmartWand(int SmartWandId)
        {
            var smartWand = _dbContext.ClientSiteSmartWands.Where(x => x.Id == SmartWandId).FirstOrDefault();
            if (smartWand != null)
            {
                smartWand.DeviceId = null;
                smartWand.DeviceName = null;
                smartWand.DeviceType = null;
                try
                {
                    _dbContext.SaveChanges();
                    return true;
                }
                catch (Exception)
                {
                    throw;
                }
            }
            else
            {
                throw new Exception("Smart Wand not found");
            }
        }

        public void DeleteClientSiteSmartWand(int id)
        {
            var deleteClientSiteSmartWand = _dbContext.ClientSiteSmartWands.SingleOrDefault(x => x.Id == id);
            if (deleteClientSiteSmartWand != null)
            {
                // Mark as deleted instead of removing it from the database
                deleteClientSiteSmartWand.IsDeleted = true;
            }
            else
            {
                throw new ArgumentException($"No Smart Wand found with ID: {id}");
            }

            _dbContext.SaveChanges();
        }

        public List<ClientSiteSmartWand> GetClientSiteAllSmartWands(int[] clientSiteIds)
        {
            return _dbContext.ClientSiteSmartWands
                .Where(x => clientSiteIds.Contains(x.ClientSiteId) && x.ClientSite.IsActive == true && x.IsDeleted == false)
                .Include(x => x.ClientSite)
                .ToList();
        }

        public List<ClientSiteRadioChecksActivityStatus_History> GetClientSiteAllSmartWandsStrikes(int[] clientSiteIds, DateTime fromDate, DateTime toDate)
        {
            toDate = toDate.AddDays(1); // Include the entire 'toDate' day

            return _dbContext.ClientSiteRadioChecksActivityStatus_History
                .Where(x => x.ClientSiteId.HasValue &&
                            clientSiteIds.Contains(x.ClientSiteId.Value) &&
                            x.ActivityType == "SW" &&
                            x.SwNotes != null &&
                            x.EventDateTime.Date >= fromDate.Date &&
                            x.EventDateTime.Date < toDate.Date)
                .Include(x => x.ClientSite)
                .OrderBy(x => x.EventDateTime)
                .ToList();
        }


        public List<ClientSitePatrolCar> GetClientSitePatrolCars(int clientSiteId)
        {
            return _dbContext.ClientSitePatrolCars
                .Where(x => x.ClientSiteId == clientSiteId && x.ClientSite.IsActive == true)
                .Include(x => x.ClientSite)
                .OrderBy(x=> x.Model)
                .ToList();
        }

        public void SaveClientSitePatrolCar(ClientSitePatrolCar clientSitePatrolCar)
        {
            if (clientSitePatrolCar.Id == -1)
            {
                clientSitePatrolCar.Id = 0;
                _dbContext.ClientSitePatrolCars.Add(clientSitePatrolCar);
            }
            else
            {
                var clientSitePatrolCarToUpdate = _dbContext.ClientSitePatrolCars.SingleOrDefault(x => x.Id == clientSitePatrolCar.Id);
                if (clientSitePatrolCarToUpdate != null)
                {
                    clientSitePatrolCarToUpdate.Model = clientSitePatrolCar.Model;
                    clientSitePatrolCarToUpdate.Rego = clientSitePatrolCar.Rego;
                }
            }
            _dbContext.SaveChanges();
        }

        public void DeleteClientSitePatrolCar(int id)
        {
            var clientSitePatrolCarToDelete = _dbContext.ClientSitePatrolCars.SingleOrDefault(x => x.Id == id);
            if (clientSitePatrolCarToDelete != null)
            {
                _dbContext.ClientSitePatrolCars.Remove(clientSitePatrolCarToDelete);
                _dbContext.SaveChanges();
            }
        }
        public List<ClientSiteSmartWandTags> GetClientSiteSmartWandTags()
        {
            var smartwandtags = _dbContext.ClientSiteSmartWandTags
                .Where(x => x.ClientSite.IsActive == true && x.IsDeleted == false)
                .Include(x => x.SmartWandTagsType)
                .Include(x => x.ClientSite.ClientType)
                .ToList();
            foreach (var item in smartwandtags)
            {
                item.TagsType = item.SmartWandTagsType.value;
            }
            return smartwandtags;
        }
        public List<SmartWandTagsType> GetSmartWandTagsType()
        {
            var smartwandtags = _configDataProvider.GetSmartWandTagsType();
            return smartwandtags;
        }

        public void SaveSmartWandTagLog(ClientSiteSmartWandTagsHitLog log)
        {
            if (log == null)
                throw new ArgumentNullException();

            _dbContext.ClientSiteSmartWandTagsHitLogs.Add(log);
            _dbContext.SaveChanges();
        }

        public void SaveClientSiteSmartWandTags(ClientSiteSmartWandTags clientSiteSmartWandTag)
        {
            if (clientSiteSmartWandTag == null)
                throw new ArgumentNullException();

            if (clientSiteSmartWandTag.TagsType == null)
                throw new ArgumentNullException("TagsType cannot be null. Please select a tag type.");

            clientSiteSmartWandTag.TagsTypeId = _dbContext.SmartWandTagsType.Where(x => x.value == clientSiteSmartWandTag.TagsType).FirstOrDefault().Id;
            var _existingTagUID = _dbContext.ClientSiteSmartWandTags.Where(x => x.UId == clientSiteSmartWandTag.UId
                && x.ClientSite.IsActive == true && x.IsDeleted == false).ToList();

            if (clientSiteSmartWandTag.Id <= 0)
            {
                clientSiteSmartWandTag.Id = 0;

                if (_existingTagUID.Any() || _existingTagUID.Count > 0)
                {
                    throw new ArgumentException($"Tag with UID: {clientSiteSmartWandTag.UId} already exists.");
                }
                _dbContext.ClientSiteSmartWandTags.Add(clientSiteSmartWandTag);
            }
            else
            {
                bool isTagExists = _dbContext.ClientSiteSmartWandTags.Any(x => x.UId == clientSiteSmartWandTag.UId
                        && x.Id != clientSiteSmartWandTag.Id && x.ClientSite.IsActive == true && x.IsDeleted == false);
                if (isTagExists)
                {
                    throw new ArgumentException($"Tag with UID: {clientSiteSmartWandTag.UId} already exists.");
                }
                var clientSiteSmartWandTagToUpdate = _dbContext.ClientSiteSmartWandTags.SingleOrDefault(x => x.Id == clientSiteSmartWandTag.Id);
                if (clientSiteSmartWandTagToUpdate != null)
                {
                    clientSiteSmartWandTagToUpdate.UId = clientSiteSmartWandTag.UId;
                    clientSiteSmartWandTagToUpdate.ClientSiteId = clientSiteSmartWandTag.ClientSiteId;
                    clientSiteSmartWandTagToUpdate.TagsTypeId = clientSiteSmartWandTag.TagsTypeId;
                    clientSiteSmartWandTagToUpdate.LabelDescription = clientSiteSmartWandTag.LabelDescription;
                    clientSiteSmartWandTagToUpdate.IsDeleted = false;
                    clientSiteSmartWandTagToUpdate.FqBypass = clientSiteSmartWandTag.FqBypass;

                }
            }
            _dbContext.SaveChanges();
        }

        public void DeleteClientSiteSmartWandTags(int id)
        {
            var deleteClientSiteSmartWandTags = _dbContext.ClientSiteSmartWandTags.SingleOrDefault(x => x.Id == id);
            if (deleteClientSiteSmartWandTags != null)
                deleteClientSiteSmartWandTags.IsDeleted = true;
            //_dbContext.ClientSiteSmartWandTags.Remove(deleteClientSiteSmartWandTags);

            _dbContext.SaveChanges();
        }

        public List<ClientSiteSmartWandTags> GetClientSiteWandTagsForClientSites(int[] clientSiteIds)
        {
            return _dbContext.ClientSiteSmartWandTags
                .Where(x => x.ClientSite.IsActive && !x.IsDeleted && clientSiteIds.Contains(x.ClientSiteId))
                .Include(x => x.SmartWandTagsType)
                .Include(x => x.ClientSite)
                .AsEnumerable() // switch to in-memory to allow property set
                .Select(x =>
                {
                    x.TagsType = x.SmartWandTagsType.value;
                    return x;
                })
                .ToList();
        }

        public List<ClientSiteSmartWandTags> GetClientSiteWandTagsForClientSitesFromLogs(int[] clientSiteIds)
        {
            var logs = _dbContext.ClientSiteSmartWandTagsHitLogs
                .Where(x => clientSiteIds.Contains(x.LoggedInClientSiteId) ||
                            (x.TagLinkedClientSiteId.HasValue && clientSiteIds.Contains(x.TagLinkedClientSiteId.Value)))
                .Include(x => x.SmartWandTagsType)
                .Include(x => x.LoggedInClientSite)
                .Include(x => x.LinkedClientSite)
                .ToList();


            var tagList = logs.Select(x => new ClientSiteSmartWandTags
            {
                // Use TagLinkedClientSiteId if available, otherwise fall back to LoggedInClientSiteId
                ClientSiteId = x.TagLinkedClientSiteId ?? x.LoggedInClientSiteId,
                UId = x.TagUId,
                TagsTypeId = x.TagsTypeId ?? 0, // or handle null case as needed
                LabelDescription = x.LabelDescription,
                SmartWandTagsType = x.SmartWandTagsType,
                TagsType = x.SmartWandTagsType?.value,
                ClientSite = x.TagLinkedClientSiteId.HasValue ? x.LinkedClientSite : x.LoggedInClientSite,
                IsDeleted = false // assuming all from logs are "not deleted"
            })
            .GroupBy(x => new { x.ClientSiteId, x.UId }) // Optional: remove duplicates
            .Select(g => g.First()) // Keep one per UID per site
            .ToList();

            return tagList;
        }

        public List<ClientSiteSmartWandTagsHitLog> GetClientSiteSmartWandTagsHitLogs(int[] clientSiteIds, DateTime fromDate, DateTime toDate)
        {
            toDate = toDate.AddDays(1); // Include the entire 'toDate' day

            // Step 1: Get UTC offsets per client site
            var utcOffsets = _dbContext.ClientSiteKpiSettings
                .Where(x => clientSiteIds.Contains(x.ClientSiteId))
                .Select(x => new
                {
                    x.ClientSiteId,
                    _siteUTC = x.UTC.Replace("+", "") ?? "+10:00" // Default to +10:00 if null
                })
                .ToList();

            // Prepare a list to accumulate matching logs
            var matchingLogs = new List<ClientSiteSmartWandTagsHitLog>();

            foreach (var site in utcOffsets)
            {
                // Step 2: Parse UTC offset like "+05:30" or "-04:00"
                if (!TimeSpan.TryParse(site._siteUTC.Replace("+", ""), out TimeSpan offset))
                {
                    // Default offset if parsing fails (you can customize this)
                    offset = TimeSpan.Zero;
                }

                // Step 3: Convert the local from/to to UTC for this site
                var fromUtc = fromDate - offset;
                var toUtc = toDate - offset;

                // Step 4: Get logs for this client site in the adjusted range
                var logs = _dbContext.ClientSiteSmartWandTagsHitLogs
                    .Where(x =>
                        (x.LoggedInClientSiteId == site.ClientSiteId ||
                         (x.TagLinkedClientSiteId.HasValue && x.TagLinkedClientSiteId.Value == site.ClientSiteId)) &&
                        x.HitUtcDateTime >= fromUtc &&
                        x.HitUtcDateTime <= toUtc)
                    .AsNoTracking()
                    .Include(x => x.SmartWandTagsType)
                    .Include(x => x.LoggedInClientSite)
                    .Include(x => x.LinkedClientSite)
                    .Include(x => x.LoggedInGuard)
                    .Include(x => x.LoggedInUser)
                    .ToList();

                var logsWithLocal = logs.Select(log => new ClientSiteSmartWandTagsHitLog
                {
                    Id = log.Id,
                    LoggedInClientSiteId = log.LoggedInClientSiteId,
                    LoggedInUserId = log.LoggedInUserId,
                    LoggedInGuardId = log.LoggedInGuardId,
                    TagUId = log.TagUId,
                    TagsTypeId = log.TagsTypeId,
                    LabelDescription = log.LabelDescription,
                    TagLinkedClientSiteId = log.TagLinkedClientSiteId,
                    HitUtcDateTime = log.HitUtcDateTime,
                    HitLocalDateTime = log.HitUtcDateTime + offset, // Add local time conversion
                    LoggedInClientSite = log.LoggedInClientSite,
                    LinkedClientSite = log.LinkedClientSite,
                    SmartWandTagsType = log.SmartWandTagsType,
                    SmartWandNameId = log.SmartWandNameId,
                    SmartWandId = log.SmartWandId,
                    LoggedInGuard = log.LoggedInGuard,
                    LoggedInUser = log.LoggedInUser,
                    GPScoordinates = log.GPScoordinates
                }).Where(l => l.HitLocalDateTime.Date >= fromDate.Date && l.HitLocalDateTime.Date < toDate.Date).ToList();

                matchingLogs.AddRange(logsWithLocal);
            }

            return matchingLogs;
        }

        public ClientSiteSmartWandTagsHitLog GetLastScannedTagDateTime(int siteId,string tagUid)
        {
            return _dbContext.ClientSiteSmartWandTagsHitLogs
                .Where(x => x.TagUId == tagUid && x.LoggedInClientSiteId == siteId)
                .OrderByDescending(x => x.HitUtcDateTime)
                .Take(1)
                .SingleOrDefault();
        }
        public List<SiteEquipmentsDetails> GetClientSiteEquipments()
        {
            var siteEquipments = _dbContext.SiteEquipmentsDetails
                .Where(x => x.ClientSite.IsActive == true && x.IsDeleted == false)
                .Include(x => x.KPITelematicsField)
                .Include(x => x.ClientSite.ClientType)
                .ToList();
            foreach (var item in siteEquipments)
            {
                item.Equipment = item.KPITelematicsField.Name;
            }
            return siteEquipments;
        }
        public void SaveClientSiteEquipments(SiteEquipmentsDetails siteEquipmentsDetails)
        {
            if (siteEquipmentsDetails == null)
                throw new ArgumentNullException();

          
            if (siteEquipmentsDetails.Id == 0)
            {
               

                
                _dbContext.SiteEquipmentsDetails.Add(siteEquipmentsDetails);
            }
            else
            {
               
                var clientSiteEquipmentTagToUpdate = _dbContext.SiteEquipmentsDetails.SingleOrDefault(x => x.Id == siteEquipmentsDetails.Id);
                if (clientSiteEquipmentTagToUpdate != null)
                {
                    clientSiteEquipmentTagToUpdate.Brand = siteEquipmentsDetails.Brand;
                    clientSiteEquipmentTagToUpdate.ClientSiteId = siteEquipmentsDetails.ClientSiteId;
                    clientSiteEquipmentTagToUpdate.EquipmentId = siteEquipmentsDetails.EquipmentId;
                    clientSiteEquipmentTagToUpdate.SerialNo = siteEquipmentsDetails.SerialNo;
                    clientSiteEquipmentTagToUpdate.IsDeleted = false;

                }
            }
            _dbContext.SaveChanges();
        }
        public void DeleteClientSiteEquipments(int id)
        {
            var deleteClientSiteEquipments = _dbContext.SiteEquipmentsDetails.SingleOrDefault(x => x.Id == id);
            if (deleteClientSiteEquipments != null)
                deleteClientSiteEquipments.IsDeleted = true;
            //_dbContext.ClientSiteSmartWandTags.Remove(deleteClientSiteSmartWandTags);

            _dbContext.SaveChanges();
        }

        public bool SaveOfflineSmartWandTagHitDataRecordError(ClientSiteSmartWandTagsHitLogCacheOfflineNotSynced _offlineRecordsNotSynced)
        {
            try
            {
                _dbContext.ClientSiteSmartWandTagsHitLogCacheOfflineNotSynced.Add(_offlineRecordsNotSynced);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        public List<ClientSiteSmartWandTags> GetAllClientSitesSmartwandTags()
        {
                       var smartwandtags = _dbContext.ClientSiteSmartWandTags
                .Where(x => x.ClientSite.IsActive == true && x.IsDeleted == false)
                .Include(x => x.SmartWandTagsType)
                .Include(x => x.ClientSite.ClientType)
                .Include(x => x.ClientSite)
                .ToList();
            foreach (var item in smartwandtags)
            {
                item.TagsType = item.SmartWandTagsType.value;
            }
            return smartwandtags;
        }

        public List<ClientSiteSmartWandTags> GetAllSmartwandTags()
        {
            var smartwandtags = _dbContext.ClientSiteSmartWandTags
             .Include(x => x.SmartWandTagsType)
             .ToList();
            foreach (var item in smartwandtags)
            {
                item.TagsType = item.SmartWandTagsType.value;
            }
            return smartwandtags;
        }

        public List<IncidentReportPosition> GetPatrolCars()
        {
            var PatrolCars = _dbContext.IncidentReportPositions.Where(x => x.IsPatrolCar == true).ToList();
            return PatrolCars;
        }

        public List<IncidentReportPosition> GetPatrolCarsForSite(int[] clientsiteid)
        {            
            var PatrolCarIds = _dbContext.ClientSiteSmartWands
               .Where(x => clientsiteid.Contains(x.ClientSiteId) && x.ClientSite.IsActive == true && x.IsDeleted == false && x.PatrolCarId != null)
               .Include(x => x.ClientSite)
               .Select(x => x.PatrolCarId)
               .Distinct()
               .ToArray();

            return _dbContext.IncidentReportPositions.Where(x => PatrolCarIds.Contains(x.Id)).ToList();

        }
    }
}
