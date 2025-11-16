using CityWatch.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Providers
{
    public interface IAppConfigurationProvider
    {
        void SaveConfiguration(AppConfiguration appConfiguration);
        AppConfiguration GetConfigurationByName(string name);
        List<AppConfiguration> GetConfigurations();
        MobileAppUpgrade GetLatestMobileAppVersion(string platformType);
        List<MobileAppUpgrade> GetAllMobileAppVersion();
        MobileAppUpgrade GetMobileAppVersionById(int Id);
        void SaveMobileAppUpgrade(MobileAppUpgrade mobileAppUpgrade);
        void DeleteMobileAppUpgrade(int id);
        void UpdateDownloadCount(int id);
        void RollBackToVersion(int recordId);
    }

    public class AppConfigurationProvider : IAppConfigurationProvider
    {
        private readonly CityWatchDbContext _context;

        public AppConfigurationProvider(CityWatchDbContext context)
        {
            _context = context;
        }

        public AppConfiguration GetConfigurationByName(string name)
        {
            return _context.Appconfigurations.SingleOrDefault(x => x.Name == name);
        }

        public List<AppConfiguration> GetConfigurations()
        {
            return _context.Appconfigurations.ToList();
        }

        public void SaveConfiguration(AppConfiguration appConfiguration)
        {
            if (appConfiguration == null)
                throw new ArgumentNullException();

            var appConfigurationToUpdate = _context.Appconfigurations.SingleOrDefault(x => x.Id == appConfiguration.Id);
            if (appConfigurationToUpdate != null)
            {
                appConfigurationToUpdate.Value = appConfiguration.Value;
                _context.SaveChanges();
            }
        }

        public MobileAppUpgrade GetLatestMobileAppVersion(string platformType)
        {
            return _context.MobileAppUpgrade
                .Where(x => x.AppType.ToLower().Equals(platformType.ToLower()))
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.AppVersionMajor).ThenByDescending(x => x.AppVersionMinor).ThenByDescending(x => x.AppVersionPatch)
                .FirstOrDefault();
        }

        public MobileAppUpgrade GetMobileAppVersionById(int Id)
        {
            return _context.MobileAppUpgrade.SingleOrDefault(x => x.Id == Id);
        }

        public List<MobileAppUpgrade> GetAllMobileAppVersion()
        {
            return _context.MobileAppUpgrade
                .OrderByDescending(x => x.AppType)
                .ThenByDescending(x => x.AppVersionMajor).ThenByDescending(x => x.AppVersionMinor).ThenByDescending(x => x.AppVersionPatch)
                .ToList();
        }
        public void SaveMobileAppUpgrade(MobileAppUpgrade mobileAppUpgrade)
        {
            if (mobileAppUpgrade == null)
                throw new ArgumentNullException(nameof(mobileAppUpgrade));

            if (mobileAppUpgrade.Id <= 0)
            {
                // Check if record for same version already exists
                var existingRecord = _context.MobileAppUpgrade
                    .FirstOrDefault(x => x.AppType == mobileAppUpgrade.AppType &&
                                         x.AppVersionMajor == mobileAppUpgrade.AppVersionMajor &&
                                         x.AppVersionMinor == mobileAppUpgrade.AppVersionMinor &&
                                         x.AppVersionPatch == mobileAppUpgrade.AppVersionPatch);

                if (existingRecord != null)
                {
                    throw new InvalidOperationException("A mobile app upgrade record for the same version already exists.");
                }

                // Check if new version is greater than existing active version
                var latestVersion = GetLatestMobileAppVersion(mobileAppUpgrade.AppType);
                if (latestVersion != null)
                {
                    if (mobileAppUpgrade.AppVersionMajor < latestVersion.AppVersionMajor ||
                        (mobileAppUpgrade.AppVersionMajor == latestVersion.AppVersionMajor && mobileAppUpgrade.AppVersionMinor < latestVersion.AppVersionMinor) ||
                        (mobileAppUpgrade.AppVersionMajor == latestVersion.AppVersionMajor && mobileAppUpgrade.AppVersionMinor == latestVersion.AppVersionMinor && mobileAppUpgrade.AppVersionPatch <= latestVersion.AppVersionPatch))
                    {
                        throw new InvalidOperationException("The new version must be greater than the existing active version.");
                    }
                }

                mobileAppUpgrade.RecordCreateDTM = DateTime.Now;
                mobileAppUpgrade.IsActive = true;
                _context.Add(mobileAppUpgrade);

                var allExistingRecord = _context.MobileAppUpgrade.Where(x => x.AppType == mobileAppUpgrade.AppType && x.IsActive).ToList();
                if (allExistingRecord != null)
                {
                    foreach (var record in allExistingRecord)
                    {
                        record.IsActive = false;
                    }
                }

                _context.SaveChanges();
            }            
        }
        public void DeleteMobileAppUpgrade(int id)
        {
            var record = _context.MobileAppUpgrade.SingleOrDefault(x => x.Id == id);
            if (record != null)
            {
                if (record.IsActive) { 
                    throw new InvalidOperationException("Cannot delete an active mobile app record.");
                }
                
                _context.MobileAppUpgrade.Remove(record);
                _context.SaveChanges();

            }
        }

        public void UpdateDownloadCount(int id)
        {
            var record = _context.MobileAppUpgrade.SingleOrDefault(x => x.Id == id);
            if (record != null)
            {
                record.TotalDownloadCount += 1;
                _context.SaveChanges();
            }
        }

        public void RollBackToVersion(int recordId)
        {
            var record = _context.MobileAppUpgrade.SingleOrDefault(x => x.Id == recordId);
            if (record != null)
            {
                var allExistingRecord = _context.MobileAppUpgrade.Where(x => x.AppType == record.AppType && x.IsActive).ToList();
                if (allExistingRecord != null)
                {
                    foreach (var Activerecord in allExistingRecord)
                    {
                        Activerecord.IsActive = false;
                    }
                }

                record.IsActive = true;
                _context.SaveChanges();
            }
            else
            {
                throw new InvalidOperationException("Record not found for rollback.");
            }
        }
    }
}
