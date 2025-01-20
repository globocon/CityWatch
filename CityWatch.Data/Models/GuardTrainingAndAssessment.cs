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
        public int TrainingCourseId { get; set; }
        public int HRGroupId { get; set; }

        
        public string Description { get; set; }

        public int TrainingCourseStatusId { get; set; }






        [NotMapped]
        public string HrGroupText { get; set; }

        [ForeignKey("GuardId")]
        public Guard Guard { get; set; }
        [ForeignKey("TrainingCourseId")]
        public TrainingCourses TrainingCourses { get; set; }
        [ForeignKey("TrainingCourseStatusId")]
        public TrainingCourseStatus TrainingCourseStatus { get; set; }
        [ForeignKey("HRGroupId")]
        public HRGroups HRGroups { get; set; }
        
        [NotMapped]
        public string NewNullColumn { get; set; }

        [NotMapped]
        public string LicenseNo { get; set; }
        [NotMapped]
        public string statusColor { get; set; }

    }
}
