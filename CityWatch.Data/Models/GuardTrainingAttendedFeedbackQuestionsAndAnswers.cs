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
    public class GuardTrainingAttendedFeedbackQuestionsAndAnswers
    {
        [Key]
        public int Id { get; set; }

        public int GuardId { get; set; }
        public int HrSettingsId { get; set; }
        public int TrainingTestFeedbackQuestionsId { get; set; }


        public int TrainingTestFeedbackQuestionsAnswersId { get; set; }

       



        [ForeignKey("GuardId")]
        public Guard Guard { get; set; }
        [ForeignKey("HrSettingsId")]
        public HrSettings HrSettings { get; set; }


        [ForeignKey("TrainingTestFeedbackQuestionsId")]
        public TrainingTestFeedbackQuestions TrainingTestFeedbackQuestions { get; set; }
        [ForeignKey("TrainingTestFeedbackQuestionsAnswersId")]
        public TrainingTestFeedbackQuestionsAnswers TrainingTestFeedbackQuestionsAnswers { get; set; }
    }
}
