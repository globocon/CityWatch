using CityWatch.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CityWatch.Data.Providers.AppConfigurationProvider;
using static Dropbox.Api.Paper.UserOnPaperDocFilter;

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
        public PcarRouteResult GetPcarDetails(string mobiledevId, DateTime? targetDate = null);
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

        public class VisitDto
        {
            public string VisitName { get; set; }
            public int VisitNumber { get; set; }

            // Visit already saved today?
            public bool IsCheckedToday { get; set; }

            // These two values MUST be returned from API for the popup
            public string SavedTimeOnSite { get; set; }
            public string SavedTimeOffSite { get; set; }

            // Optional: To disable modification in MAUI cleanly
            public bool IsReadOnly => IsCheckedToday;
            public Enums.PcarVisitStatusEnum? Status { get; set; }
            public int? PushedTo { get; set; }
        }

        public class PcarRouteResponse
        {
            public int SmartWandId { get; set; }   // New
            public int? PatrolCarId { get; set; }  // New
            public int SiteId { get; set; }        // New
            public string DayName { get; set; }    // New
            public int PcarRouteId { get; set; }           // New
            public int PcarRouteDetailsId { get; set; }    // New
            public string SiteName { get; set; }
            public string Address { get; set; }
            public string GPSLocation { get; set; }  // ADD THIS
            public int VisitCount { get; set; }
            public List<VisitDto> Visits { get; set; }
        }

        public class PcarRouteResult
        {
           
            public bool Success { get; set; }
            public string Message { get; set; }
            public List<PcarRouteResponse> Data { get; set; }
        }


        public PcarRouteResult GetPcarDetails(string mobiledevId, DateTime? targetDate = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(mobiledevId))
                    return new PcarRouteResult { Success = false, Message = "Device ID is required" };

                var smartWand = _context.ClientSiteSmartWands
                    .FirstOrDefault(x => x.DeviceId != null &&
                                         x.DeviceId.Trim().ToLower() == mobiledevId.Trim().ToLower());

                if (smartWand == null)
                    return new PcarRouteResult { Success = false, Message = "SmartWand not found for this device" };

                var route = _context.PcarRoute
                    .Include(r => r.RouteDetails)
                    .ThenInclude(d => d.ClientSite)
                    .Include(r => r.SmartWand)
                    .FirstOrDefault(r => r.Smartwandallocation == smartWand.Id);

                if (route == null)
                    return new PcarRouteResult { Success = false, Message = "Route not found for this device" };

                var dateToUse = targetDate ?? DateTime.Today;
                string dayName = dateToUse.DayOfWeek.ToString().Substring(0, 3);

                // Load saved visits for targetDate
                var savedVisits = _context.PcarRouteDailyVisits
                    .Where(v => v.SmartWandId == smartWand.Id && v.CreatedAt.Date == dateToUse.Date)
                    .Select(v => new
                    {
                        v.SiteId,
                        v.VisitName,
                        v.TimeOn,
                        v.TimeOff,
                        v.Status,
                        v.PushedTo
                    })
                    .ToList();

                // Load pushed tasks to this route (via its PatrolCarId)
                int? currentPatrolCarId = route.SmartWand?.PatrolCarId;
                var pushedTasks = new List<PcarRouteDailyVisits>();
                if (currentPatrolCarId.HasValue)
                {
                    var yesterday = DateTime.Today.AddDays(-1);
                    var tomorrow = DateTime.Today.AddDays(1);
                    pushedTasks = _context.PcarRouteDailyVisits
                        .Where(v => v.PushedTo == currentPatrolCarId.Value && 
                                    v.CreatedAt.Date >= yesterday && 
                                    v.CreatedAt.Date <= tomorrow)
                        .ToList();
                }

                var response = route.RouteDetails.Select(rd =>
                {
                    int visitCount = dateToUse.DayOfWeek switch
                    {
                        DayOfWeek.Monday => rd.VisitMon,
                        DayOfWeek.Tuesday => rd.VisitTue,
                        DayOfWeek.Wednesday => rd.VisitWed,
                        DayOfWeek.Thursday => rd.VisitThu,
                        DayOfWeek.Friday => rd.VisitFri,
                        DayOfWeek.Saturday => rd.VisitSat,
                        DayOfWeek.Sunday => rd.VisitSun,
                        _ => 0
                    };

                    var visitsList = Enumerable.Range(1, visitCount)
                        .Select(i =>
                        {
                            var visitName = $"Visit {i}";

                            // Find matching saved visit
                            var saved = savedVisits
                                .FirstOrDefault(sv =>
                                    sv.SiteId == rd.ClientSite.Id &&
                                    sv.VisitName == visitName);

                            return new VisitDto
                            {
                                VisitName = visitName,
                                VisitNumber = i,
                                IsCheckedToday = saved != null,
                                SavedTimeOnSite = saved?.TimeOn,
                                SavedTimeOffSite = saved?.TimeOff,
                                Status = saved?.Status,
                                PushedTo = saved?.PushedTo
                            };
                        })
                        .ToList();

                    // Append pushed visits for this site
                    var sitePushed = pushedTasks.Where(p => p.SiteId == rd.ClientSite.Id).ToList();
                    foreach (var pt in sitePushed)
                    {
                        var pushedVisitName = $"{pt.VisitName} (Pushed)";
                        var saved = savedVisits.FirstOrDefault(sv => sv.SiteId == rd.ClientSite.Id && sv.VisitName == pushedVisitName);

                        visitsList.Add(new VisitDto
                        {
                            VisitName = pushedVisitName,
                            VisitNumber = pt.VisitNumber,
                            IsCheckedToday = saved != null,
                            SavedTimeOnSite = saved?.TimeOn,
                            SavedTimeOffSite = saved?.TimeOff,
                            Status = saved != null ? Enums.PcarVisitStatusEnum.Completed : (Enums.PcarVisitStatusEnum?)null
                        });
                    }

                    return new PcarRouteResponse
                    {
                        SmartWandId = smartWand.Id,
                        PatrolCarId = currentPatrolCarId,
                        SiteId = rd.ClientSite.Id,
                        PcarRouteId = route.Id,
                        PcarRouteDetailsId = rd.Id,
                        DayName = dayName,
                        SiteName = rd.ClientSite.Name,
                        Address = rd.ClientSite.Address,
                        GPSLocation = rd.ClientSite.Gps,
                        VisitCount = visitsList.Count,
                        Visits = visitsList
                    };
                }).ToList();

                // Handle pushed tasks for sites that are NOT in this route's route details
                var existingSiteIds = response.Select(r => r.SiteId).ToHashSet();
                var extraPushedTasks = pushedTasks.Where(p => !existingSiteIds.Contains(p.SiteId)).ToList();

                if (extraPushedTasks.Count > 0)
                {
                    var extraSitesGrouped = extraPushedTasks.GroupBy(p => p.SiteId);
                    foreach (var group in extraSitesGrouped)
                    {
                        var siteId = group.Key;
                        var site = _context.ClientSites.FirstOrDefault(s => s.Id == siteId);
                        if (site != null)
                        {
                            var visitsList = new List<VisitDto>();
                            foreach (var pt in group)
                            {
                                var pushedVisitName = $"{pt.VisitName} (Pushed)";
                                var saved = savedVisits.FirstOrDefault(sv => sv.SiteId == siteId && sv.VisitName == pushedVisitName);

                                visitsList.Add(new VisitDto
                                {
                                    VisitName = pushedVisitName,
                                    VisitNumber = pt.VisitNumber,
                                    IsCheckedToday = saved != null,
                                    SavedTimeOnSite = saved?.TimeOn,
                                    SavedTimeOffSite = saved?.TimeOff,
                                    Status = saved != null ? Enums.PcarVisitStatusEnum.Completed : (Enums.PcarVisitStatusEnum?)null
                                });
                            }

                            response.Add(new PcarRouteResponse
                            {
                                SmartWandId = smartWand.Id,
                                PatrolCarId = currentPatrolCarId,
                                SiteId = siteId,
                                PcarRouteId = route.Id,
                                PcarRouteDetailsId = 0, // No details record for pushed extra site
                                DayName = dayName,
                                SiteName = site.Name,
                                Address = site.Address,
                                GPSLocation = site.Gps,
                                VisitCount = visitsList.Count,
                                Visits = visitsList
                            });
                        }
                    }
                }

                return new PcarRouteResult
                {
                    Success = true,
                    Message = "Success",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new PcarRouteResult
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }



    }
}
