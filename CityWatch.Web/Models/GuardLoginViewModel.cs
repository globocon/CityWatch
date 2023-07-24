using CityWatch.Data.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CityWatch.Web.Models
{
    public class GuardLoginViewModel : IValidatableObject
    {
        public int? Id { get; set; }

        public string ClientSiteName { get; set; }

        public Guard Guard { get; set; }

        public bool IsNewGuard { get; set; }

        public bool IsPosition { get; set; }

        public string SmartWandOrPosition { get; set; }

        public DateTime OnDuty { get; set; }

        public DateTime? OffDuty { get; set; }

        public ClientSite ClientSite { get; set; }

        public int? SmartWandOrPositionId { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var errors = new List<ValidationResult>();

            if (string.IsNullOrEmpty(ClientSiteName))
                errors.Add(new ValidationResult("Client Site is required"));

            if (string.IsNullOrEmpty(SmartWandOrPosition))
                errors.Add(new ValidationResult("Smart Wand or Position is required"));

            if (OnDuty == DateTime.MinValue)
                errors.Add(new ValidationResult("On Duty is required"));

            if (!OffDuty.HasValue || OffDuty.Value == DateTime.MinValue)
                errors.Add(new ValidationResult("Off Duty is required"));

            return errors;
        }
    }
}
