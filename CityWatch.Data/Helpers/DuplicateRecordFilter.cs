using CityWatch.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Data.Helpers
{
    /// <summary>
    /// Read-time removal of duplicate activity rows before they are shown or put in a report.
    ///
    /// Why it is needed: the mobile app can send the same NFC/BLE scan to the server twice (or three
    /// times) within a few milliseconds. The server's rescan-window check in
    /// MobileAppDataServices.CreateSmartWandScannerHitLogRecord (P1#375: 5 minutes, was 60 seconds)
    /// reads the last hit and then inserts, so concurrent requests all pass it however long the
    /// window is - widening it does not help here. Each request writes its own ClientSiteSmartWandTagsHitLogs
    /// row and its own GuardLogs row, and the Insert_GuardLogs / Trigger_RcHistory triggers then write
    /// one ClientSiteRadioChecksActivityStatus_History row per GuardLogs row - which is what Fusion
    /// reads. Seen on VISY-Coolaroo [VR] (FSO) for one guard's device: identical HitUtcDateTime to the
    /// millisecond, separate hit-log ids, IsOfflineRecord = 0.
    ///
    /// Two rows are duplicates when their time matches to the minute (seconds and milliseconds are
    /// ignored) and the rest of what is displayed matches. The row with the lowest Id is kept and
    /// the input order is preserved.
    /// </summary>
    public static class DuplicateRecordFilter
    {
        /// <summary>Truncates to the minute: 22:20:44.957 and 22:20:49.913 both become 22:20.</summary>
        public static DateTime ToMinute(DateTime value) =>
            new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, value.Kind);

        private static string Norm(string value) => (value ?? string.Empty).Trim();

        /// <summary>
        /// Fusion rows (ClientSiteRadioChecksActivityStatus_History): same site, guard, source
        /// (activity type), event/notes text and minute. Call it after EventDateTime and Notes have
        /// been adjusted for display, so the comparison uses what the user actually sees.
        /// </summary>
        public static List<ClientSiteRadioChecksActivityStatus_History> RemoveDuplicateFusionLogs(
            IEnumerable<ClientSiteRadioChecksActivityStatus_History> rows)
        {
            if (rows == null)
                return new List<ClientSiteRadioChecksActivityStatus_History>();

            return KeepFirstById(rows.ToList(), x => x.Id, x => (
                x.ClientSiteId,
                x.GuardId,
                GuardName: Norm(x.GuardName),
                ActivityType: Norm(x.ActivityType).ToUpperInvariant(),
                Notes: Norm(x.Notes),
                // Part of the displayed text for KV rows in some readers; keeps distinct KV
                // entries whose Notes column alone is generic from being merged.
                ActivityDescription: Norm(x.ActivityDescription),
                Minute: ToMinute(x.EventDateTime)));
        }

        /// <summary>
        /// Wand strike rows (ClientSiteSmartWandTagsHitLogs): same logged-in site, guard, wand,
        /// tag, label and minute.
        /// </summary>
        public static List<ClientSiteSmartWandTagsHitLog> RemoveDuplicateTagHits(
            IEnumerable<ClientSiteSmartWandTagsHitLog> rows)
        {
            if (rows == null)
                return new List<ClientSiteSmartWandTagsHitLog>();

            return KeepFirstById(rows.ToList(), x => x.Id, x => (
                x.LoggedInClientSiteId,
                x.LoggedInGuardId,
                x.SmartWandId,
                TagUId: Norm(x.TagUId).ToUpperInvariant(),
                LabelDescription: Norm(x.LabelDescription),
                Minute: ToMinute(x.HitUtcDateTime)));
        }

        private static List<T> KeepFirstById<T, TKey>(List<T> list, Func<T, int> idOf, Func<T, TKey> keyOf) where T : class
        {
            if (list.Count < 2)
                return list;

            // Decide which row survives in Id order (the first one written), then filter the
            // original list so the caller's ordering is untouched.
            var seen = new HashSet<TKey>();
            var keep = new HashSet<T>(ReferenceEqualityComparer.Instance);
            foreach (var row in list.OrderBy(idOf))
            {
                if (seen.Add(keyOf(row)))
                    keep.Add(row);
            }

            return keep.Count == list.Count ? list : list.Where(keep.Contains).ToList();
        }
    }
}
