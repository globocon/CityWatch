using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public  class TrainingCourseInstructor
    {
        [Key]
        public int Id { get; set; }
        public int HRSettingsId { get; set; }
        public int? TrainingInstructorId { get; set; }
        [ForeignKey("HRSettingsId")]
        public HrSettings HrSetting { get; set; }
        [ForeignKey("TrainingInstructorId")]
        public TrainingInstructor TrainingInstructor { get; set; }
        [NotMapped]
        public string InstructorName { get; set; }
        [NotMapped]
        public string InstructorPosition { get; set; }
    }
}
