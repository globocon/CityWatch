using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{

    public class TrailerDeatilsViewModel
    {
        [Key]
        public int Id { get; set; }
        public string plate { get; set; }
        public string companyName { get; set; }
        public string PersonName { get; set; }
        public string personTypeText { get; set; }
        public string Trailer1Rego { get; set; }
        public string Trailer1State { get; set; }
        public string Trailer2Rego { get; set; }
        public string Trailer2State { get; set; }
        public string Trailer3Rego { get; set; }
        public string Trailer3State { get; set; }
        public string Trailer4Rego { get; set; }
        public string Trailer4State { get; set; }
        public string TrailerTypeName { get; set; }
        
    }
}
