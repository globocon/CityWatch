using CityWatch.Data.Enums;
using CityWatch.Data.Models;
using iText.Layout.Element;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Globalization;
using System.Linq;
using static Dropbox.Api.TeamLog.EventCategory;
using static Dropbox.Api.TeamLog.TimeUnit;

namespace CityWatch.Data.Providers
{
    public interface IGuardLogDataProvider
    {
        List<GuardLog> GetGuardLogs(int logBookId, DateTime logDate);
        List<GuardLog> GetGuardLogs(int clientSiteId, DateTime logFromDate, DateTime logToDate, bool excludeSystemLogs);
        GuardLog GetLatestGuardLog(int clientSiteId, int guardId);
        void SaveGuardLog(GuardLog guardLog);
        void DeleteGuardLog(int id);
        //logBookId delete for radio checklist-start
        void DeleteClientSiteRadioCheckActivityStatusForLogBookEntry(int id);
        void DeleteClientSiteRadioCheckActivityStatusForKeyVehicleEntry(int id);
        void SignOffClientSiteRadioCheckActivityStatusForLogBookEntry(int GuardId, int ClientSiteId);

        //logBookId delete for radio checklist-end
        List<KeyVehicleLog> GetOpenKeyVehicleLogsByVehicleRego(string vehicleRego);
        List<KeyVehicleLog> GetKeyVehicleLogs(int logBookId);
        List<KeyVehicleLog> GetKeyVehicleLogs(int[] clientSiteIds, DateTime logFromDate, DateTime logToDate);
        List<KeyVehicleLog> GetKeyVehicleLogsWithPOI(int[] clientSiteIds, int[] personOfInterestIds, DateTime logFromDate, DateTime logToDate);
        KeyVehicleLog GetKeyVehicleLogById(int id);
        List<KeyVehicleLog> GetKeyVehicleLogByIds(int[] ids);
        List<KeyVehicleLog> GetPOIAlert(string companyname, string individualname, int individualtype);
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
        List<string> GetVehicleRegosForKVL(string regoStart = null);
        List<string> GetCompanyNames(string companyNameStart);
        List<string> GetSenderNames(string senderNameStart);
        KeyVehicleLogProfile GetKeyVehicleLogVisitorProfile(string truckRego);
        List<KeyVehicleLogVisitorPersonalDetail> GetKeyVehicleLogVisitorPersonalDetails(string truckRego);
        List<KeyVehicleLogVisitorPersonalDetail> GetKeyVehicleLogVisitorPersonalDetailsWithIndividualType(int individualtype);
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
        List<CompanyDetails> GetCompanyDetails();
        //logBookId entry for radio checklist-start
        void SaveRadioChecklistEntry(ClientSiteRadioChecksActivityStatus clientSiteActivity);
        List<ClientSiteRadioChecksActivityStatus> GetClientSiteRadioChecksActivityDetails();
        void DeleteClientSiteRadioChecksActivity(ClientSiteRadioChecksActivityStatus ClientSiteRadioChecksActivityStatus);
        List<RadioCheckListGuardData> GetActiveGuardDetails();
        List<RadioCheckListInActiveGuardData> GetInActiveGuardDetails();
        public Guard GetGuards(int guardId);
        //logBookId entry for radio checklist-end

        //for getting logBook details of the  guard-start

        List<RadioCheckListGuardLoginData> GetActiveGuardlogBookDetails(int clientSiteId, int guardId);
        //for getting logBook details of the  guard-end

        //for getting list of guards not available-start
        List<RadioCheckListNotAvailableGuardData> GetNotAvailableGuardDetails();
        //for getting list of guards not available-end
        //for getting key vehicle log details of the  guard-start

        List<RadioCheckListGuardKeyVehicleData> GetActiveGuardKeyVehicleLogDetails(int clientSiteId, int guardId);
        //for getting  key vehicle log details of the  guard-end

        //for getting incident report details of the  guard-start

        List<RadioCheckListGuardIncidentReportData> GetActiveGuardIncidentReportDetails(int clientSiteId, int guardId);
        //for getting  incident report details of the  guard-end
        void SaveRadioCheckDuress(string UserID);
        public bool IsRadiocheckDuressEnabled(int UserID);
        public int UserIDDuress(int UserID);

        //rc status save Start
        void SaveClientSiteRadioCheck(ClientSiteRadioCheck clientSiteRadioCheck);
        //rc status save end

        int GetClientSiteLogBookId(int clientsiteId, LogBookType type, DateTime date);
        int GetGuardLoginId(int clientsitelogbookId, int guardId, DateTime date);
        List<GuardLog> GetGuardLogsId(int logBookId, DateTime logDate, int guardLoginId, IrEntryType type, string notes);
        void UpdateRadioChecklistEntry(ClientSiteRadioChecksActivityStatus clientSiteActivity);
        List<GuardLogin> GetGuardLogins(int guardLoginId);

        /* new Change by dileep for p4 task 17 start*/
        void UpdateRadioChecklistLogOffEntry(ClientSiteRadioChecksActivityStatus clientSiteActivity);
        void GetGuardManningDetails(DayOfWeek CurrentDay);
        void RemoveTheeRadioChecksActivityWithNotifcationtypeOne(int ClientSiteId);
        public void RemoveClientSiteRadioChecksGreaterthanTwoHours();
        public void SaveClientSiteRadioCheckStatusFromlogBook(ClientSiteRadioCheck clientSiteRadioCheck);
        public bool getIfAnyActivityInbufferTime(int GuardId, int ClientSiteId);
        /* new Change by dileep for p4 task 17 end*/




        //listing clientsites for radio check
        List<ClientSite> GetClientSites(int? Id);
        List<ClientSiteSmartWand> GetClientSiteSmartWands(int? clientSiteId);
        int GetGuardLoginId(int guardId, DateTime date);
        List<GuardLogin> GetGuardLoginsByClientSiteId(int clientsiteId, DateTime date);


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
                .Where(z => z.ClientSiteId == clientSiteId)
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
        public List<KeyVehicleLog> GetKeyVehicleLogsWithPOI(int[] clientSiteIds, int[] personOfInterestIds, DateTime logFromDate, DateTime logToDate)

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
        public List<KeyVehicleLog> GetPOIAlert(string companyname, string individualname, int individualtype)
        {
            //return _context.KeyVehicleLogs.Where(z =>  z.CompanyName==companyname && z.PersonName==individualname && z.PersonType==individualtype && z.IsPOIAlert==true)
            // .Include(z => z.ClientSiteLogBook)
            //    .ThenInclude(z => z.ClientSite)
            //    .ToList();
            return _context.KeyVehicleLogs.Where(z => z.CompanyName == companyname && z.PersonName == individualname && z.PersonType == individualtype && z.PersonOfInterest != 0)
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
                keyVehicleLogToUpdate.PersonOfInterest = keyVehicleLog.PersonOfInterest;
                keyVehicleLogToUpdate.IsBDM = keyVehicleLog.IsBDM;
                if (keyVehicleLog.CRMId != null)
                {
                    keyVehicleLogToUpdate.CRMId = keyVehicleLog.CRMId;
                    keyVehicleLogToUpdate.IndividualTitle = keyVehicleLog.IndividualTitle;
                    keyVehicleLogToUpdate.Gender = keyVehicleLog.Gender;
                    keyVehicleLogToUpdate.CompanyABN = keyVehicleLog.CompanyABN;
                    keyVehicleLogToUpdate.CompanyLandline = keyVehicleLog.CompanyLandline;
                    keyVehicleLogToUpdate.Email = keyVehicleLog.Email;
                    keyVehicleLogToUpdate.Website = keyVehicleLog.Website;
                    keyVehicleLogToUpdate.BDMList = keyVehicleLog.BDMList;

                }
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
        public List<string> GetVehicleRegosForKVL(string regoStart = null)
        {
            return _context.KeyVehicleLogVisitorProfiles
                .Where(z => string.IsNullOrEmpty(regoStart) ||
                            (!string.IsNullOrEmpty(z.VehicleRego) &&
                                z.VehicleRego.Contains(regoStart)))
                .Select(z => z.VehicleRego)
                .Distinct()
                .OrderBy(z => z)
                .ToList();
        }

        public List<string> GetCompanyNames(string companyNameStart)
        {
            return _context.KeyVehicleLogVisitorPersonalDetails
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

        public List<KeyVehicleLogVisitorPersonalDetail> GetKeyVehicleLogVisitorPersonalDetailsWithIndividualType(int individualtype)
        {
            return _context.KeyVehicleLogVisitorPersonalDetails
                .Include(z => z.KeyVehicleLogProfile)
                .Where(z => z.PersonType == individualtype)
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
            kvlPersonalDetailsToDb.PersonOfInterest = keyVehicleLogVisitorPersonalDetail.PersonOfInterest;
            if (keyVehicleLogVisitorPersonalDetail.PersonOfInterest != null)
            {
                string imagepath = "~/images/ziren.png";
                kvlPersonalDetailsToDb.POIImage = keyVehicleLogVisitorPersonalDetail.POIImage;
            }
            kvlPersonalDetailsToDb.IsBDM = keyVehicleLogVisitorPersonalDetail.IsBDM;
            if (keyVehicleLogVisitorPersonalDetail.CRMId != null)
            {
                kvlPersonalDetailsToDb.CRMId = keyVehicleLogVisitorPersonalDetail.CRMId;
                kvlPersonalDetailsToDb.IndividualTitle = keyVehicleLogVisitorPersonalDetail.IndividualTitle;
                kvlPersonalDetailsToDb.Gender = keyVehicleLogVisitorPersonalDetail.Gender;
                kvlPersonalDetailsToDb.CompanyABN = keyVehicleLogVisitorPersonalDetail.CompanyABN;
                kvlPersonalDetailsToDb.CompanyLandline = keyVehicleLogVisitorPersonalDetail.CompanyLandline;
                kvlPersonalDetailsToDb.Email = keyVehicleLogVisitorPersonalDetail.Email;
                kvlPersonalDetailsToDb.Website = keyVehicleLogVisitorPersonalDetail.Website;
                kvlPersonalDetailsToDb.BDMList = keyVehicleLogVisitorPersonalDetail.BDMList;

            }

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
        public List<CompanyDetails> GetCompanyDetails()
        {
            return _context.CompanyDetails.ToList();
        }
        public void SaveRadioChecklistEntry(ClientSiteRadioChecksActivityStatus clientSiteActivity)
        {
            try
            {
                if (clientSiteActivity.Id == 0)
                {

                    _context.ClientSiteRadioChecksActivityStatus.Add(new ClientSiteRadioChecksActivityStatus()
                    {
                        ClientSiteId = clientSiteActivity.ClientSiteId,
                        GuardId = clientSiteActivity.GuardId,
                        LastIRCreatedTime = clientSiteActivity.LastIRCreatedTime,
                        LastKVCreatedTime = clientSiteActivity.LastKVCreatedTime,
                        LastLBCreatedTime = clientSiteActivity.LastLBCreatedTime,
                        GuardLoginTime = clientSiteActivity.GuardLoginTime,
                        GuardLogoutTime = clientSiteActivity.GuardLogoutTime,
                        IRId = clientSiteActivity.IRId,
                        KVId = clientSiteActivity.KVId,
                        LBId = clientSiteActivity.LBId,
                        ActivityType = clientSiteActivity.ActivityType,
                        OnDuty = clientSiteActivity.OnDuty,
                        OffDuty = clientSiteActivity.OffDuty
                    });

                }
                else
                {

                    var clientSiteActivityToUpdate = _context.ClientSiteRadioChecksActivityStatus.SingleOrDefault(x => x.Id == clientSiteActivity.Id);
                    if (clientSiteActivityToUpdate == null)
                        throw new InvalidOperationException();

                    clientSiteActivityToUpdate.ClientSiteId = clientSiteActivity.ClientSiteId;
                    clientSiteActivityToUpdate.GuardId = clientSiteActivity.GuardId;
                    clientSiteActivityToUpdate.LastIRCreatedTime = clientSiteActivity.LastIRCreatedTime;
                    clientSiteActivityToUpdate.LastKVCreatedTime = clientSiteActivity.LastKVCreatedTime;
                    clientSiteActivityToUpdate.LastLBCreatedTime = clientSiteActivity.LastLBCreatedTime;
                    clientSiteActivityToUpdate.GuardLoginTime = clientSiteActivity.GuardLoginTime;
                    clientSiteActivityToUpdate.GuardLogoutTime = clientSiteActivity.GuardLogoutTime;
                    clientSiteActivityToUpdate.IRId = clientSiteActivity.IRId;
                    clientSiteActivityToUpdate.KVId = clientSiteActivity.KVId;
                    clientSiteActivityToUpdate.LBId = clientSiteActivity.LBId;
                    clientSiteActivityToUpdate.ActivityType = clientSiteActivity.ActivityType;
                }

                _context.SaveChanges();
            }
            catch (Exception ex)
            {

            }
        }

        public List<ClientSiteRadioChecksActivityStatus> GetClientSiteRadioChecksActivityDetails()
        {
            return _context.ClientSiteRadioChecksActivityStatus.ToList();
        }
        public List<RadioCheckListGuardData> GetActiveGuardDetails()
        {

            var allvalues = _context.RadioCheckListGuardData.FromSqlRaw($"EXEC sp_GetActiveGuardDetailsForRC").ToList();
            foreach (var item in allvalues)
            {

                item.SiteName = item.SiteName + " <i class=\"fa fa-mobile\" aria-hidden=\"true\"></i> " + string.Join(",", _context.ClientSiteSmartWands.Where(x => x.ClientSiteId == item.ClientSiteId).Select(x => x.PhoneNumber).ToList()) + " <i class=\"fa fa-caret-down\" aria-hidden=\"true\" id=\"btnUpArrow\"></i> ";
                item.Address = " <a id=\"btnActiveGuardsMap\" href=\"https://www.google.com/maps?q=" + item.GPS + "\"target=\"_blank\"><i class=\"fa fa-map-marker\" aria-hidden=\"true\"></i> </a>" + item.Address + " <input type=\"hidden\" class=\"form-control\" value=\"" + item.GPS + "\" id=\"txtGPSActiveguards\" />";
            }
            return allvalues;
        }

        public List<RadioCheckListInActiveGuardData> GetInActiveGuardDetails()
        {

            var allvalues = _context.RadioCheckListInActiveGuardData.FromSqlRaw($"EXEC sp_GetInActiveGuardDetailsForRC").ToList();
            foreach (var item in allvalues)
            {

                item.SiteName = item.SiteName + " <i class=\"fa fa-mobile\" aria-hidden=\"true\"></i> " + string.Join(",", _context.ClientSiteSmartWands.Where(x => x.ClientSiteId == item.ClientSiteId).Select(x => x.PhoneNumber).ToList()) + " <i class=\"fa fa-caret-down\" aria-hidden=\"true\" id=\"btnUpArrow\"></i> ";
                item.Address = " <a id=\"btnActiveGuardsMap\" href=\"https://www.google.com/maps?q=" + item.GPS + "\"target=\"_blank\"><i class=\"fa fa-map-marker\" aria-hidden=\"true\"></i> </a>" + item.Address + " <input type=\"hidden\" class=\"form-control\" value=\"" + item.GPS + "\" id=\"txtGPSActiveguards\" />";
            }
            return allvalues;
        }


        //logBookId delete for radio checklist-start
        public void DeleteClientSiteRadioCheckActivityStatusForLogBookEntry(int id)
        {
            var clientSiteRadioCheckActivityStatusToDelete = _context.ClientSiteRadioChecksActivityStatus.Where(x => x.LBId == id);
            if (clientSiteRadioCheckActivityStatusToDelete == null)
                throw new InvalidOperationException();
            foreach (var item in clientSiteRadioCheckActivityStatusToDelete)
            {
                _context.Remove(item);
            }


            _context.SaveChanges();
        }
        public void SignOffClientSiteRadioCheckActivityStatusForLogBookEntry(int GuardId, int ClientSiteId)
        {
            var clientSiteRadioCheckActivityStatusToDelete = _context.ClientSiteRadioChecksActivityStatus.Where(x => x.GuardId == GuardId && x.ClientSiteId == ClientSiteId);
            if (clientSiteRadioCheckActivityStatusToDelete == null)
                throw new InvalidOperationException();
            foreach (var item in clientSiteRadioCheckActivityStatusToDelete)
            {
                _context.Remove(item);
            }



            _context.SaveChanges();

        }
        /* Find all the Activity of the user */
        public bool getIfAnyActivityInbufferTime(int GuardId, int ClientSiteId)
        {
            bool status = false;
            var clientSiteRadioCheckActivityStatusToDelete = _context.ClientSiteRadioChecksActivityStatus.Where(x => x.GuardId == GuardId && x.ClientSiteId == ClientSiteId && x.GuardLoginTime == null).ToList();

            if (clientSiteRadioCheckActivityStatusToDelete.Count > 0)
            {
                foreach (var activity in clientSiteRadioCheckActivityStatusToDelete)
                {
                    if (activity.LastIRCreatedTime != null)
                    {
                        if ((DateTime.Now - activity.LastIRCreatedTime).Value.TotalMinutes < 90)
                        {
                            status = true;
                            break;
                        }
                    }
                    if (activity.LastKVCreatedTime != null)
                    {
                        if ((DateTime.Now - activity.LastKVCreatedTime).Value.TotalMinutes < 90)
                        {
                            status = true;
                            break;
                        }
                    }
                    if (activity.LastLBCreatedTime != null)
                    {
                        if ((DateTime.Now - activity.LastLBCreatedTime).Value.TotalMinutes < 90)
                        {
                            status = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                return status = false;
            }

            return status;

        }
        //logBookId delete for radio checklist-end

        public void DeleteClientSiteRadioChecksActivity(ClientSiteRadioChecksActivityStatus ClientSiteRadioChecksActivityStatus)
        {
            var ClientSiteRadioChecksActivity = _context.ClientSiteRadioChecksActivityStatus.SingleOrDefault(x => x.Id == ClientSiteRadioChecksActivityStatus.Id);
            if (ClientSiteRadioChecksActivity != null)
            {
                var clientSiteRcStatus = _context.ClientSiteRadioChecks.Where(x => x.GuardId == ClientSiteRadioChecksActivity.GuardId && x.ClientSiteId == ClientSiteRadioChecksActivity.ClientSiteId);
                /* remove the Pervious Status*/
                if (clientSiteRcStatus != null)
                    _context.ClientSiteRadioChecks.RemoveRange(clientSiteRcStatus);

                _context.ClientSiteRadioChecksActivityStatus.Remove(ClientSiteRadioChecksActivity);

            }
            _context.SaveChanges();

        }

        //for getting logbookdetails of the guard-start
        public List<RadioCheckListGuardLoginData> GetActiveGuardlogBookDetails(int clientSiteId, int guardId)
        {
            var param1 = new SqlParameter();
            param1.ParameterName = "@ClientSiteId";
            param1.SqlDbType = SqlDbType.Int;
            param1.SqlValue = clientSiteId;

            var param2 = new SqlParameter();
            param2.ParameterName = "@GuardId";
            param2.SqlDbType = SqlDbType.Int;
            param2.SqlValue = guardId;


            var allvalues = _context.RadioCheckListGuardLoginData.FromSqlRaw($"EXEC sp_GetActiveGuardLogBookDetailsForRC @ClientSiteId,@GuardId", param1, param2).ToList();

            return allvalues;
        }
        //for getting logbookdetails of the guard-end

        //for getting the details of guards not available-start
        public List<RadioCheckListNotAvailableGuardData> GetNotAvailableGuardDetails()
        {

            var allvalues = _context.RadioCheckListNotAvailableGuardData.FromSqlRaw($"EXEC sp_GetNotAvailableGuardDetailsForRC").ToList();
            foreach (var item in allvalues)
            {

                item.SiteName = item.SiteName + " <i class=\"fa fa-mobile\" aria-hidden=\"true\"></i> " + string.Join(",", _context.ClientSiteSmartWands.Where(x => x.ClientSiteId == item.ClientSiteId).Select(x => x.PhoneNumber).ToList()) + " <i class=\"fa fa-caret-down\" aria-hidden=\"true\" id=\"btnUpArrow\"></i> ";
                item.Address = " <a id=\"btnActiveGuardsMap\" href=\"https://www.google.com/maps?q=" + item.GPS + "\"target=\"_blank\"><i class=\"fa fa-map-marker\" aria-hidden=\"true\"></i> </a>" + item.Address + " <input type=\"hidden\" class=\"form-control\" value=\"" + item.GPS + "\" id=\"txtGPSActiveguards\" />";
            }
            return allvalues;
        }
        //for getting the details of guards not available-end//for getting key vehicle log details of the  guard-start

        public List<RadioCheckListGuardKeyVehicleData> GetActiveGuardKeyVehicleLogDetails(int clientSiteId, int guardId)
        {
            var param1 = new SqlParameter();
            param1.ParameterName = "@ClientSiteId";
            param1.SqlDbType = SqlDbType.Int;
            param1.SqlValue = clientSiteId;

            var param2 = new SqlParameter();
            param2.ParameterName = "@GuardId";
            param2.SqlDbType = SqlDbType.Int;
            param2.SqlValue = guardId;


            var allvalues = _context.RadioCheckListGuardKeyVehicleData.FromSqlRaw($"EXEC sp_GetActiveGuardKeyVehicleDetailsForRC @ClientSiteId,@GuardId", param1, param2).ToList();

            return allvalues;
        }
        //for getting  key vehicle log details of the  guard-end

        //for getting incident report details of the  guard-start

        public List<RadioCheckListGuardIncidentReportData> GetActiveGuardIncidentReportDetails(int clientSiteId, int guardId)
        {
            var param1 = new SqlParameter();
            param1.ParameterName = "@ClientSiteId";
            param1.SqlDbType = SqlDbType.Int;
            param1.SqlValue = clientSiteId;

            var param2 = new SqlParameter();
            param2.ParameterName = "@GuardId";
            param2.SqlDbType = SqlDbType.Int;
            param2.SqlValue = guardId;


            var allvalues = _context.RadioCheckListGuardIncidentReportData.FromSqlRaw($"EXEC sp_GetActiveGuardIncidentReportsDetailsForRC @ClientSiteId,@GuardId", param1, param2).ToList();

            return allvalues;
        }
        //for getting  incident report details of the  guard-end
        public Guard GetGuards(int guardId)
        {





            return _context.Guards.Where(x => x.Id == guardId).FirstOrDefault();
        }
        public void DeleteClientSiteRadioCheckActivityStatusForKeyVehicleEntry(int id)
        {
            var clientSiteRadioCheckActivityStatusToDelete = _context.ClientSiteRadioChecksActivityStatus.Where(x => x.KVId == id);
            if (clientSiteRadioCheckActivityStatusToDelete == null)
                throw new InvalidOperationException();
            foreach (var item in clientSiteRadioCheckActivityStatusToDelete)
            {
                _context.Remove(item);
            }


            _context.SaveChanges();
        }
        public int GetClientSiteLogBookId(int clientsiteId, LogBookType type, DateTime date)
        {
            return _context.ClientSiteLogBooks
                 .SingleOrDefault(z => z.ClientSiteId == clientsiteId && z.Type == type && z.Date == date).Id;
        }

        public void SaveClientSiteRadioCheck(ClientSiteRadioCheck clientSiteRadioCheck)
        {

            try
            {

                var clientSiteRcStatus = _context.ClientSiteRadioChecks.Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId);
                /* remove the Pervious Status*/
                if (clientSiteRcStatus != null)
                {
                    _context.ClientSiteRadioChecks.RemoveRange(clientSiteRcStatus);


                    if (clientSiteRadioCheck.Status == "Off Duty")
                    {
                        /* Check if Manning type notfication */
                        var checkIfTypeOneManning = GetClientSiteRadioChecksActivityDetails().Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId && x.GuardLoginTime != null && x.NotificationType == 1).ToList();

                        if (checkIfTypeOneManning.Count == 0)
                        {
                            var logbook = _context.ClientSiteLogBooks
                         .SingleOrDefault(z => z.ClientSiteId == clientSiteRadioCheck.ClientSiteId && z.Type == LogBookType.DailyGuardLog && z.Date == DateTime.Today);

                            int logBookId;
                            if (logbook == null)
                            {
                                var newLogBook = new ClientSiteLogBook()
                                {
                                    ClientSiteId = clientSiteRadioCheck.ClientSiteId,
                                    Type = LogBookType.DailyGuardLog,
                                    Date = DateTime.Today
                                };

                                if (newLogBook.Id == 0)
                                {
                                    _context.ClientSiteLogBooks.Add(newLogBook);
                                }
                                else
                                {
                                    var logBookToUpdate = _context.ClientSiteLogBooks.SingleOrDefault(z => z.Id == newLogBook.Id);
                                    if (logBookToUpdate != null)
                                    {
                                        // nothing to update
                                    }
                                }
                                _context.SaveChanges();
                                logBookId = newLogBook.Id;

                            }
                            else
                            {
                                logBookId = logbook.Id;
                            }

                            var guardLoginId = _context.GuardLogins
                          .SingleOrDefault(z => z.ClientSiteLogBookId == logBookId && z.GuardId == clientSiteRadioCheck.GuardId && z.OnDuty.Date == DateTime.Today);
                            if (guardLoginId != null)
                            {
                                var guardLog = new GuardLog()
                                {
                                    ClientSiteLogBookId = logBookId,
                                    GuardLoginId = guardLoginId.Id,
                                    EventDateTime = DateTime.Now,
                                    Notes = "Guard Off Duty (Logbook Signout)",
                                    IsSystemEntry = true

                                };
                                SaveGuardLog(guardLog);
                                var guardLoginToUpdate = _context.GuardLogins.SingleOrDefault(x => x.Id == guardLoginId.Id);
                                if (guardLoginToUpdate != null)
                                {
                                    guardLoginToUpdate.OffDuty = DateTime.Now;
                                    _context.SaveChanges();
                                }

                            }
                            else
                            {
                                var latestRecord = _context.GuardLogins
                                .Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId)
                                .OrderByDescending(r => r.Id)
                                 .FirstOrDefault();
                                if (latestRecord != null)
                                {
                                    var guardLog = new GuardLog()
                                    {
                                        ClientSiteLogBookId = logBookId,
                                        GuardLoginId = latestRecord.Id,
                                        EventDateTime = DateTime.Now,
                                        Notes = "Guard Off Duty (Logbook Signout)",
                                        IsSystemEntry = true

                                    };
                                    SaveGuardLog(guardLog);
                                    var guardLoginToUpdate = _context.GuardLogins.SingleOrDefault(x => x.Id == latestRecord.Id);
                                    if (guardLoginToUpdate != null)
                                    {
                                        guardLoginToUpdate.OffDuty = DateTime.Now;
                                        _context.SaveChanges();
                                    }

                                }

                            }


                            //var signOffEntry = new GuardLog()
                            //{
                            //    ClientSiteLogBookId = clientSiteLogBookId,
                            //    GuardLoginId = guardLoginId,
                            //    EventDateTime = DateTime.Now,
                            //    Notes = "Guard Off Duty (Logbook Signout)",
                            //    IsSystemEntry = true
                            //};
                            //_guardLogDataProvider.SaveGuardLog(signOffEntry);
                            //_guardDataProvider.UpdateGuardOffDuty(guardLoginId, DateTime.Now);


                            var ClientSiteRadioChecksActivityDetails = GetClientSiteRadioChecksActivityDetails().Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId && x.GuardLoginTime != null);
                            foreach (var ClientSiteRadioChecksActivity in ClientSiteRadioChecksActivityDetails)
                            {
                                ClientSiteRadioChecksActivity.GuardLogoutTime = DateTime.Now;
                                UpdateRadioChecklistLogOffEntry(ClientSiteRadioChecksActivity);

                                var newstatu = new ClientSiteRadioCheck()
                                {
                                    ClientSiteId = ClientSiteRadioChecksActivity.ClientSiteId,
                                    GuardId = ClientSiteRadioChecksActivity.GuardId,
                                    Status = "Off Duty",
                                    CheckedAt = DateTime.Now,
                                    Active = true
                                };
                                _context.ClientSiteRadioChecks.RemoveRange(clientSiteRcStatus);
                                _context.ClientSiteRadioChecks.Add(newstatu);
                                _context.SaveChanges();
                                /* Update Radio check status logOff*/

                            }

                        }
                        else
                        {
                            _context.ClientSiteRadioChecks.Add(clientSiteRadioCheck);
                            _context.SaveChanges();

                            /* Remove the Notification Row */
                            var removeList = GetClientSiteRadioChecksActivityDetails().Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId && x.GuardLoginTime != null && x.NotificationType == 1).ToList();
                            _context.ClientSiteRadioChecksActivityStatus.RemoveRange(removeList);
                            _context.SaveChanges();
                        }

                    }
                    else
                    {
                        _context.ClientSiteRadioChecks.Add(clientSiteRadioCheck);
                        _context.SaveChanges();


                    }
                }
            }
            catch (Exception ex)
            {


            }
        }





        public void SaveClientSiteRadioCheckStatusFromlogBook(ClientSiteRadioCheck clientSiteRadioCheck)
        {
            var clientSiteRcStatus = _context.ClientSiteRadioChecks.Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId);
            /* remove the Pervious Status*/
            if (clientSiteRcStatus != null)
            {
                var ClientSiteRadioChecksActivityDetails = GetClientSiteRadioChecksActivityDetails().Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId && x.GuardLoginTime != null);
                foreach (var ClientSiteRadioChecksActivity in ClientSiteRadioChecksActivityDetails)
                {
                    ClientSiteRadioChecksActivity.GuardLogoutTime = DateTime.Now;
                    UpdateRadioChecklistLogOffEntry(ClientSiteRadioChecksActivity);

                    var newstatu = new ClientSiteRadioCheck()
                    {
                        ClientSiteId = ClientSiteRadioChecksActivity.ClientSiteId,
                        GuardId = ClientSiteRadioChecksActivity.GuardId,
                        Status = "Off Duty",
                        CheckedAt = DateTime.Now,
                        Active = true
                    };
                    _context.ClientSiteRadioChecks.RemoveRange(clientSiteRcStatus);
                    _context.ClientSiteRadioChecks.Add(newstatu);
                    _context.SaveChanges();
                    /* Update Radio check status logOff*/


                }
            }
        }
        public int GetGuardLoginId(int clientsitelogbookId, int guardId, DateTime date)
        {
            return _context.GuardLogins
                 .SingleOrDefault(z => z.ClientSiteLogBookId == clientsitelogbookId && z.GuardId == guardId && z.OnDuty.Date == date.Date).Id;
        }
        public List<GuardLog> GetGuardLogsId(int logBookId, DateTime logDate, int guardLoginId, IrEntryType type, string notes)
        {
            return _context.GuardLogs
               .Where(z => z.ClientSiteLogBookId == logBookId && z.EventDateTime >= logDate && z.EventDateTime < logDate.AddDays(1)
               && z.GuardLoginId == guardLoginId && z.IrEntryType == type && z.Notes == notes).ToList();


        }
        public void UpdateRadioChecklistEntry(ClientSiteRadioChecksActivityStatus clientSiteActivity)
        {
            try
            {


                var clientSiteActivityToUpdate = _context.ClientSiteRadioChecksActivityStatus.SingleOrDefault(x => x.Id == clientSiteActivity.Id);
                if (clientSiteActivityToUpdate == null)
                    throw new InvalidOperationException();
                clientSiteActivityToUpdate.NotificationCreatedTime = clientSiteActivity.NotificationCreatedTime;




                _context.SaveChanges();
            }
            catch (Exception ex)
            {

            }
        }

        public void UpdateRadioChecklistLogOffEntry(ClientSiteRadioChecksActivityStatus clientSiteActivity)
        {
            try
            {


                var clientSiteActivityToUpdate = _context.ClientSiteRadioChecksActivityStatus.SingleOrDefault(x => x.Id == clientSiteActivity.Id);
                if (clientSiteActivityToUpdate == null)
                    throw new InvalidOperationException();
                clientSiteActivityToUpdate.GuardLogoutTime = clientSiteActivity.GuardLogoutTime;
                _context.SaveChanges();
            }
            catch (Exception ex)
            {

            }
        }
        public List<GuardLogin> GetGuardLogins(int guardLoginId)
        {
            return _context.GuardLogins.Where(z => z.Id == guardLoginId).ToList();
        }

        /* New Change by dileep for P4 task 17 Start */
        public void GetGuardManningDetails(DayOfWeek currentDay)
        {
            try
            {
                /*remove all the manning notification Start for showing today's manning*/

                var notificationDetailsAll = _context.ClientSiteRadioChecksActivityStatus.Where(x => x.GuardLoginTime != null && x.NotificationType == 1);
                _context.ClientSiteRadioChecksActivityStatus.RemoveRange(notificationDetailsAll);
                _context.SaveChanges();

                /* remove all the manning notification end */

                /* get the manning details corresponding to the currentDay*/
                var clientSiteManningKpiSettings = _context.ClientSiteManningKpiSettings.Include(x => x.ClientSiteKpiSetting).Where(x => x.WeekDay == currentDay && x.Type == "2").ToList();
                foreach (var manning in clientSiteManningKpiSettings)
                {
                    if (manning.EmpHoursStart != null)
                    {
                        /* Check the number of logins */
                        var numberOfLogin = _context.ClientSiteRadioChecksActivityStatus.Where(x => x.ClientSiteId == manning.ClientSiteKpiSetting.ClientSiteId && x.GuardLoginTime != null && x.NotificationType == null).Count() == 0;
                        if (numberOfLogin)
                        {    /* No login found */
                            /* find the emp Hours  Start time -5 (ie show notification 5 min before the guard login in the site) */
                            var dateTime = DateTime.ParseExact(manning.EmpHoursStart, "H:mm", null, System.Globalization.DateTimeStyles.None).AddMinutes(-5);
                            if (DateTime.Now >= dateTime)
                            {

                                var radioChecklist = _context.ClientSiteRadioChecksActivityStatus.Where(z => z.GuardId == 4 && z.ClientSiteId == manning.ClientSiteKpiSetting.ClientSiteId && z.GuardLoginTime != null && z.NotificationType == 1)
                                  .ToList();
                                if (radioChecklist.Count == 0)
                                {
                                    /* Check if any off duty status checked for this row */
                                    var rcOffDutyStatus = _context.ClientSiteRadioChecks.Where(z => z.GuardId == 4 && z.ClientSiteId == manning.ClientSiteKpiSetting.ClientSiteId && z.CheckedAt.Date == DateTime.Today.Date && z.Status == "Off Duty")
                                  .ToList();
                                    if (rcOffDutyStatus.Count == 0)
                                    {
                                        var clientsiteRadioCheck = new ClientSiteRadioChecksActivityStatus()
                                        {
                                            ClientSiteId = manning.ClientSiteKpiSetting.ClientSiteId,
                                            GuardId = 4,/* temp Guard(bruno) Id because forgin key  is set*/
                                            GuardLoginTime = DateTime.ParseExact(manning.EmpHoursStart, "H:mm", null, System.Globalization.DateTimeStyles.None),/* Expected Time for Login
                                    /* New Field Added for NotificationType only for manning notification*/
                                            NotificationType = 1
                                        };
                                        _context.ClientSiteRadioChecksActivityStatus.Add(clientsiteRadioCheck);
                                        _context.SaveChanges();
                                    }
                                }
                            }

                        }
                        else
                        {
                            /* if login  found  remove the notification*/
                            var notificationCountIsZero = _context.ClientSiteRadioChecksActivityStatus.Where(x => x.ClientSiteId == manning.ClientSiteKpiSetting.ClientSiteId && x.GuardLoginTime != null && x.NotificationType == 1).Count() == 0;
                            if (!notificationCountIsZero)
                            {
                                /* Remove notification because login found */
                                var notificationDetails = _context.ClientSiteRadioChecksActivityStatus.Where(x => x.ClientSiteId == manning.ClientSiteKpiSetting.ClientSiteId && x.GuardLoginTime != null && x.NotificationType == 1);
                                _context.ClientSiteRadioChecksActivityStatus.RemoveRange(notificationDetails);
                                _context.SaveChanges();
                            }
                        }

                    }
                }


            }
            catch (Exception ex)
            {

            }
        }


        public void RemoveTheeRadioChecksActivityWithNotifcationtypeOne(int ClientSiteId)
        {
            var clientSiteRadioCheckActivityStatusToDelete = _context.ClientSiteRadioChecksActivityStatus.Where(x => x.ClientSiteId == ClientSiteId && x.NotificationType == 1);
            if (clientSiteRadioCheckActivityStatusToDelete == null)
                throw new InvalidOperationException();
            else

            {
                _context.RemoveRange(clientSiteRadioCheckActivityStatusToDelete);
                _context.SaveChanges();
            }

        }

        public void RemoveClientSiteRadioChecksGreaterthanTwoHours()
        {
            var clientSiteRadioChecksToDelete = _context.ClientSiteRadioChecks.Where(x => 1 == 1).ToList();
            if (clientSiteRadioChecksToDelete == null)
            {
                throw new InvalidOperationException();
            }
            else
            {
                foreach (var item in clientSiteRadioChecksToDelete)
                {


                    var isActive = (DateTime.Now - item.CheckedAt).TotalHours < 2;
                    if (!isActive)
                    {
                        var clientSiteRadioCheckActivityStatusToDelete = _context.ClientSiteRadioChecks.Where(x => x.Id == item.Id);
                        if (clientSiteRadioCheckActivityStatusToDelete == null)
                            throw new InvalidOperationException();
                        else

                        {
                            _context.RemoveRange(clientSiteRadioCheckActivityStatusToDelete);
                            _context.SaveChanges();
                        }


                    }
                }
            }
        }

        /* New Change by dileep for P4 task 17 end */




        //code added to save Duress radio check start
        public void SaveRadioCheckDuress(string UserID)
        {
            _context.RadioCheckDuress.Add(new RadioCheckDuress()
            {
                UserID = Convert.ToInt32(UserID),
                IsActive = true,
                CurrentDateTime = DateTime.Today
            });
            _context.SaveChanges();
        }
        public bool IsRadiocheckDuressEnabled(int UserID)
        {
            return _context.RadioCheckDuress
    .Where(z => z.UserID == UserID)
    .OrderByDescending(z => z.Id)
    .Select(z => z.IsActive)
    .LastOrDefault();
        }
        public int UserIDDuress(int UserID)
        {
            return _context.RadioCheckDuress
    .Where(z => z.UserID == UserID)
    .OrderByDescending(z => z.Id)
    .Select(z => z.UserID)
    .LastOrDefault();
        }


        //listing clientsites for radio check
        public List<ClientSite> GetClientSites(int? Id)
        {
            return _context.ClientSites
                .Where(x => !Id.HasValue || (Id.HasValue && x.Id == Id.Value)).ToList();

        }
        public List<ClientSiteSmartWand> GetClientSiteSmartWands(int? clientSiteId)
        {
            return _context.ClientSiteSmartWands
                .Where(x => !clientSiteId.HasValue || (clientSiteId.HasValue && x.ClientSiteId == clientSiteId.Value))
                .Include(x => x.ClientSite)
                .ToList();
        }
        public int GetGuardLoginId(int guardId, DateTime date)
        {
            return _context.GuardLogins
                 .Where(z => z.GuardId == guardId && z.OnDuty.Date == date.Date).Max(x => x.Id);
        }
        public List<GuardLogin> GetGuardLoginsByClientSiteId(int clientsiteId, DateTime date)
        {
            var guarlogins = _context.GuardLogins.Where(z => z.ClientSiteId == clientsiteId && z.OnDuty.Date == date.Date).ToList();

            foreach (var item in guarlogins)
            {
                item.Guard = GetGuards(item.GuardId);
            }
            return guarlogins;
        }


    }
}