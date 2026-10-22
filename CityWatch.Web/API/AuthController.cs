using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using CityWatch.Data.Models;
using CityWatch.Data.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace CityWatch.Web.API
{

    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserAuthenticationService _userAuthentication;

        public AuthController(IUserAuthenticationService userAuthentication)
        {
            _userAuthentication = userAuthentication;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest loginRequest)
        {
            if (string.IsNullOrEmpty(loginRequest.UserName) || string.IsNullOrEmpty(loginRequest.Password))
                return BadRequest(new { message = "Username and password are required." });
            User LoginUser = new User();
            LoginUser.UserName = loginRequest.UserName;
            LoginUser.Password = loginRequest.Password;

            var isValidLogin = _userAuthentication.TryGetLoginUser(LoginUser, out User user);

            if (!isValidLogin)
                return Unauthorized(new { message = "Incorrect Username or Password" });

            if (user.IsDeleted)
                return Unauthorized(new { message = "User is not active" });

            return Ok(new
            {
                UserId = user.Id,
                Name = user.UserName,
                Role = user.IsAdmin ? "Administrator" : "User",
            });
        }

    }

    public class LoginRequest
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }

}
