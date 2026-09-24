using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class TrainingTestFeedbackQuestionsAnswers
    {
        [Key]
        public int Id { get; set; }
        public int TrainingTestFeedbackQuestionsId { get; set; }

        public string Options { get; set; }
        public bool IsDeleted { get; set; }

        [ForeignKey("TrainingTestFeedbackQuestionsId")]
        public TrainingTestFeedbackQuestions TrainingTestFeedbackQuestions { get; set; }
    }
}
