using Microsoft.AspNetCore.StaticFiles;


namespace CityWatch.Web.Helpers
{
    public static class FileTypeHelper
    {
        public static string GetMimeType(string fileName)
        {
            var provider = new FileExtensionContentTypeProvider();
            if (provider.TryGetContentType(fileName, out var contentType))
            {
                return contentType;
            }
            return "application/octet-stream"; // fallback
        }
    }
}
