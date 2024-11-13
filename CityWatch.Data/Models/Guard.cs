using CityWatch.Data.Providers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Security.Cryptography;
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
        public string Pin { get; set; }
        

        public string State { get; set; }

        public string Provider { get; set; }

        public DateTime? DateEnrolled { get; set; }

        public bool IsActive { get; set; }
        public bool IsLB_KV_IR { get; set; }
        public bool IsAdminPowerUser { get; set; }
        public bool IsAdminGlobal { get; set; }
        
        public bool IsSTATS { get; set; }
        [NotMapped]
        public List<string> GuardAccess { get;  set; }
      
        [NotMapped]
        public string? ProviderNo
        {
            get;
            set ;
        }
        //p1-224 RC Bypass For HR -start
        public string Gender { get; set; }
        //p1-224 RC Bypass For HR -start
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var errors = new List<ValidationResult>();

            if (string.IsNullOrEmpty(SecurityNo))
                errors.Add(new ValidationResult("Guard Security License Number is required"));

            if (string.IsNullOrEmpty(Name))
                errors.Add(new ValidationResult("Guard Name is required"));

            if (string.IsNullOrEmpty(Initial))
                errors.Add(new ValidationResult("Guard Initial is required"));
            //p1-224 RC Bypass For HR -start
            if (string.IsNullOrEmpty(Gender))
                errors.Add(new ValidationResult("Gender is required"));
            //p1-224 RC Bypass For HR -end
            //if (string.IsNullOrEmpty(Email))
            //    errors.Add(new ValidationResult("Guard Email is required"));

            //if (string.IsNullOrEmpty(Mobile))
            //    errors.Add(new ValidationResult("Guard Mobile is required"));

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
        public bool IsReActive { get; set; }
        //p1-224 RC Bypass For HR -start
        public bool IsRCBypass { get; set; }
        //p1-224 RC Bypass For HR -end
        //p1-273 access level- start
        public bool IsSTATSChartsAccess { get; set; }
        public bool IsRCFusionAccess { get; set; }

        public bool IsAdminSOPToolsAccess { get; set; }


        public bool IsAdminAuditorAccess { get; set; }


        public bool IsAdminInvestigatorAccess { get; set; }


        public bool IsAdminThirdPartyAccess { get; set; }
        //p1-273 access level- end
    }
}
