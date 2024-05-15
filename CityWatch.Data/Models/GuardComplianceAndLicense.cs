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
        public DateTime? ExpiryDate { get; set; }

        
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

    }
}
