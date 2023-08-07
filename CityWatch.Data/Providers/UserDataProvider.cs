using CityWatch.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

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
            return _context.Users
                .Where(x => includeAdminUsers || x.IsAdmin == includeAdminUsers)
                .OrderBy(x => x.UserName)
                .ToList(); 
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
                .Where(x => !userId.HasValue || userId.HasValue && x.UserId == userId)
                .Include(x => x.ClientSite)
                .Include(x => x.ClientSite.ClientType)
                .Include(x => x.User)
                .ToList();
        }

        public void SaveUserClientSiteAccess(int userId, List<UserClientSiteAccess> userClientSiteAccess)
        { 
            var currentAccess = _context.UserClientSiteAccess.Where(x => x.UserId == userId).ToList();
            _context.RemoveRange(currentAccess);
            _context.AddRange(userClientSiteAccess);
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
    }
}
