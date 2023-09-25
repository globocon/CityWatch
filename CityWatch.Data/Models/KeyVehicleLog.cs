using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    [Table("VehicleKeyLogs")]
    public class KeyVehicleLog : IValidatableObject
    {
        [Key]
        public int Id { get; set; }

        public int ClientSiteLogBookId { get; set; }

        public int GuardLoginId { get; set; }

        [NotMapped]
        public int? ActiveGuardLoginId { get; set; }

        public DateTime? InitialCallTime { get; set; }

        public DateTime? EntryTime { get; set; }

        public DateTime? SentInTime { get; set; }

        public DateTime? ExitTime { get; set; }

        public string TimeSlotNo { get; set; }

        public string DocketSerialNo { get; set; }

        public string VehicleRego { get; set; }

        public string Trailer1Rego { get; set; }

        public string Trailer2Rego { get; set; }

        public string Trailer3Rego { get; set; }

        public string Trailer4Rego { get; set; }

        public int PlateId { get; set; }
       
        
        public string KeyNo { get; set; }

        public string CompanyName { get; set; }

        public string PersonName { get; set; }
        public string POIImage { get; set; }

        public string MobileNumber { get; set; }

        /// <summary>
        /// Vehicle Config
        /// </summary>
        public int? TruckConfig { get; set; }

        public int? TrailerType { get; set; }

        /// <summary>
        /// Type of Individual
        /// </summary>
        public int? PersonType { get; set; }

        public int? EntryReason { get; set; }

        [Column("PurposeOfEntry")]
        public string Product { get; set; }

        [NotMapped]
        public string ProductOther { get; set; }
        public decimal? InWeight { get; set; }

        public decimal? OutWeight { get; set; }

        public decimal? TareWeight { get; set; }

        public decimal? MaxWeight { get; set; }

        public string Notes { get; set; }

        public int? ClientSiteLocationId { get; set; }

        public int? ClientSitePocId { get; set; }

        public decimal? Reels { get; set; } 

        public string CustomerRef { get; set; }

        [Column("Wvi")]
        public string Vwi { get; set; }

        [HiddenInput]
        public bool IsSender { get; set; }

        
        public int? PersonOfInterest { get; set; }

        public string Sender { get; set; }

        public string InWeightTerm
        {
            get
            {
                return (InWeight.HasValue && OutWeight.GetValueOrDefault() > InWeight.Value) ? "Net" : "Gross";
            }
        }

        public string OutWeightTerm
        {
            get
            {
                return (InWeight.HasValue && OutWeight.GetValueOrDefault() > InWeight.Value) ? "Gross" : "Net";
            }
        }

        public Guid? ReportReference { get; set; }

        [ForeignKey("ClientSiteLogBookId")]
        public ClientSiteLogBook ClientSiteLogBook { get; set; }

        [ForeignKey("GuardLoginId")]
        public GuardLogin GuardLogin { get; set; }

        [ForeignKey("ClientSiteLocationId")]
        public ClientSiteLocation ClientSiteLocation { get; set; }

        [ForeignKey("ClientSitePocId")]
        public ClientSitePoc ClientSitePoc { get; set; }

        public bool MoistureDeduction { get; set; }

        public bool RubbishDeduction { get; set; }

        public decimal? DeductionPercentage { get; set; }

        public int? CopiedFromId { get; set; }

        [HiddenInput]
        public bool IsTimeSlotNo { get; set; }

        [NotMapped]
        public bool IsPreviousDayEntry { get { return CopiedFromId.HasValue; } }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var errors = new List<ValidationResult>();

            if (!InitialCallTime.HasValue && !EntryTime.HasValue)
                errors.Add(new ValidationResult("Initial Call or Entry Time is required"));

            if (string.IsNullOrEmpty(VehicleRego))
                errors.Add(new ValidationResult("ID No or Vehicle Registration is required"));

            if (!string.IsNullOrEmpty(VehicleRego) && PlateId <= 0)
                errors.Add(new ValidationResult("State of ID / Plate is required"));

            if (!PersonType.HasValue)
                errors.Add(new ValidationResult("Type of Individual is required"));

            if (InWeight.HasValue && InWeight.Value < 0)
                errors.Add(new ValidationResult("Weight In is invalid"));

            if (OutWeight.HasValue && OutWeight.Value < 0)
                errors.Add(new ValidationResult("Weight Out is invalid"));

            if (MaxWeight.HasValue && MaxWeight.Value < 0)
                errors.Add(new ValidationResult("Max Weight is invalid"));

            return errors;
        }
    }
}
