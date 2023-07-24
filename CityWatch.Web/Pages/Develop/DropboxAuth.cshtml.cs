using Dropbox.Api;
using Dropbox.Api.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CityWatch.Web.Pages.Develop
{
    public class DropboxAuthModel : PageModel
    {
        private const string DbxAppKey = "5d6qpsel0g66i43";
        private const string DbxAppSecret = "5o4eoai91b9wx4d";
        private const string DbxAccessToken = "sl.BX6TELcK8L5cQAzhsOfFMNtO90MkqF3yyQEbfGIn9G5xLHM6L8C6vgFvKJRvmcPPVv5o8g-1ICeV8SSE1LkR3vGA015Ayp0VD05Wt8IfZkDypd8rql7eNnlRVrP5vAAPDLLRQZsX0HYgF7sShqA";
        private const string DbxRefreshToken = "EUHKG2n5IbUAAAAAAAAAAZfTMJrSpCl6oGa-yBm20AXaTJcGOiIKQzY0ohXfYSvq";
        private const string userEmail = "aravindg@cribsontechnologies.com";
        private const string redirectUri = "https://localhost:44356/Develop/DropboxAuth?handler=Auth";

        private readonly IWebHostEnvironment _webHostEnvironment;

        public DropboxAuthModel(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> OnGet()
        {
            if (!_webHostEnvironment.IsDevelopment())
            {
                throw new NotSupportedException("Page is not available in production environment");
            }

            var DbxAccessToken = GetDbxAccessToken();
            var DbxRefreshToken = GetDbxRefreshToken();

            if (string.IsNullOrEmpty(DbxAccessToken))
                return Redirect("DropboxAuth?handler=Connect");

            var count = 0;
            try
            {
                using var dbxTeam = new DropboxTeamClient(DbxAccessToken, DbxRefreshToken, DbxAppKey, DbxAppSecret, new DropboxClientConfig());
                var team = dbxTeam.Team.MembersListAsync().Result;
                if (team.Members.Count > 0)
                {
                    var cwsMember = team.Members.SingleOrDefault(z => z.Profile.Email == userEmail);
                    if (cwsMember != null)
                    {
                        var dbx = dbxTeam.AsMember(cwsMember.Profile.TeamMemberId);
                        var account = await dbx.Users.GetCurrentAccountAsync();
                        var nsId = new PathRoot.NamespaceId(account.RootInfo.RootNamespaceId);

                        var folder = "/CWS-VISY Project/VIC/VISY - VIC - Banyule/FLIR - Wand Recordings - IRs - Daily Logs/2022/202205 - MAY DATA/20220501/";
                        var result = await dbx.WithPathRoot(nsId).Files.ListFolderAsync(folder);
                        count = result.Entries.Count;
                        var hasMore = result.HasMore;
                        while (hasMore)
                        {
                            result = await dbx.WithPathRoot(nsId).Files.ListFolderContinueAsync(result.Cursor);
                            count += result.Entries.Count;
                            hasMore = result.HasMore;
                        }
                    }
                }
            }
            catch (ApiException<Dropbox.Api.Files.GetMetadataError> e)
            {

            }
            catch (Exception ex)
            {

            }

            ViewData["result"] = $"Count: {count}";

            return Page();
        }

        public IActionResult OnGetConnect()
        {
            var redirect = DropboxOAuth2Helper.GetAuthorizeUri(
                OAuthResponseType.Code,
                DbxAppKey,
                redirectUri,
                Guid.NewGuid().ToString("N"),
                tokenAccessType: TokenAccessType.Offline);

            return Redirect(redirect.ToString());
        }

        public async Task<ActionResult> OnGetAuth(string code, string state)
        {
            var response = await DropboxOAuth2Helper.ProcessCodeFlowAsync(
                    code,
                    DbxAppKey,
                    DbxAppSecret,
                    redirectUri);

            var DbxAccessToken = response.AccessToken;
            var DbxRefreshToken = response.RefreshToken;
            HttpContext.Session.SetString("dbx_token", DbxAccessToken);
            HttpContext.Session.SetString("dbx_rtoken", DbxRefreshToken);

            return Redirect("DropboxAuth");
        }

        private string GetDbxAccessToken(bool fromSession = false)
        {
            if (fromSession) 
            {
                return HttpContext.Session.GetString("dbx_token");
            }
            
            return DbxAccessToken;
        }

        private string GetDbxRefreshToken(bool fromSession = false) 
        {
            if (fromSession)
            {
                return HttpContext.Session.GetString("dbx_rtoken");
            }

            return DbxRefreshToken;
        }
    }
}
