using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Security.Claims;
namespace CityWatch.Web.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly IUserAuthenticationService _userAuthentication;
        private readonly IGuardDataProvider _guardDataProvider;

        public LoginController(IUserAuthenticationService userAuthentication, IGuardDataProvider guardDataProvider)
        {
            _userAuthentication = userAuthentication;
            _guardDataProvider = guardDataProvider;
        }
      
        public User LoginUser = new User();
       
        [Route("[action]", Name = "GetUserLogin")]
        public JsonResult GetUserLogin( string userName,  string password)
        {
            LoginUser.UserName = userName;
            LoginUser.Password = password;
            //string Message = null;
            var isValidLogin = _userAuthentication.TryGetLoginUser(LoginUser, out User user);

            if (!isValidLogin)
                // ModelState.AddModelError("Username", "Incorrect User Name or Password");
                // Message = "Incorrect User Name or Password";
                return new JsonResult(false);
            //else if (!user.IsAdmin && returnUrl == Url.Page("/Admin/Settings"))
            //    ModelState.AddModelError("Username", "Not authorized to access this page");
            else if (user.IsDeleted)
                // ModelState.AddModelError("Username", "User is not active");
                // Message = "User is not active";
                return new JsonResult(false);
            else
            {
                SignInUser(user);
                // return Redirect(Url.Page(returnUrl));
                return new JsonResult(true);
            }

            //return Redirect(Url.Page(returnUrl));
            //return new JsonResult(Message);
        }
        /// <summary>
        /// P4#153: the app reports what build it is running, once after login. Fire-and-forget
        /// by design — new APKs call it, old APKs never will, and NOTHING in the login flow
        /// depends on it. A guard with no report on file is therefore known to be on a
        /// pre-reporting (old) build, which is the diagnostic signal the control room wants.
        /// </summary>
        [Route("[action]", Name = "ReportAppVersion")]
        public JsonResult ReportAppVersion(int guardId, string version, string platform = null, string deviceInfo = null)
        {
            try
            {
                _guardDataProvider.SaveGuardMobileAppVersion(guardId, version, platform, deviceInfo);
                return new JsonResult(true);
            }
            catch (Exception)
            {
                /* Never bounce the app over telemetry — including when the table has not
                   been created on this database yet (DbScript 371). */
                return new JsonResult(false);
            }
        }

        private void SignInUser(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Sid, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.IsAdmin ? "Administrator" : "User"),
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(120),
                RedirectUri = Url.Page("/Account/Login")
            };

            HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }

    }
}
