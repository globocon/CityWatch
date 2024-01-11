using CityWatch.Data.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class GuardLog : IValidatableObject
    {
        [Key]
        public int Id { get; set; }

        public int ClientSiteLogBookId { get; set; }

        [Required(ErrorMessage = "Event Date and Time is required")]
        public DateTime EventDateTime { get; set; }

        [Required(ErrorMessage = "Notes are required")]
        [StringLength(2048, ErrorMessage = "The {0} value cannot exceed {1} characters.")]
        public string Notes { get; set; }

        public int? GuardLoginId { get; set; }

        public bool IsSystemEntry { get; set; }

        [NotMapped]
        public string TimePartOnly { get; set; }

        public IrEntryType? IrEntryType { get; set; }

        [ForeignKey("ClientSiteLogBookId")]
        public ClientSiteLogBook ClientSiteLogBook { get; set; }

        [ForeignKey("GuardLoginId")]
        public GuardLogin GuardLogin { get; set; }

        public int? RcPushMessageId { get; set; }

        public DateTime? EventDateTimeLocal { get; set; }

        public DateTimeOffset? EventDateTimeLocalWithOffset { get; set; }

        public string EventDateTimeZone { get; set; }

        public string EventDateTimeZoneShort { get; set; }

        public int? EventDateTimeUtcOffsetMinute { get; set; }

        // Project 4 , Task 48, Audio notification, Added By Binoy
        public bool? PlayNotificationSound { get; set; } = true;


        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var errors = new List<ValidationResult>();

            if (ClientSiteLogBook.Date != DateTime.Today)
            {
                errors.Add(new ValidationResult("A new day started and this logbook expired. Please logout and login again."));
            }

            return errors;
        }
    }
}

   