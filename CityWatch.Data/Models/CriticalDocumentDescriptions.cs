using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace CityWatch.Data.Models
{
    public class CriticalDocumentDescriptions
    {
        [Key]
        public int Id { get; set; }

        public int CriticalDocumentID { get; set; }
        [ForeignKey("CriticalDocumentID")]
        public CriticalDocuments CriticalDocument { get; set; }

        public int DescriptionID { get; set; }
        [ForeignKey("DescriptionID")]
        public HrSettings HRSettings { get; set; }
        
    }
}
