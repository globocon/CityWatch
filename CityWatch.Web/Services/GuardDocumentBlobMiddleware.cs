using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CityWatch.Web.Services
{
    /// <summary>
    /// Keeps the existing links /Uploads/Guards/License/{SecurityNo}/{FileName} working (web pages,
    /// RadioCheck and the mobile app all build them) once documents are in Azure Blob:
    ///   - file still on the VM disk  -> served from disk by UseStaticFiles, as before
    ///   - file only in Blob          -> redirected to a short-lived Blob read link
    ///   - nowhere                    -> 404, as before
    /// Does nothing while GuardDocumentStorage:UseBlobStorage is off.
    /// </summary>
    public class GuardDocumentBlobMiddleware
    {
        private static readonly PathString LicenseRoot = new PathString("/Uploads/Guards/License");

        private readonly RequestDelegate _next;
        private readonly FileExtensionContentTypeProvider _contentTypes = new FileExtensionContentTypeProvider();

        public GuardDocumentBlobMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IGuardDocumentStorage storage)
        {
            if (!storage.IsBlobEnabled ||
                !(HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method)) ||
                !context.Request.Path.StartsWithSegments(LicenseRoot, StringComparison.OrdinalIgnoreCase, out var remaining) ||
                !remaining.HasValue || remaining.Value.Length <= 1)
            {
                await _next(context);
                return;
            }

            var relativePath = remaining.Value.TrimStart('/');

            string localPath;
            try
            {
                localPath = storage.GetLocalPath(relativePath);
            }
            catch (ArgumentException)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            if (File.Exists(localPath))
            {
                await _next(context);
                return;
            }

            var blobUri = await storage.GetBlobReadUriAsync(relativePath);
            if (blobUri != null)
            {
                context.Response.Headers.CacheControl = "no-store";
                context.Response.Redirect(blobUri.ToString());
                return;
            }

            // no read link possible (e.g. connection string without an account key) - stream it instead
            if (!_contentTypes.TryGetContentType(relativePath, out var contentType))
                contentType = "application/octet-stream";

            using var buffer = new MemoryStream();
            if (await storage.TryCopyFromBlobAsync(relativePath, buffer))
            {
                context.Response.ContentType = contentType;
                context.Response.ContentLength = buffer.Length;
                if (HttpMethods.IsGet(context.Request.Method))
                {
                    buffer.Position = 0;
                    await buffer.CopyToAsync(context.Response.Body);
                }
                return;
            }

            await _next(context);
        }
    }

    public static class GuardDocumentBlobMiddlewareExtensions
    {
        /// <summary>
        /// Must run before UseStaticFiles.
        /// </summary>
        public static IApplicationBuilder UseGuardDocumentBlobFallback(this IApplicationBuilder app)
        {
            return app.UseMiddleware<GuardDocumentBlobMiddleware>();
        }
    }
}
