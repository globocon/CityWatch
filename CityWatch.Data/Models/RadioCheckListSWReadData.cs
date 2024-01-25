using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CityWatch.Data.Models
{
    public  class RadioCheckListSWReadData
    {
        public int Id { get; set; }
        public string Guard { get; set; }
        public string EmployeePhone { get; set; }
        public string SmartWand { get; set; }
        public string TemplateName { get; set; }
        public DateTime InspectionStartDatetimeLocal { get; set; }
        public DateTime InspectionEndDatetimeLocal { get; set; }
       
    }
}
