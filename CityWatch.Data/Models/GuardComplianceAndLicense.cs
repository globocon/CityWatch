using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public enum GuardLicenseType2
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
    public class GuardComplianceAndLicense
    {
        [Key]
        public int Id { get; set; }

        public int GuardId { get; set; }
        [Required]
        public string ReferenceNo { get; set; }

        public string Description { get; set; }

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

                return $"{GuardHelper.GetGuardDocumentDbxRootUrl(Guard)}/{ReferenceNo}/{FileName}";
            }
        }

        public HrGroup? HrGroup { get; set; }

        [NotMapped]
        public string HrGroupText { get { return HrGroup?.ToDisplayName(); } }

        [ForeignKey("GuardId")]
        public Guard Guard { get; set; }

        [Required]
        public string LicenseNo { get; set; }

        [Required]
        public GuardLicenseType2? LicenseType { get; set; }

        [NotMapped]
        public string LicenseTypeText { get { return LicenseType?.ToDisplayName(); } }
        public string LicenseTypeName { get; set; }
    }
}
