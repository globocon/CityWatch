using CityWatch.Common.Helpers;
using CityWatch.Data.Helpers;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public enum GuardLicenseType
    {
        Other = 0,

        Investigator,

        [Display(Name = "Crowd Controller")]
        CrowdController,

        [Display(Name = "Security Guard")]
        SecurityGuard,

        [Display(Name = "Bodyguard")]
        BodyGuard,

        [Display(Name = "Driver (Car)")]
        CarDriver,

        [Display(Name = "Driver (Boat)")]
        BoatDriver,

        Firearm
    }

    public class GuardLicense
    {
        [Key]
        public int Id { get; set; }

        public int GuardId { get; set; }

        [Required]
        public string LicenseNo { get; set; }

        [Required]
        public GuardLicenseType? LicenseType { get; set; }

        [NotMapped]
        public string LicenseTypeText { get { return LicenseType?.ToDisplayName(); } }

        public DateTime? ExpiryDate { get; set; }

        public int? Reminder1 { get; set; }

        public int? Reminder2 { get; set; }

        public string FileName { get; set; }

        [NotMapped]
        public string FileUrl
        {
            get
            {
                if (string.IsNullOrEmpty(FileName))
                    return string.Empty;

                if (Guard == null)
                    return FileName;

                return $"{GuardHelper.GetGuardDocumentDbxRootUrl(Guard)}/{LicenseNo}/{FileName}";
            }
        }

        [ForeignKey("GuardId")]
        public Guard Guard { get; set; }
        public string LicenseTypeName { get; set; }
    }
}