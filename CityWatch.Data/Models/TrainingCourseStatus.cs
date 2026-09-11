using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class TrainingCourseStatus
    {
        public int Id { get; set; }
        public string ReferenceNo { get; set; }
        public string Name { get; set; }
        public int? TrainingCourseStatusColorId { get; set; }
        [ForeignKey("TrainingCourseStatusColorId")]
        public TrainingCourseStatusColor TrainingCourseStatusColor { get; set; }
    }
}
