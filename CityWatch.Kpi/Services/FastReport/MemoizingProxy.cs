using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;

namespace CityWatch.Kpi.Services.FastReport
{
    /// <summary>
    /// Transparent memoising decorator built on <see cref="DispatchProxy"/>.
    ///
    /// The existing report generator issues the same provider calls thousands of times
    /// with identical arguments (see <c>DailyKpiGuard.LEDStatusForLoginUser</c>, which calls
    /// <c>GetHRDescFull()</c> and <c>GetGuardLicensesandcompliance(guardId)</c> once per HR cell
    /// per shift per day per guard). This proxy collapses those to one call each while
    /// returning byte-for-byte the same values, so the rendered PDF cannot change.
    ///
    /// Safety rules:
    ///  - Only methods on an explicit allow-list are cached. Everything else - notably every
    ///    write path - passes straight through to the real provider, untouched.
    ///  - A call whose arguments cannot be turned into a stable key is not cached.
    ///  - Cache hits that return a <see cref="List{T}"/> hand back a fresh list instance
    ///    wrapping the same elements. That mirrors today's behaviour exactly (each call
    ///    already returned a new list over EF's identity-mapped entities), so a caller that
    ///    mutates the returned list cannot corrupt a later caller.
    /// </summary>
    public class MemoizingProxy<T> : DispatchProxy where T : class
    {
        private T _target;
        private ReportScopeCache _cache;
        private IReadOnlyDictionary<string, string> _cacheableMethods;
        private string _interfaceName;

        public static T Create(T target, ReportScopeCache cache, IReadOnlyDictionary<string, string> cacheableMethods)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (cache == null) throw new ArgumentNullException(nameof(cache));

            object proxy = Create<T, MemoizingProxy<T>>();
            var typed = (MemoizingProxy<T>)proxy;
            typed._target = target;
            typed._cache = cache;
            typed._cacheableMethods = cacheableMethods ?? new Dictionary<string, string>();
            typed._interfaceName = typeof(T).Name;
            return (T)proxy;
        }

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            var methodLabel = $"{_interfaceName}.{targetMethod.Name}";

            var isCacheable = _cacheableMethods.TryGetValue(targetMethod.Name, out var friendlyLabel)
                              && targetMethod.ReturnType != typeof(void);

            string key = null;
            if (isCacheable)
            {
                key = BuildKey(methodLabel, args);
                if (key == null)
                {
                    // Arguments we cannot key on - fall back to a pass-through call rather
                    // than risk returning the wrong cached value.
                    isCacheable = false;
                }
                else if (_cache.TryGet(key, out var cached))
                {
                    _cache.RecordHit(methodLabel);
                    // Null label: count the call for progress, but do not churn the step text.
                    _cache.ReportActivity(null);
                    return CloneIfList(cached, targetMethod.ReturnType);
                }
            }

            if (isCacheable && !string.IsNullOrEmpty(friendlyLabel))
                _cache.ReportActivity(friendlyLabel);

            var stopwatch = Stopwatch.StartNew();
            object result;
            try
            {
                result = targetMethod.Invoke(_target, args);
            }
            catch (TargetInvocationException tie) when (tie.InnerException != null)
            {
                stopwatch.Stop();
                _cache.RecordPassThrough(methodLabel, stopwatch.ElapsedTicks);
                ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
                throw; // unreachable, keeps the compiler happy
            }
            stopwatch.Stop();

            if (isCacheable)
            {
                _cache.RecordMiss(methodLabel, stopwatch.ElapsedTicks);
                _cache.Set(key, result);
                return CloneIfList(result, targetMethod.ReturnType);
            }

            _cache.RecordPassThrough(methodLabel, stopwatch.ElapsedTicks);
            return result;
        }

        /// <summary>
        /// Returns a fresh <see cref="List{T}"/> over the same elements so callers cannot
        /// mutate a shared cached instance. Non-list results are returned as-is.
        /// </summary>
        private static object CloneIfList(object value, Type declaredReturnType)
        {
            if (value == null)
                return null;

            if (!declaredReturnType.IsGenericType || declaredReturnType.GetGenericTypeDefinition() != typeof(List<>))
                return value;

            try
            {
                return Activator.CreateInstance(declaredReturnType, new[] { value });
            }
            catch
            {
                // A copy is a nicety, not a correctness requirement for read-only callers.
                return value;
            }
        }

        /// <summary>
        /// Builds a stable, structural cache key. Returns null when any argument cannot be
        /// represented deterministically, which disables caching for that call.
        /// </summary>
        private static string BuildKey(string methodLabel, object[] args)
        {
            var builder = new StringBuilder(methodLabel);

            if (args == null || args.Length == 0)
                return builder.ToString();

            foreach (var arg in args)
            {
                builder.Append('|');
                if (!AppendArg(builder, arg))
                    return null;
            }

            return builder.ToString();
        }

        private static bool AppendArg(StringBuilder builder, object arg)
        {
            switch (arg)
            {
                case null:
                    builder.Append("<null>");
                    return true;
                case string s:
                    builder.Append('"').Append(s).Append('"');
                    return true;
                case bool b:
                    builder.Append(b ? '1' : '0');
                    return true;
                case DateTime dt:
                    builder.Append(dt.Ticks);
                    return true;
                case Enum e:
                    builder.Append(e.GetType().Name).Append(':').Append(Convert.ToInt64(e));
                    return true;
                case IFormattable f when arg.GetType().IsPrimitive || arg is decimal:
                    builder.Append(f.ToString(null, CultureInfo.InvariantCulture));
                    return true;
                case IEnumerable enumerable:
                    builder.Append('[');
                    foreach (var item in enumerable)
                    {
                        if (!AppendArg(builder, item))
                            return false;
                        builder.Append(',');
                    }
                    builder.Append(']');
                    return true;
            }

            // Complex argument objects (e.g. PatrolRequest) - key on their serialised shape.
            try
            {
                builder.Append(JsonSerializer.Serialize(arg, JsonKeyOptions));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static readonly JsonSerializerOptions JsonKeyOptions = new()
        {
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
            MaxDepth = 8,
            WriteIndented = false
        };
    }

    /// <summary>
    /// The allow-list. A method appears here only if it is a pure read used by the report
    /// path, verified against the current implementation. The value is the label shown to
    /// the user while that call is in flight - empty means "do not narrate".
    ///
    /// Anything absent from these maps is never cached.
    /// </summary>
    public static class FastReportCachePolicy
    {
        public static readonly IReadOnlyDictionary<string, string> GuardDataProvider = new Dictionary<string, string>
        {
            // The two calls that dominate the current runtime.
            ["GetHRDescFull"] = "Loading HR document master",
            ["GetGuardLicensesandcompliance"] = "Loading guard compliance",

            ["GetGuards"] = "Loading guards",
            ["GetGuardLogins"] = "Loading guard logins",
            ["GetGuardLoginsWithClientTypesAndSites"] = "Loading guard logins",
            ["GetAllGuardLicensesAndCompliances"] = "Loading licences and compliance",
            ["GetGuardCompliancesList"] = "Loading guard compliance",
            ["GetGuardCompliances"] = "Loading guard compliance",
            ["GetGuardLicenses"] = "Loading guard licences",
            ["GetHRGroups"] = "Loading HR groups",
            ["GetHRDesc"] = "Loading HR document master",
            ["GetInActiveGuardDetails"] = "Loading guard history",
            ["GetGuardLanguages"] = "Loading guard languages"
        };

        public static readonly IReadOnlyDictionary<string, string> ViewDataService = new Dictionary<string, string>
        {
            ["GetKpiReportData"] = "Loading KPI data",
            ["GetMonthlyKpiGuardData"] = "Building guard roster grid",
            ["GetKpiGuardDetailsData"] = "Loading guard details",
            ["GetKpiGuardDetailsCompliance"] = "Loading guard compliance",
            ["GetKpiGuardDetailsComplianceData"] = "Loading guard compliance",
            ["GetKpiGuardDetailsComplianceAndLicense"] = "Loading licences and compliance",
            ["GetKpiGuardDetailsComplianceAndLicenseHR"] = "Loading licences and compliance",
            ["GetKpiGuardDetailsComplianceAndLicenseHRList"] = "Loading licences and compliance",
            ["GetKpiGuardHRGroup"] = "Loading HR groups",
            ["GetHRGroupslist"] = "Loading HR groups",
            ["GetHRSettings"] = "Loading HR settings",
            ["GetHRSettingsCriticalDoc"] = "Loading HR settings",
            ["GetTagStatusPendingForSpecificClientSite"] = "Loading wand tag status",
            ["GetGuards"] = "Loading guards",
            ["GetInActiveGuardDetails"] = "Loading guard history",
            ["GetGuardLanguages"] = "Loading guard languages",
            ["GetGuardLoginsWithClientTypesAndSites"] = "Loading guard logins",
            ["ClientSitesUsingId"] = ""
        };

        public static readonly IReadOnlyDictionary<string, string> ClientDataProvider = new Dictionary<string, string>
        {
            ["GetClientSiteKpiSetting"] = "Loading site settings",
            ["GetClientSites"] = "Loading client sites",
            ["GetClientTypes"] = "Loading client types",
            ["GetClientSiteLogBook"] = ""
        };

        public static readonly IReadOnlyDictionary<string, string> GuardLogDataProvider = new Dictionary<string, string>
        {
            ["GetGuardLogsWithWandStrikes"] = "Loading wand strike data",
            ["GetGuardLogs"] = "Loading guard logs",
            ["GetClientSiteCustomFields"] = "",
            ["GetCustomFieldsByClientSiteId"] = "",
            ["GetActionlist"] = ""
        };

        public static readonly IReadOnlyDictionary<string, string> ClientSiteWandDataProvider = new Dictionary<string, string>
        {
            ["GetClientSiteSmartWandTags"] = "Loading smart wand tags",
            ["GetClientSiteSmartWands"] = "Loading smart wands",
            ["GetClientSitePatrolCars"] = "",
            ["GetPatrolCars"] = ""
        };

        public static readonly IReadOnlyDictionary<string, string> PatrolDataReportService = new Dictionary<string, string>
        {
            ["GetDailyPatrolData"] = "Loading patrol data"
        };
    }
}
