using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class RadioChecksSmartWandScanResults
    {
        [Key]
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string EmployeePhone { get; set; }
        public int TemplateId { get; set; }
        public string TemplateIdentificationNumber { get; set; }
        public string TemplateName { get; set; }
        public int ClientId { get; set; }
        public string SiteId { get; set; }
        public string SiteName { get; set; }
        public string LocationId { get; set; }
        public string LocationName { get; set; }
        public string LocationScan { get; set; }
        public DateTime InspectionStartDatetimeLocal { get; set; }
        public DateTime InspectionEndDatetimeLocal { get; set; }
        public int ClientSiteId { get; set; }
        public int GuardId { get; set; }
        public string SmartWandId { get; set; }

    }
}
