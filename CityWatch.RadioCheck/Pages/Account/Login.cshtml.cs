using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace CityWatch.Kpi.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly IUserAuthenticationService _userAuthentication;
        private readonly IGuardDataProvider _guardDataProvider;

        public LoginModel(IUserAuthenticationService userAuthentication, IGuardDataProvider guardDataProvider)
        {
            _userAuthentication = userAuthentication;
            _guardDataProvider = guardDataProvider;
        }

        public void OnGet()
        {
            HttpContext.Session.SetInt32("GuardId", 0);
            LoginUser = new User();
        }

        [BindProperty]
        public User LoginUser { get; set; }

        public IActionResult OnPost(string returnUrl)
        {
            if (string.IsNullOrEmpty(returnUrl))
                returnUrl = Url.Page("/RadioCheckV2");

            var isValidLogin = _userAuthentication.TryGetLoginUser(LoginUser, out User user);

            //if (!isValidLogin)
            //    ModelState.AddModelError("Username", "Incorrect User Name or Password");
            //else if (!user.IsAdmin)
            //    ModelState.AddModelError("Username", "Not authorized to access this page");
            //else
            //{
            //    SignInUser(user);
            //    HttpContext.Session.SetInt32("GuardId", 0);
            //    return Redirect(Url.Page(returnUrl));
            //}
            if (!isValidLogin)
            {
                var guard = _guardDataProvider.GetGuards()
            .FirstOrDefault(g => g.Name == LoginUser.UserName
                              && g.SecurityNo == LoginUser.Password);

                if (guard == null)
                {
                    // Neither User nor Guard matched ? show error
                    ModelState.AddModelError("Username", "Incorrect User Name or Password");
                    return Page();
                }
                else
                {

                    //  Guard must have at least one RC access type
                    if (!(guard.IsRCAccess || guard.IsRCHRAccess || guard.IsRCFusionAccess || guard.IsRCLiteAccess))
                    {
                        ModelState.AddModelError("Username", "You do not have RC access.");
                        return Page();
                    }

                    HttpContext.Session.SetInt32("clientprofile", guard.Id);

                    // Option A: Just rely on GuardId in session
                    // return Redirect(returnUrl);

                    // Option B: Use claims auth like users
                    SignInGuard(guard);

                    return Redirect(Url.Page("/ClientProfile"));
                }
            }
            else
            {
                // Step 3: Normal user login successful
                SignInUser(user);
                HttpContext.Session.SetInt32("GuardId", 0);
                return Redirect(returnUrl);
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


        private void SignInGuard(Guard guard)
        {
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, guard.Name),
        new Claim(ClaimTypes.Sid, guard.Id.ToString()),
        new Claim(ClaimTypes.Role, "Guard"),
        new Claim("ClientProfileId", guard.Id.ToString()) // custom claim
    };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(120),
                RedirectUri = Url.Page("/Account/Login")
            };

            HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );
        }

    }
}
