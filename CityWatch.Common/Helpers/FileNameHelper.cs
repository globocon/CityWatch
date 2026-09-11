using System;
using System.IO;

namespace CityWatch.Common.Helpers
{
    public static class FileNameHelper
    {
        public static string GetSanitizedFileNamePart(string fileNamePart)
        {
            if (!string.IsNullOrEmpty(fileNamePart))
            {
                foreach (var item in Path.GetInvalidFileNameChars())
                    fileNamePart = fileNamePart.Replace(item, '_');
            }

            return fileNamePart;
        }

        public static string GetSanitizedDropboxFileNamePart(string fileNamePart)
        {
            if (!string.IsNullOrEmpty(fileNamePart))
            {
                char[] invalidChars = new char[] { ']', '[' };
                foreach (var item in invalidChars)
                    fileNamePart = fileNamePart.Replace(item, '_');
            }

            return fileNamePart;
        }
    }
}
