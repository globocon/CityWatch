using CityWatch.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CityWatch.Data.Providers
{
    public interface IUserDataProvider
    {
        List<User> GetUsers(bool includeAdminUsers = false);
        List<CompanyDetails> GetCompanyDetails();
        void SaveUser(User user);
        void UpdateUserStatus(int id, bool deleted);
        List<UserClientSiteAccess> GetUserClientSiteAccess(int? userId);
        void SaveUserClientSiteAccess(int userId, List<UserClientSiteAccess> userClientSiteAccess);
        List<LoginUserHistory> GetUserLoginHistory(int userId);
        public SubDomain GetDomainDeatils(int typeId);
        List<HrSettingsLockedClientSites> GetHrSettingsLockedClientSites(int? hrSettingsId);
        void SaveHrSettingsLockedClientSites(int hrSettingsId, List<HrSettingsLockedClientSites> hrSettingsLockedClientSites);
         void SaveUserClientSiteAccessThirdParty(int userId, List<UserClientSiteAccessThirdparty> userClientSiteAccess);
         List<UserClientSiteAccessThirdparty> GetUserClientSiteAccessThirdparty(int? userId);
    }
    public class UserDataProvider : IUserDataProvider
    {
        private readonly CityWatchDbContext _context;

        public UserDataProvider(CityWatchDbContext context)
        {
            _context = context;
        }

        public List<User> GetUsers(bool includeAdminUsers = false)
        {

            var users = _context.Users
       .Where(x => includeAdminUsers || !x.IsAdmin)
       .OrderBy(x => x.UserName)
       .ToList();

            // Get last login information for all users from LoginUserHistory, including guard and site name
            var lastLogins = _context.LoginUserHistory
                .Select(l => new
                {
                    l.LoginUserId,
                    l.LoginTime,
                    l.IPAddress,
                    GuardName = l.GuardId != 0 ? _context.Guards.FirstOrDefault(g => g.Id == l.GuardId).Name : string.Empty,
                    SiteName = l.ClientSiteId != 0 ? _context.ClientSites.FirstOrDefault(c => c.Id == l.ClientSiteId).Name : string.Empty
                })
                .GroupBy(x => x.LoginUserId)  // Group by LoginUserId to get the latest login for each user
                .Select(g => g.OrderByDescending(x => x.LoginTime).FirstOrDefault()) // Take the most recent login per user
                .ToDictionary(x => x.LoginUserId, x => x);

            // Loop through users and assign the last login information to each user
            foreach (var user in users)
            {
                if (lastLogins.TryGetValue(user.Id, out var lastLoginRecord))
                {
                    // Assign the login time, IP, and additional information to the user object
                    user.LastLoginDate = lastLoginRecord.LoginTime;
                    user.LastLoginIPAdress = lastLoginRecord.IPAddress;


                }
            }

            return users;

            // Get users based on whether to include admin users
            //var users = _context.Users
            //    .Where(x => includeAdminUsers || !x.IsAdmin)
            //    .OrderBy(x => x.UserName)
            //    .ToList();

            // Get last login information for all users from LoginUserHistory
            //var lastLogins = _context.LoginUserHistory
            //    .GroupBy(x => x.LoginUserId)
            //    .Select(g => new
            //    {
            //        LoginUserId = g.Key,
            //        LastLoginRecord = g.OrderByDescending(x => x.LoginTime).FirstOrDefault()
            //    })
            //    .ToDictionary(x => x.LoginUserId, x => x.LastLoginRecord);



            // Get last login information for guards from GuardLogins
            //var guardLogins = _context.GuardLogins
            //    .GroupBy(x => x.UserId) // Use UserId to match with the login table
            //    .Select(g => new
            //    {
            //        LoginUserId = g.Key,
            //        LastGuardLoginRecord = g.OrderByDescending(x => x.LoginDate).FirstOrDefault()
            //    })
            //    .ToDictionary(x => x.LoginUserId, x => x.LastGuardLoginRecord);

            // Loop through users and populate the last login information
            //foreach (var user in users)
            //{
            //    // Check if the user has a login history in LoginUserHistory
            //    if (lastLogins.TryGetValue(user.Id, out var lastLoginRecord))
            //    {
            //        if (lastLoginRecord != null)
            //        {
            //            user.LastLoginDate = lastLoginRecord.LoginTime;
            //            user.LastLoginIPAdress = lastLoginRecord.IPAddress;
            //        }
            //    }

            //    // Check if the user has a login history in GuardLogins
            //    if (guardLogins.TryGetValue(user.Id, out var lastGuardLoginRecord))
            //    {
            //        if (lastGuardLoginRecord != null)
            //        {
            //            // Compare dates to choose the latest login date
            //            if (user.LastLoginDate == null || lastGuardLoginRecord.LoginDate > user.LastLoginDate)
            //            {
            //                user.LastLoginDate = lastGuardLoginRecord.LoginDate;
            //                user.LastLoginIPAdress = lastGuardLoginRecord.IPAddress; // Assuming IP is stored similarly
            //            }
            //        }
            //    }
            //}

            //return users;
        }




        public void SaveUser(User user)
        {
            var userUpdate = _context.Users.SingleOrDefault(x => x.Id == user.Id);
            if (userUpdate == null)
                _context.Add(user);
            else
            {
                userUpdate.UserName = user.UserName;
                userUpdate.Password = user.Password;
                userUpdate.IsDeleted = user.IsDeleted;
            }
            _context.SaveChanges();
        }

        public List<UserClientSiteAccess> GetUserClientSiteAccess(int? userId)
        {
            return _context.UserClientSiteAccess
                .Where(x => (!userId.HasValue || userId.HasValue && x.UserId == userId) && x.ClientSite.IsActive == true)
                .Include(x => x.ClientSite)
                .Include(x => x.ClientSite.ClientType)
                .Include(x => x.User)
                .ToList();
        }
        public List<UserClientSiteAccessThirdparty> GetUserClientSiteAccessThirdparty(int? userId)
        {
            return _context.UserClientSiteAccessThirdparty
                .Where(x => (!userId.HasValue || userId.HasValue && x.UserId == userId) && x.ClientSite.IsActive == true)
                .Include(x => x.ClientSite)
                .Include(x => x.ClientSite.ClientType)
                .Include(x => x.User)
                .ToList();
        }


        public List<HrSettingsLockedClientSites> GetHrSettingsLockedClientSites(int? hrSettingsId)
        {
            return _context.HrSettingsLockedClientSites
                .Where(x => (!hrSettingsId.HasValue || hrSettingsId.HasValue && x.HrSettingsId == hrSettingsId) && x.ClientSite.IsActive == true)
                .Include(x => x.ClientSite)
                .Include(x => x.ClientSite.ClientType)
                .Include(x => x.HrSettings)
                .ToList();
        }

        public void SaveUserClientSiteAccess(int userId, List<UserClientSiteAccess> userClientSiteAccess)
        {
            var currentAccess = _context.UserClientSiteAccess.Where(x => x.UserId == userId).ToList();
            _context.RemoveRange(currentAccess);
            _context.AddRange(userClientSiteAccess);
            _context.SaveChanges();
        }
        public void SaveUserClientSiteAccessThirdParty(int userId, List<UserClientSiteAccessThirdparty> userClientSiteAccess)
        {
            var currentAccess = _context.UserClientSiteAccessThirdparty.Where(x => x.UserId == userId).ToList();
            _context.RemoveRange(currentAccess);
            _context.AddRange(userClientSiteAccess);
            _context.SaveChanges();
        }

        public void SaveHrSettingsLockedClientSites(int hrSettingsId, List<HrSettingsLockedClientSites> hrSettingsLockedClientSites)
        {
            var currentAccess = _context.HrSettingsLockedClientSites.Where(x => x.HrSettingsId == hrSettingsId).ToList();
            _context.RemoveRange(currentAccess);
            _context.AddRange(hrSettingsLockedClientSites);
            _context.SaveChanges();
        }

        public void UpdateUserStatus(int id, bool deleted)
        {
            var userToDelete = _context.Users.SingleOrDefault(x => x.Id == id) ?? throw new InvalidOperationException();
            userToDelete.IsDeleted = deleted;
            _context.SaveChanges();
        }
        public List<CompanyDetails> GetCompanyDetails()
        {
            return _context.CompanyDetails.ToList();
        }



        public List<LoginUserHistory> GetUserLoginHistory(int userId)
        {

            // Fetch user login history
            //var userLastLogins = _context.LoginUserHistory
            //    .Where(x => x.LoginUserId == userId)
            //    .ToList();


            var userLastLogins = _context.LoginUserHistory
       .Where(l => l.LoginUserId == userId)
       .Select(l => new LoginUserHistory
       {
           Id = l.Id,
           LoginUserId = l.LoginUserId,
           LoginTime = l.LoginTime,
           IPAddress = l.IPAddress,
           GuardId = l.GuardId,
           ClientSiteId = l.ClientSiteId,
           // Populate guard and SiteName if GuardId and ClientSiteId are not null
           guard = l.GuardId != 0 ? _context.Guards.FirstOrDefault(g => g.Id == l.GuardId).Name : string.Empty,
           SiteName = l.ClientSiteId != 0 ? _context.ClientSites.FirstOrDefault(c => c.Id == l.ClientSiteId).Name : string.Empty,
           loginType = null
       })
       .ToList();

            // Fetch guard logins
            //var guardLogins = _context.GuardLogins
            //    .Where(gl => gl.UserId == userId)
            //    .Include(x => x.Guard)
            //    .Include(x => x.ClientSite)
            //    .ToList();

            // Convert guardLogins to List<LoginUserHistory>
            //var guardLoginHistory = guardLogins.Select(gl => new LoginUserHistory
            //{
            //    // Assuming these properties exist in both classes; adjust as needed
            //    LoginUserId = gl.UserId,
            //    LoginTime = gl.LoginDate, 
            //    SiteName= gl.ClientSite.Name,
            //    guard= gl.Guard.Name,
            //    IPAddress=gl.IPAddress,
            //}).ToList();

            // Combine both login histories
            //var combinedHistory = userLastLogins.Concat(guardLoginHistory).ToList();

            // Sort the combined history by LoginTime
            return userLastLogins
       .OrderByDescending(x => x.LoginTime) // Sort by LoginTime, latest first
                                            //.Take(10)                            // Get the top 10 records
       .ToList();
        }



        public SubDomain GetDomainDeatils(int typeId)
        {
            return _context.SubDomain.FirstOrDefault(x => x.TypeId == typeId);
        }

    }
}
