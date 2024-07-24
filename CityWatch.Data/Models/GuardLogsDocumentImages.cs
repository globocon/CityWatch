using CityWatch.Data.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class GuardLogsDocumentImages : IValidatableObject
    {
        [Key]
        public int Id { get; set; }
        public string ImagePath { get; set; }

        public bool? IsRearfile { get; set; } = false;

        public bool? IsTwentyfivePercentfile { get; set; } = false;
        public int? GuardLogId { get; set; }
        [ForeignKey("GuardLogId")]
        public GuardLog GuardLog { get; set; }
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            throw new NotImplementedException();
        }
    }
}
