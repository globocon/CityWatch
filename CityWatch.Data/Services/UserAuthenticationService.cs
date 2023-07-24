using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using System.Linq;

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
    }

    public interface IUserAuthenticationService
    {
        bool TryGetLoginUser(User userLogin, out User user);
    }
}
