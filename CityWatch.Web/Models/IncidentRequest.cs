using CityWatch.Data.Enums;
using CityWatch.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DataHelper = CityWatch.Data.Helpers;

namespace CityWatch.Web.Models
{
    public enum PatrolType
    {
        None = 0,

        Alarm = 1,

        General = 2
    }

    public class IncidentRequest
    {
        public int Id { get; set; }

        public string ReportReference { get; set; }

        public EventType EventType { get; set; }

        [Display(Name = "Yes (3a)")]
        public bool WandScannedYes3a { get; set; }

        [Display(Name = "Yes (3b)")]
        public bool WandScannedYes3b { get; set; }

        [Display(Name = "NO (n/a)")]
        public bool WandScannedNo { get; set; }

        [Display(Name = "Yes")]
        public bool BodyCameraYes { get; set; }

        [Display(Name = "NO (n/a)")]
        public bool BodyCameraNo { get; set; }

        public Officer Officer { get; set; }

        public DateLocation DateLocation { get; set; }

        [Display(Name = "Situation / Incident Report or Feedback:")]
        public string Feedback { get; set; }        
        
        public string SiteColourCode { get; set; }
        
        public int? SiteColourCodeId { get; set; }

        public string LinkedSerialNos { get; set; }
        [Display(Name = "No(n/a)")]
        public bool PlateLoadedNo { get; set; }

        [Display(Name = "Yes")]
        public bool PlateLoadedYes { get; set; }
        public string VehicleRego { get; set; }
        public int? PlateId { get; set; }

        [Display(Name = "Supervisor or person you reported this to:")]
        public string ReportedBy { get; set; }

        public List<string> Attachments { get; set; }

        public bool IsPositionPatrolCar { get; set; }
        

        public string OccurrenceNo
        {
            get
            {
                if (!IsPositionPatrolCar)
                    return null;

                return $"{Officer.Position.Substring(Officer.Position.Length - 2).Trim()}{DateLocation.JobNumber}";
            }
        }

        public PatrolType PatrolType
        {
            get
            {
                if (IsPositionPatrolCar)
                {
                    if (IrSettings.GeneralPatrolJobNumber.Equals(DateLocation.JobNumber, StringComparison.OrdinalIgnoreCase))
                        return PatrolType.General;
                    else
                        return PatrolType.Alarm;
                }

                return PatrolType.None;
            }
        }

        public string SerialNumber { get; set; }

        public List<IrEventType> IrEventTypes
        {
            get
            {
                var eventTypes = new List<IrEventType>();

                if (EventType != null)
                {
                    if (EventType.HrRelated) eventTypes.Add(IrEventType.HrRelated);
                    if (EventType.OhsMatters) eventTypes.Add(IrEventType.OhsMatters);
                    if (EventType.SecurtyBreach) eventTypes.Add(IrEventType.SecurtyBreach);
                    if (EventType.EquipmentDamage) eventTypes.Add(IrEventType.EquipmentDamage);
                    if (EventType.Thermal) eventTypes.Add(IrEventType.Thermal);
                    if (EventType.Emergency) eventTypes.Add(IrEventType.Emergency);
                    if (EventType.SiteColour) eventTypes.Add(IrEventType.SiteColour);
                    if (EventType.HealthDepart) eventTypes.Add(IrEventType.HealthDepart);
                    if (EventType.GeneralSecurity) eventTypes.Add(IrEventType.GeneralSecurity);
                    if (EventType.AlarmActive) eventTypes.Add(IrEventType.AlarmActive);
                    if (EventType.AlarmDisabled) eventTypes.Add(IrEventType.AlarmDisabled);
                    if (EventType.AuthorisedPerson) eventTypes.Add(IrEventType.AuthorisedPerson);
                    if (EventType.Equipment) eventTypes.Add(IrEventType.Equipment);
                    if (EventType.Other) eventTypes.Add(IrEventType.Other);
                }

                return eventTypes;
            }
        }

        public string PSPFName { get; set; }

        public string HASH { get; set; }
        public string IP { get; set; }
        
        public int? FeedbackType { get; set; }
        public int? FeedbackTemplates { get; set; }

        public ReportCreatedLocalTimeZone ReportCreatedLocalTimeZone { get; set; }

    }

    public class EventType
    {
        public bool HrRelated { get; set; }
        public bool OhsMatters { get; set; }
        public bool SecurtyBreach { get; set; }
        public bool EquipmentDamage { get; set; }
        public bool Thermal { get; set; }
        public bool Emergency { get; set; }
        public bool SiteColour { get; set; }
        public bool HealthDepart { get; set; }
        public bool GeneralSecurity { get; set; }
        public bool AlarmActive { get; set; }
        public bool AlarmDisabled { get; set; }
        public bool AuthorisedPerson { get; set; }
        public bool Equipment { get; set; }
        public bool Other { get; set; }
    }

    public class Officer
    {
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        public string Gender { get; set; }

        [Display(Name = "Mobile or Landline No.")]
        public string Phone { get; set; }

        public string Position { get; set; }

        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Display(Name = "Security License No.")]
        public string LicenseNumber { get; set; }

        [Display(Name = "License State")]
        public string LicenseState { get; set; }

        [Display(Name = "Your Callsign")]
        public string CallSign { get; set; }

        [Required(ErrorMessage = "Guard months or years on site is required")]
        [Display(Name = "Guard Months or Years on site ")]
        public string GuardMonth { get; set; }

        [Display(Name = "Notified By")]
        public string NotifiedBy { get; set; }

        public string Billing { get; set; }
    }

    public class DateLocation
    {
        [Display(Name = "Date / Time of Incident or Patrol Onsite")]
        public DateTime? IncidentDate { get; set; }

        [Required(ErrorMessage = "Report date is required")]
        [Display(Name = "Date / Time of Report or Patrol Offsite")]
        public DateTime ReportDate { get; set; }

        [Display(Name = "NO (n/a)")]
        public bool ReimbursementNo { get; set; }

        [Display(Name = "YES (Receipt & Attached)")]
        public bool ReimbursementYes { get; set; }

        [Display(Name = "J:")]
        public string JobNumber { get; set; }

        [Display(Name = "T:")]
        public string JobTime { get; set; }

        [Display(Name = "Du:")]
        public int? Duration { get; set; }

        [Display(Name = "Tr:")]
        public int? Travel { get; set; }

        [Display(Name = "External")]
        public bool PatrolExternal { get; set; }

        [Display(Name = "Internal")]
        public bool PatrolInternal { get; set; }

        [Display(Name = "Client State")]
        public string State { get; set; }

        [Required(ErrorMessage = "Client Type is required")]
        [Display(Name = "Client Type")]
        public string ClientType { get; set; }

        [Required(ErrorMessage = "Client Site is required")]
        [Display(Name = "Client Site")]
        public string ClientSite { get; set; }

        [Display(Name = "Area / Ward")]
        [StringLength(50, ErrorMessage = "The {0} value cannot exceed {1} characters.")]
        public string ClientArea { get; set; }

        [Display(Name = "Client Address")]
        public string ClientAddress { get; set; }

        [Display(Name = "Client Status")]
        public int ClientStatus { get; set; }

        public DateTime? ClientStatusDate { get; set; }

        [HiddenInput]
        public string ClientSiteLiveGps { get; set; }

       

        public string ClientSiteLiveGpsInDegrees 
        {
            get
            {
                if (!string.IsNullOrEmpty(ClientSiteLiveGps))
                    return DataHelper.GoogleMapHelper.GpsInDegree(ClientSiteLiveGps);
                    
                return string.Empty;
            }
        }

        public bool ShowIncidentLocationAddress { get; set; }
    }

    public class ReportCreatedLocalTimeZone
    {
        public DateTime? CreatedOnDateTimeLocal { get; set; }
        public DateTimeOffset? CreatedOnDateTimeLocalWithOffset { get; set; }
        public string CreatedOnDateTimeZone { get; set; }
        public string CreatedOnDateTimeZoneShort { get; set; }
        public int? CreatedOnDateTimeUtcOffsetMinute { get; set; }
    }
}
