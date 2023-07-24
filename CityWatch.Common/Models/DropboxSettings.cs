namespace CityWatch.Common.Models
{
    public class DropboxSettings
    {
        public DropboxSettings(string appKey, string appSecret, string accessToken, string refreshToken, string userEmail)
        {
            AppKey = appKey;
            AppSecret = appSecret;
            AccessToken = accessToken;
            RefreshToken = refreshToken;
            UserEmail = userEmail;
        }

        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }

        public string AppKey { get; set; }

        public string AppSecret { get; set; }

        public string UserEmail { get; set; }
    }
}
