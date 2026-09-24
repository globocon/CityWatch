using CityWatch.Data.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Security.Claims;

namespace CityWatch.RadioCheck.Helpers
{
    public static class AuthUserHelper
    {

        public static bool IsAdminUserLoggedIn { get; set; }
        public static bool IsAdminPowerUser { get; set; }
        public static bool IsAdminGlobal { get; set; }
    }
}
