using System;
using System.Collections.Generic;
using CityWatch.Data.Models;

namespace CityWatch.Web.Models
{
    /// <summary>
    /// P1#375: what the "Potential Wand Strike Manipulation" email reports - one guard hitting
    /// the same NFC checkpoint several times in a row instead of walking the patrol.
    ///
    /// Carries only what the email prints, already resolved to names and to the site's own local
    /// time, so composing the message needs no further lookups.
    /// </summary>
    public class WandStrikeManipulationAlert
    {
        public int ClientSiteId { get; set; }

        /// <summary>Site the guard is signed in at, as it should read in the email.</summary>
        public string ClientSiteName { get; set; }

        public int GuardId { get; set; }
        public string GuardName { get; set; }

        /// <summary>Tag UID as scanned - the identifier an admin can search the tag history for.</summary>
        public string TagUId { get; set; }

        /// <summary>The tag's label, e.g. the checkpoint description. Empty for an unknown tag.</summary>
        public string TagLabelDescription { get; set; }

        /// <summary>How many times in a row the tag was hit, at the moment the alert fired.</summary>
        public int ConsecutiveHitCount { get; set; }

        /// <summary>The hit that tripped the alert, in UTC as stored.</summary>
        public DateTime HitUtcDateTime { get; set; }

        /// <summary>
        /// The same hit in the SITE's local time. Sites span AU timezones and the server's own
        /// clock is not the site's, so this is what the email shows.
        /// </summary>
        public DateTime HitSiteLocalDateTime { get; set; }

        /// <summary>Site UTC offset the local time was derived with, printed so the time is unambiguous.</summary>
        public string SiteUtcOffset { get; set; }

        /// <summary>Times of the run, oldest first, in site local time. Lets the reader judge the spacing.</summary>
        public DateTime[] RunSiteLocalDateTimes { get; set; } = Array.Empty<DateTime>();

        /// <summary>True when the run was found while syncing scans the handset had cached offline.</summary>
        public bool IsOfflineRecord { get; set; }

        /// <summary>
        /// P1#375: the mobile build this guard's app reported most recently, e.g. "1.55.7".
        ///
        /// Taken from GuardMobileAppVersion (what the app reports at login) rather than from
        /// the scan request, so no app change is needed and builds released before this are
        /// still attributed. The hit log records no device, so for a guard who carries two this
        /// is their latest report rather than necessarily the handset that made the strike -
        /// which is why <see cref="MobileAppPlatform"/> is shown with it.
        ///
        /// Null when the guard has never reported - itself worth seeing, since it means a build
        /// from before the app began reporting its version.
        /// </summary>
        public string MobileAppVersion { get; set; }

        /// <summary>Device type that report came from ("android" / "ios"), when known.</summary>
        public string MobileAppPlatform { get; set; }
    }
}
