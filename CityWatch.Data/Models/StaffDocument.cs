using System;
using CityWatch.Data.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CityWatch.Data.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace CityWatch.Data.Models
{

    public class StaffDocument
    {
        
        [Key]
        public int Id { get; set; }
        public string FileName { get; set; }
        public DateTime LastUpdated { get; set; }
        public int DocumentType { get; set; }
        public string DocumentModuleName { get; set; }
        public string SOP { get; set; }

        public int? ClientSite { get; set; }

        [NotMapped]
        public List<SelectListItem> ClientSites { get; set; }

        [NotMapped]
        public string ClientTypeName { get; set; }
        

        [NotMapped]
        public string ClientSiteName { get; set; }
        


        [NotMapped]
        public string FormattedLastUpdated { get { return LastUpdated.ToString("dd MMM yyyy @ HH:mm"); } } 
    }
}
