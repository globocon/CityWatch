using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Data.Models
{
    /// <summary>
    /// Sort options offered by the Tag History report. The value is what the page posts back in
    /// <see cref="TagHistoryReportRequest.SortBy"/>; anything unrecognised falls back to DateTime.
    /// </summary>
    public static class TagHistorySortBy
    {
        public const string DateTime = "DateTime";
        public const string Site = "Site";
        public const string TagUId = "TagUId";
        public const string TagType = "TagType";
        public const string Action = "Action";
        public const string User = "User";
        public const string Guard = "Guard";

        public static readonly IReadOnlyList<KeyValuePair<string, string>> Options = new List<KeyValuePair<string, string>>
        {
            new(DateTime, "Date Time"),
            new(Site, "Site Name"),
            new(TagUId, "Tag UID"),
            new(TagType, "Tag Type"),
            new(Action, "Tag Action"),
            new(User, "User"),
            new(Guard, "Guard"),
        };
    }

    /// <summary>
    /// Filters for the Tag History report (Audit Site Log > Tag History, and the weekly Kpi
    /// schedule). Multi-value filters are comma separated, the way the page's hidden inputs
    /// post a multiselect. An empty site list means every site - which is what the weekly
    /// schedule relies on, so a site created after the schedule was set up is still covered.
    /// </summary>
    public class TagHistoryReportRequest
    {
        public DateTime LogFromDate { get; set; }

        public DateTime LogToDate { get; set; }

        public string ClientSiteId { get; set; }

        public int[] ClientSiteIds => SplitInts(ClientSiteId);

        public string TagUId { get; set; }

        public string[] TagUIds => SplitStrings(TagUId);

        public string TagTypeId { get; set; }

        public int[] TagTypeIds => SplitInts(TagTypeId);

        public string GuardName { get; set; }

        public string GuardSecurityNo { get; set; }

        public string UserName { get; set; }

        /// <summary>One of <see cref="TagHistorySortBy"/>. Blank means Date Time.</summary>
        public string SortBy { get; set; }

        public bool SortDescending { get; set; }

        private static int[] SplitInts(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Array.Empty<int>();

            return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(z => int.TryParse(z, out var id) ? id : (int?)null)
                .Where(z => z.HasValue)
                .Select(z => z.Value)
                .ToArray();
        }

        private static string[] SplitStrings(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Array.Empty<string>();

            return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }

    /// <summary>One row of the Tag History report - a ClientSiteSmartWandTagsHist row resolved to names.</summary>
    public class TagHistoryReportViewModel
    {
        public int Id { get; set; }

        public int TagId { get; set; }

        public int? ClientSiteId { get; set; }

        public string SiteName { get; set; }

        public string ClientTypeName { get; set; }

        /// <summary>Added, Changed, Deleted or Restored.</summary>
        public string Action { get; set; }

        public string Note { get; set; }

        public string TagUId { get; set; }

        public int? TagTypeId { get; set; }

        public string TagType { get; set; }

        public string LabelDescription { get; set; }

        public int? ChangedByUserId { get; set; }

        public int? ChangedByGuardId { get; set; }

        public string UserName { get; set; }

        public string GuardName { get; set; }

        public string GuardInitial { get; set; }

        public string GuardSecurityNo { get; set; }

        /// <summary>The readable actor the history row stored, used when neither id resolves.</summary>
        public string ChangedBy { get; set; }

        public string AppType { get; set; }

        public string AppVersion { get; set; }

        public DateTime ChangedOn { get; set; }

        /// <summary>"Changed [label changed from 'A' to 'B']" - the action with its note.</summary>
        public string TagAction => string.IsNullOrWhiteSpace(Note) ? Action : $"{Action} [{Note}]";

        /// <summary>"Sam Smith [S.S] - Security No: 556-998-3333", blank when no guard acted.</summary>
        public string GuardDisplay
        {
            get
            {
                if (string.IsNullOrWhiteSpace(GuardName))
                    return string.Empty;

                var text = GuardName;
                if (!string.IsNullOrWhiteSpace(GuardInitial))
                    text += $" [{GuardInitial}]";
                if (!string.IsNullOrWhiteSpace(GuardSecurityNo))
                    text += $" - Security No: {GuardSecurityNo}";
                return text;
            }
        }

        /// <summary>The user, or the stored actor name when the change has no user or guard id.</summary>
        public string UserDisplay
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(UserName))
                    return UserName;

                return string.IsNullOrWhiteSpace(GuardName) ? (ChangedBy ?? string.Empty) : string.Empty;
            }
        }

        /// <summary>"23 AUG 2025 @ 12:00 hrs".</summary>
        public string ActionDateTimeText => $"{ChangedOn.ToString("dd MMM yyyy").ToUpper()} @ {ChangedOn:HH:mm} hrs";

        /// <summary>Day heading the table groups rows under.</summary>
        public string GroupText => ChangedOn.ToString("dd MMM yyyy");
    }
}
