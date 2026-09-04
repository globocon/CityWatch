using System;


namespace CityWatch.Data.Models
{   
    public class MobileCrowdControlReportData
    {
        public int OrderId { get; set; }
        public int ClientSiteId { get; set; }
        public int ClientSiteLogBookId { get; set; }
        public string ColHeaderName { get; set; }
        public DateTime CrowdControlDate { get; set; }
        public string CellValue { get; set; }
    }    
}
