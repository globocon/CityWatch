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
        void SaveGuardLog(GuardLog guardLog);
        void DeleteGuardLog(int id);
        List<KeyVehicleLog> GetKeyVehicleLogs(int logBookId);
        List<KeyVehicleLog> GetKeyVehicleLogs(int[] clientSiteIds, DateTime logFromDate, DateTime logToDate);
        KeyVehicleLog GetKeyVehicleLogById(int id);
        List<KeyVehicleLog> GetKeyVehicleLogByIds(int[] ids);
        void SaveKeyVehicleLog(KeyVehicleLog keyVehicleLog);
        void DeleteKeyVehicleLog(int id);
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
        List<string> GetVehicleRegos();
        List<string> GetVehicleRegos(string regoStart);
        List<string> GetCompanyNames(string companyNameStart);
        List<string> GetSenderNames(string senderNameStart);
        KeyVehicleLogProfile GetKeyVehicleLogProfile(int id);
        List<KeyVehicleLogProfile> GetKeyVehicleLogProfiles();
        List<KeyVehicleLogProfile> GetKeyVehicleLogProfiles(string truckRego);
        List<KeyVehicleLogProfile> GetKeyVehicleLogProfiles(string truckRego, string personName);
        void SaveKeyVehicleLogProfile(KeyVehicleLogProfile keyVehicleLogProfile);
        void DeleteKeyVehicleLogProfile(int id);
        List<KeyVehcileLogField> GetKeyVehicleLogFields(bool includeDeleted = false);
        List<KeyVehcileLogField> GetKeyVehicleLogFieldsByType(KvlFieldType type);
        void SaveKeyVehicleLogField(KeyVehcileLogField field);
        void DeleteKeyVehicleLogField(int id);
        List<KeyVehicleLogAuditHistory> GetAuditHistory(int id);
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
            KeyVehicleLogAuditHistory keyVehicleLogAuditHistory = null;

            if (keyVehicleLog.Id == 0)
            {
                keyVehicleLogAuditHistory = new KeyVehicleLogAuditHistory(keyVehicleLog, null);
                _context.KeyVehicleLogs.Add(keyVehicleLog);
            }
            else
            {
                var keyVehicleLogToUpdate = _context.KeyVehicleLogs.SingleOrDefault(x => x.Id == keyVehicleLog.Id);
                keyVehicleLogAuditHistory = new KeyVehicleLogAuditHistory(keyVehicleLog, keyVehicleLogToUpdate);

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
                keyVehicleLogToUpdate.Wvi = keyVehicleLog.Wvi;
                keyVehicleLogToUpdate.Sender = keyVehicleLog.Sender;
                keyVehicleLogToUpdate.IsSender = keyVehicleLog.IsSender;
            }
            _context.SaveChanges();

            SaveKeyVehicleLogAuditHistory(keyVehicleLog.Id, keyVehicleLogAuditHistory);
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

        public List<string> GetVehicleRegos()
        {
            return _context.KeyVehicleLogProfiles
                .Select(z => z.VehicleRego)
                .Distinct()
                .OrderBy(z => z)
                .ToList();
        }

        public List<string> GetVehicleRegos(string regoStart)
        {
            return _context.KeyVehicleLogProfiles
                .Where(z => !string.IsNullOrEmpty(z.VehicleRego) && z.VehicleRego.Substring(0, regoStart.Length).ToLower() == regoStart.ToLower())
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

        public KeyVehicleLogProfile GetKeyVehicleLogProfile(int id)
        {
            return _context.KeyVehicleLogProfiles.SingleOrDefault(z => z.Id == id);
        }

        public List<KeyVehicleLogProfile> GetKeyVehicleLogProfiles(string truckRego)
        {
            return _context.KeyVehicleLogProfiles
                .Where(z => string.IsNullOrEmpty(truckRego) || string.Equals(z.VehicleRego, truckRego))
                .ToList();
        }

        public List<KeyVehicleLogProfile> GetKeyVehicleLogProfiles(string truckRego, string personName)
        {
            return _context.KeyVehicleLogProfiles
                .Where(z => string.Equals(z.VehicleRego, truckRego) && string.Equals(z.PersonName, personName))
                .ToList();
        }

        public List<KeyVehicleLogProfile> GetKeyVehicleLogProfiles()
        {
            return _context.KeyVehicleLogProfiles.OrderBy(x => x.PersonName).ToList();
        }

        public void SaveKeyVehicleLogProfile(KeyVehicleLogProfile keyVehicleLogProfile)
        {
            if (keyVehicleLogProfile.Id == 0)
            {
                _context.KeyVehicleLogProfiles.Add(keyVehicleLogProfile);
            }
            else
            {
                var vehicleLogProfileToUpdate = _context.KeyVehicleLogProfiles.SingleOrDefault(x => x.Id == keyVehicleLogProfile.Id);
                if (vehicleLogProfileToUpdate != null)
                {
                    vehicleLogProfileToUpdate.PlateId = keyVehicleLogProfile.PlateId;
                    vehicleLogProfileToUpdate.Trailer1Rego = keyVehicleLogProfile.Trailer1Rego;
                    vehicleLogProfileToUpdate.Trailer2Rego = keyVehicleLogProfile.Trailer2Rego;
                    vehicleLogProfileToUpdate.Trailer3Rego = keyVehicleLogProfile.Trailer3Rego;
                    vehicleLogProfileToUpdate.Trailer4Rego = keyVehicleLogProfile.Trailer4Rego;
                    vehicleLogProfileToUpdate.TruckConfig = keyVehicleLogProfile.TruckConfig;
                    vehicleLogProfileToUpdate.TrailerType = keyVehicleLogProfile.TrailerType;
                    vehicleLogProfileToUpdate.MaxWeight = keyVehicleLogProfile.MaxWeight;
                    vehicleLogProfileToUpdate.PersonName = keyVehicleLogProfile.PersonName;
                    vehicleLogProfileToUpdate.CompanyName = keyVehicleLogProfile.CompanyName;
                    vehicleLogProfileToUpdate.PersonType = keyVehicleLogProfile.PersonType;
                    vehicleLogProfileToUpdate.MobileNumber = keyVehicleLogProfile.MobileNumber;
                    vehicleLogProfileToUpdate.Product = keyVehicleLogProfile.Product;
                    vehicleLogProfileToUpdate.EntryReason = keyVehicleLogProfile.EntryReason;
                }
            }
            _context.SaveChanges();
        }

        public void DeleteKeyVehicleLogProfile(int id)
        {
            var vehicleLogProfileToDelete = _context.KeyVehicleLogProfiles.SingleOrDefault(x => x.Id == id);
            if (vehicleLogProfileToDelete != null)
            {
                _context.Remove(vehicleLogProfileToDelete);
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
                .Where(z => z.KeyVehicleLogId == id)
                .Include(z => z.GuardLogin)
                .ThenInclude(z => z.Guard)
                .ToList();
        }

        private void SaveKeyVehicleLogAuditHistory(int keyVehicleLogId, KeyVehicleLogAuditHistory keyVehicleLogAuditHistory)
        {
            if (keyVehicleLogAuditHistory != null)
            {
                keyVehicleLogAuditHistory.KeyVehicleLogId = keyVehicleLogId;
                _context.KeyVehicleLogAuditHistory.Add(keyVehicleLogAuditHistory);
                _context.SaveChanges();
            }
        }
    }
}