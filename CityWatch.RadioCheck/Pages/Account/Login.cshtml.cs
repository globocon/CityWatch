using CityWatch.Data.Models;
using CityWatch.Data.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace CityWatch.Kpi.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly IUserAuthenticationService _userAuthentication;

        public LoginModel(IUserAuthenticationService userAuthentication)
        {
            _userAuthentication = userAuthentication;
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
                returnUrl = Url.Page("/RadioCheckNew");
           
            var isValidLogin = _userAuthentication.TryGetLoginUser(LoginUser, out User user);

            if (!isValidLogin)
                ModelState.AddModelError("Username", "Incorrect User Name or Password");
            else if (!user.IsAdmin)
                ModelState.AddModelError("Username", "Not authorized to access this page");
            else
            {
                SignInUser(user);
                HttpContext.Session.SetInt32("GuardId", 0);
                return Redirect(Url.Page(returnUrl));
            }
            return Page();
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
