using CityWatch.Web.Helpers;
using System;
using System.Collections.Generic;

namespace CityWatch.Web.Helpers
{
    public class FormField
    {
        public string Name { get; set; }
        public string PropName { get; set; }
        public Type PropType { get; set; }

        public FormField()
        {

        }
        public FormField(string name, string propName, Type propType)
        {
            Name = name;
            PropName = propName;
            PropType = propType;
        }
    }

    public static class PdfFormHelper
    {
        public static List<FormField> GetPdfFormFields()
        {
            return new List<FormField>()
            {
                new FormField("HR", "EventType.HrRelated", typeof(bool)),
                new FormField("OH&S", "EventType.OhsMatters", typeof(bool)),
                new FormField("SEC", "EventType.SecurtyBreach", typeof(bool)),
                new FormField("DMG", "EventType.EquipmentDamage", typeof(bool)),
                new FormField("Ti", "EventType.Thermal", typeof(bool)),
                new FormField("FF", "EventType.Emergency", typeof(bool)),
                new FormField("COL", "EventType.SiteColour", typeof(bool)),
                new FormField("RES", "EventType.HealthDepart", typeof(bool)),
                new FormField("GEN", "EventType.GeneralSecurity", typeof(bool)),
                new FormField("ALA", "EventType.AlarmActive", typeof(bool)),
                new FormField("ALD", "EventType.AlarmDisabled", typeof(bool)),
                new FormField("CLI", "EventType.AuthorisedPerson", typeof(bool)),
                new FormField("EQP", "EventType.Equipment", typeof(bool)),
                new FormField("OTR", "EventType.Other", typeof(bool)),
                new FormField("CC-List", "SiteColourCode", typeof(string)),

                new FormField("IR-YES-3a", "WandScannedYes3a", typeof(bool)),
                new FormField("IR-YES-3b", "WandScannedYes3b", typeof(bool)),
                new FormField("IR-NO", "WandScannedNo", typeof(bool)),

                new FormField("IR-YES-BC", "BodyCameraYes", typeof(bool)),
                new FormField("IR-NO-BC", "BodyCameraNo", typeof(bool)),

                new FormField("First Name", "Officer.FirstName", typeof(string)),
                new FormField("Last Name", "Officer.LastName", typeof(string)),
                new FormField("Gender", "Officer.Gender", typeof(string)),
                new FormField("Mobile", "Officer.Phone", typeof(string)),
                new FormField("Position", "Officer.Position", typeof(string)),
                new FormField("Email Address", "Officer.Email", typeof(string)),
                new FormField("LIC No", "Officer.LicenseNumber", typeof(string)),
                new FormField("LIC State", "Officer.LicenseState", typeof(string)),
                new FormField("Callsign", "Officer.CallSign", typeof(string)),
                new FormField("Guard-Exp", "Officer.GuardMonth", typeof(string)),
                new FormField("Notified", "Officer.NotifiedBy", typeof(string)),
                new FormField("Billing", "Officer.Billing", typeof(string)),

                new FormField("Date of Incident", "DateLocation.IncidentDate", typeof(DateTime?)),
                new FormField("Incident Time", "DateLocation.IncidentDate", typeof(DateTime?)),
                new FormField("Date of Report", "DateLocation.ReportDate", typeof(DateTime)),
                new FormField("Report Time", "DateLocation.ReportDate", typeof(DateTime)),
                new FormField("IR-YES9", "DateLocation.ReimbursementYes", typeof(bool)),
                new FormField("IR-NO9", "DateLocation.ReimbursementNo", typeof(bool)),
                new FormField("Job No", "DateLocation.JobNumber", typeof(string)),
                new FormField("Job Time", "DateLocation.JobTime", typeof(string)),
                new FormField("Duration", "DateLocation.Duration", typeof(int?)),
                new FormField("Travel", "DateLocation.Travel", typeof(int?)),
                new FormField("PTL-EX", "DateLocation.PatrolInternal", typeof(bool)),
                new FormField("PTL-IN", "DateLocation.PatrolExternal", typeof(bool)),
                new FormField("Australian State", "DateLocation.State", typeof(string)),
                new FormField("Client_Type", "DateLocation.ClientType", typeof(string)),
                new FormField("Client_Site", "DateLocation.ClientSite", typeof(string)),
                new FormField("Area", "DateLocation.ClientArea", typeof(string)),
                new FormField("Client-Add", "DateLocation.ClientAddress", typeof(string)),
                new FormField("Report", "Feedback", typeof(string)),
                new FormField("Supervisor Reported To", "ReportedBy", typeof(string)),
                new FormField("SN", "SerialNumber", typeof(string)),
                new FormField("LINKED-IR", "LinkedSerialNos", typeof(string)),
                new FormField("GPS", "DateLocation.ClientSiteLiveGpsInDegrees", typeof(string))
            };
        }
    }
}
