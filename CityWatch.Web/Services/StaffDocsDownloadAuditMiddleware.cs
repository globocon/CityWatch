using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CityWatch.Web.Services
{
    /// <summary>
    /// p1-373 Records every download/view of a file under /StaffDocs (Site SOP, Alarm SOP, Company SOP,
    /// Training, Multimedia, Templates &amp; Forms, Help documents) and /Pdf/Template (IR template) in
    /// FileDownloadAuditLogs, whichever page or app (Web, RadioCheck, direct link) the request came from.
    /// Pages that already log the download with the guard's details (Downloads, Help PDF) write their row
    /// first; this then sees it and adds nothing.
    /// Never blocks the file: any failure here is logged and the file is served as normal.
    /// </summary>
    public class StaffDocsDownloadAuditMiddleware
    {
        private static readonly PathString StaffDocsRoot = new PathString("/StaffDocs");
        private static readonly PathString IrTemplateRoot = new PathString("/Pdf/Template");

        // a PDF / video opens as several requests (range requests); count it once
        private static readonly TimeSpan DuplicateWindow = TimeSpan.FromMinutes(2);

        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _env;
        private readonly IMemoryCache _cache;
        private readonly ILogger<StaffDocsDownloadAuditMiddleware> _logger;

        public StaffDocsDownloadAuditMiddleware(RequestDelegate next, IWebHostEnvironment env, IMemoryCache cache,
            ILogger<StaffDocsDownloadAuditMiddleware> logger)
        {
            _next = next;
            _env = env;
            _cache = cache;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IGuardLogDataProvider guardLogDataProvider)
        {
            if (HttpMethods.IsGet(context.Request.Method) && IsFirstPartOfFile(context.Request))
            {
                try
                {
                    await RecordDownload(context, guardLogDataProvider);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Download audit failed for {Path}", context.Request.Path);
                }
            }

            await _next(context);
        }

        private async Task RecordDownload(HttpContext context, IGuardLogDataProvider guardLogDataProvider)
        {
            string folder;
            bool isIrTemplate = false;
            if (context.Request.Path.StartsWithSegments(StaffDocsRoot, StringComparison.OrdinalIgnoreCase, out var remaining))
                folder = "StaffDocs";
            else if (context.Request.Path.StartsWithSegments(IrTemplateRoot, StringComparison.OrdinalIgnoreCase, out remaining))
            {
                folder = Path.Combine("Pdf", "Template");
                isIrTemplate = true;
            }
            else
                return;

            var fileName = remaining.HasValue ? remaining.Value.TrimStart('/') : string.Empty;
            if (string.IsNullOrEmpty(fileName))
                return;

            // only files that exist - not 404s from bots or stale links
            var root = Path.GetFullPath(Path.Combine(_env.WebRootPath, folder));
            var fullPath = Path.GetFullPath(Path.Combine(root, fileName));
            if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
                return;

            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            var cacheKey = $"dwnl-audit|{ipAddress}|{fileName.ToLowerInvariant()}";
            if (_cache.TryGetValue(cacheKey, out _))
                return;
            _cache.Set(cacheKey, true, DuplicateWindow);

            var fdal = new FileDownloadAuditLogs();
            if (guardLogDataProvider.HasRecentDownloadAuditLog(fileName, ipAddress, fdal.EventDateTime - DuplicateWindow))
                return;

            // static files run before UseAuthentication, so read the login cookie here
            var auth = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var sid = auth.Succeeded ? auth.Principal.FindFirst(ClaimTypes.Sid)?.Value : null;

            fdal.UserID = int.TryParse(sid, out var userId) ? userId : null;
            fdal.IPAddress = ipAddress;
            fdal.DwnlCatagory = isIrTemplate ? "IR Template" : guardLogDataProvider.GetStaffDocumentAuditCategory(fileName);
            fdal.DwnlFileName = fileName;
            guardLogDataProvider.CreateDownloadFileAuditLogEntry(fdal);
        }

        private static bool IsFirstPartOfFile(HttpRequest request)
        {
            var range = request.Headers.Range.ToString();
            return string.IsNullOrEmpty(range) || range.Replace(" ", "").StartsWith("bytes=0-", StringComparison.OrdinalIgnoreCase);
        }
    }

    public static class StaffDocsDownloadAuditMiddlewareExtensions
    {
        public static IApplicationBuilder UseStaffDocsDownloadAudit(this IApplicationBuilder app)
        {
            return app.UseMiddleware<StaffDocsDownloadAuditMiddleware>();
        }
    }
}
