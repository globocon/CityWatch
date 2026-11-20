using CityWatch.Data.Enums;
using CityWatch.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace CityWatch.Data.Helpers
{
    /// <summary>
    /// Patrol Randomisation rules, shared by the Kpi settings page (validation) and the Web scan
    /// API (is the window open, which tag next). Kept free of EF and DI so it is unit tested
    /// directly.
    ///
    /// Rules agreed with the client (modelled on Contracted Manning):
    ///   - only in Standard (STND) touring mode - PCAR and INSP switch it off;
    ///   - off by default;
    ///   - any number of days a week, one window per day of at most two hours, start before
    ///     end on that day, in the site's local time.
    /// </summary>
    public static class PatrolRandomisationHelper
    {
        public static readonly TimeSpan MaxWindow = TimeSpan.FromHours(2);

        /// <summary>A day's start/end as posted from the settings grid.</summary>
        public class DayWindow
        {
            public DayOfWeek Day { get; set; }
            public TimeSpan? Start { get; set; }
            public TimeSpan? End { get; set; }
        }

        /// <summary>
        /// Validates the settings grid and reduces it to the windows to store, one per day.
        /// Days with neither time set are ignored. Returns an error message, or null when valid.
        /// </summary>
        public static string Validate(PatrolTouringMode mode, bool isEnabled, IEnumerable<DayWindow> days,
            out List<DayWindow> windows)
        {
            windows = new List<DayWindow>();
            var filled = (days ?? Enumerable.Empty<DayWindow>())
                .Where(d => d != null && (d.Start.HasValue || d.End.HasValue))
                .ToList();

            if (isEnabled && mode != PatrolTouringMode.STND)
                return "Patrol Randomisation can only be enabled when Patrol Touring Mode is Standard (STND).";

            if (filled.Count == 0)
                return isEnabled ? "Enter a start and end time for at least one day." : null;

            var duplicate = filled.GroupBy(d => d.Day).FirstOrDefault(g => g.Count() > 1);
            if (duplicate != null)
                return $"{duplicate.Key} is entered more than once.";

            foreach (var day in filled.OrderBy(d => DayOrder(d.Day)))
            {
                if (!day.Start.HasValue || !day.End.HasValue)
                    return $"Enter both a start and an end time for {day.Day}.";

                if (day.End.Value <= day.Start.Value)
                    return $"{day.Day}: end time must be after the start time (the window cannot cross midnight).";

                if (day.End.Value - day.Start.Value > MaxWindow)
                    return $"{day.Day}: Patrol Randomisation can run for a maximum of 2 hours a day.";

                windows.Add(day);
            }
            return null;
        }

        /// <summary>Monday first, the way the settings grid shows the week.</summary>
        public static int DayOrder(DayOfWeek day) => ((int)day + 6) % 7;

        /// <summary>
        /// The window open at this site-local time: enabled, STND, a schedule for that day, and
        /// start &lt;= time &lt; end. Null when randomisation does not apply right now.
        /// </summary>
        public static ClientSitePatrolRandomisationSchedule FindActiveWindow(ClientSitePatrolRandomisationSetting setting,
            PatrolTouringMode mode, DateTime siteLocalNow)
        {
            if (setting == null || !setting.IsEnabled || mode != PatrolTouringMode.STND || setting.Schedules == null)
                return null;

            var t = siteLocalNow.TimeOfDay;
            return setting.Schedules.FirstOrDefault(s =>
                s.DayOfWeek == siteLocalNow.DayOfWeek && t >= s.StartTime && t < s.EndTime);
        }

        public static bool IsWindowActive(ClientSitePatrolRandomisationSetting setting, PatrolTouringMode mode,
            DateTime siteLocalNow) => FindActiveWindow(setting, mode, siteLocalNow) != null;

        /// <summary>"Mon 02:00-03:59, Thu 10:00-12:00" for display.</summary>
        public static string Describe(IEnumerable<ClientSitePatrolRandomisationSchedule> schedules) =>
            string.Join(", ", (schedules ?? Enumerable.Empty<ClientSitePatrolRandomisationSchedule>())
                .OrderBy(s => DayOrder(s.DayOfWeek))
                .Select(s => s.DayOfWeek.ToString().Substring(0, 3) + " " + s.StartTime.ToString(@"hh\:mm") + "-" + s.EndTime.ToString(@"hh\:mm")));

        /// <summary>
        /// Picks the next checkpoint. Works like a shuffled deck: tags already handed to this guard
        /// in the current window are left out until every tag has been used, so the whole site
        /// is still covered but the order cannot be learned. The tag just struck is never
        /// returned (unless the site has only one tag, in which case there is nothing to pick
        /// and null is returned).
        ///
        /// Within the deck the pick is weighted: checkpoints missed in the last 72 hours
        /// (<paramref name="priorityTagUIds"/>) are <see cref="PriorityWeight"/> times as likely as the
        /// rest, so neglected points get visited sooner while the route stays unpredictable.
        /// </summary>
        /// <param name="randomIndex">For tests; defaults to a cryptographic RNG. Called with the total weight.</param>
        public static ClientSiteSmartWandTags PickNextTag(IReadOnlyList<ClientSiteSmartWandTags> siteTags,
            string currentTagUId, IEnumerable<string> alreadyAssignedThisWindow, Func<int, int> randomIndex = null,
            IEnumerable<string> priorityTagUIds = null)
        {
            if (siteTags == null || siteTags.Count == 0)
                return null;

            randomIndex ??= RandomNumberGenerator.GetInt32;

            var notCurrent = siteTags
                .Where(t => !string.Equals(t.UId, currentTagUId, StringComparison.OrdinalIgnoreCase))
                .GroupBy(t => t.UId, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
            if (notCurrent.Count == 0)
                return null;

            var used = new HashSet<string>(alreadyAssignedThisWindow ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);
            var fresh = notCurrent.Where(t => !used.Contains(t.UId)).ToList();
            var pool = fresh.Count > 0 ? fresh : notCurrent;

            var priority = new HashSet<string>(priorityTagUIds ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            var weights = pool.Select(t => priority.Contains(t.UId) ? PriorityWeight : 1).ToList();
            var roll = randomIndex(weights.Sum());
            for (var i = 0; i < pool.Count; i++)
            {
                if (roll < weights[i])
                    return pool[i];
                roll -= weights[i];
            }
            return pool[pool.Count - 1];
        }

        /// <summary>How much more likely a checkpoint missed in the last 72 hours is to be picked.</summary>
        public const int PriorityWeight = 3;

        /// <summary>How far back "missed" looks when weighting the pick.</summary>
        public static readonly TimeSpan PriorityLookback = TimeSpan.FromHours(72);
    }
}
