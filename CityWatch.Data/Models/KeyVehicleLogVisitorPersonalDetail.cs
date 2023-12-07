using Dropbox.Api.TeamLog;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class KeyVehicleLogVisitorPersonalDetail : IEquatable<KeyVehicleLogVisitorPersonalDetail>, IValidatableObject
    {
        public KeyVehicleLogVisitorPersonalDetail()
        { }

        public KeyVehicleLogVisitorPersonalDetail(KeyVehicleLog keyVehicleLog)
        {
            KeyVehicleLogProfile = new KeyVehicleLogProfile(keyVehicleLog);
            ProfileId = keyVehicleLog.Id;
            CompanyName = keyVehicleLog.CompanyName;
            PersonName = keyVehicleLog.PersonName;
            PersonType = keyVehicleLog.PersonType;
            PersonOfInterest = keyVehicleLog.PersonOfInterest;
            POIImage = keyVehicleLog.POIImage;
            IndividualTitle = keyVehicleLog.IndividualTitle;
            Gender = keyVehicleLog.Gender;
            CompanyABN = keyVehicleLog.CompanyABN;
            CompanyLandline = keyVehicleLog.CompanyLandline;
            Email = keyVehicleLog.Email;
            Website = keyVehicleLog.Website;
            CRMId = keyVehicleLog.CRMId;
            BDMList = keyVehicleLog.BDMList;
            IsBDM = keyVehicleLog.IsBDM;
            POIId = keyVehicleLog.VehicleRego;
        }

        [Key]
        public int Id { get; set; }

        public int ProfileId { get; set; }

        public string CompanyName { get; set; }

        public string PersonName { get; set; }
        public int? PersonOfInterest { get; set; }
      
        public string POIImage { get; set; }
        [NotMapped]
        public string POIImageDisplay { get; set; }

        public int? PersonType { get; set; }
        [HiddenInput]
        public bool IsBDM { get; set; }
        public string IndividualTitle { get; set; }
        public string Gender { get; set; }
        public string CompanyABN { get; set; }
        public string CompanyLandline { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
        public string CRMId { get; set; }
        public string BDMList { get; set; }
        public string POIId { get; set; }

        [NotMapped]
        public string POIOrBDM { get; set; }
        [ForeignKey("ProfileId")]
        public KeyVehicleLogProfile KeyVehicleLogProfile { get; set; }
        [NotMapped]
        public List<string> Attachments { get; set; }

        public bool Equals(KeyVehicleLogVisitorPersonalDetail other)
        {
            return (KeyVehicleLogProfile.VehicleRego == other.KeyVehicleLogProfile.VehicleRego) &&
                    (string.Compare(CompanyName, other.CompanyName, true) == 0) &&
                    (string.Compare(PersonName, other.PersonName, true) == 0) &&
                    (PersonType == other.PersonType);
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var errors = new List<ValidationResult>();

            if (string.IsNullOrEmpty(KeyVehicleLogProfile.VehicleRego))
                errors.Add(new ValidationResult("ID No or Vehicle Registration is required"));

            if (!KeyVehicleLogProfile.PlateId.HasValue)
                errors.Add(new ValidationResult("State of ID / Plate is required"));

            if (!PersonType.HasValue)
                errors.Add(new ValidationResult("Type of Individual is required"));

            return errors;
        }
    }
}
