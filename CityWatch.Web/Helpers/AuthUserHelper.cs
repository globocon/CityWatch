using CityWatch.Data.Models;
using Microsoft.AspNetCore.Http;
using System;
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

        public static int? GetLoggedInUserId
        {
            get
            {
                int? userId = null;
                var userClaims = _httpContextAccessor.HttpContext.User.Claims;
                if (userClaims != null)
                {
                    userId = int.Parse(userClaims.Single(x => x.Type == ClaimTypes.Sid).Value);
                }
                return userId;

            }
        }

        public static bool IsAdminPowerUser
        {
            get => _httpContextAccessor.HttpContext?.Session?.GetString("IsAdminPowerUser") == "true";
            set => _httpContextAccessor.HttpContext?.Session?.SetString("IsAdminPowerUser", value ? "true" : "false");
        }

        public static bool IsAdminGlobal
        {
            get => _httpContextAccessor.HttpContext?.Session?.GetString("IsAdminGlobal") == "true";
            set => _httpContextAccessor.HttpContext?.Session?.SetString("IsAdminGlobal", value ? "true" : "false");
        }

        public static bool IsAdminThirdParty
        {
            get => _httpContextAccessor.HttpContext?.Session?.GetString("IsAdminThirdParty") == "true";
            set => _httpContextAccessor.HttpContext?.Session?.SetString("IsAdminThirdParty", value ? "true" : "false");
        }

        public static bool IsAdminInvestigator
        {
            get => _httpContextAccessor.HttpContext?.Session?.GetString("IsAdminInvestigator") == "true";
            set => _httpContextAccessor.HttpContext?.Session?.SetString("IsAdminInvestigator", value ? "true" : "false");
        }

        public static bool IsAdminAuditor
        {
            get => _httpContextAccessor.HttpContext?.Session?.GetString("IsAdminAuditor") == "true";
            set => _httpContextAccessor.HttpContext?.Session?.SetString("IsAdminAuditor", value ? "true" : "false");
        }

        public static bool DoseGuardHaveRcClientSitesControl
        {
            get => _httpContextAccessor.HttpContext?.Session?.GetString("DoseGuardHaveRcClientSitesControl") == "true";
            set => _httpContextAccessor.HttpContext?.Session?.SetString("DoseGuardHaveRcClientSitesControl", value ? "true" : "false");
        }

        public static bool IsOnboardingUserLoggedIn
        {
            get
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.User != null && _httpContextAccessor.HttpContext.User.Identity != null)
                {
                    return _httpContextAccessor.HttpContext.User.Identity.Name == "onboarding";
                }
                return false;
            }
        }
    }
}
