using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public enum ReportFieldType
    {
        Position = 1,
        NotifiedBy = 2,
        CallSign = 3,
        ClientArea = 4, 
        Reimburse=6,
    }

    public class IncidentReportField
    {
        [Key]
        public int Id { get; set; }
        public ReportFieldType TypeId { get; set; }
        public string Name { get; set; }    
        public string EmailTo { get;set; }
        public string ClientSiteIds { get; set; }
        [NotMapped]
        public string clientSites { get; set; }
        public int[] ClientSiteIdsNew
        {
            get
            {
                return ClientSiteIds?.Split(";").Select(z => int.Parse(z)).ToArray() ?? Array.Empty<int>();
            }
        }
    }
}
