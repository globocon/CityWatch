using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace CityWatch.Web.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IUserAuthenticationService _userAuthentication;
        private readonly IConfigDataProvider _dataProvider;
        public LoginModel(IUserAuthenticationService userAuthentication, IConfigDataProvider dataProvider)
        {
            _userAuthentication = userAuthentication;
            _dataProvider = dataProvider;
        }

        [BindProperty]
        public User LoginUser { get; set; }

        public void OnGet()
        {
            LoginUser = new User();

        }

        public IActionResult OnPost(string returnUrl)
        {

            if (string.IsNullOrEmpty(returnUrl))
                returnUrl = Url.Page("/");

            var isValidLogin = _userAuthentication.TryGetLoginUser(LoginUser, out User user);

            if (!isValidLogin)
                ModelState.AddModelError("Username", "Incorrect User Name or Password");
            else if (!user.IsAdmin && returnUrl == Url.Page("/Admin/Settings"))
                ModelState.AddModelError("Username", "Not authorized to access this page");
            else if (user.IsDeleted)
                ModelState.AddModelError("Username", "User is not active");
            else
            {
                SignInUser(user);
                _userAuthentication.SaveUserLoginHistoryDetails(user, Request.HttpContext.Connection.RemoteIpAddress.ToString());
                var subDomainRedirect = GetClientDetailsUsingSubDomain();
                // Check if the subDomainRedirect has a value and update the returnUrl accordingly
                if (!string.IsNullOrEmpty(subDomainRedirect))
                {
                    return Redirect(subDomainRedirect);
                }
                else
                {
                    return Redirect(Url.Page(returnUrl));
                }

                
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
            // clear the GuardId for IR when the user Login
            HttpContext.Session.Remove("GuardId");
        }

        public string GetClientDetailsUsingSubDomain()
        {

            var host = HttpContext.Request.Host.Host;
            var clientName = host.Split('.')[0];
            var clientLogo = string.Empty;
            var url = string.Empty;
            if (clientName != string.Empty)
            {
                if (clientName.Trim() != "cws-ir" 
                //&& clientName.Trim() != "test"
                && clientName.Trim() != "localhost"
                )
                {
                    var domain = _dataProvider.GetSubDomainDetails(clientName);
                    if (domain != null)
                    {
                        url = "/Guard/Login?t=gl";

                    }
                    else
                    {
                        url = "/Account/Login";

                    }

                }
            }

            return url;
        }
    }



}
