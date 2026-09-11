using CityWatch.Data.Models;
using System;
using System.IO;

namespace CityWatch.Web.Helpers
{
    public static class DocketDropboxHelper
    {
        // Builds the Dropbox path used for manual docket PDFs. Upload passes DateTime.Today;
        // download passes the docket generation date (yyyyMMdd prefix of the file name),
        // so both sides always resolve to the same day folder.
        public static string GetManualDocketDbxFilePath(ClientSiteKpiSetting clientSiteKpiSettings, string fileName, string clientSiteLocation, DateTime date)
        {
            var siteBasePath = clientSiteKpiSettings.DropboxImagesDir;
            var dayPathFormat = clientSiteKpiSettings.IsWeekendOnlySite ? "yyyyMMdd - ddd" : "yyyyMMdd";
            var docketsFolder = string.IsNullOrEmpty(clientSiteLocation)
                ? "Dockets - General"
                : $"Dockets - {string.Join("_", clientSiteLocation.Split(Path.GetInvalidFileNameChars()))}";

            return $"{siteBasePath}/FLIR - Wand Recordings - IRs - Daily Logs/{date.Year}/{date:yyyyMM} - {date.ToString("MMMM").ToUpper()} DATA/{date.ToString(dayPathFormat).ToUpper()}/{docketsFolder}/{fileName}";
        }
    }
}
