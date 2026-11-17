using System.Reflection;

namespace CityWatch.Data.Models
{
    /// <summary>The applications that can change a wand tag.</summary>
    public static class WandTagAppType
    {
        public const string Web = "Web";
        public const string Android = "Android";
        public const string Ios = "iOS";

        /// <summary>
        /// Settles the spelling the mobile app happened to send - "android", "ANDROID", "iphone"
        /// - onto one of the three constants above, so the column can be grouped on. Anything
        /// unrecognised is kept as it came, since a wrong label is worse than an odd one.
        /// </summary>
        public static string Normalise(string appType)
        {
            if (string.IsNullOrWhiteSpace(appType))
                return null;

            var trimmed = appType.Trim();

            if (trimmed.Equals(Web, System.StringComparison.OrdinalIgnoreCase))
                return Web;

            if (trimmed.Equals(Android, System.StringComparison.OrdinalIgnoreCase))
                return Android;

            if (trimmed.Equals(Ios, System.StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("iphone", System.StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("ipad", System.StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("apple", System.StringComparison.OrdinalIgnoreCase))
                return Ios;

            return trimmed;
        }
    }

    /// <summary>
    /// Who is changing a wand tag, and from where. Handed to the save and delete methods on
    /// IClientSiteWandDataProvider, which record it against the change in
    /// ClientSiteSmartWandTagsHist.
    ///
    /// A single object rather than four trailing parameters because a user id, a guard id and
    /// two strings are easy to transpose at a call site and impossible to spot afterwards - the
    /// change would simply be attributed to the wrong person.
    ///
    /// Everything on it is optional. Null, or a change saved without one of these at all, still
    /// saves the tag and still writes the history row; it just has less to say about who did it.
    /// </summary>
    public class WandTagChange
    {
        /// <summary>The signed-in user, 0 when it is a guard or not known.</summary>
        public int UserId { get; set; }

        /// <summary>The guard, 0 when it is a user or not known.</summary>
        public int GuardId { get; set; }

        /// <summary>One of <see cref="WandTagAppType"/>.</summary>
        public string AppType { get; set; }

        /// <summary>
        /// The build making the change, e.g. "1.54.2". The mobile app need not send it: when it
        /// is blank and a guard is known, the provider falls back to the version that guard's
        /// app last reported on login, so builds released before this still get attributed.
        /// </summary>
        public string AppVersion { get; set; }

        /// <summary>
        /// A change made from one of the web portals by the given user, stamped with that
        /// portal's build.
        /// </summary>
        /// <param name="app">
        /// The assembly whose version to record - pass Assembly.GetExecutingAssembly() from the
        /// calling portal, which is the same expression its own page footer and report headers
        /// use, so the history shows the version a user reading the footer would quote back.
        /// Left out, it falls back to the entry assembly, which under ASP.NET Core is the
        /// running application; GetExecutingAssembly cannot be used from in here, as that would
        /// only ever name CityWatch.Data.
        /// </param>
        public static WandTagChange FromWeb(int userId, int guardId = 0, Assembly app = null)
        {
            return new WandTagChange
            {
                UserId = userId,
                GuardId = guardId,
                AppType = WandTagAppType.Web,
                AppVersion = (app ?? Assembly.GetEntryAssembly())?.GetName()?.Version?.ToString()
            };
        }
    }
}
