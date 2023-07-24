using System.ComponentModel;

namespace CityWatch.Data.Enums
{
    public enum IrEventType
    {
        [Description("HR Related")]
        HrRelated = 1,

        [Description("OH&S / Patrol KPI Issues")]
        OhsMatters,

        [Description("Security / Site Policy Breach")]
        SecurtyBreach,

        [Description("Equipment damage / Missing Items")]
        EquipmentDamage,

        [Description("CCTV related OR Thermal Temp")]
        Thermal,

        [Description("Emergency Services / LEA on Site")]
        Emergency,

        [Description("Site COLOUR Code Alert")]
        SiteColour,

        [Description("DHHS - Restraints / Seclusion / SASH Watch")]
        HealthDepart,

        [Description("Security Patrols / Site \"Lock-up\" / Site \"Unlock\"")]
        GeneralSecurity,

        [Description("Alarm is Active (Alarm Panel, FIP, VESDA, duress)")]
        AlarmActive,

        [Description("Alarm is Disabled (Late to Close / site not sealed)")]
        AlarmDisabled,

        [Description("Client was still onsite (ie; not an intruder)")]
        AuthorisedPerson,

        [Description("Carrying a Batton & Cuffs / Firearm / Ballistic Vest")]
        Equipment,

        [Description("Other Categories, including Feedback & Suggestions")]
        Other
    }
}