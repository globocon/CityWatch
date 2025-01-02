using CityWatch.Data.Enums;
using CityWatch.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;

namespace CityWatch.Data.Providers
{
    public interface IGuardDataProvider
    {
        List<Guard> GetGuards();
        List<Guard> GetActiveGuards();
        int SaveGuard(Guard guard, out string initalsUsed);
        int UpdateGuard(Guard guard, string state, out string initalsUsed);
        List<GuardLogin> GetGuardLogins(int clientSiteId, DateTime fromDate, DateTime toDate);
        List<GuardLogin> GetUniqueGuardLogins();
        List<GuardLogin> GetGuardLoginsBySmartWandId(int smartWandId);
        List<GuardLogin> GetGuardLoginsByLogBookId(int logBookId);
        GuardLogin GetGuardLoginById(int id);
        GuardLogin GetGuardLastLogin(int guardId);
        GuardLogin GetGuardLastLogin(int guardId, int? userId);
        int SaveGuardLogin(GuardLogin guardLogin);
        void UpdateGuardOffDuty(int guardLoginId, DateTime offDuty);
        List<GuardLicense> GetAllGuardLicenses();
        List<GuardComplianceAndLicense> GetAllGuardLicensesAndCompliances();
        List<GuardLicense> GetGuardLicenses(int guardId);
        List<GuardComplianceAndLicense> GetGuardLicensesandcompliance(int guardId);
        GuardComplianceAndLicense GetGuardComplianceFile(int id);
        GuardLicense GetGuardLicense(int id);
        void SaveGuardLicense(GuardLicense guardLicense);
        void DeleteGuardLicense(int id);
        List<GuardCompliance> GetAllGuardCompliances();
        List<GuardCompliance> GetGuardCompliances(int guardId);
        HrSettings GetHRRefernceNo(int HRid, string Description);
        List<HrSettings> GetHRDesc(int HRid);
        GuardCompliance GetGuardCompliance(int id);
        void SaveGuardCompliance(GuardCompliance guardCompliance);
        void SaveGuardComplianceandlicanse(GuardComplianceAndLicense guardComplianceandlicense);
        void DeleteGuardCompliance(int id);
        Guard GetGuardDetailsbySecurityLicenseNo(string securityLicenseNo);

        public string GetDefaultEmailAddress();
        DateTime? GetLogbookDateFromLogbook(int logbookId);

        List<GuardCompliance> GetGuardCompliancesList(int[] guardIds);

        //List<Guard> GetGuardsCount();
        List<GuardLogin> GetGuardLogins(int[] guardIds);
        //for toggle areas - start 
        List<ClientSiteToggle> GetClientSiteToggle();
        List<ClientSiteToggle> GetClientSiteToggle(int siteId);
        List<ClientSiteToggle> GetClientSiteToggle(int siteId, int toggleId);

        KeyVehicleLog GetEmailPOCVehiclelog(int id);


        //for toggle areas - end 
        //p1-191 hr files task 3-start
        List<HRGroups> GetHRGroups();
        List<ReferenceNoNumbers> GetReferenceNoNumbers();

        List<ReferenceNoAlphabets> GetReferenceNoAlphabets();

        //p1-191 hr files task 3-end
        List<LicenseTypes> GetLicenseTypes();
        GuardComplianceAndLicense GetDescriptionList(HrGroup hrGroup, string Description, int GuardID);
        List<GuardComplianceAndLicense> GetGuardCompliancesAndLicense(int guardId);
        List<GuardComplianceAndLicense> GetGuardCompliancesAndLicenseList(string hrGroup);
        List<GuardComplianceAndLicense> GetGuardCompliancesAndLicenseHR(int guardId, HrGroup hrGroup);
        List<CriticalDocumentsClientSites> GetCriticalDocs(int clientSiteID);
        ClientSite GetClientSiteID(string ClientSite);
        public DropboxDirectory GetDrobox();
        public GuardComplianceAndLicense GetDescriptionUsed(HrGroup hrGroup, string Description, int GuardID);

        public List<Guard> GetGuardDetailsUsingId(int Id);
        public void SetGuardNewPIN(int guardId, string NewPIN);
        List<HrSettings> GetHRDescFull();
        public void SaveRecordingFileDetails(AudioRecordingLog audioRecordingLog);
        public int GetGuardID(string LicenseNo);

        List<HrSettingsLockedClientSites> GetHrDocumentLockDetailsForASite(int clientSiteId);
        public int SaveLanguageDetails(Guard guard);
        List<GuardLogin> GetGuardLoginsByGuardIdAndDate(int guardIds, DateTime startdate, DateTime enddate);
        public void SaveGuardLotes(Guard guard);
        public void DeleteGuardLotes(int guardid);
        List<LanguageDetails> GetGuardLotes(int[] guardIds);
        public List<LanguageDetails> GetGuardLanguages(int[] guardIds);


    }

    public class GuardDataProvider : IGuardDataProvider
    {
        private readonly CityWatchDbContext _context;

        public GuardDataProvider(CityWatchDbContext context)
        {
            _context = context;
        }

        public List<Guard> GetGuards()
        {
            return _context.Guards.ToList();

        }


        public List<Guard> GetGuardDetailsUsingId(int Id)
        {
            return _context.Guards.Where(x => x.Id == Id).ToList(); ;

        }

        public void SetGuardNewPIN(int guardId, string NewPIN)
        {
            var updateGuard = _context.Guards.SingleOrDefault(x => x.Id == guardId);
            if (updateGuard != null)
            {
                updateGuard.Pin = NewPIN;
                _context.SaveChanges();

            }


        }

        //P4#70 to display only active guards PartB-C -x => x.IsActive == true-Added by Manju -start

        public List<Guard> GetActiveGuards()
        {
            return _context.Guards.Where(x => x.IsActive == true).OrderBy(x => x.Name).ToList();
        }
        public List<CriticalDocumentsClientSites> GetCriticalDocs(int clientSiteID)
        {
            return _context.CriticalDocumentsClientSites
                //.Include(x=>x.HRSettings)
                //    .ThenInclude(z => z.ReferenceNoNumbers)
                //    .Include(x => x.HRSettings)
                // .ThenInclude(z => z.ReferenceNoAlphabets)
                // .Include(x => x.HRSettings)
                // .ThenInclude(z => z.HRGroups)
                .Where(x => x.ClientSiteId == clientSiteID).ToList();
        }
        public ClientSite GetClientSiteID(string ClientSite)
        {
            return _context.ClientSites.Where(x => x.Name == ClientSite).FirstOrDefault();
        }
        //public List<Guard> GetGuardsCount()
        //{
        //    var guards = _context.Guards.ToList();
        //    foreach (var guard in guards)
        //    {

        //        guard.IsActiveCount = CalculateIsActiveCountForGuard(guard.Id); 
        //    }

        //    return guards;
        //}
        //public int CalculateIsActiveCountForGuard(int GuradID)
        //{
        //    var guardLoginsForId = _context.GuardLogins
        //            .Where(z => z.GuardId == GuradID && z.ClientSite.IsActive && z.GuardLogs.Notes== "Logbook Logged In")
        //            .Include(z => z.ClientSite)
        //            .Include(z => z.Guard)
        //            .Include(z => z.GuardLogs)
        //            .ToList();

        //    int isActiveCount = guardLoginsForId.Count;
        //    return isActiveCount;
        //}

        public Guard GetGuardDetailsbySecurityLicenseNo(string securityLicenseNo)
        {
            return _context.Guards.SingleOrDefault(x => x.SecurityNo.Trim() == securityLicenseNo.Trim());
        }

        public int GetGuardID(string LicenseNo)
        {
            var Number = _context.Guards.Where(x => x.SecurityNo == LicenseNo).Select(x => x.Id);
            return Number.FirstOrDefault();
        }

        public int SaveGuard(Guard guard, out string initalsUsed)
        {
            initalsUsed = guard.Initial;
            var isNewGuard = guard.Id == -1;
            if (isNewGuard)
            {
                guard.Id = 0;
                guard.IsActive = true;
                initalsUsed = MakeGuardInitials(guard.Initial);
                guard.Initial = initalsUsed;
                guard.DateEnrolled = DateTime.Today;
                _context.Guards.Add(guard);
            }
            else
            {
                var updateGuard = _context.Guards.SingleOrDefault(x => x.Id == guard.Id);
                updateGuard.Name = guard.Name;
                updateGuard.SecurityNo = guard.SecurityNo;
                updateGuard.IsActive = guard.IsActive;
                updateGuard.IsReActive = guard.IsActive;
                if (updateGuard.Initial != guard.Initial)
                {
                    initalsUsed = MakeGuardInitials(guard.Initial);
                    updateGuard.Initial = initalsUsed;
                }
                else
                {
                    updateGuard.Initial = guard.Initial;
                }
                updateGuard.State = guard.State;
                updateGuard.Provider = guard.Provider;
                updateGuard.Mobile = guard.Mobile;
                updateGuard.Email = guard.Email;
                updateGuard.IsKPIAccess = guard.IsKPIAccess;
                updateGuard.IsRCAccess = guard.IsRCAccess;
                updateGuard.IsSTATS = guard.IsSTATS;
                updateGuard.IsLB_KV_IR = guard.IsLB_KV_IR;
                updateGuard.IsAdminPowerUser = guard.IsAdminPowerUser;
                updateGuard.IsAdminGlobal = guard.IsAdminGlobal;
                updateGuard.Pin = guard.Pin;
                //p1-224 RC Bypass For HR -start
                updateGuard.Gender = guard.Gender;
                updateGuard.IsRCBypass = guard.IsRCBypass;
                //p1-224 RC Bypass For HR -end
                updateGuard.IsSTATSChartsAccess = guard.IsSTATSChartsAccess;
                updateGuard.IsRCFusionAccess = guard.IsRCFusionAccess;
                updateGuard.IsAdminSOPToolsAccess = guard.IsAdminSOPToolsAccess;
                updateGuard.IsAdminAuditorAccess = guard.IsAdminAuditorAccess;
                updateGuard.IsAdminInvestigatorAccess = guard.IsAdminInvestigatorAccess;
                updateGuard.IsAdminThirdPartyAccess = guard.IsAdminThirdPartyAccess;
                updateGuard.IsRCHRAccess = guard.IsRCHRAccess;
                updateGuard.IsRCLiteAccess = guard.IsRCLiteAccess;
            }

            _context.SaveChanges();

            if (isNewGuard)
            {
                _context.GuardLicenses.Add(new GuardLicense()
                {
                    LicenseNo = guard.SecurityNo,
                    LicenseType = GuardLicenseType.Other,
                    GuardId = guard.Id
                });
                _context.SaveChanges();
            }


            return guard.Id;
        }
        public int UpdateGuard(Guard guard, string state, out string initalsUsed)
        {
            initalsUsed = guard.Initial;
            var isNewGuard = guard.Id == -1;
            if (isNewGuard)
            {
                guard.Id = 0;
                guard.IsActive = true;
                initalsUsed = MakeGuardInitials(guard.Initial);
                guard.Initial = initalsUsed;
                guard.DateEnrolled = DateTime.Today;
                guard.IsLB_KV_IR = true;
                guard.IsKPIAccess = false;
                guard.IsRCAccess = false;
                guard.IsSTATS = false;
                //P1-273 access levels-start
                guard.IsSTATSChartsAccess = false;
                guard.IsRCFusionAccess = false;

                //P1-273 access levels-end
                _context.Guards.Add(guard);
            }
            else
            {
                var updateGuard = _context.Guards.SingleOrDefault(x => x.Id == guard.Id);


                updateGuard.Mobile = guard.Mobile;
                updateGuard.Email = guard.Email;
                updateGuard.State = state;


            }

            _context.SaveChanges();

            if (isNewGuard)
            {
                _context.GuardLicenses.Add(new GuardLicense()
                {
                    LicenseNo = guard.SecurityNo,
                    LicenseType = GuardLicenseType.Other,
                    GuardId = guard.Id
                });
                _context.SaveChanges();
            }

            return guard.Id;
        }
        public List<GuardLogin> GetGuardLogins(int[] guardIds)
        {
            List<GuardLogin> guardLogins = new List<GuardLogin>();
            foreach (int guardId in guardIds)
            {

                guardLogins.AddRange(_context.GuardLogins
                .Where(z => z.GuardId == guardId && z.ClientSite.IsActive == true)
                    .Include(z => z.ClientSite)
                    .Include(z => z.Guard)
                    .ToList());
            }
            //for query optimization Comment the old code
            //var guardLogins = _context.GuardLogins
            //    .Where(z => guardIds.Contains(z.GuardId))

            //    .Include(z => z.Guard)
            //    .Include(z => z.ClientSite)
            //    .ToList();


            return guardLogins
                .GroupBy(z => new { z.GuardId, z.ClientSiteId })
                .Select(z => z.Last())
                .ToList();


        }

        public List<LanguageDetails> GetGuardLotes(int[] guardIds)
        {
            List<LanguageDetails> guardLogins = new List<LanguageDetails>();
            if (guardIds.Length != 0)
            {
                foreach (int guardId in guardIds)
                {
                    var temp = _context.LanguageDetails.Where(z => z.GuardId == guardId).Include(z=>z.LanguageMaster).ToList();
                    if (temp.Any())
                    {
                        guardLogins.AddRange(temp);


                    }
                }


            }


            return guardLogins;
        }

        public List<LanguageDetails> GetGuardLanguages(int[] guardIds)
        {
            List<LanguageDetails> language = new List<LanguageDetails>();
            foreach (int guardId in guardIds)
            {

                language.AddRange(_context.LanguageDetails
                .Where(z => z.GuardId == guardId && z.IsDeleted == false)
                    .Include(z => z.LanguageMaster)
                    .ToList());
            }

            return language;





        }

        public List<GuardLogin> GetGuardLoginsByGuardIdAndDate(int guardIds, DateTime startdate, DateTime endDate)
        {
            List<GuardLogin> guardLogins = new List<GuardLogin>();

            guardLogins.AddRange(_context.GuardLogins
            .Where(z => z.GuardId == guardIds && z.ClientSite.IsActive == true && z.LoginDate.Date >= startdate.Date && z.LoginDate.Date <= endDate.Date)
                .Include(z => z.ClientSite)
                .Include(z => z.Guard)
                .ToList());

            //for query optimization Comment the old code
            //var guardLogins = _context.GuardLogins
            //    .Where(z => guardIds.Contains(z.GuardId))

            //    .Include(z => z.Guard)
            //    .Include(z => z.ClientSite)
            //    .ToList();


            return guardLogins

                .ToList();


        }

        public List<GuardLogin> GetGuardLoginsBySmartWandId(int smartWandId)
        {
            return _context.GuardLogins
                .Where(z => z.SmartWandId == smartWandId)
                .ToList();
        }

        public List<GuardLogin> GetGuardLoginsByLogBookId(int logBookId)
        {
            return _context.GuardLogins
                .Where(z => z.ClientSiteLogBookId == logBookId)
                .Include(z => z.ClientSiteLogBook)
                .Include(z => z.Guard)
                .Include(z => z.SmartWand)
                .Include(z => z.Position)
                .ToList();
        }

        public List<GuardLogin> GetGuardLogins(int clientSiteId, DateTime fromDate, DateTime toDate)
        {
            var logbookIds = _context.ClientSiteLogBooks
                                    .Where(z => z.Date >= fromDate && z.Date <= toDate &&
                                            z.ClientSiteId == clientSiteId &&
                                            z.Type == LogBookType.DailyGuardLog)
                                    .Select(z => z.Id);

            var guardLogins = _context.GuardLogins.Where(z => logbookIds.Contains(z.ClientSiteLogBookId));

            guardLogins.Include(z => z.Guard).Load();

            return guardLogins.ToList();
        }

        public List<GuardLogin> GetUniqueGuardLogins()
        {
            return _context.GuardLogins.ToList()
                .GroupBy(z => new { z.GuardId, z.ClientSiteId })
                .Select(z => z.First())
                .ToList();
        }

        public GuardLogin GetGuardLoginById(int id)
        {
            return _context.GuardLogins
                .Include(z => z.Guard)
                .Include(z => z.SmartWand)
                .Include(z => z.Position)
                .SingleOrDefault(z => z.Id == id);
        }
        public KeyVehicleLog GetEmailPOCVehiclelog(int id)
        {
            return _context.KeyVehicleLogs
                .Where(x => x.Id == id).SingleOrDefault();
        }
        public GuardLogin GetGuardLastLogin(int guardId)
        {
            return _context.GuardLogins
                .Include(z => z.ClientSite)
                .Include(z => z.SmartWand)
                .Include(z => z.Position)
                .Include(z => z.ClientSite.ClientType)
                .Where(z => z.GuardId == guardId)
                .OrderByDescending(z => z.LoginDate)
                .FirstOrDefault();
        }

        //to get the details of the guard which is logined with a specific user id
        public GuardLogin GetGuardLastLogin(int guardId, int? userId)
        {
            return _context.GuardLogins
                .Include(z => z.ClientSite)
                .Include(z => z.SmartWand)
                .Include(z => z.Position)
                .Include(z => z.ClientSite.ClientType)
                .Where(z => z.GuardId == guardId && z.UserId == userId)
                .OrderByDescending(z => z.LoginDate)
                .FirstOrDefault();
        }

        public int SaveGuardLogin(GuardLogin guardLogin)
        {
            if (guardLogin.Id == 0)
            {
                _context.GuardLogins.Add(guardLogin);
            }
            else
            {
                var guardLoginToUpdate = _context.GuardLogins.SingleOrDefault(x => x.Id == guardLogin.Id);
                if (guardLoginToUpdate != null)
                {
                    guardLoginToUpdate.ClientSiteId = guardLogin.ClientSiteId;
                    guardLoginToUpdate.LoginDate = guardLogin.LoginDate;
                    guardLoginToUpdate.SmartWandId = guardLogin.SmartWandId;
                    guardLoginToUpdate.PositionId = guardLogin.PositionId;
                    guardLoginToUpdate.OnDuty = guardLogin.OnDuty;
                    guardLoginToUpdate.IPAddress = guardLogin.IPAddress;
                }
            }
            _context.SaveChanges();
            return guardLogin.Id;
        }

        public void UpdateGuardOffDuty(int guardLoginId, DateTime offDuty)
        {
            var guardLoginToUpdate = _context.GuardLogins.SingleOrDefault(x => x.Id == guardLoginId);
            if (guardLoginToUpdate != null)
            {
                guardLoginToUpdate.OffDuty = offDuty;
                _context.SaveChanges();
            }
        }

        private string MakeGuardInitials(string initial)
        {
            var latestInitial = _context.Guards.Where(z => z.Initial.StartsWith(initial)).Select(y => y.Initial).OrderBy(x => x).LastOrDefault();
            if (latestInitial != null)
            {
                var nextSeqNumber = 2;
                if (latestInitial.Contains('['))
                {
                    var lastSeqNumber = int.Parse(latestInitial.Split('[', ']')[1]);
                    nextSeqNumber = lastSeqNumber + 1;
                }

                return string.Format($"{initial} [{nextSeqNumber}]");
            }

            return initial;
        }

        public GuardLicense GetGuardLicense(int id)
        {
            return _context.GuardLicenses
                .Include(z => z.Guard)
                .SingleOrDefault(z => z.Id == id);
        }
        public GuardComplianceAndLicense GetGuardComplianceFile(int id)
        {
            return _context.GuardComplianceLicense
                .Include(z => z.Guard)
                .SingleOrDefault(z => z.Id == id);
        }

        public List<GuardLicense> GetAllGuardLicenses()
        {
            return _context.GuardLicenses.Include(z => z.Guard).ToList();
        }
        public List<GuardComplianceAndLicense> GetAllGuardLicensesAndCompliances()
        {
            return _context.GuardComplianceLicense.Include(z => z.Guard).ToList();
        }

        public List<GuardLicense> GetGuardLicenses(int guardId)
        {
            // var LicenceType= _context.GuardLicenses.Where(x => x.GuardId == guardId).Select(x=>x.LicenseType).F
            var result = _context.GuardLicenses
                 .Where(x => x.GuardId == guardId)
                 .Include(z => z.Guard).ToList();
            //GuardLicenseType? licenseType = null;
            // int intValueToCompare = 3;

            foreach (var item in result)
            {
                //GuardLicenseType? licenseType = null;
                int intValueToCompare = -1;
                if ((int)item.LicenseType == intValueToCompare)
                {

                    result = _context.GuardLicenses
                    .Where(x => x.GuardId == guardId)
                    .Include(z => z.Guard)
                    .Select(x => new GuardLicense
                    {
                        Id = x.Id,
                        LicenseNo = x.LicenseNo,
                        LicenseType = x.LicenseType,
                        ExpiryDate = x.ExpiryDate,
                        Reminder1 = x.Reminder1,
                        Reminder2 = x.Reminder2,
                        FileName = x.FileName,
                        LicenseTypeName = x.LicenseTypeName,
                        GuardId = x.GuardId
                    })
                    .ToList();

                }

            }
            return result;



        }

        public List<GuardComplianceAndLicense> GetGuardLicensesandcompliance(int guardId)
        {
            // var LicenceType= _context.GuardLicenses.Where(x => x.GuardId == guardId).Select(x=>x.LicenseType).F
            var result = _context.GuardComplianceLicense
                 .Where(x => x.GuardId == guardId)
                 .Include(z => z.Guard).ToList();
            //GuardLicenseType? licenseType = null;
            // int intValueToCompare = 3;


            result = _context.GuardComplianceLicense
            .Where(x => x.GuardId == guardId)
            .Include(z => z.Guard)
            .Select(x => new GuardComplianceAndLicense
            {
                Id = x.Id,
                ExpiryDate = x.ExpiryDate,
                FileName = x.FileName,
                GuardId = x.GuardId,
                Description = x.Description,
                HrGroup = x.HrGroup,
                CurrentDateTime = x.CurrentDateTime,
                LicenseNo = x.Guard.SecurityNo,
                DateType = x.DateType,
            }).OrderBy(x => x.FileName)
            .ToList();


            return result;



        }
        public void SaveGuardLicense(GuardLicense guardLicense)
        {
            if (guardLicense.Id == 0)
            {
                guardLicense.Reminder1 = guardLicense.Reminder1 == null ? 45 : guardLicense.Reminder1;
                guardLicense.Reminder2 = guardLicense.Reminder2 == null ? 7 : guardLicense.Reminder2;
                _context.GuardLicenses.Add(guardLicense);
            }
            else
            {
                var guardLicenseToUpdate = _context.GuardLicenses.SingleOrDefault(x => x.Id == guardLicense.Id);
                if (guardLicenseToUpdate != null)
                {

                    guardLicenseToUpdate.LicenseNo = guardLicense.LicenseNo;
                    guardLicenseToUpdate.LicenseType = guardLicense.LicenseType;
                    guardLicenseToUpdate.Reminder1 = guardLicense.Reminder1 == null ? 45 : guardLicense.Reminder1;
                    guardLicenseToUpdate.Reminder2 = guardLicense.Reminder2 == null ? 7 : guardLicense.Reminder2;
                    guardLicenseToUpdate.ExpiryDate = guardLicense.ExpiryDate;
                    guardLicenseToUpdate.FileName = guardLicense.FileName;
                    guardLicenseToUpdate.LicenseTypeName = guardLicense.LicenseTypeName;
                }
            }
            _context.SaveChanges();
        }

        public void DeleteGuardLicense(int id)
        {
            var guardLicenseToDelete = _context.GuardComplianceLicense.SingleOrDefault(x => x.Id == id);
            if (guardLicenseToDelete == null)
                throw new InvalidOperationException();

            _context.Remove(guardLicenseToDelete);
            _context.SaveChanges();
        }

        public void SaveGuardCompliance(GuardCompliance guardCompliance)
        {
            if (guardCompliance.Id == 0)
            {
                guardCompliance.Reminder1 = guardCompliance.Reminder1 == null ? 45 : guardCompliance.Reminder1;
                guardCompliance.Reminder2 = guardCompliance.Reminder2 == null ? 7 : guardCompliance.Reminder2;
                _context.GuardCompliances.Add(guardCompliance);
            }
            else
            {
                var guardComplianceToUpdate = _context.GuardCompliances.SingleOrDefault(x => x.Id == guardCompliance.Id);
                if (guardComplianceToUpdate != null)
                {
                    guardComplianceToUpdate.ReferenceNo = guardCompliance.ReferenceNo;
                    guardComplianceToUpdate.Description = guardCompliance.Description;
                    guardComplianceToUpdate.Reminder1 = guardCompliance.Reminder1 == null ? 45 : guardCompliance.Reminder1;
                    guardComplianceToUpdate.Reminder2 = guardCompliance.Reminder2 == null ? 7 : guardCompliance.Reminder2;
                    guardComplianceToUpdate.ExpiryDate = guardCompliance.ExpiryDate;
                    guardComplianceToUpdate.FileName = guardCompliance.FileName;
                    guardComplianceToUpdate.HrGroup = guardCompliance.HrGroup;
                }
            }
            _context.SaveChanges();
        }
        public void SaveGuardComplianceandlicanse(GuardComplianceAndLicense guardComplianceandlicense)
        {
            if (guardComplianceandlicense.Id == 0)
            {

                _context.GuardComplianceLicense.Add(guardComplianceandlicense);
            }
            else
            {
                var guardComplianceToUpdate = _context.GuardComplianceLicense.SingleOrDefault(x => x.Id == guardComplianceandlicense.Id);
                if (guardComplianceToUpdate != null)
                {

                    guardComplianceToUpdate.Description = guardComplianceandlicense.Description;
                    guardComplianceToUpdate.CurrentDateTime = guardComplianceandlicense.CurrentDateTime;
                    guardComplianceToUpdate.ExpiryDate = guardComplianceandlicense.ExpiryDate;
                    guardComplianceToUpdate.FileName = guardComplianceandlicense.FileName;
                    guardComplianceToUpdate.HrGroup = guardComplianceandlicense.HrGroup;
                    guardComplianceToUpdate.DateType = guardComplianceandlicense.DateType;
                }
            }
            _context.SaveChanges();
        }
        public List<GuardCompliance> GetAllGuardCompliances()
        {
            return _context.GuardCompliances.Include(z => z.Guard).ToList();
        }

        public List<GuardCompliance> GetGuardCompliances(int guardId)
        {
            return _context.GuardCompliances
                .Include(z => z.Guard)
                .Where(x => x.GuardId == guardId && x.ExpiryDate != null)
                .OrderBy(x => x.ReferenceNo).ToList();
        }
        public List<GuardComplianceAndLicense> GetGuardCompliancesAndLicense(int guardId)
        {
            return _context.GuardComplianceLicense
                .Include(z => z.Guard)
                .Where(x => x.GuardId == guardId && x.ExpiryDate != null)
                .ToList();
        }
        public List<GuardComplianceAndLicense> GetGuardCompliancesAndLicenseList(string hrGroup)
        {
            var ddd = _context.GuardComplianceLicense
                .Include(z => z.Guard)
                .Where(x => x.ExpiryDate != null && x.HrGroupText == hrGroup)
                .ToList();
            return _context.GuardComplianceLicense
                .Include(z => z.Guard)
                .Where(x => x.ExpiryDate != null && x.HrGroupText == hrGroup)
                .ToList();
        }
        public List<GuardComplianceAndLicense> GetGuardCompliancesAndLicenseHR(int guardId, HrGroup hrGroup)
        {
            return _context.GuardComplianceLicense
                 .Include(z => z.Guard)
                 .Where(x => x.GuardId == guardId && x.HrGroup == hrGroup)
                 .ToList();
        }
        public List<HrSettings> GetHRDesc(int HRid)
        {
            var descriptions = _context.HrSettings.Include(z => z.HRGroups)
                .Include(z => z.ReferenceNoNumbers)
                .Include(z => z.ReferenceNoAlphabets)
                .OrderBy(x => x.HRGroups.Name).ThenBy(x => x.ReferenceNoNumbers.Name).
                ThenBy(x => x.ReferenceNoAlphabets.Name).Where(z => z.HRGroups.Id == HRid).ToList();
            return descriptions;
        }
        public GuardComplianceAndLicense GetDescriptionUsed(HrGroup hrGroup, string Description, int GuardID)
        {
            var valueReturn = _context.GuardComplianceLicense
          .Where(x => x.HrGroup == hrGroup && x.Description == Description && x.GuardId == GuardID)
          .FirstOrDefault();
            return valueReturn;
        }
        public GuardComplianceAndLicense GetDescriptionList(HrGroup hrGroup, string Description, int GuardID)
        {
            var guardAddedDoc = _context.GuardComplianceLicense
        .Where(x => x.HrGroup == hrGroup && x.GuardId == GuardID).ToList();

            var valueReturn = _context.GuardComplianceLicense
          .Where(x => x.HrGroup == hrGroup && x.Description == Description && x.GuardId == 0)
          .FirstOrDefault();

            if (guardAddedDoc != null)
            {
                foreach (var doc in guardAddedDoc)
                {
                    var s = doc.Description.Trim();
                    var firstSpaceIndex = s.IndexOf(' ');
                    if (firstSpaceIndex != -1)
                    {
                        var firstString = s.Substring(0, firstSpaceIndex); // INAGX4
                        var secondString = s.Substring(firstSpaceIndex + 1); // Agatti Island
                        if (Description.Trim() == secondString.Trim())
                        {
                            valueReturn = doc;


                        }
                    }


                }


            }


            return valueReturn;
            //    return _context.GuardComplianceLicense
            //.Where(x => x.HrGroup == hrGroup && x.Description== Description && x.GuardId==GuardID)
            //.FirstOrDefault();
        }
        public HrSettings GetHRRefernceNo(int HRid, string Description)
        {

            return _context.HrSettings.Include(z => z.HRGroups)
                .Include(z => z.ReferenceNoNumbers)
                .Include(z => z.ReferenceNoAlphabets)
                .OrderBy(x => x.HRGroups.Name).ThenBy(x => x.ReferenceNoNumbers.Name).
                ThenBy(x => x.ReferenceNoAlphabets.Name).Where(z => z.HRGroups.Id == HRid && z.Description == Description).FirstOrDefault();

        }
        public List<GuardCompliance> GetGuardCompliancesList(int[] guardIds)
        {
            return _context.GuardCompliances
        .Include(z => z.Guard)
        .Where(x => guardIds.Contains(x.GuardId))
        .OrderBy(x => x.ReferenceNo).ToList();
        }
        public GuardCompliance GetGuardCompliance(int id)
        {
            return _context.GuardCompliances
                .Include(z => z.Guard)
                .SingleOrDefault(x => x.Id == id);
        }

        public void DeleteGuardCompliance(int id)
        {
            var guardComplianceToDelete = _context.GuardCompliances.SingleOrDefault(x => x.Id == id);
            if (guardComplianceToDelete == null)
                throw new InvalidOperationException();

            _context.Remove(guardComplianceToDelete);
            _context.SaveChanges();
        }

        //To Get the Default Email Address start
        public string GetDefaultEmailAddress()
        {
            return _context.ReportTemplates.Select(x => x.DefaultEmail).FirstOrDefault();
        }
        //To Get the Default Email Address stop


        public DateTime? GetLogbookDateFromLogbook(int logbookId)
        {
            var lbdt = _context.ClientSiteLogBooks.Where(x => x.Id == logbookId).FirstOrDefault().Date;
            return lbdt;
        }
        //for toggle areas - start 
        public List<ClientSiteToggle> GetClientSiteToggle()
        {
            return _context.ClientSiteToggle.ToList();
        }
        public List<ClientSiteToggle> GetClientSiteToggle(int siteId)
        {
            var toggles = _context.ClientSiteToggle.Where(x => x.ClientSiteId == siteId).ToList();
            return toggles;
        }
        public List<ClientSiteToggle> GetClientSiteToggle(int siteId, int toggleId)
        {
            var toggles = _context.ClientSiteToggle.Where(x => x.ClientSiteId == siteId && x.ToggleTypeId == toggleId).ToList();
            return toggles;
        }
        //for toggle areas - end 
        //p1-191 hr files task 3-start
        public List<HRGroups> GetHRGroups()
        {
            return _context.HRGroups.ToList();
        }
        public List<ReferenceNoNumbers> GetReferenceNoNumbers()
        {
            return _context.ReferenceNoNumbers.ToList();
        }
        public List<ReferenceNoAlphabets> GetReferenceNoAlphabets()
        {
            return _context.ReferenceNoAlphabets.ToList();
        }
        //p1-191 hr files task 3-end
        public List<LicenseTypes> GetLicenseTypes()
        {
            return _context.LicenseTypes.ToList();
        }
        public DropboxDirectory GetDrobox()
        {
            return _context.DropboxDirectory.FirstOrDefault();
        }
        public List<HrSettings> GetHRDescFull()
        {
            var descriptions = _context.HrSettings.Include(z => z.HRGroups)
                .Include(z => z.ReferenceNoNumbers)
                .Include(z => z.ReferenceNoAlphabets)
                .OrderBy(x => x.HRGroups.Name).ThenBy(x => x.ReferenceNoNumbers.Name).
                ThenBy(x => x.ReferenceNoAlphabets.Name).ToList();
            return descriptions;
        }

        public void SaveRecordingFileDetails(AudioRecordingLog audioRecordingLog)
        {
            if (audioRecordingLog.Id == 0)
            {
                audioRecordingLog.UploadedDate = DateTime.Now;
                _context.AudioRecordingLog.Add(audioRecordingLog);
            }
            else
            {
                var audioRecordingLogToUpdate = _context.AudioRecordingLog.SingleOrDefault(x => x.Id == audioRecordingLog.Id);
                if (audioRecordingLogToUpdate != null)
                {
                    audioRecordingLogToUpdate.FileName = audioRecordingLog.FileName;
                    audioRecordingLogToUpdate.BlobUrl = audioRecordingLog.BlobUrl;
                }
            }
            _context.SaveChanges();

        }

        public List<HrSettingsLockedClientSites> GetHrDocumentLockDetailsForASite(int clientSiteId)
        {
            return _context.HrSettingsLockedClientSites
        .Where(x => x.ClientSite.Id == clientSiteId && x.HrSettings.HRLock == true) // Filter by ClientSite ID if needed
        .Include(x => x.ClientSite)
        .Include(x => x.HrSettings)
        .ToList();

        }


        public int SaveLanguageDetails(Guard guard)
        {
            var getGuardsLotes = _context.LanguageDetails.Where(x => x.GuardId == guard.Id).ToList();
            if (getGuardsLotes.Count() > 0)
            {
                DeleteGuardLotes(guard.Id);
            }
            LanguageDetails languageDetails = new LanguageDetails();
            foreach (var item in guard.LanguageDetails)
            {
                languageDetails.Id = 0;
                languageDetails.LanguageID = Convert.ToInt32(item);
                languageDetails.CreatedDate = DateTime.Now;
                languageDetails.GuardId = guard.Id;
                languageDetails.IsDeleted = false;
                _context.LanguageDetails.Add(languageDetails);
                _context.SaveChanges();
            }
            return guard.Id;
        }

        public void SaveGuardLotes(Guard guard)
        {

            var getGuardsLotes = _context.LanguageDetails.Where(x => x.GuardId == guard.Id).ToList();
            if (getGuardsLotes.Count() > 0)
            {
                DeleteGuardLotes(guard.Id);
            }
            LanguageDetails languageDetails = new LanguageDetails();
            foreach (var item in guard.LanguageDetails)
            {
                languageDetails.Id = 0;
                languageDetails.LanguageID = Convert.ToInt32(item);
                languageDetails.CreatedDate = DateTime.Now;
                languageDetails.GuardId = guard.Id;
                languageDetails.IsDeleted = false;
                _context.LanguageDetails.Add(languageDetails);
                _context.SaveChanges();
            }

        }
        public void DeleteGuardLotes(int guardid)
        {
            var guardLotesToDelete = _context.LanguageDetails.Where(x => x.GuardId == guardid).ToList();
            if (guardLotesToDelete == null)
                throw new InvalidOperationException();
            foreach (var item in guardLotesToDelete)
            {
                _context.Remove(item);
                _context.SaveChanges();
            }
        }

    }
}