using CityWatch.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace CityWatch.Web.Models
{
    public class SiteEquipmentsViewModelcs
    {
        public string EquipmentType { get; set; }
        public List<EquipmentItemDetails> Items { get; set; }
    }
   
    public class EquipmentItemDetails
    {
        public int Id { get; set; }
        public string SerialNumber { get; set; }
        public string Brand { get; set; }
    }
}
