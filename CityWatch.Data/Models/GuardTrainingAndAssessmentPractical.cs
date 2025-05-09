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
    public class GuardTrainingAndAssessmentPractical
    {
        [Key]
        public int Id { get; set; }

        public int GuardId { get; set; }
        public int HRSettingsId { get; set; }
        public int PracticalocationlId { get; set; }


        public DateTime PracticalDate { get; set; }

        public int InstructorId { get; set; }

      





        [ForeignKey("GuardId")]
        public Guard Guard { get; set; }
        [ForeignKey("HRSettingsId")]
        public HrSettings HrSettings { get; set; }
        [ForeignKey("PracticalocationlId")]
        public TrainingLocation TrainingLocation { get; set; }
        [ForeignKey("InstructorId")]
        public TrainingInstructor TrainingInstructor { get; set; }

        
        
  
	
	

    }
}
