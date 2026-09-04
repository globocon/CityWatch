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
    
    public class GuardComplianceAndLicense
    {
        [Key]
        public int Id { get; set; }

        public int GuardId { get; set; }

        [Required]
        public string Description { get; set; }

        // Added for Graceful Migration: Store the master ID instead of relying purely on Description
        public int? HrSettingsId { get; set; }
        [ForeignKey("HrSettingsId")]
        public HrSettings HrSettings { get; set; }
        
        public DateTime? ExpiryDate { get; set; }

        [Required(ErrorMessage = "Please ensure a file is attached before saving a new compliance.All HR Groups require a genuine file to be attached of the licence or certificate.")]
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

                return $"{GuardHelper.GetGuardDocumentDbxRootUrl(Guard)}/{GuardId}/{FileName}";
            }
        }
        [Required]
        public HrGroup? HrGroup { get; set; }

        [NotMapped]
        public string HrGroupText { get { return HrGroup?.ToDisplayName(); } }

        [ForeignKey("GuardId")]
        public Guard Guard { get; set; }
        public string CurrentDateTime { get; set; }
        public int Reminder1 { get; set; }

        public int Reminder2 { get; set; }
        [NotMapped]
        public string LicenseNo { get; set; }
        public bool DateType { get; set; }
        [NotMapped]
        public bool IsDateFilterEnabledHidden { get; set; }
        [NotMapped]
        public bool HRBanEdit { get; set; }
        [NotMapped]
        public string IsLogin { get; set; }
        [NotMapped]
        public int MasterDateType { get; set; }
        [NotMapped]
        public string StatusColor
        {
            get
            {
                // Default
                var statusColor = "green";

                // If DateType is true → always green
                if (DateType)
                    return "green";

                // If ExpiryDate exists
                if (ExpiryDate.HasValue)
                {
                    var currentDate = DateTime.UtcNow.Date;
                    var expiryDate = ExpiryDate.Value.Date;

                    var daysDifference = (expiryDate - currentDate).TotalDays;

                    //// Expired → red (highest priority)
                    //if (expiryDate < currentDate && !DateType && !IsPending)
                    //{
                    //    statusColor = "red";
                    //}
                    //else if(IsPending == true)
                    //{
                    //    statusColor = "orange";
                    //}
                    //// Expiring within 45 days → yellow
                    //else if (daysDifference <= 45)
                    //{
                    //    statusColor = "yellow";
                    //}
                    var daysAfterExpiry = (currentDate - expiryDate).TotalDays;

                    // Expired
                    if (expiryDate < currentDate)
                    {
                        // EXPLANATION: If the record is expired but marked as "Pending" (toggle ON), 
                        // it will show an ORANGE clock to indicate a grace period.
                        // After 99 days past the expiry date, this grace period expires and it forcefully turns RED.
                        if (IsPending && daysAfterExpiry <= 99)
                        {
                            statusColor = "orange";
                        }
                        else
                        {
                            statusColor = "red";
                        }
                    }
                    // Expiring within 45 days
                    else if (daysDifference <= 45)
                    {
                        statusColor = "yellow";
                    }
                }

                return statusColor;
            }
        }
        public bool IsPending { get; set; }

    }
}
