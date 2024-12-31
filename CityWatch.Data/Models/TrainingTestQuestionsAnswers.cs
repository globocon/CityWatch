using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class TrainingTestQuestionsAnswers
    {
        public int Id { get; set; }
        public int TrainingTestQuestionsId { get; set; }
      
        public string Options { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsAnswer { get; set; }

        [ForeignKey("TrainingTestQuestionsId")]
        public TrainingTestQuestions TrainingTestQuestions { get; set; }
        
    }
}
