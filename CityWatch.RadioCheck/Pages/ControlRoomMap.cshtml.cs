using CityWatch.Data;
using CityWatch.Data.Providers;
using CityWatch.Web.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.RadioCheck.Pages
{
    public class ControlRoomMapModel : PageModel
    {
        private readonly IClientDataProvider _clientDataProvider;
        private readonly Settings _settings;
        private readonly IConfiguration _configuration;
        private readonly CityWatchDbContext _context;

        public ControlRoomMapModel(IClientDataProvider clientDataProvider, IOptions<Settings> settings,
            IConfiguration configuration, CityWatchDbContext context)
        {
            _clientDataProvider = clientDataProvider;
            _settings = settings.Value;
            _configuration = configuration;
            _context = context;
        }

        public string SignalRConnectionUrl { get; set; }

        /* Tracking feature pack: gates the two script tags in the view. Off = the page is
           byte-identical to today (RT9). */
        public bool TrackingEnabled => _configuration.GetSection("Tracking")["Enabled"] == "True"
                                       || _configuration.GetSection("Tracking")["Enabled"] == "true";

        // AllowAnonymousToFolder("/") in Program.cs suppresses [Authorize], so the page
        // guards itself the same way RadioCheckV2/ClientProfile do.
        private bool IsLoggedIn => User.Identity != null && User.Identity.IsAuthenticated;

        public async Task<IActionResult> OnGetAsync(string key = null)
        {
            /* Keyed direct link (field/test use): when ControlRoomMap:AccessKey is set in
               configuration AND the request carries a matching ?key=, sign in a read-only
               map-viewer principal (the same cookie the interactive login issues) and
               redirect to the clean URL so the key does not linger in the address bar.
               Absent/empty config = feature OFF — a production deploy without the key
               behaves byte-identically to today. Treat the key like a password: anyone
               holding the link sees the live map. Rotate or clear it in appsettings. */
            if (!IsLoggedIn)
            {
                var accessKey = _configuration["ControlRoomMap:AccessKey"];
                if (!string.IsNullOrWhiteSpace(accessKey) && !string.IsNullOrEmpty(key)
                    && CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(accessKey)))
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, "MapViewerLink"),
                        new Claim(ClaimTypes.Sid, "0"),
                        new Claim(ClaimTypes.Role, "User"),
                    };
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity),
                        new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12) });
                    return RedirectToPage("/ControlRoomMap");
                }

                return Redirect(Url.Page("/Account/Login", new { returnUrl = Url.Page("/ControlRoomMap") }));
            }

            SignalRConnectionUrl = _configuration.GetSection("SignalRConnectionUrl").Value;
            return Page();
        }

        public IActionResult OnGetSiteInfo(int clientSiteId)
        {
            if (!IsLoggedIn)
                return new UnauthorizedResult();
            var siteImage = string.Empty;
            var kpiSetting = _clientDataProvider.GetClientSiteKpiSetting(clientSiteId);
            if (kpiSetting != null && !string.IsNullOrEmpty(kpiSetting.SiteImage))
            {
                if (kpiSetting.SiteImage.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    siteImage = kpiSetting.SiteImage;
                }
                else
                {
                    siteImage = $"{GetKpiBaseUrl()}/{kpiSetting.SiteImage.TrimStart('/')}";
                }
            }
            return new JsonResult(new { siteImage });
        }

        /// <summary>
        /// Base URL of the KPI site that hosts the uploaded site images.
        /// Prefers Settings:KpiWebUrl from configuration; falls back to the same
        /// hard-coded base the Site Settings page uses (localhost dev values are
        /// useless to the operator's browser, so they are ignored too).
        /// </summary>
        private string GetKpiBaseUrl()
        {
            var url = _settings.KpiWebUrl;
            if (string.IsNullOrWhiteSpace(url))
                url = _configuration.GetSection("Settings")["KpiWebUrl"];
            if (string.IsNullOrWhiteSpace(url) || url.Contains("localhost", StringComparison.OrdinalIgnoreCase))
                url = "https://kpi.cws-ir.com";
            return url.TrimEnd('/');
        }

        /// <summary>
        /// Per-site patrol frequency targets and today's wand scan counts.
        /// MinPatrolFreq comes from Site Settings (ClientSiteKpiSetting); wandFq is today's
        /// DailyWandFq count (traditional wands). SmartWand rounds already arrive per guard
        /// in the activity feed, so the client combines both.
        /// </summary>
        public IActionResult OnGetSiteFq()
        {
            if (!IsLoggedIn)
                return new UnauthorizedResult();
            try
            {
                var today = DateTime.Today;
                var mins = _context.ClientSiteKpiSettings
                    .Where(x => x.MinPatrolFreq != null && x.MinPatrolFreq > 0)
                    .Select(x => new { x.ClientSiteId, MinFq = x.MinPatrolFreq.Value })
                    .ToList();
                var wands = _context.DailyWandFq
                    .Where(x => x.FqDate >= today)
                    .GroupBy(x => x.ClientSiteId)
                    .Select(g => new { ClientSiteId = g.Key, WandFq = g.Sum(x => x.Fq) })
                    .ToList();

                var result = mins.Select(m => new
                {
                    clientSiteId = m.ClientSiteId,
                    minFq = m.MinFq,
                    wandFq = wands.Where(w => w.ClientSiteId == m.ClientSiteId).Select(w => w.WandFq).FirstOrDefault()
                }).ToList();

                return new JsonResult(result);
            }
            catch (Exception)
            {
                return new JsonResult(Array.Empty<object>());
            }
        }

        /// <summary>
        /// PCAR live route data: today's wand-scan confirmed site visits per patrol guard,
        /// in chronological order, plus the planned route (for "next expected site").
        /// A patrol's current location is the site of its most recent scan.
        /// </summary>
        public IActionResult OnGetPcarRoutes()
        {
            if (!IsLoggedIn)
                return new UnauthorizedResult();
            try
            {
                var today = DateTime.Today;
                var visits = _context.PcarRouteDailyVisits
                    .Where(v => v.CreatedAt >= today)
                    .OrderBy(v => v.CreatedAt)
                    .ToList();
                if (visits.Count == 0)
                    return new JsonResult(Array.Empty<object>());

                var siteIds = visits.Select(v => v.SiteId).Distinct().ToList();
                var sitesById = _context.ClientSites
                    .Where(s => siteIds.Contains(s.Id))
                    .Select(s => new { s.Id, s.Name, s.Gps })
                    .ToList()
                    .ToDictionary(s => s.Id);

                var routeIds = visits.Select(v => v.PcarRouteId).Distinct().ToList();
                var routesById = _context.PcarRoute
                    .Where(r => routeIds.Contains(r.Id))
                    .Select(r => new { r.Id, r.Pcarroutename, PatrolCarName = r.SmartWand.PatrolCarName })
                    .ToList()
                    .ToDictionary(r => r.Id);
                var plannedByRoute = _context.PcarRouteDetails
                    .Where(d => routeIds.Contains(d.PcarRouteId))
                    .OrderBy(d => d.OrderNo)
                    .Select(d => new { d.PcarRouteId, d.ClientSiteId, d.OrderNo, SiteName = d.ClientSite.Name })
                    .ToList();

                var result = visits
                    .GroupBy(v => v.GuardId)
                    .Select(g =>
                    {
                        var ordered = g.OrderBy(v => v.CreatedAt).ToList();
                        var last = ordered[ordered.Count - 1];
                        var route = routesById.ContainsKey(last.PcarRouteId) ? routesById[last.PcarRouteId] : null;
                        var planned = plannedByRoute.Where(p => p.PcarRouteId == last.PcarRouteId).ToList();
                        var curPlanned = planned.FirstOrDefault(p => p.ClientSiteId == last.SiteId);
                        var next = curPlanned == null ? null : planned.FirstOrDefault(p => p.OrderNo > curPlanned.OrderNo);

                        return new
                        {
                            guardId = g.Key,
                            routeName = route?.Pcarroutename ?? "",
                            patrolCarName = route?.PatrolCarName ?? "",
                            nextSite = next?.SiteName ?? "",
                            plannedTotal = planned.Count,
                            visits = ordered.Select(v => new
                            {
                                siteId = v.SiteId,
                                siteName = sitesById.ContainsKey(v.SiteId) ? sitesById[v.SiteId].Name : ("Site " + v.SiteId),
                                gps = !string.IsNullOrWhiteSpace(v.GpsCoordinates)
                                    ? v.GpsCoordinates
                                    : (sitesById.ContainsKey(v.SiteId) ? sitesById[v.SiteId].Gps : null),
                                timeOn = v.TimeOn,
                                timeOff = v.TimeOff,
                                at = v.CreatedAt
                            }).ToList()
                        };
                    })
                    .ToList();

                return new JsonResult(result);
            }
            catch (Exception)
            {
                return new JsonResult(Array.Empty<object>());
            }
        }

        /// <summary>
        /// Lightweight change token for the radio check activity status table.
        /// The map polls this cheaply and only reloads the full data when the token changes,
        /// so a guard adding a new IR/LB/KV/SW record shows up within seconds without a hard refresh.
        /// </summary>
        public IActionResult OnGetChangeToken()
        {
            if (!IsLoggedIn)
                return new UnauthorizedResult();
            try
            {
                var token = _context.ClientSiteRadioChecksActivityStatus
                    .GroupBy(x => 1)
                    .Select(g => new
                    {
                        MaxId = (int?)g.Max(x => x.Id),
                        Count = g.Count(),
                        LastIR = g.Max(x => x.LastIRCreatedTime),
                        LastKV = g.Max(x => x.LastKVCreatedTime),
                        LastLB = g.Max(x => x.LastLBCreatedTime),
                        LastSW = g.Max(x => x.LastSWCreatedTime),
                        LastLogin = g.Max(x => x.GuardLoginTime),
                        LastLogout = g.Max(x => x.GuardLogoutTime),
                        LastNotification = g.Max(x => x.NotificationCreatedTime)
                    })
                    .FirstOrDefault();

                /* traditional wand scans land in DailyWandFq, not the activity table —
                   include them so plain wand scans also trigger a refresh */
                var wandToken = _context.DailyWandFq
                    .GroupBy(x => 1)
                    .Select(g => new
                    {
                        MaxId = (int?)g.Max(x => x.Id),
                        Count = g.Count(),
                        LastUpdate = (DateTime?)g.Max(x => x.UpdatedAt)
                    })
                    .FirstOrDefault();

                var wandPart = wandToken == null ? "w0" : $"{wandToken.MaxId}|{wandToken.Count}|{wandToken.LastUpdate:O}";

                /* PCAR site-visit scans → route tracking updates */
                var visitToken = _context.PcarRouteDailyVisits
                    .GroupBy(x => 1)
                    .Select(g => new { MaxId = (int?)g.Max(x => x.Id), Count = g.Count() })
                    .FirstOrDefault();
                wandPart += visitToken == null ? "|v0" : $"|{visitToken.MaxId}|{visitToken.Count}";

                if (token == null)
                    return new JsonResult(new { token = "empty|" + wandPart });

                var value = $"{token.MaxId}|{token.Count}|{token.LastIR:O}|{token.LastKV:O}|{token.LastLB:O}|{token.LastSW:O}|{token.LastLogin:O}|{token.LastLogout:O}|{token.LastNotification:O}|{wandPart}";
                return new JsonResult(new { token = value });
            }
            catch (Exception)
            {
                // Never break the page over the optimisation endpoint — fall back to interval refresh
                return new JsonResult(new { token = DateTime.UtcNow.Ticks.ToString() });
            }
        }
    }
}
