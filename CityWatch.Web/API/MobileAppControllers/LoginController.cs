using CityWatch.Data.Models;
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

        public LoginController(IUserAuthenticationService userAuthentication)
        {
            _userAuthentication = userAuthentication;
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
