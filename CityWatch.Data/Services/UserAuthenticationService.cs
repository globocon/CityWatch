using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using Dropbox.Api.Files;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace CityWatch.Data.Services
{
    public class UserAuthenticationService : IUserAuthenticationService
    {
        private readonly CityWatchDbContext _context;

        public UserAuthenticationService(CityWatchDbContext context)
        {
            _context = context;
        }

        public bool TryGetLoginUser(User userLogin, out User user)
        {
            user = null;

            if (userLogin != null &&
                !string.IsNullOrEmpty(userLogin.UserName) &&
                !string.IsNullOrEmpty(userLogin.Password))
            {
                user = _context.Users.SingleOrDefault(u => u.UserName == userLogin.UserName);
                if (user != null && PasswordHelper.VerifyEncryptedPassword(user.Password, userLogin.Password))
                    return true;
            }

            return false;
        }

        public void SaveUserLoginHistoryDetails(User user,string IPAddress)
        {
            if (!user.IsAdmin)
            {
                if (user.Id != 0)
                {
                    _context.LoginUserHistory.Add(new LoginUserHistory()
                    {
                        LoginUserId = user.Id,
                        IPAddress = IPAddress,
                        LoginTime = DateTime.Now
                    });
                    _context.SaveChanges();
                }

            }
            
        }
        public UserClientSiteAccess GetUserClientSiteAccessThirdParty(int? userId)
        {
            return _context.UserClientSiteAccess.Where(x => x.UserId == userId).FirstOrDefault();
        }
    }

    public interface IUserAuthenticationService
    {
        bool TryGetLoginUser(User userLogin, out User user);
        void SaveUserLoginHistoryDetails(User user, string IPAddress);
        UserClientSiteAccess GetUserClientSiteAccessThirdParty(int? userId);
    }
}
