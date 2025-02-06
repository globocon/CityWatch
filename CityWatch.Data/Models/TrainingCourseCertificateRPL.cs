using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace CityWatch.Data.Models
{
    public class TrainingCourseCertificateRPL
    {
        [Key]
        public int Id { get; set; }
        public int TrainingPracticalLocationId { get; set; }
        public int TrainingInstructorId { get; set; }
        public DateTime AssessmentStartDate { get; set; }
        public DateTime AssessmentEndDate { get; set; }
        public int TrainingCourseCertificateId { get; set; }

        [ForeignKey("TrainingPracticalLocationId")]
        public TrainingLocation TrainingLocation { get; set; }
        [ForeignKey("TrainingCourseCertificateId")]
        public TrainingCourseCertificate TrainingCourseCertificate { get; set; }
        [ForeignKey("TrainingInstructorId")]
        public TrainingInstructor TrainingInstructor { get; set; }
        public bool isDeleted { get; set; }


    }
}
