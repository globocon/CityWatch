using System;
using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Models
{
    /// <summary>
    /// P4#153: which mobile app build each guard is running — one row per guard per
    /// platform, upserted every time the app reports in. Deliberately its OWN table:
    /// nothing in the existing login flow reads or writes it, so a missing report (or a
    /// missing table) can never affect a login. A guard with no row is running a build
    /// from before the app started reporting — which is exactly the "old version" signal
    /// the control room needs when triaging an issue.
    /// </summary>
    public class GuardMobileAppVersion
    {
        [Key]
        public int Id { get; set; }

        public int GuardId { get; set; }

        /// <summary>"1.54.2" — whatever the APK says about itself.</summary>
        public string AppVersion { get; set; }

        /// <summary>"android" / "ios".</summary>
        public string Platform { get; set; }

        /// <summary>Optional free text from the device ("Samsung SM-A155F, Android 14").</summary>
        public string DeviceInfo { get; set; }

        public DateTime FirstSeen { get; set; }

        public DateTime LastSeen { get; set; }
    }
}
