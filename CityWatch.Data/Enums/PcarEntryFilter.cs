namespace CityWatch.Data.Enums
{
    /// <summary>
    /// Which entries of a site log report to keep, by whether a patrol car made them
    /// (GuardLog.IsEntryByPCAR).
    /// </summary>
    public enum PcarEntryFilter
    {
        /// <summary>Every entry - the report exactly as it has always been.</summary>
        All = 0,

        /// <summary>"Disable PCAR" on the Schedule tab: patrol car entries left out.</summary>
        ExcludePcar = 1,

        /// <summary>The "Schedule PCAR" dumps: patrol car entries only.</summary>
        OnlyPcar = 2
    }
}
