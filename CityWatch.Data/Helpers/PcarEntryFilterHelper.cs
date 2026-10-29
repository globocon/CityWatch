using CityWatch.Data.Enums;
using CityWatch.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Data.Helpers
{
    /// <summary>
    /// The PCAR rules of the site log dump scheduler, in one place so the daily, weekly and monthly
    /// runs and the report generator cannot drift apart.
    /// </summary>
    public static class PcarEntryFilterHelper
    {
        /// <summary>Guard log report: filtered on the entry's own flag.</summary>
        public static List<GuardLog> Apply(IEnumerable<GuardLog> guardLogs, PcarEntryFilter filter)
        {
            return filter switch
            {
                PcarEntryFilter.ExcludePcar => guardLogs.Where(x => !x.IsEntryByPCAR).ToList(),
                PcarEntryFilter.OnlyPcar => guardLogs.Where(x => x.IsEntryByPCAR).ToList(),
                _ => guardLogs.ToList()
            };
        }

        /// <summary>
        /// Smart wand and fusion reports, which read ClientSiteRadioChecksActivityStatus_History.
        /// A history row carries no PCAR flag of its own; LB and SW rows point at their guard log
        /// through LBId, and <paramref name="pcarGuardLogIds"/> is the set of those guard logs that
        /// a patrol car made. KV and IR rows have no PCAR marker at all, so they are never PCAR
        /// entries: kept when PCAR is excluded, dropped when only PCAR is wanted.
        /// </summary>
        public static List<ClientSiteRadioChecksActivityStatus_History> Apply(
            IEnumerable<ClientSiteRadioChecksActivityStatus_History> rows,
            ISet<int> pcarGuardLogIds,
            PcarEntryFilter filter)
        {
            if (filter == PcarEntryFilter.All)
                return rows.ToList();

            bool IsPcar(ClientSiteRadioChecksActivityStatus_History row) =>
                row.LBId.HasValue && pcarGuardLogIds.Contains(row.LBId.Value);

            return filter == PcarEntryFilter.ExcludePcar
                ? rows.Where(x => !IsPcar(x)).ToList()
                : rows.Where(IsPcar).ToList();
        }

        /// <summary>
        /// The filter the normal (Schedule tab) LB/SW/Fusion dump of a period uses: PCAR entries
        /// excluded when "Disable PCAR" is on for that period, unless the site is a PCAR site.
        /// </summary>
        public static PcarEntryFilter GetScheduleFilter(ClientSite site, LogDumpPeriodType? periodType)
        {
            if (site == null || !site.IsDisablePcarApplicable)
                return PcarEntryFilter.All;

            var disabled = periodType switch
            {
                null => site.DisablePcarDailyLog,
                LogDumpPeriodType.Weekly => site.DisablePcarWeeklyLog,
                LogDumpPeriodType.Monthly => site.DisablePcarMonthlyLog,
                _ => false
            };

            return disabled ? PcarEntryFilter.ExcludePcar : PcarEntryFilter.All;
        }

        /// <summary>The marker every "Schedule PCAR" file name ends with, before the extension.</summary>
        public const string PcarReportFileSuffix = "PCAR-Report";

        /// <summary>"x.pdf" becomes "x - PCAR-Report.pdf".</summary>
        public static string AppendPcarReportSuffix(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return fileName;

            var extension = System.IO.Path.GetExtension(fileName);
            var name = fileName.Substring(0, fileName.Length - extension.Length);
            return $"{name} - {PcarReportFileSuffix}{extension}";
        }

        public static bool IsPcarReportType(LogBookType type) =>
            type == LogBookType.PcarGuardLog || type == LogBookType.PcarSmartWandLog || type == LogBookType.PcarFusionLog;

        /// <summary>The report a PCAR type is built with: PcarGuardLog is the guard log report, etc.</summary>
        public static LogBookType GetBaseReportType(LogBookType type) => type switch
        {
            LogBookType.PcarGuardLog => LogBookType.DailyGuardLog,
            LogBookType.PcarSmartWandLog => LogBookType.SmartWandLog,
            LogBookType.PcarFusionLog => LogBookType.FusionLog,
            _ => type
        };
    }
}
