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
    
    public class GuardTrainingAndAssessment
    {
        [Key]
        public int Id { get; set; }

        public int GuardId { get; set; }
        public int CourseId { get; set; }

        [Required]
        public string Description { get; set; }
        

        


        [Required]
        public HrGroup? HrGroup { get; set; }

        [NotMapped]
        public string HrGroupText { get { return HrGroup?.ToDisplayName(); } }

        [ForeignKey("GuardId")]
        public Guard Guard { get; set; }
        [ForeignKey("CourseId")]
        public Courses Courses { get; set; }
        [NotMapped]
        public string NewNullColumn { get; set; }

        [NotMapped]
        public string LicenseNo { get; set; }
        
    }
}
