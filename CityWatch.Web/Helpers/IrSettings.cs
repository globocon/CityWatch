namespace CityWatch.Web.Helpers
{
    public static class IrSettings
    {
        public const string GeneralPatrolJobNumber = "G";
    }

    public class Settings
    {
        public const string Name = "Settings";

        public string KpiWebUrl { get; set; }

        public string DropboxUserEmail { get; set; }

        public string DropboxAccessToken { get; set; }

        public string DropboxRefreshToken { get; set; }

        public string DropboxAppKey { get; set; }

        public string DropboxAppSecret { get; set; }
        public string WebActionListKpiImageFolder { get; set; }
    }
}
