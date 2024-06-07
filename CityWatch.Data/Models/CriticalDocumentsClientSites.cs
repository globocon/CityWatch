using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class CriticalDocumentsClientSites
    {
        [Key]
        public int Id { get; set; }
        public int CriticalDocumentID { get; set; }

        public int ClientSiteId { get; set; }
        public int DescriptionID { get; set; }

        [ForeignKey("CriticalDocumentID")]
        [JsonIgnore]
        public CriticalDocuments CriticalDoc { get; set; }

        [ForeignKey("ClientSiteId")]
        public ClientSite ClientSite { get; set; }
        [ForeignKey("DescriptionID")]
        public HrSettings HRSettings { get; set; }
    }
}
