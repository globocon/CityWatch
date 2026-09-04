using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public  class TrainingTestQuestions
    {
        [Key]
        public int Id { get; set; }
        public int HRSettingsId { get; set; }
        public int QuestionNoId { get; set; }
        public int TQNumberId { get; set; }
        public string Question { get; set; }
        public bool IsDeleted { get; set; }

        [ForeignKey("HRSettingsId")]
        public HrSettings HrSettings { get; set; }
        [ForeignKey("QuestionNoId")]
        public TrainingTestQuestionNumbers QuestionNumbers { get; set; }
        [ForeignKey("TQNumberId")]
        public TrainingTQNumbers TQNumbers { get; set; }
       
	
    }
}
