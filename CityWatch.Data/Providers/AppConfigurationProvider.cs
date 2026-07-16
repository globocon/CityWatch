using CityWatch.Data.Enums;
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
        public PcarRouteResult GetPcarDetails(string mobiledevId, DateTime targetDate);
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
                if (record.IsActive)
                {
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


        //public PcarRouteResult GetPcarDetailsOld(string mobiledevId, DateTime targetDate)
        //{
        //    var result = new PcarRouteResult();

        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(mobiledevId))
        //            return new PcarRouteResult { Success = false, Message = "Device ID is required" };

        //        var smartWand = _context.ClientSiteSmartWands
        //            .FirstOrDefault(x => x.DeviceId != null &&
        //                                 x.DeviceId.Trim().ToLower() == mobiledevId.Trim().ToLower());

        //        if (smartWand == null)
        //            return new PcarRouteResult { Success = false, Message = "SmartWand not found for this device" };


        //        var routes = _context.PcarRoute
        //            .AsNoTracking()
        //            .Include(r => r.RouteDetails)
        //            .ThenInclude(d => d.ClientSite)
        //            .Include(r => r.SmartWand)
        //            .Where(r => r.Smartwandallocation == smartWand.Id).ToList();

        //        var visitDate = targetDate.Date;
        //        string dayName = visitDate.DayOfWeek.ToString().Substring(0, 3);

        //        foreach (var _route in routes)
        //        {
        //            var existingVisits = _context.PcarRouteDailyVisits
        //                .Where(v =>
        //                    v.SmartWandId == smartWand.Id &&
        //                    v.PcarRouteId == _route.Id &&
        //                    v.VisitDate == visitDate)
        //                .ToList();

        //            foreach (var detail in _route.RouteDetails.OrderBy(x => x.OrderNo))
        //            {
        //                var (visitCount, startTime, endTime) = GetVisitInfo(detail, visitDate.DayOfWeek);

        //                if (visitCount <= 0)
        //                    continue;

        //                for (int visitNo = 1; visitNo <= visitCount; visitNo++)
        //                {
        //                    bool exists = existingVisits.Any(v => v.PcarRouteDetailsId == detail.Id && v.VisitNumber == visitNo);

        //                    if (exists)
        //                        continue;

        //                    var visit = new PcarRouteDailyVisits
        //                    {
        //                        SmartWandId = smartWand.Id,
        //                        SiteId = detail.ClientSiteId,
        //                        VisitName = $"Visit {visitNo}",
        //                        VisitNumber = visitNo,
        //                        DayName = dayName,
        //                        PcarRouteId = _route.Id,
        //                        PcarRouteDetailsId = detail.Id,
        //                        CreatedAt = DateTime.Now,
        //                        VisitDate = visitDate,
        //                        Status = PcarVisitStatusEnum.Assigned,
        //                        IsVisitPickedUp = false,
        //                        GuardId = null,
        //                        LoginUserId = null,
        //                        LoginSiteId = null,
        //                        GpsCoordinates = null,
        //                        ParentVisitId = null,
        //                        TimeOn = null,
        //                        TimeOff = null
        //                    };

        //                    _context.PcarRouteDailyVisits.Add(visit);
        //                }
        //            }
        //        }

        //        _context.SaveChanges();

        //        //========


        //        var route = _context.PcarRoute
        //             .AsNoTracking()
        //             .Include(r => r.RouteDetails)
        //             .ThenInclude(d => d.ClientSite)
        //             .Include(r => r.SmartWand)
        //             .Where(r => r.Smartwandallocation == smartWand.Id).ToList();


        //        if (route == null)
        //            return new PcarRouteResult { Success = false, Message = "No Pcar route found for this device" };

        //        // Load B's (current smartwand's) own saved visits for targetDate
        //        var savedVisits = _context.PcarRouteDailyVisits
        //            .Where(v => v.SmartWandId == smartWand.Id && v.VisitDate == visitDate)
        //            .Select(v => new
        //            {
        //                v.Id,
        //                v.SiteId,
        //                v.VisitName,
        //                v.TimeOn,
        //                v.TimeOff,
        //                v.Status,
        //                v.ParentVisitId,
        //                v.IsVisitPickedUp
        //            })
        //            .ToList();

        //        // Find all OTHER smartwands at the same client site
        //        var otherSmartWandIds = _context.ClientSiteSmartWands
        //            .Where(sw => sw.ClientSiteId == smartWand.ClientSiteId && sw.Id != smartWand.Id && !sw.IsDeleted)
        //            .Select(sw => sw.Id)
        //            .ToList();

        //        //// Load cancelled visits from other smartwands in the group within yesterday, today, and tomorrow
        //        //var yesterday = dateToUse.Date.AddDays(-1);
        //        //var tomorrow = dateToUse.Date.AddDays(1);
        //        //var cancelledVisits = _context.PcarRouteDailyVisits
        //        //    .Where(v => otherSmartWandIds.Contains(v.SmartWandId) &&
        //        //                v.Status == Enums.PcarVisitStatusEnum.CancelledOrDelegated &&
        //        //                v.VisitDate >= yesterday &&
        //        //                v.VisitDate <= tomorrow)
        //        //    .ToList();

        //        // Load cancelled visits from other smartwands in the group within today.


        //        var cancelledVisits = _context.PcarRouteDailyVisits
        //            .Where(v => otherSmartWandIds.Contains(v.SmartWandId) &&
        //                        v.Status == Enums.PcarVisitStatusEnum.CancelledOrDelegated &&
        //                        v.VisitDate == visitDate)
        //            .ToList();

        //        // Find which of these cancelled visits have been accepted/handled (by anyone)
        //        var cancelledIds = cancelledVisits.Select(cv => cv.Id).ToList();
        //        var acceptances = _context.PcarRouteDailyVisits
        //            .Where(v => v.ParentVisitId.HasValue && cancelledIds.Contains(v.ParentVisitId.Value))
        //            .ToList();

        //        var response = route.RouteDetails.Select(rd =>
        //        {
        //            int visitCount = visitDate.DayOfWeek switch
        //            {
        //                DayOfWeek.Monday => rd.VisitMon,
        //                DayOfWeek.Tuesday => rd.VisitTue,
        //                DayOfWeek.Wednesday => rd.VisitWed,
        //                DayOfWeek.Thursday => rd.VisitThu,
        //                DayOfWeek.Friday => rd.VisitFri,
        //                DayOfWeek.Saturday => rd.VisitSat,
        //                DayOfWeek.Sunday => rd.VisitSun,
        //                _ => 0
        //            };

        //            var visitsList = new List<VisitDto>();

        //            // 1. Populate normal scheduled visits
        //            for (int i = 1; i <= visitCount; i++)
        //            {
        //                var visitName = $"Visit {i}";
        //                var saved = savedVisits
        //                    .Where(sv => sv.SiteId == rd.ClientSite.Id && sv.VisitName == visitName)
        //                    .OrderByDescending(sv => sv.Id)
        //                    .FirstOrDefault();

        //                visitsList.Add(new VisitDto
        //                {
        //                    VisitName = visitName,
        //                    VisitNumber = i,
        //                    IsCheckedToday = saved != null,
        //                    SavedTimeOnSite = saved?.TimeOn,
        //                    SavedTimeOffSite = saved?.TimeOff,
        //                    Status = saved?.Status,
        //                    ParentVisitId = saved?.ParentVisitId
        //                });
        //            }

        //            // 2. Append uncompleted cancelled tasks for this site that belong to the group
        //            var siteCancelled = cancelledVisits.Where(c => c.SiteId == rd.ClientSite.Id).ToList();
        //            foreach (var cv in siteCancelled)
        //            {
        //                // Check if anyone has accepted this cancelled task
        //                var acceptedByAnyone = acceptances.FirstOrDefault(a => a.ParentVisitId == cv.Id && a.Status != Enums.PcarVisitStatusEnum.CancelledOrDelegated);
        //                if (acceptedByAnyone == null)
        //                {
        //                    // Nobody has accepted it yet -> Show it as orange (pending acceptance) to this PCAR
        //                    var pushedVisitName = $"{cv.VisitName} (Group-Cancelled)";
        //                    visitsList.Add(new VisitDto
        //                    {
        //                        VisitName = pushedVisitName,
        //                        VisitNumber = cv.VisitNumber,
        //                        IsCheckedToday = false,
        //                        SavedTimeOnSite = null,
        //                        SavedTimeOffSite = null,
        //                        Status = null,
        //                        ParentVisitId = cv.Id // Link to parent cancelled task!
        //                    });
        //                }
        //                else if (acceptedByAnyone.SmartWandId == smartWand.Id)
        //                {
        //                    // This PCAR is the one who accepted it -> Show it as accepted/completed
        //                    var pushedVisitName = $"{cv.VisitName} (Group-Cancelled)";
        //                    visitsList.Add(new VisitDto
        //                    {
        //                        VisitName = pushedVisitName,
        //                        VisitNumber = cv.VisitNumber,
        //                        IsCheckedToday = true,
        //                        SavedTimeOnSite = acceptedByAnyone.TimeOn,
        //                        SavedTimeOffSite = acceptedByAnyone.TimeOff,
        //                        Status = acceptedByAnyone.Status,
        //                        ParentVisitId = cv.Id
        //                    });
        //                }
        //                // If accepted by someone else, we hide it!
        //            }

        //            return new PcarRouteResponse
        //            {
        //                SmartWandId = smartWand.Id,
        //                PatrolCarId = smartWand.PatrolCarId,
        //                SiteId = rd.ClientSite.Id,
        //                PcarRouteId = route.Id,
        //                PcarRouteDetailsId = rd.Id,
        //                DayName = dayName,
        //                SiteName = rd.ClientSite.Name,
        //                Address = rd.ClientSite.Address,
        //                GPSLocation = rd.ClientSite.Gps,
        //                VisitCount = visitsList.Count,
        //                Visits = visitsList
        //            };
        //        }).ToList();

        //        // 3. Handle cancelled tasks for sites that are NOT in this route's scheduled details
        //        var existingSiteIds = response.Select(r => r.SiteId).ToHashSet();
        //        var extraCancelledTasks = cancelledVisits.Where(cv => !existingSiteIds.Contains(cv.SiteId)).ToList();

        //        if (extraCancelledTasks.Count > 0)
        //        {
        //            var extraSitesGrouped = extraCancelledTasks.GroupBy(cv => cv.SiteId);
        //            foreach (var group in extraSitesGrouped)
        //            {
        //                var siteId = group.Key;
        //                var site = _context.ClientSites.FirstOrDefault(s => s.Id == siteId);
        //                if (site != null)
        //                {
        //                    var visitsList = new List<VisitDto>();
        //                    foreach (var cv in group)
        //                    {
        //                        var acceptedByAnyone = acceptances.FirstOrDefault(a => a.ParentVisitId == cv.Id && a.Status != Enums.PcarVisitStatusEnum.CancelledOrDelegated);
        //                        if (acceptedByAnyone == null)
        //                        {
        //                            var pushedVisitName = $"{cv.VisitName} (Group-Cancelled)";
        //                            visitsList.Add(new VisitDto
        //                            {
        //                                VisitName = pushedVisitName,
        //                                VisitNumber = cv.VisitNumber,
        //                                IsCheckedToday = false,
        //                                SavedTimeOnSite = null,
        //                                SavedTimeOffSite = null,
        //                                Status = null,
        //                                PushedTo = null,
        //                                ParentVisitId = cv.Id
        //                            });
        //                        }
        //                        else if (acceptedByAnyone.SmartWandId == smartWand.Id)
        //                        {
        //                            var pushedVisitName = $"{cv.VisitName} (Group-Cancelled)";
        //                            visitsList.Add(new VisitDto
        //                            {
        //                                VisitName = pushedVisitName,
        //                                VisitNumber = cv.VisitNumber,
        //                                IsCheckedToday = true,
        //                                SavedTimeOnSite = acceptedByAnyone.TimeOn,
        //                                SavedTimeOffSite = acceptedByAnyone.TimeOff,
        //                                Status = acceptedByAnyone.Status,
        //                                ParentVisitId = cv.Id
        //                            });
        //                        }
        //                    }

        //                    if (visitsList.Count > 0)
        //                    {
        //                        response.Add(new PcarRouteResponse
        //                        {
        //                            SmartWandId = smartWand.Id,
        //                            PatrolCarId = smartWand.PatrolCarId,
        //                            SiteId = siteId,
        //                            PcarRouteId = route.Id,
        //                            PcarRouteDetailsId = 0,
        //                            DayName = dayName,
        //                            SiteName = site.Name,
        //                            Address = site.Address,
        //                            GPSLocation = site.Gps,
        //                            VisitCount = visitsList.Count,
        //                            Visits = visitsList
        //                        });
        //                    }
        //                }
        //            }
        //        }

        //        return new PcarRouteResult { Success = true, Message = "Success", Data = response };
        //    }
        //    catch (Exception ex)
        //    {
        //        return new PcarRouteResult { Success = false, Message = ex.Message };
        //    }
        //}



        public PcarRouteResult GetPcarDetails(string mobiledevId, DateTime targetDate)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(mobiledevId))
                {
                    return new PcarRouteResult
                    {
                        Success = false,
                        Message = "Device ID is required"
                    };
                }

                var smartWand = _context.ClientSiteSmartWands
                    .FirstOrDefault(x =>
                        x.DeviceId != null &&
                        x.DeviceId.Trim().ToLower() == mobiledevId.Trim().ToLower());

                if (smartWand == null)
                {
                    return new PcarRouteResult
                    {
                        Success = false,
                        Message = "SmartWand not found."
                    };
                }

                var visitDate = targetDate.Date;
                var dayName = visitDate.ToString("ddd");

                var routes = _context.PcarRoute
                    .AsNoTracking()
                    .Include(r => r.RouteDetails)
                        .ThenInclude(d => d.ClientSite)
                    .Include(r => r.SmartWand)
                    .Where(r => r.Smartwandallocation == smartWand.Id)
                    .ToList();

                if (!routes.Any())
                {
                    return new PcarRouteResult
                    {
                        Success = false,
                        Message = "No route assigned."
                    };
                }

                // Ensure today's scheduled visits exist
                EnsureDailyVisits(routes, smartWand, visitDate);

                // -----------------------------------------------------------------
                // 1. Scheduled Visits
                // Load all today's visits for this SmartWand
                // -----------------------------------------------------------------

                var savedVisits = LoadSavedVisits(smartWand.Id, visitDate);

                // Other SmartWands in same group
                var otherSmartWandIds = _context.ClientSiteSmartWands
                    .Where(x => x.ClientSiteId == smartWand.ClientSiteId && x.Id != smartWand.Id && !x.IsDeleted)
                    .Select(x => x.Id)
                    .ToList();

                // -----------------------------------------------------------------
                // 1. Scheduled Visits
                // Load all today's cancelled visits by other SmartWands of same smartwand site
                // -----------------------------------------------------------------
                var cancelledVisits = LoadCancelledVisits(otherSmartWandIds, visitDate);

                var allclientSites = _context.ClientSites
                    .AsNoTracking()
                    .Where(x => x.IsActive == true)
                    .ToList();

                var response = BuildRouteResponse(
                    routes,
                    smartWand,
                    visitDate,
                    dayName,
                    savedVisits,
                    cancelledVisits,
                    allclientSites);

                var existingSiteIds = response.Select(x => x.SiteId).ToHashSet();

                var extraSiteIds = cancelledVisits.Where(x => !existingSiteIds.Contains(x.SiteId)).Select(x => x.SiteId).Distinct().ToList();

                var clientSites = _context.ClientSites
                    .AsNoTracking()
                    .Where(x => extraSiteIds.Contains(x.Id))
                    .ToDictionary(x => x.Id);

                AddExtraCancelledSites(response, smartWand, dayName, cancelledVisits, clientSites);

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
                    Message = ex.Message
                };
            }
        }

        private void EnsureDailyVisits(List<PcarRoute> routes, ClientSiteSmartWand smartWand, DateTime visitDate)
        {
            string dayName = visitDate.ToString("ddd");

            // Load all existing visits for this SmartWand/date in ONE query
            var existingVisitKeys = _context.PcarRouteDailyVisits
                .Where(v =>
                    v.SmartWandId == smartWand.Id &&
                    v.VisitDate == visitDate)
                .Select(v => new
                {
                    v.PcarRouteId,
                    v.PcarRouteDetailsId,
                    v.VisitNumber
                })
                .AsEnumerable()
                .Select(v => $"{v.PcarRouteId}_{v.PcarRouteDetailsId}_{v.VisitNumber}")
                .ToHashSet();

            var visitsToAdd = new List<PcarRouteDailyVisits>();

            foreach (var route in routes)
            {
                foreach (var detail in route.RouteDetails.OrderBy(x => x.OrderNo))
                {
                    var (visitCount, _, _) = GetVisitInfo(detail, visitDate.DayOfWeek);

                    if (visitCount <= 0)
                        continue;

                    for (int visitNo = 1; visitNo <= visitCount; visitNo++)
                    {
                        string key = $"{route.Id}_{detail.Id}_{visitNo}";

                        if (existingVisitKeys.Contains(key))
                            continue;

                        visitsToAdd.Add(new PcarRouteDailyVisits
                        {
                            SmartWandId = smartWand.Id,
                            SiteId = detail.ClientSiteId,

                            GuardId = null,
                            LoginUserId = null,
                            LoginSiteId = null,

                            VisitName = $"Visit {visitNo}",
                            VisitNumber = visitNo,
                            DayName = dayName,

                            PcarRouteId = route.Id,
                            PcarRouteDetailsId = detail.Id,

                            TimeOn = null,
                            TimeOff = null,
                            GpsCoordinates = null,

                            VisitDate = visitDate,
                            CreatedAt = DateTime.Now,

                            Status = PcarVisitStatusEnum.Assigned,
                            ParentVisitId = null,
                            IsVisitPickedUp = false
                        });

                        // Prevent duplicate inserts if the same key appears again
                        existingVisitKeys.Add(key);
                    }
                }
            }

            if (visitsToAdd.Count > 0)
            {
                _context.PcarRouteDailyVisits.AddRange(visitsToAdd);
                _context.SaveChanges();
            }
        }

        private List<VisitDto> LoadSavedVisits(int smartWandId, DateTime visitDate)
        {
            var sv = _context.PcarRouteDailyVisits
                .AsNoTracking()
                .Where(v => v.SmartWandId == smartWandId && v.VisitDate == visitDate)
                .Select(v => new VisitDto
                {
                    VisitId = v.Id,
                    VisitName = v.VisitName,
                    VisitNumber = v.VisitNumber,
                    VisitDate = v.VisitDate,
                    SiteId = v.SiteId,
                    IsCheckedToday = true,
                    SavedTimeOnSite = v.TimeOn,
                    SavedTimeOffSite = v.TimeOff,
                    Status = v.Status,
                    ParentVisitId = v.ParentVisitId,
                    PcarRouteId = v.PcarRouteId,
                    PcarRouteDetailsId = v.PcarRouteDetailsId
                }).ToList();

            return sv;
        }

        private List<VisitDto> LoadCancelledVisits(List<int> otherSmartWandIds, DateTime visitDate)
        {
            var cv = _context.PcarRouteDailyVisits
                .AsNoTracking()
                .Where(v =>
                    otherSmartWandIds.Contains(v.SmartWandId) && v.Status == PcarVisitStatusEnum.CancelledOrDelegated &&
                    !v.IsVisitPickedUp && v.VisitDate == visitDate)
                .OrderBy(v => v.SiteId)
                .ThenBy(v => v.VisitNumber)
                .Select(v => new VisitDto
                {
                    VisitId = 0,
                    VisitName = v.VisitName,
                    VisitNumber = v.VisitNumber,
                    VisitDate = v.VisitDate,
                    SiteId = v.SiteId,
                    IsCheckedToday = true,
                    SavedTimeOnSite = v.TimeOn,
                    SavedTimeOffSite = v.TimeOff,
                    Status = PcarVisitStatusEnum.Assigned,
                    ParentVisitId = v.Id,
                    PcarRouteId = v.PcarRouteId,
                    PcarRouteDetailsId = v.PcarRouteDetailsId
                }).ToList();

            return cv;
        }


        private List<PcarRouteResponse> BuildRouteResponse(List<PcarRoute> routes, ClientSiteSmartWand smartWand, DateTime visitDate, string dayName,
                                                            List<VisitDto> savedVisits, List<VisitDto> cancelledVisits,List<ClientSite> allclientsites)
        {
            var response = new List<PcarRouteResponse>();
            var routeSiteIds = routes.SelectMany(r => r.RouteDetails).Select(d => d.ClientSiteId).Distinct().ToHashSet();

            foreach (var route in routes)
            {
                foreach (var detail in route.RouteDetails.OrderBy(x => x.OrderNo))
                {
                    var (visitCount, _, _) = GetVisitInfo(detail, visitDate.DayOfWeek);

                    if (visitCount > 0)
                    {
                        var siteSavedVisits = savedVisits
                            .Where(x => x.SiteId == detail.ClientSiteId)
                            .ToList();

                        var siteCancelled = cancelledVisits
                            .Where(x => x.SiteId == detail.ClientSiteId)
                            .ToList();

                        var visits = BuildVisits(
                            detail,
                            visitCount,
                            siteSavedVisits,
                            siteCancelled);

                        response.Add(new PcarRouteResponse
                        {
                            SmartWandId = smartWand.Id,
                            PatrolCarId = smartWand.PatrolCarId,
                            SiteId = detail.ClientSiteId,

                            PcarRouteId = route.Id,
                            PcarRouteDetailsId = detail.Id,

                            DayName = dayName,

                            SiteName = detail.ClientSite.Name,
                            Address = detail.ClientSite.Address,
                            GPSLocation = detail.ClientSite.Gps,

                            VisitCount = visits.Count,
                            Visits = visits
                        });
                    }
                }
            }

            // Find visits that are not part of the current route
            var visitsNotInRoute = savedVisits
                .Where(x => !routeSiteIds.Contains(x.SiteId))
                .GroupBy(x => new
                {
                    x.SiteId,
                    x.PcarRouteId,
                    x.PcarRouteDetailsId
                });

            foreach (var group in visitsNotInRoute)
            {
                var clientSite = allclientsites
                    .FirstOrDefault(x => x.Id == group.Key.SiteId);

                if (clientSite == null)
                    continue;

                var visits = group
                    .OrderBy(x => x.VisitNumber)
                    .ToList();

                if(visits.Count > 0)
                {
                    response.Add(new PcarRouteResponse
                    {
                        SmartWandId = smartWand.Id,
                        PatrolCarId = smartWand.PatrolCarId,

                        SiteId = group.Key.SiteId,
                        PcarRouteId = group.Key.PcarRouteId,
                        PcarRouteDetailsId = group.Key.PcarRouteDetailsId,

                        DayName = dayName,

                        SiteName = clientSite.Name,
                        Address = clientSite.Address,
                        GPSLocation = clientSite.Gps,

                        VisitCount = visits.Count,
                        Visits = visits
                    });
                }
                
            }

            return response;
        }

        private List<VisitDto> BuildVisits(PcarRouteDetails routeDetail, int visitCount, List<VisitDto> savedVisits, List<VisitDto> cancelledVisits)
        {
            var visits = new List<VisitDto>();
            visits.AddRange(savedVisits);
            visits.AddRange(cancelledVisits);
            return visits;
        }


        private void AddExtraCancelledSites(List<PcarRouteResponse> response, ClientSiteSmartWand smartWand, string dayName,
                                            List<VisitDto> cancelledVisits, Dictionary<int, ClientSite> clientSites)
        {
            var existingSites = response.Select(x => x.SiteId).ToHashSet();

            foreach (var siteId in cancelledVisits.Select(x => x.SiteId).Distinct())
            {
                if (existingSites.Contains(siteId))
                    continue;

                if (!clientSites.TryGetValue(siteId, out var site))
                    continue;

                var visits = cancelledVisits
                    .Where(x => x.SiteId == siteId)
                    .OrderBy(x => x.VisitNumber)
                    .ToList();

                if (!visits.Any())
                    continue;

                response.Add(new PcarRouteResponse
                {
                    SmartWandId = smartWand.Id,
                    PatrolCarId = smartWand.PatrolCarId,

                    SiteId = site.Id,
                    SiteName = site.Name,
                    Address = site.Address,
                    GPSLocation = site.Gps,

                    DayName = dayName,

                    PcarRouteId = 0,
                    PcarRouteDetailsId = 0,

                    VisitCount = visits.Count,
                    Visits = visits
                });
            }
        }

        private (int VisitCount, string Start, string End) GetVisitInfo(PcarRouteDetails detail, DayOfWeek day)
        {
            return day switch
            {
                DayOfWeek.Monday => (detail.VisitMon, detail.StartMon, detail.EndMon),
                DayOfWeek.Tuesday => (detail.VisitTue, detail.StartTue, detail.EndTue),
                DayOfWeek.Wednesday => (detail.VisitWed, detail.StartWed, detail.EndWed),
                DayOfWeek.Thursday => (detail.VisitThu, detail.StartThu, detail.EndThu),
                DayOfWeek.Friday => (detail.VisitFri, detail.StartFri, detail.EndFri),
                DayOfWeek.Saturday => (detail.VisitSat, detail.StartSat, detail.EndSat),
                DayOfWeek.Sunday => (detail.VisitSun, detail.StartSun, detail.EndSun),
                _ => (0, null, null)
            };
        }


        public class VisitDto
        {
            public int? VisitId { get; set; }
            public string VisitName { get; set; }
            public int VisitNumber { get; set; }
            public DateTime VisitDate { get; set; }
            public int SiteId { get; set; }

            // Visit already saved today?
            public bool IsCheckedToday { get; set; }

            // These two values MUST be returned from API for the popup
            public string SavedTimeOnSite { get; set; }
            public string SavedTimeOffSite { get; set; }

            // Optional: To disable modification in MAUI cleanly
            public bool IsReadOnly => IsCheckedToday;
            public PcarVisitStatusEnum Status { get; set; }
            public int? ParentVisitId { get; set; }
            public int PcarRouteId { get; set; }
            public int PcarRouteDetailsId { get; set; }
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




    }
}
