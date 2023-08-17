using CityWatch.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Data.Providers
{
    public interface IGuardLogDataProvider
    {
        List<GuardLog> GetGuardLogs(int logBookId, DateTime logDate);
        List<GuardLog> GetGuardLogs(int clientSiteId, DateTime logFromDate, DateTime logToDate, bool excludeSystemLogs);
        GuardLog GetLatestGuardLog(int clientSiteId, int guardId);        
        void SaveGuardLog(GuardLog guardLog);
        void DeleteGuardLog(int id);
        List<KeyVehicleLog> GetOpenKeyVehicleLogsByVehicleRego(string vehicleRego);
        List<KeyVehicleLog> GetKeyVehicleLogs(int logBookId);
        List<KeyVehicleLog> GetKeyVehicleLogs(int[] clientSiteIds, DateTime logFromDate, DateTime logToDate);
        KeyVehicleLog GetKeyVehicleLogById(int id);
        List<KeyVehicleLog> GetKeyVehicleLogByIds(int[] ids);
        void SaveDocketSerialNo(int id, string serialNo);
        void SaveKeyVehicleLog(KeyVehicleLog keyVehicleLog);
        void DeleteKeyVehicleLog(int id);
        void KeyVehicleLogQuickExit(int id);
        List<PatrolCarLog> GetPatrolCarLogs(int logBookId);
        List<PatrolCarLog> GetPatrolCarLogs(int clientSiteId, DateTime logFromDate, DateTime logToDate);
        void SavePatrolCarLog(PatrolCarLog patrolCarLog);
        void SavePatrolCarLogs(IEnumerable<PatrolCarLog> patrolCarLogs);
        List<ClientSiteCustomField> GetClientSiteCustomFields();
        List<ClientSiteCustomField> GetCustomFieldsByClientSiteId(int clientSiteId);
        int SaveClientSiteCustomFields(ClientSiteCustomField clientSiteCustomField);
        void DeleteClientSiteCustomFields(int id);
        List<CustomFieldLog> GetCustomFieldLogs(int logBookId);
        List<CustomFieldLog> GetCustomFieldLogs(int clientSiteId, DateTime logFromDate, DateTime logToDate);
        void SaveCustomFieldLogs(List<CustomFieldLog> customFieldLogs);
        void SaveCustomFieldLog(CustomFieldLog customFieldLog);        
        List<string> GetVehicleRegos(string regoStart = null);
        List<string> GetCompanyNames(string companyNameStart);
        List<string> GetSenderNames(string senderNameStart);
        KeyVehicleLogProfile GetKeyVehicleLogVisitorProfile(string truckRego);
        List<KeyVehicleLogVisitorPersonalDetail> GetKeyVehicleLogVisitorPersonalDetails(string truckRego);
        List<KeyVehicleLogVisitorPersonalDetail> GetKeyVehicleLogVisitorPersonalDetails(string truckRego, string personName);
        KeyVehicleLogVisitorPersonalDetail GetKeyVehicleLogProfileWithPersonalDetails(int id);
        int SaveKeyVehicleLogProfileWithPersonalDetail(KeyVehicleLogVisitorPersonalDetail keyVehicleLogProfile);
        int SaveKeyVehicleLogVisitorPersonalDetail(KeyVehicleLogVisitorPersonalDetail keyVehicleLogVisitorPersonalDetail);
        void SaveKeyVehicleLogProfileNotes(string truckRego, string notes);
        void DeleteKeyVehicleLogPersonalDetails(int id);
        List<KeyVehcileLogField> GetKeyVehicleLogFields(bool includeDeleted = false);
        List<KeyVehcileLogField> GetKeyVehicleLogFieldsByType(KvlFieldType type);
        void SaveKeyVehicleLogField(KeyVehcileLogField field);
        void DeleteKeyVehicleLogField(int id);
        List<KeyVehicleLogAuditHistory> GetAuditHistory(int id);
        void SaveKeyVehicleLogAuditHistory(KeyVehicleLogAuditHistory keyVehicleLogAuditHistory);
        void SaveClientSiteDuress(int clientSiteId, int guardId);
        ClientSiteDuress GetClientSiteDuress(int clientSiteId);
    }

    public class GuardLogDataProvider : IGuardLogDataProvider
    {
        private readonly CityWatchDbContext _context;

        public GuardLogDataProvider(CityWatchDbContext context)
        {
            _context = context;
        }

        public List<GuardLog> GetGuardLogs(int logBookId, DateTime logDate)
        {
            return _context.GuardLogs
               .Where(z => z.ClientSiteLogBookId == logBookId && z.EventDateTime >= logDate && z.EventDateTime < logDate.AddDays(1))
               .Include(z => z.ClientSiteLogBook)
               .Include(z => z.GuardLogin.Guard)
               .OrderBy(z => z.Id)
               .ThenBy(z => z.EventDateTime)
               .ToList();
        }

        public List<GuardLog> GetGuardLogs(int clientSiteId, DateTime logFromDate, DateTime logToDate, bool excludeSystemLogs)
        {
            return _context.GuardLogs
                .Where(z => z.ClientSiteLogBook.ClientSiteId == clientSiteId && z.ClientSiteLogBook.Type == LogBookType.DailyGuardLog
                        && z.ClientSiteLogBook.Date >= logFromDate && z.ClientSiteLogBook.Date <= logToDate &&
                        (!excludeSystemLogs || (excludeSystemLogs && (!z.IsSystemEntry || z.IrEntryType.HasValue))))
                .Include(z => z.GuardLogin.Guard)
                .OrderBy(z => z.Id)
                .ThenBy(z => z.EventDateTime)
                .ToList();
        }

        public GuardLog GetLatestGuardLog(int clientSiteId, int guardId)
        {
            var latestGuardLogin = _context.GuardLogins
                                    .Where(z => z.ClientSiteId == clientSiteId && z.GuardId == guardId)
                                    .OrderByDescending(x => x.Id)
                                    .FirstOrDefault();

            if (latestGuardLogin != null)
            {
                return _context.GuardLogs.Where(z => z.GuardLoginId == latestGuardLogin.Id)
                                            .OrderBy(z => z.Id)
                                            .ThenBy(z => z.EventDateTime)
                                            .LastOrDefault();
            }

            return null;
        }

        public ClientSiteDuress GetClientSiteDuress(int clientSiteId)
        {
            return _context.ClientSiteDuress
                .Where(z => z.ClientSiteId == clientSiteId && z.IsEnabled == true)
                .OrderBy(z => z.Id)
                .LastOrDefault();
        }

        public void SaveClientSiteDuress(int clientSiteId, int guardId)
        {
            _context.ClientSiteDuress.Add(new ClientSiteDuress()
            {
                ClientSiteId = clientSiteId,
                IsEnabled = true,
                EnabledBy = guardId,
                EnabledDate = DateTime.Today
            });
            _context.SaveChanges();
        }

        public void SaveGuardLog(GuardLog guardLog)
        {
            // Insert new guardlog entry
            if (guardLog.Id == 0)
            {
                _context.GuardLogs.Add(new GuardLog()
                {
                    ClientSiteLogBookId = guardLog.ClientSiteLogBookId,
                    EventDateTime = guardLog.EventDateTime,
                    Notes = guardLog.Notes,
                    GuardLoginId = guardLog.GuardLoginId,
                    IsSystemEntry = guardLog.IsSystemEntry,
                    IrEntryType = guardLog.IrEntryType
                });
            }
            else
            {
                var guardLogToUpdate = _context.GuardLogs.SingleOrDefault(x => x.Id == guardLog.Id);
                if (guardLogToUpdate == null)
                    throw new InvalidOperationException();

                guardLogToUpdate.Notes = guardLog.Notes;
            }
            _context.SaveChanges();
        }

        public void DeleteGuardLog(int id)
        {
            var guardLogToDelete = _context.GuardLogs.SingleOrDefault(x => x.Id == id);
            if (guardLogToDelete == null)
                throw new InvalidOperationException();

            _context.Remove(guardLogToDelete);
            _context.SaveChanges();
        }

        public List<KeyVehicleLog> GetOpenKeyVehicleLogsByVehicleRego(string vehicleRego)
        {
            var results = _context.KeyVehicleLogs.Where(x => x.VehicleRego == vehicleRego && !x.ExitTime.HasValue && x.EntryTime >= DateTime.Today);
                
            results.Include(x => x.ClientSiteLogBook)
                .ThenInclude(x => x.ClientSite)
                .Load();

            return results.ToList();
        }

        public List<KeyVehicleLog> GetKeyVehicleLogs(int logBookId)
        {
            var results = _context.KeyVehicleLogs.Where(z => z.ClientSiteLogBookId == logBookId);

            results.Include(x => x.ClientSiteLogBook)
                .Include(x => x.GuardLogin)
                .Include(x => x.ClientSiteLocation)
                .Include(x => x.ClientSitePoc)
                .Load();

            return results.OrderBy(z => z.EntryTime).ToList();
        }

        public List<KeyVehicleLog> GetKeyVehicleLogs(int[] clientSiteIds, DateTime logFromDate, DateTime logToDate)
        {
            var results = _context.KeyVehicleLogs
               .Where(z => clientSiteIds.Contains(z.ClientSiteLogBook.ClientSiteId) && z.ClientSiteLogBook.Type == LogBookType.VehicleAndKeyLog
                            && z.EntryTime >= logFromDate && z.EntryTime < logToDate.AddDays(1))
               .Include(z => z.GuardLogin.Guard)
               .Include(x => x.ClientSiteLocation)
               .Include(x => x.ClientSitePoc);

            results.Include(x => x.ClientSiteLogBook)
               .ThenInclude(z => z.ClientSite)
               .Load();

            return results.OrderBy(z => z.EntryTime).ToList();
        }

        public KeyVehicleLog GetKeyVehicleLogById(int id)
        {
            return _context.KeyVehicleLogs
                .Include(z => z.GuardLogin.Guard)
                .Include(z => z.ClientSiteLogBook)
                .ThenInclude(z => z.ClientSite)
                .Include(z => z.ClientSitePoc)
                .Include(z => z.ClientSiteLocation)
                .SingleOrDefault(z => z.Id == id);
        }

        public List<KeyVehicleLog> GetKeyVehicleLogByIds(int[] ids)
        {
            return _context.KeyVehicleLogs.Where(z => ids.Contains(z.Id))
                .Include(z => z.ClientSiteLogBook)
                .ThenInclude(z => z.ClientSite)
                .ToList();
        }

        public void SaveKeyVehicleLog(KeyVehicleLog keyVehicleLog)
        {
            if (keyVehicleLog.Id == 0)
            {
                _context.KeyVehicleLogs.Add(keyVehicleLog);
            }
            else
            {
                var keyVehicleLogToUpdate = _context.KeyVehicleLogs.SingleOrDefault(x => x.Id == keyVehicleLog.Id);

                keyVehicleLogToUpdate.InitialCallTime = keyVehicleLog.InitialCallTime;
                keyVehicleLogToUpdate.EntryTime = keyVehicleLog.EntryTime;
                keyVehicleLogToUpdate.SentInTime = keyVehicleLog.SentInTime;
                keyVehicleLogToUpdate.ExitTime = keyVehicleLog.ExitTime;
                keyVehicleLogToUpdate.TimeSlotNo = keyVehicleLog.TimeSlotNo;
                keyVehicleLogToUpdate.PersonType = keyVehicleLog.PersonType;
                keyVehicleLogToUpdate.VehicleRego = keyVehicleLog.VehicleRego;
                keyVehicleLogToUpdate.CompanyName = keyVehicleLog.CompanyName;
                keyVehicleLogToUpdate.Trailer1Rego = keyVehicleLog.Trailer1Rego;
                keyVehicleLogToUpdate.Trailer2Rego = keyVehicleLog.Trailer2Rego;
                keyVehicleLogToUpdate.Trailer3Rego = keyVehicleLog.Trailer3Rego;
                keyVehicleLogToUpdate.Trailer4Rego = keyVehicleLog.Trailer4Rego;
                keyVehicleLogToUpdate.PlateId = keyVehicleLog.PlateId;
                keyVehicleLogToUpdate.TruckConfig = keyVehicleLog.TruckConfig;
                keyVehicleLogToUpdate.KeyNo = keyVehicleLog.KeyNo;
                keyVehicleLogToUpdate.PersonName = keyVehicleLog.PersonName;
                keyVehicleLogToUpdate.MobileNumber = keyVehicleLog.MobileNumber;
                keyVehicleLogToUpdate.TrailerType = keyVehicleLog.TrailerType;
                keyVehicleLogToUpdate.InWeight = keyVehicleLog.InWeight;
                keyVehicleLogToUpdate.OutWeight = keyVehicleLog.OutWeight;
                keyVehicleLogToUpdate.TareWeight = keyVehicleLog.TareWeight;
                keyVehicleLogToUpdate.MaxWeight = keyVehicleLog.MaxWeight;
                keyVehicleLogToUpdate.Notes = keyVehicleLog.Notes;
                keyVehicleLogToUpdate.Product = keyVehicleLog.Product;
                keyVehicleLogToUpdate.EntryReason = keyVehicleLog.EntryReason;
                keyVehicleLogToUpdate.ClientSitePocId = keyVehicleLog.ClientSitePocId;
                keyVehicleLogToUpdate.ClientSiteLocationId = keyVehicleLog.ClientSiteLocationId;
                keyVehicleLogToUpdate.MoistureDeduction = keyVehicleLog.MoistureDeduction;
                keyVehicleLogToUpdate.RubbishDeduction = keyVehicleLog.RubbishDeduction;
                keyVehicleLogToUpdate.DeductionPercentage = keyVehicleLog.DeductionPercentage;
                keyVehicleLogToUpdate.IsTimeSlotNo = keyVehicleLog.IsTimeSlotNo;
                keyVehicleLogToUpdate.Reels = keyVehicleLog.Reels;
                keyVehicleLogToUpdate.CustomerRef = keyVehicleLog.CustomerRef;
                keyVehicleLogToUpdate.Vwi = keyVehicleLog.Vwi;
                keyVehicleLogToUpdate.Sender = keyVehicleLog.Sender;
                keyVehicleLogToUpdate.IsSender = keyVehicleLog.IsSender;
            }
            _context.SaveChanges();
        }

        public void SaveDocketSerialNo(int id, string serialNo)
        {
            var keyVehicleLog = _context.KeyVehicleLogs.SingleOrDefault(i => i.Id == id);
            if (keyVehicleLog != null)
            {
                keyVehicleLog.DocketSerialNo = serialNo;
                _context.SaveChanges();
            }
        }

        public void DeleteKeyVehicleLog(int id)
        {
            var keyVehicleLogToDelete = _context.KeyVehicleLogs.SingleOrDefault(i => i.Id == id);
            if (keyVehicleLogToDelete != null)
            {
                _context.Remove(keyVehicleLogToDelete);
                _context.SaveChanges();
            }
        }

        public void KeyVehicleLogQuickExit(int id)
        {
            var keyVehicleLog = _context.KeyVehicleLogs.SingleOrDefault(x => x.Id == id);
            if (keyVehicleLog != null)
            {
                keyVehicleLog.ExitTime = DateTime.Now;
                _context.SaveChanges();
            }
        }

        public List<PatrolCarLog> GetPatrolCarLogs(int logBookId)
        {
            var result = _context.PatrolCarLogs
                .Where(z => z.ClientSiteLogBookId == logBookId)
                .Include(x => x.ClientSiteLogBook)
                .Include(x => x.ClientSitePatrolCar)
                .ToList();

            return result;
        }

        public List<PatrolCarLog> GetPatrolCarLogs(int clientSiteId, DateTime logFromDate, DateTime logToDate)
        {
            var result = _context.PatrolCarLogs
                .Where(z => z.ClientSiteLogBook.ClientSiteId == clientSiteId && z.ClientSiteLogBook.Date >= logFromDate && z.ClientSiteLogBook.Date <= logToDate)
                .Include(x => x.ClientSiteLogBook)
                .Include(x => x.ClientSitePatrolCar)
                .ToList();

            return result;
        }

        public void SavePatrolCarLog(PatrolCarLog patrolCarLog)
        {
            if (patrolCarLog.Id == 0)
            {
                _context.PatrolCarLogs.Add(patrolCarLog);
            }
            else
            {
                var patrolCarDetailsToUpdate = _context.PatrolCarLogs.SingleOrDefault(x => x.Id == patrolCarLog.Id);
                patrolCarDetailsToUpdate.Mileage = patrolCarLog.Mileage;
            }
            _context.SaveChanges();
        }

        public void SavePatrolCarLogs(IEnumerable<PatrolCarLog> patrolCarLogs)
        {
            var patrolCarLogsToInsert = patrolCarLogs.Where(z => z.Id == 0);
            if (patrolCarLogsToInsert.Any())
            {
                _context.PatrolCarLogs.AddRange(patrolCarLogsToInsert);
                _context.SaveChanges();
            }
        }

        public List<ClientSiteCustomField> GetClientSiteCustomFields()
        {
            return _context.ClientSiteCustomFields
                .Include(x => x.ClientSite)
                .ToList();
        }

        public List<ClientSiteCustomField> GetCustomFieldsByClientSiteId(int clientSiteId)
        {
            return _context.ClientSiteCustomFields.Where(z => z.ClientSiteId == clientSiteId).ToList();
        }

        public int SaveClientSiteCustomFields(ClientSiteCustomField clientSiteCustomField)
        {
            if (clientSiteCustomField.Id == 0)
            {
                _context.ClientSiteCustomFields.Add(clientSiteCustomField);
            }
            else
            {
                var clientSiteCustomFieldToUpdate = _context.ClientSiteCustomFields.SingleOrDefault(x => x.Id == clientSiteCustomField.Id);
                if (clientSiteCustomFieldToUpdate != null)
                {
                    clientSiteCustomFieldToUpdate.Name = clientSiteCustomField.Name;
                    clientSiteCustomFieldToUpdate.TimeSlot = clientSiteCustomField.TimeSlot;
                }
            }
            _context.SaveChanges();
            return clientSiteCustomField.Id;
        }

        public void DeleteClientSiteCustomFields(int id)
        {
            var clientSiteCustomFieldToDelete = _context.ClientSiteCustomFields.SingleOrDefault(i => i.Id == id);
            if (clientSiteCustomFieldToDelete != null)
            {
                _context.Remove(clientSiteCustomFieldToDelete);
                _context.SaveChanges();
            }
        }

        public List<CustomFieldLog> GetCustomFieldLogs(int logBookId)
        {
            return _context.CustomFieldLogs
                .Include(z => z.ClientSiteCustomField)
                .Include(z => z.ClientSiteLogBook)
                .Where(x => x.ClientSiteLogBookId == logBookId)
                .ToList();
        }

        public List<CustomFieldLog> GetCustomFieldLogs(int clientSiteId, DateTime logFromDate, DateTime logToDate)
        {
            return _context.CustomFieldLogs
                .Where(z => z.ClientSiteLogBook.ClientSiteId == clientSiteId && z.ClientSiteLogBook.Date >= logFromDate && z.ClientSiteLogBook.Date <= logToDate)
                .Include(x => x.ClientSiteLogBook)
                .Include(x => x.ClientSiteCustomField)
                .ToList();
        }

        public void SaveCustomFieldLogs(List<CustomFieldLog> customFieldLogs)
        {
            foreach (var customFieldLog in customFieldLogs)
            {
                SaveCustomFieldLog(customFieldLog);
            }
        }

        public void SaveCustomFieldLog(CustomFieldLog customFieldLog)
        {
            if (customFieldLog.Id == 0)
            {
                _context.CustomFieldLogs.Add(customFieldLog);
            }
            else
            {
                var customFieldLogToUpdate = _context.CustomFieldLogs.SingleOrDefault(x => x.Id == customFieldLog.Id);
                if (customFieldLogToUpdate != null)
                {
                    customFieldLogToUpdate.DayValue = customFieldLog.DayValue;
                }
            }
            _context.SaveChanges();
        }

        public List<string> GetVehicleRegos(string regoStart = null)
        {
            return _context.KeyVehicleLogVisitorProfiles
                .Where(z => string.IsNullOrEmpty(regoStart) || 
                            (!string.IsNullOrEmpty(z.VehicleRego) && 
                                z.VehicleRego.Substring(0, regoStart.Length).ToLower() == regoStart.ToLower()))
                .Select(z => z.VehicleRego)
                .Distinct()
                .OrderBy(z => z)
                .ToList();
        }

        public List<string> GetCompanyNames(string companyNameStart)
        {
            return _context.KeyVehicleLogs
                .Where(x => !string.IsNullOrEmpty(x.CompanyName) && x.CompanyName.Substring(0, companyNameStart.Length).ToLower() == companyNameStart.ToLower())
                .Select(x => x.CompanyName)
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }

        public List<string> GetSenderNames(string senderNameStart)
        {
            return _context.KeyVehicleLogs
                .Where(x => !string.IsNullOrEmpty(x.Sender) && x.Sender.Substring(0, senderNameStart.Length).ToLower() == senderNameStart.ToLower())
                .Select(x => x.Sender)
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }

        public KeyVehicleLogVisitorPersonalDetail GetKeyVehicleLogProfileWithPersonalDetails(int id)
        {
            return _context.KeyVehicleLogVisitorPersonalDetails
                .Include(z => z.KeyVehicleLogProfile)
                .SingleOrDefault(z => z.Id == id);
        }

        public KeyVehicleLogProfile GetKeyVehicleLogVisitorProfile(string truckRego)
        {
            return _context.KeyVehicleLogVisitorProfiles
                            .SingleOrDefault(z => z.VehicleRego == truckRego);
        }

        public List<KeyVehicleLogVisitorPersonalDetail> GetKeyVehicleLogVisitorPersonalDetails(string truckRego)
        {
            return _context.KeyVehicleLogVisitorPersonalDetails
                .Include(z => z.KeyVehicleLogProfile)
                .Where(z => string.IsNullOrEmpty(truckRego) || string.Equals(z.KeyVehicleLogProfile.VehicleRego, truckRego))
                .ToList();
        }

        public List<KeyVehicleLogVisitorPersonalDetail> GetKeyVehicleLogVisitorPersonalDetails(string truckRego, string personName)
        {
            return _context.KeyVehicleLogVisitorPersonalDetails
                .Where(z => string.Equals(z.KeyVehicleLogProfile.VehicleRego, truckRego) && string.Equals(z.PersonName, personName))
                .ToList();
        }

        public int SaveKeyVehicleLogProfileWithPersonalDetail(KeyVehicleLogVisitorPersonalDetail kvlVisitorPersonalDetail)
        {
            kvlVisitorPersonalDetail.ProfileId = SaveKeyVehicleLogProfile(kvlVisitorPersonalDetail.KeyVehicleLogProfile);
            SaveKeyVehicleLogVisitorPersonalDetail(kvlVisitorPersonalDetail);
            return kvlVisitorPersonalDetail.ProfileId;
        }

        public int SaveKeyVehicleLogVisitorPersonalDetail(KeyVehicleLogVisitorPersonalDetail keyVehicleLogVisitorPersonalDetail)
        {
            var kvlPersonalDetailsToDb = _context.KeyVehicleLogVisitorPersonalDetails
                                            .SingleOrDefault(z => z.Id == keyVehicleLogVisitorPersonalDetail.Id) ??
                                            new KeyVehicleLogVisitorPersonalDetail();

            kvlPersonalDetailsToDb.ProfileId = keyVehicleLogVisitorPersonalDetail.ProfileId;
            kvlPersonalDetailsToDb.CompanyName = keyVehicleLogVisitorPersonalDetail.CompanyName;
            kvlPersonalDetailsToDb.PersonName = keyVehicleLogVisitorPersonalDetail.PersonName;
            kvlPersonalDetailsToDb.PersonType = keyVehicleLogVisitorPersonalDetail.PersonType;

            if (kvlPersonalDetailsToDb.Id == 0)
            {
                _context.KeyVehicleLogVisitorPersonalDetails.Add(kvlPersonalDetailsToDb);
            }
            _context.SaveChanges();

            return kvlPersonalDetailsToDb.Id;
        }

        public void SaveKeyVehicleLogProfileNotes(string truckRego, string notes)
        {
            var profileDetailsInDb = _context.KeyVehicleLogVisitorProfiles.SingleOrDefault(z => z.VehicleRego == truckRego);
            if (profileDetailsInDb != null && !string.Equals(profileDetailsInDb.Notes, notes))
            {
                profileDetailsInDb.Notes = notes;
                _context.SaveChanges();
            }
        }

        public void DeleteKeyVehicleLogPersonalDetails(int id)
        {
            var kvlPersonalDetailsToDelete = _context.KeyVehicleLogVisitorPersonalDetails.SingleOrDefault(x => x.Id == id);
            if (kvlPersonalDetailsToDelete != null)
            {
                _context.KeyVehicleLogVisitorPersonalDetails.Remove(kvlPersonalDetailsToDelete);

                var personalDetailsCount = _context.KeyVehicleLogVisitorPersonalDetails.Count(x => x.ProfileId == kvlPersonalDetailsToDelete.ProfileId);
                if (personalDetailsCount == 1)
                {
                    var kvlProfileToDelete = _context.KeyVehicleLogVisitorProfiles.SingleOrDefault(x => x.Id == kvlPersonalDetailsToDelete.ProfileId);
                    if (kvlProfileToDelete != null)
                    {
                        _context.KeyVehicleLogVisitorProfiles.Remove(kvlProfileToDelete);
                    }
                }

                _context.SaveChanges();
            }
        }

        public List<KeyVehcileLogField> GetKeyVehicleLogFields(bool includeDeleted = false)
        {
            return _context.KeyVehcileLogFields
                .Where(x => includeDeleted || !x.IsDeleted)
                .OrderBy(x => x.TypeId)
                .ThenBy(x => x.Name)
                .ToList();
        }

        public List<KeyVehcileLogField> GetKeyVehicleLogFieldsByType(KvlFieldType type)
        {
            return GetKeyVehicleLogFields()
                .Where(x => x.TypeId == type)
                .OrderBy(x => x.Name)
                .ToList();
        }

        public void SaveKeyVehicleLogField(KeyVehcileLogField keyVehcileLogField)
        {
            if (keyVehcileLogField.Id == -1)
            {
                keyVehcileLogField.Id = 0;
                _context.KeyVehcileLogFields.Add(keyVehcileLogField);
            }
            else
            {
                var kvlFieldToUpdate = _context.KeyVehcileLogFields.SingleOrDefault(x => x.Id == keyVehcileLogField.Id);
                if (kvlFieldToUpdate != null)
                {
                    kvlFieldToUpdate.Name = keyVehcileLogField.Name;
                    kvlFieldToUpdate.TypeId = keyVehcileLogField.TypeId;
                    kvlFieldToUpdate.IsDeleted = keyVehcileLogField.IsDeleted;
                }
            }
            _context.SaveChanges();
        }

        public void DeleteKeyVehicleLogField(int id)
        {
            var kvlFieldToDelete = _context.KeyVehcileLogFields.SingleOrDefault(x => x.Id == id);
            if (kvlFieldToDelete != null)
                kvlFieldToDelete.IsDeleted = true;
            _context.SaveChanges();
        }

        public List<KeyVehicleLogAuditHistory> GetAuditHistory(int id)
        {
            return _context.KeyVehicleLogAuditHistory
                .Where(z => z.ProfileId == id)
                .Include(z => z.GuardLogin)
                .ThenInclude(z => z.Guard)
                .ToList();
        }

        public void SaveKeyVehicleLogAuditHistory(KeyVehicleLogAuditHistory keyVehicleLogAuditHistory)
        {
            if (keyVehicleLogAuditHistory != null)
            {
                _context.KeyVehicleLogAuditHistory.Add(keyVehicleLogAuditHistory);
                _context.SaveChanges();
            }
        }

        private int SaveKeyVehicleLogProfile(KeyVehicleLogProfile keyVehicleLogProfile)
        {
            var kvlProfileToDb = _context.KeyVehicleLogVisitorProfiles.SingleOrDefault(z => z.VehicleRego == keyVehicleLogProfile.VehicleRego) ?? new KeyVehicleLogProfile();
            kvlProfileToDb.VehicleRego = keyVehicleLogProfile.VehicleRego;
            kvlProfileToDb.Trailer1Rego = keyVehicleLogProfile.Trailer1Rego;
            kvlProfileToDb.Trailer2Rego = keyVehicleLogProfile.Trailer2Rego;
            kvlProfileToDb.Trailer3Rego = keyVehicleLogProfile.Trailer3Rego;
            kvlProfileToDb.Trailer4Rego = keyVehicleLogProfile.Trailer4Rego;
            kvlProfileToDb.TruckConfig = keyVehicleLogProfile.TruckConfig;
            kvlProfileToDb.TrailerType = keyVehicleLogProfile.TrailerType;
            kvlProfileToDb.MaxWeight = keyVehicleLogProfile.MaxWeight;
            kvlProfileToDb.MobileNumber = keyVehicleLogProfile.MobileNumber;
            kvlProfileToDb.Product = keyVehicleLogProfile.Product;
            kvlProfileToDb.EntryReason = keyVehicleLogProfile.EntryReason;
            kvlProfileToDb.CreatedLogId = keyVehicleLogProfile.CreatedLogId;
            kvlProfileToDb.PlateId = keyVehicleLogProfile.PlateId;
            kvlProfileToDb.Sender = keyVehicleLogProfile.Sender;
            kvlProfileToDb.IsSender = keyVehicleLogProfile.IsSender;
            kvlProfileToDb.Notes = keyVehicleLogProfile.Notes;

            if (kvlProfileToDb.Id == 0)
            {
                _context.KeyVehicleLogVisitorProfiles.Add(kvlProfileToDb);
            }

            _context.SaveChanges();

            return kvlProfileToDb.Id;
        }
    }
}