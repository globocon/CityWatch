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
    public class GuardTrainingStartTest
    {
        [Key]
        public int Id { get; set; }

        public int GuardId { get; set; }
        public int TrainingCourseId { get; set; }
        public int ClassroomLocationId { get; set; }
        public DateTime TestDate { get; set; }









        [ForeignKey("GuardId")]
        public Guard Guard { get; set; }
        [ForeignKey("TrainingCourseId")]
        public TrainingCourses TrainingCourses { get; set; }
       
        [ForeignKey("ClassroomLocationId")]
        public TrainingLocation TrainingLocation { get; set; }

       
    }
}
