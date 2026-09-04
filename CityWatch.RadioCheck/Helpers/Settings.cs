namespace CityWatch.RadioCheck.Helpers
{
    public class Settings
    {
        public const string Name = "Settings";

        public string IrApiUrl { get; set; }

        public string WandApiUrl { get; set; }

        public string WandApiToken { get; set; }

        public string DropboxUserEmail { get; set; }

        public string DropboxAccessToken { get; set; }

        public string DropboxRefreshToken { get; set; }
        
        public string DropboxAppKey { get; set; }
        
        public string DropboxAppSecret { get; set; }

        public bool GuardListOn { get; set; }
        public string RCActionListKpiImageFolder { get; set; }

    }
}
