using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Security.Claims;

namespace CityWatch.Web.Helpers
{
    public static class AuthUserHelper
    {
        private static IHttpContextAccessor _httpContextAccessor;

        public static void Configure(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public static int? LoggedInUserId
        {
            get
            {
                int? userId = null;
                var userClaims = _httpContextAccessor.HttpContext.User.Claims;
                if (userClaims != null)
                {
                    var isUserLoggedIn = userClaims.Single(x => x.Type == ClaimTypes.Role).Value == "User";
                    if (isUserLoggedIn)
                        userId = int.Parse(userClaims.Single(x => x.Type == ClaimTypes.Sid).Value);
                }
                return userId;
            }
        }

        public static bool IsAdminUserLoggedIn
        {
            get
            {
                var userClaims = _httpContextAccessor.HttpContext.User.Claims;
                if (userClaims != null)
                {
                    return userClaims.Single(x => x.Type == ClaimTypes.Role).Value == "Administrator";
                }
                return false;
            }
        }
    }
}
