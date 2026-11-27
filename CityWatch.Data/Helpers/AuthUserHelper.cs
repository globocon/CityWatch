using CityWatch.Data.Models;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Security.Claims;

namespace CityWatch.Data.Helpers
{
    public static class AuthUserHelperRadio
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
                int? userIdRadio = null;
                var claimsIdentity = User.Identity as ClaimsIdentity;



                if (claimsIdentity != null)
                {
                    var userClaims = claimsIdentity.Claims;
                    var isUserLoggedIn = userClaims.Single(x => x.Type == ClaimTypes.Role).Value == "User";
                    if (isUserLoggedIn)
                        userIdRadio = int.Parse(userClaims.Single(x => x.Type == ClaimTypes.Sid).Value);
                }


                return userIdRadio;
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