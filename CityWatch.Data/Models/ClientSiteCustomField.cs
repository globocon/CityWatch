using CityWatch.Data.Providers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.RegularExpressions;

namespace CityWatch.Data.Models
{
    public class ClientSiteCustomField : IValidatableObject
    {
        [Key]
        public int Id { get; set; }
        public int ClientSiteId { get; set; }

        [ForeignKey("ClientSiteId")]
        public ClientSite ClientSite { get; set; }

        [MaxLength(25, ErrorMessage = "Exceeded 25 characters")]
        public string Name { get; set; }

        public string TimeSlot { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var errors = new List<ValidationResult>();

            if (string.IsNullOrEmpty(Name))
                errors.Add(new ValidationResult("Field Name is required"));

            if (string.IsNullOrEmpty(TimeSlot))
                errors.Add(new ValidationResult("Time Slot is required"));

            if (!string.IsNullOrEmpty(TimeSlot) && !string.IsNullOrEmpty(Name))
            {
                var pattern24hrTime = new Regex(@"^(?:[01][0-9]|2[0-3]):[0-5][0-9]$");
                var isValidTimeSlot = pattern24hrTime.IsMatch(TimeSlot) && TimeSpan.TryParse(TimeSlot, out _);
                if (!isValidTimeSlot)
                {
                    errors.Add(new ValidationResult("Invalid time slot. Please enter a value in 24 Hr format (HH:mm)"));
                }
                else
                {
                    var guardLogDataProvider = (IGuardLogDataProvider)validationContext.GetService(typeof(IGuardLogDataProvider));
                    var duplicateFieldConfig = guardLogDataProvider.GetCustomFieldsByClientSiteId(ClientSiteId)
                        .SingleOrDefault(x => x.Name.Equals(Name, StringComparison.OrdinalIgnoreCase) && x.TimeSlot.Equals(TimeSlot, StringComparison.OrdinalIgnoreCase));
                    if (duplicateFieldConfig != null)
                    {
                        errors.Add(new ValidationResult($"An entry with same field name {Name} at time slot {TimeSlot} exists"));
                    }
                }
            }

            return errors;
        }
    }
}
