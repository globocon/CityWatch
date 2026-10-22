using System;
using System.Text.Json.Serialization;

namespace CityWatch.RadioCheck.Models
{
    public class SWandReaderResults
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("employee_id")]
        public int EmployeeId { get; set; }

        [JsonPropertyName("employee_name")]
        public string EmployeeName { get; set; }

        [JsonPropertyName("employee_phone")]
        public string EmployeePhone { get; set; }

        [JsonPropertyName("template_id")]
        public int TemplateId { get; set; }

        [JsonPropertyName("template_identification_number")]
        public string TemplateIdentificationNumber { get; set; }

        [JsonPropertyName("template_name")]
        public string TemplateName { get; set; }

        [JsonPropertyName("client_id")]
        public int ClientId { get; set; }

        [JsonPropertyName("site_id")]
        public string SiteId { get; set; }

        [JsonPropertyName("site_name")]
        public string SiteName { get; set; }

        [JsonPropertyName("location_id")]
        public string LocationId { get; set; }

        [JsonPropertyName("location_name")]
        public string LocationName { get; set; }

        [JsonPropertyName("location_scan")]
        public string LocationScan { get; set; }

        [JsonPropertyName("inspection_start_datetime_local")]
        public string InspectionStartDatetimeLocal { get; set; }

        [JsonPropertyName("inspection_end_datetime_local")]
        public string InspectionEndDatetimeLocal { get; set; }

        
 
    }

    public class RootObject
    {
        public SWandReaderResults[] results { get; set; }
    }
}