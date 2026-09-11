using CityWatch.Data.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Web.Models
{
    public class ScannerTagDetails 
    {
        public int Id { get; set; }
        public int ClientSiteId { get; set; }
        public string ClientSiteName { get; set; }
        public string UId { get; set; }
        public int TagsTypeId { get; set; }
        public string TagsType { get; set; }
        public string LabelDescription { get; set; }
    }
}
