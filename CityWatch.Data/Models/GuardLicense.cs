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
        Other = 7,

        Investigator = 6,

        [Display(Name = "Crowd Controller")]
        CrowdController = 2,

        [Display(Name = "Security Guard")]
        SecurityGuard = 8,

        [Display(Name = "Bodyguard")]
        BodyGuard = 1,

        [Display(Name = "Driver (Car)")]
        CarDriver = 4,

        [Display(Name = "Driver (Boat)")]
        BoatDriver = 3,

        Firearm = 5
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
        //public int LicenseTypeId { get; set; }
        //[ForeignKey("LicenseTypeId")]
        //public LicenseTypes LicenseTypes { get; set; }
    }
    public class LicenseTypes
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
    }
}