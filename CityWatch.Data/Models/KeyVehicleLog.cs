using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.RegularExpressions;

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

        public string Trailer5Rego { get; set; }

        public string Trailer6Rego { get; set; }

        public string Trailer7Rego { get; set; }

        public string Trailer8Rego { get; set; }
        public string IndividualTitle { get; set; }
        public string Gender { get; set; }
        public string CompanyABN { get; set; }
        public string CompanyLandline { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
        public string CRMId { get; set; }
        public string BDMList { get; set; }

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

        public string ClientSitePocIdsVehicleLog { get; set; }
        public decimal? Reels { get; set; }

        public string CustomerRef { get; set; }

        [Column("Wvi")]
        public string Vwi { get; set; }

        [HiddenInput]
        public bool IsSender { get; set; }

        [HiddenInput]
        public bool IsBDM { get; set; }
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
        //[ForeignKey("CRMId")]
        //public  CRMPersonalDec  { get; set; }

        public bool MoistureDeduction { get; set; }

        public bool RubbishDeduction { get; set; }

        public decimal? DeductionPercentage { get; set; }

        public int? CopiedFromId { get; set; }

        [HiddenInput]
        public bool IsTimeSlotNo { get; set; }

        [NotMapped]
        public bool IsPreviousDayEntry { get { return CopiedFromId.HasValue; } }

        public DateTime? EntryCreatedDateTimeLocal { get; set; }
        public DateTimeOffset? EntryCreatedDateTimeLocalWithOffset { get; set; }
        public string EntryCreatedDateTimeZone { get; set; }
        public string EntryCreatedDateTimeZoneShort { get; set; }
        public int? EntryCreatedDateTimeUtcOffsetMinute { get; set; }



        public int? Trailer1PlateId { get; set; }

        public int? Trailer2PlateId { get; set; }

        public int? Trailer3PlateId { get; set; }

        public int? Trailer4PlateId { get; set; }

        public int? Trailer5PlateId { get; set; }

        public int? Trailer6PlateId { get; set; }

        public int? Trailer7PlateId { get; set; }

        public int? Trailer8PlateId { get; set; }


        [HiddenInput]
        public bool IsDocketNo { get; set; }
        public string LoaderName { get; set; }
        public string DispatchName { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var errors = new List<ValidationResult>();
            var RegoStatus = false;

            if (!InitialCallTime.HasValue && !EntryTime.HasValue)
                errors.Add(new ValidationResult("Initial Call or Entry Time is required"));
            // if (string.IsNullOrEmpty(VehicleRego))
            //errors.Add(new ValidationResult("ID No or Vehicle Registration is required"));
            /* New change for Add rigo without plate number 21032024 dileep*/
            if (string.IsNullOrEmpty(VehicleRego))
            {
                if (!string.IsNullOrEmpty(Trailer1Rego) || !string.IsNullOrEmpty(Trailer2Rego)
                    || !string.IsNullOrEmpty(Trailer3Rego) || !string.IsNullOrEmpty(Trailer4Rego)
                    || !string.IsNullOrEmpty(Trailer5Rego) || !string.IsNullOrEmpty(Trailer6Rego)
                    || !string.IsNullOrEmpty(Trailer7Rego) || !string.IsNullOrEmpty(Trailer8Rego))
                {
                    RegoStatus = true;
                }
                else
                {
                    errors.Add(new ValidationResult("ID No or Vehicle Registration or Trailer Rego is required"));
                }
            }

            if (IsCarsStock.HasValue && IsCarsStock.Value)
            {
                var msg = "Vehicle colour selection is required in Car Stock";
                if (!string.IsNullOrEmpty(Trailer1Rego))
                {
                    if (Trailer1PlateId == null || Trailer1PlateId == 0)
                    {
                        errors.Add(new ValidationResult($"{msg} 1."));
                    }
                }
                if (!string.IsNullOrEmpty(Trailer2Rego))
                {
                    if (Trailer2PlateId == null || Trailer2PlateId == 0)
                    {
                        errors.Add(new ValidationResult($"{msg} 2."));
                    }
                }
                if (!string.IsNullOrEmpty(Trailer3Rego))
                {
                    if (Trailer3PlateId == null || Trailer3PlateId == 0)
                    {
                        errors.Add(new ValidationResult($"{msg} 3."));
                    }
                }
                if (!string.IsNullOrEmpty(Trailer4Rego))
                {
                    if (Trailer4PlateId == null || Trailer4PlateId == 0)
                    {
                        errors.Add(new ValidationResult($"{msg} 4."));
                    }
                }
                if (!string.IsNullOrEmpty(Trailer5Rego))
                {
                    if (Trailer5PlateId == null || Trailer5PlateId == 0)
                    {
                        errors.Add(new ValidationResult($"{msg} 5."));
                    }
                }
                if (!string.IsNullOrEmpty(Trailer6Rego))
                {
                    if (Trailer6PlateId == null || Trailer6PlateId == 0)
                    {
                        errors.Add(new ValidationResult($"{msg} 6."));
                    }
                }
                if (!string.IsNullOrEmpty(Trailer7Rego))
                {
                    if (Trailer7PlateId == null || Trailer7PlateId == 0)
                    {
                        errors.Add(new ValidationResult($"{msg} 7."));
                    }
                }
                if (!string.IsNullOrEmpty(Trailer8Rego))
                {
                    if (Trailer8PlateId == null || Trailer8PlateId == 0)
                    {
                        errors.Add(new ValidationResult($"{msg} 8."));
                    }
                }
            }

            if (IsCarsStock != true)
            {
                var duplicates = new[] { Trailer1Rego, Trailer2Rego, Trailer3Rego, Trailer4Rego, Trailer5Rego, Trailer6Rego, Trailer7Rego, Trailer8Rego, VehicleRego }
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
                            .Where(g => g.Count() > 1);

                foreach (var duplicate in duplicates)
                {
                    errors.Add(new ValidationResult(
                        $"The same Trailer Rego or Vehicle Rego ({duplicate.Key}) is not allowed."));
                }
            }


            if (RegoStatus)
            {
                if (!string.IsNullOrEmpty(Trailer1Rego))
                {
                    if (Trailer1PlateId == null || Trailer1PlateId == 0)
                    {
                        errors.Add(new ValidationResult("State of ID / Plate is required for Trailer 1 Rego"));
                    }

                }
                if (!string.IsNullOrEmpty(Trailer2Rego))
                {
                    if (Trailer2PlateId == null || Trailer2PlateId == 0)
                    {
                        errors.Add(new ValidationResult("State of ID / Plate is required for Trailer 2 Rego"));
                    }

                }
                if (!string.IsNullOrEmpty(Trailer3Rego))
                {
                    if (Trailer3PlateId == null || Trailer3PlateId == 0)
                    {
                        errors.Add(new ValidationResult("State of ID / Plate is required for Trailer 3 Rego"));
                    }

                }
                if (!string.IsNullOrEmpty(Trailer4Rego))
                {
                    if (Trailer4PlateId == null || Trailer4PlateId == 0)
                    {
                        errors.Add(new ValidationResult("State of ID / Plate is required for Trailer 4 Rego"));
                    }

                }
                if (!string.IsNullOrEmpty(Trailer5Rego))
                {
                    if (Trailer5PlateId == null || Trailer5PlateId == 0)
                    {
                        errors.Add(new ValidationResult("State of ID / Plate is required for Trailer 5 Rego"));
                    }

                }
                if (!string.IsNullOrEmpty(Trailer6Rego))
                {
                    if (Trailer6PlateId == null || Trailer6PlateId == 0)
                    {
                        errors.Add(new ValidationResult("State of ID / Plate is required for Trailer 6 Rego"));
                    }

                }
                if (!string.IsNullOrEmpty(Trailer7Rego))
                {
                    if (Trailer7PlateId == null || Trailer7PlateId == 0)
                    {
                        errors.Add(new ValidationResult("State of ID / Plate is required for Trailer 7 Rego"));
                    }

                }
                if (!string.IsNullOrEmpty(Trailer8Rego))
                {
                    if (Trailer8PlateId == null || Trailer8PlateId == 0)
                    {
                        errors.Add(new ValidationResult("State of ID / Plate is required for Trailer 8 Rego"));
                    }

                }

                if (TrailerType == null || TrailerType == 0)
                {
                    errors.Add(new ValidationResult("Please select Trailer Type"));

                }
            }

            if (!string.IsNullOrEmpty(VehicleRego) && PlateId <= 0)
                errors.Add(new ValidationResult("State of ID / Plate is required"));

            //Tailer Change New change for Add rigo without plate number 21032024
            //if (!PersonType.HasValue)
            //errors.Add(new ValidationResult("Type of Individual is required"));
            if (!PersonType.HasValue && !RegoStatus)
                errors.Add(new ValidationResult("Type of Individual is required"));

            if (InWeight.HasValue && InWeight.Value < 0)
                errors.Add(new ValidationResult("Weight In is invalid"));

            if (OutWeight.HasValue && OutWeight.Value < 0)
                errors.Add(new ValidationResult("Weight Out is invalid"));

            if (MaxWeight.HasValue && MaxWeight.Value < 0)
                errors.Add(new ValidationResult("Max Weight is invalid"));

            if (!string.IsNullOrEmpty(Email))
            {
                if (!Email.Contains('@'))
                {
                    //KV wont accept email - bug
                    //It will NOT accept group@swcsecurity.com.au   
                    errors.Add(new ValidationResult("Email is invalid"));
                }
                //if (!Regex.IsMatch(Email, regex))
                //{

                //    errors.Add(new ValidationResult("Email is invalid"));
                //}
            }

            return errors;
        }
        [HiddenInput]
        public bool IsReels { get; set; }
        [HiddenInput]
        public bool IsVWI { get; set; }
        public string EmailCompany { get; set; }
        public string Emailindividual { get; set; }
        [NotMapped]
        public string SitePocNames { get; set; }
        [HiddenInput]
        public bool IsISOVIN { get; set; }
        [HiddenInput]
        public bool? IsISO { get; set; }
        [HiddenInput]
        public bool? IsVin { get; set; }
        [HiddenInput]
        public bool? IsTrailerRego { get; set; }
        [HiddenInput]
        public bool? IsCarsStock { get; set; }
        public bool HasLoadVariation { get; set; } = false;
        public bool IsLoadVariationDuplicate { get; set; } = false;
        public int? CopiedFromKVLogId { get; set; }

    }
}
