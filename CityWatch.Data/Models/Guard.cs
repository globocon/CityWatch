using CityWatch.Data.Providers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;

namespace CityWatch.Data.Models
{
    public class Guard : IValidatableObject
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }

        public string SecurityNo { get; set; }

        public string Initial { get; set; }

        public string State { get; set; }

        public string Provider { get; set; }

        public DateTime? DateEnrolled { get; set; }

        public bool IsActive { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var errors = new List<ValidationResult>();

            if (string.IsNullOrEmpty(SecurityNo))
                errors.Add(new ValidationResult("Guard Security License Number is required"));

            if (string.IsNullOrEmpty(Name))
                errors.Add(new ValidationResult("Guard Name is required"));

            if (string.IsNullOrEmpty(Initial))
                errors.Add(new ValidationResult("Guard Initial is required"));

            if (string.IsNullOrEmpty(Email))
                errors.Add(new ValidationResult("Guard Email is required"));

            if (string.IsNullOrEmpty(Mobile))
                errors.Add(new ValidationResult("Guard Mobile is required"));

            if (!string.IsNullOrEmpty(SecurityNo))
            {
                var invalidSecNo = SecurityNumberIsNotValid();
                if (invalidSecNo)
                {
                    errors.Add(new ValidationResult("Invalid Security Number"));
                }
                
                if (!invalidSecNo && !string.IsNullOrEmpty(Name))
                {
                    var guardDataProvider = (IGuardDataProvider)validationContext.GetService(typeof(IGuardDataProvider));
                    var duplicateGuard = guardDataProvider.GetGuards().FirstOrDefault(x => (Id == -1 || Id != x.Id) && x.SecurityNo.Equals(SecurityNo, StringComparison.OrdinalIgnoreCase));
                    if (duplicateGuard != null)
                    {
                        errors.Add(new ValidationResult($"Guard with Security No: {SecurityNo} already exists"));
                    }
                }
            }

            return errors;
        }

        private bool SecurityNumberIsNotValid()
        {
            // same number pattern
            // e.g: 0000, 1111, 222222 etc.
            var regex = new Regex(@"^([0-9])\1*$");

            // sequence number pattern
            // e.g: 0123, 1234, 789, 890, 123456789 etc.
            var seqPattern = "0123456789012345789";

            // sequence number pattern
            // e.g:098, 3210, 0987654321 etc.
            var revPattern = "09876543210987654321";

            return seqPattern.IndexOf(SecurityNo) != -1 || regex.IsMatch(SecurityNo) || revPattern.IndexOf(SecurityNo) != -1;
        }
        public string Mobile { get; set; }
        public string Email { get; set; }

        public bool IsRCAccess { get; set; }
        public bool IsKPIAccess { get; set; }
    }
}
