using CityWatch.Common.Helpers;
using CityWatch.Data.Helpers;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class GuardCompliance
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

        public string HrGroup { get; set; }

        [ForeignKey("GuardId")]
        public Guard Guard { get; set; }
    }
}
