using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class CriticalDocuments
    {
        [Key]
        public int Id { get; set; }
        public int ClientTypeId { get; set; }
        public int HRGroupID { get; set; }
        public string GroupName { get; set; }

        public bool IsCriticalDocumentDownselect { get; set; }
        public ICollection<CriticalDocumentsClientSites> CriticalDocumentsClientSites { get; set; }
        public ICollection<CriticalDocumentDescriptions> CriticalDocumentDescriptions { get; set; }

    }
}
