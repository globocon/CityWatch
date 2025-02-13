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
    public class GuardTrainingAttendedQuestionsAndAnswers
    {
        [Key]
        public int Id { get; set; }

        public int GuardId { get; set; }
        public int TrainingCourseId { get; set; }
        public int TrainingTestQuestionsId { get; set; }


        public int TrainingTestQuestionsAnswersId { get; set; }

        public bool IsCorrect { get; set; }





        [ForeignKey("GuardId")]
        public Guard Guard { get; set; }
        [ForeignKey("TrainingCourseId")]
        public TrainingCourses TrainingCourses { get; set; }
        

        [ForeignKey("TrainingTestQuestionsId")]
        public TrainingTestQuestions TrainingTestQuestions { get; set; }
        [ForeignKey("TrainingTestQuestionsAnswersId")]
        public TrainingTestQuestionsAnswers TrainingTestQuestionsAnswers { get; set; }

       
    }
}
