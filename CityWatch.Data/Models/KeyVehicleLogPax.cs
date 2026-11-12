using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace CityWatch.Data.Models
{
    [Table("VehicleKeyLogsPax")]
    public  class KeyVehicleLogPax : IValidatableObject
    {
        [Key]
        public int Id { get; set; }
        public int KeyVehicleLogId { get; set; }

        public string PersonName { get; set; }
        public string MobileNumber { get; set; }
        /// <summary>
        /// Type of Individual
        /// </summary>
        public int? PersonType { get; set; }

        [ForeignKey("KeyVehicleLogId")]
        public KeyVehicleLog KeyVehicleLog { get; set; }
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var errors = new List<ValidationResult>();
            var RegoStatus = false;

        
            

            if (string.IsNullOrEmpty(PersonName) )
                errors.Add(new ValidationResult("Individual Name is required"));

            if (string.IsNullOrEmpty(MobileNumber))
                errors.Add(new ValidationResult("Mobile Number is required"));




            return errors;
        }
    }
}
