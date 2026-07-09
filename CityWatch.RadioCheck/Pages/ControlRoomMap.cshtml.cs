using CityWatch.Data;
using CityWatch.Data.Providers;
using CityWatch.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

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

        public IActionResult OnGet()
        {
            SignalRConnectionUrl = _configuration.GetSection("SignalRConnectionUrl").Value;
            return Page();
        }

        public JsonResult OnGetSiteInfo(int clientSiteId)
        {
            var siteImage = string.Empty;
            var kpiSetting = _clientDataProvider.GetClientSiteKpiSetting(clientSiteId);
            if (kpiSetting != null && !string.IsNullOrEmpty(kpiSetting.SiteImage) && !string.IsNullOrEmpty(_settings.KpiWebUrl))
            {
                siteImage = $"{new Uri(_settings.KpiWebUrl)}{kpiSetting.SiteImage}";
            }
            return new JsonResult(new { siteImage });
        }

        /// <summary>
        /// Per-site patrol frequency targets and today's wand scan counts.
        /// MinPatrolFreq comes from Site Settings (ClientSiteKpiSetting); wandFq is today's
        /// DailyWandFq count (traditional wands). SmartWand rounds already arrive per guard
        /// in the activity feed, so the client combines both.
        /// </summary>
        public JsonResult OnGetSiteFq()
        {
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
        /// Lightweight change token for the radio check activity status table.
        /// The map polls this cheaply and only reloads the full data when the token changes,
        /// so a guard adding a new IR/LB/KV/SW record shows up within seconds without a hard refresh.
        /// </summary>
        public JsonResult OnGetChangeToken()
        {
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
