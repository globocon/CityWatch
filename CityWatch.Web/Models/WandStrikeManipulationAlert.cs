using System;

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
    }
}
