using CityWatch.Data.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Helpers
{
    public static class GoogleMapHelper
    {
        public static void DownloadGpsImage(string gpsImageDir, ClientSite clientSite, GoogleMapSettings mapSettings)
        {
            if (!Directory.Exists(gpsImageDir))
                Directory.CreateDirectory(gpsImageDir);

            var gps = clientSite.Gps;
            var url = $"https://maps.googleapis.com/maps/api/staticmap?center={gps}&zoom={mapSettings.GpsImageZoom}&size={mapSettings.GpsImageSize}&markers={gps}&key={mapSettings.ApiKey}";
            WebRequest request = WebRequest.Create(url);
            var response = request.GetResponse();
            using (Stream dataStream = response.GetResponseStream())
            {
                using (FileStream fs = new FileStream(Path.Combine(gpsImageDir, $"Client_{clientSite.Id}.jpg"), FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    dataStream.CopyTo(fs);
                }
            }
            response.Close();
        }

        public static string DownloadGpsImage(string gpsImageDir, string gpsCoordinates, GoogleMapSettings mapSettings)
        {
            if (!Directory.Exists(gpsImageDir))
                Directory.CreateDirectory(gpsImageDir);

            var imageFileName = Path.Combine(gpsImageDir, $"Gps_{gpsCoordinates}_{DateTime.Now:ddMMyyyyHHmmss}.jpg");
            var url = $"https://maps.googleapis.com/maps/api/staticmap?center={gpsCoordinates}&zoom={mapSettings.GpsImageZoom}&size={mapSettings.GpsImageSize}&markers={gpsCoordinates}&key={mapSettings.ApiKey}";
            WebRequest request = WebRequest.Create(url);
            var response = request.GetResponse();
            using (Stream dataStream = response.GetResponseStream())
            {
                using (FileStream fs = new FileStream(imageFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    dataStream.CopyTo(fs);
                }
            }
            response.Close();

            return imageFileName;
        }

        public static string GpsInDegree(string gpsCoordinates)
        {
            if (string.IsNullOrEmpty(gpsCoordinates))
                return string.Empty;

            var values = gpsCoordinates.Split(',');
            if (values.Length < 2)
                return string.Empty;

            _ = double.TryParse(values[0], out double latitude);
            _ = double.TryParse(values[1], out double longitude);            
            var latDir = latitude >= 0 ? "N" : "S";
            latitude = Math.Abs(latitude);
            var latMinPart = (latitude - Math.Truncate(latitude) / 1) * 60;
            var latSecPart = (latMinPart - Math.Truncate(latMinPart) / 1) * 60;
            var lat = $"{Math.Truncate(latitude)}.{Math.Truncate(latMinPart)}{Math.Truncate(latSecPart)}\u00B0 {latDir}";

            var lonDir = longitude >= 0 ? "E" : "W";
            longitude = Math.Abs(longitude);
            var lonMinPart = (longitude - Math.Truncate(longitude) / 1) * 60;
            var lonSecPart = (lonMinPart - Math.Truncate(lonMinPart) / 1) * 60;
            var lon = $"{Math.Truncate(longitude)}.{Math.Truncate(lonMinPart)}{Math.Truncate(lonSecPart)}\u00B0 {lonDir}";
            return $"{lat}, {lon}"; 
        }
    }
}
