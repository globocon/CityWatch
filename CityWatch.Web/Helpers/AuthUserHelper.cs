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
                var userClaims = _httpContextAccessor?.HttpContext?.User?.Claims;
                if (userClaims != null)
                {
                    var roleClaim = userClaims.FirstOrDefault(x => x.Type == ClaimTypes.Role);
                    if (roleClaim != null && roleClaim.Value == "User")
                    {
                        var sidClaim = userClaims.FirstOrDefault(x => x.Type == ClaimTypes.Sid);
                        if (sidClaim != null)
                            userId = int.Parse(sidClaim.Value);
                    }
                }
                return userId;
            }
        }

        public static bool IsAdminUserLoggedIn
        {
            get
            {
                var userClaims = _httpContextAccessor?.HttpContext?.User?.Claims;
                if (userClaims != null)
                {
                    var roleClaim = userClaims.FirstOrDefault(x => x.Type == ClaimTypes.Role);
                    return roleClaim != null && roleClaim.Value == "Administrator";
                }
                return false;
            }
        }

        public static int? GetLoggedInUserId
        {
            get
            {
                int? userId = null;
                var userClaims = _httpContextAccessor?.HttpContext?.User?.Claims;
                if (userClaims != null)
                {
                    var sidClaim = userClaims.FirstOrDefault(x => x.Type == ClaimTypes.Sid);
                    if (sidClaim != null)
                    {
                        userId = int.Parse(sidClaim.Value);
                    }
                }
                return userId;
            }
        }

        public static bool IsAdminPowerUser
        {
            get => _httpContextAccessor?.HttpContext?.Session?.GetString("IsAdminPowerUser") == "true";
            set => _httpContextAccessor?.HttpContext?.Session?.SetString("IsAdminPowerUser", value ? "true" : "false");
        }

        public static bool IsAdminGlobal
        {
            get => _httpContextAccessor?.HttpContext?.Session?.GetString("IsAdminGlobal") == "true";
            set => _httpContextAccessor?.HttpContext?.Session?.SetString("IsAdminGlobal", value ? "true" : "false");
        }

        public static bool IsAdminThirdParty
        {
            get => _httpContextAccessor?.HttpContext?.Session?.GetString("IsAdminThirdParty") == "true";
            set => _httpContextAccessor?.HttpContext?.Session?.SetString("IsAdminThirdParty", value ? "true" : "false");
        }

        public static bool IsAdminInvestigator
        {
            get => _httpContextAccessor?.HttpContext?.Session?.GetString("IsAdminInvestigator") == "true";
            set => _httpContextAccessor?.HttpContext?.Session?.SetString("IsAdminInvestigator", value ? "true" : "false");
        }

        public static bool IsAdminAuditor
        {
            get => _httpContextAccessor?.HttpContext?.Session?.GetString("IsAdminAuditor") == "true";
            set => _httpContextAccessor?.HttpContext?.Session?.SetString("IsAdminAuditor", value ? "true" : "false");
        }

        public static bool DoseGuardHaveRcClientSitesControl
        {
            get => _httpContextAccessor?.HttpContext?.Session?.GetString("DoseGuardHaveRcClientSitesControl") == "true";
            set => _httpContextAccessor?.HttpContext?.Session?.SetString("DoseGuardHaveRcClientSitesControl", value ? "true" : "false");
        }

    }
}
