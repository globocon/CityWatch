using CityWatch.Web.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CityWatch.Web.Pages.Account
{
    public class LogoutModel : PageModel
    {
        public void OnGet()
        {
            AuthUserHelper.IsAdminPowerUser = false;
            AuthUserHelper.IsAdminGlobal= false;
            AuthUserHelper.IsAdminAuditor = false;
            AuthUserHelper.IsAdminInvestigator = false;
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme, 
                new AuthenticationProperties { RedirectUri = Url.Page("/Index") });
        }
    }
}
