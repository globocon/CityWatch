using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class GuardLoginSmartWandUse
    {
        [Key]
        public int Id { get; set; }

        public int GuardLoginId { get; set; }

        public DateTime CreatedDate { get; set; }

       

        public int? SmartWandId { get; set; }

     

       

        public string IPAddress { get; set; }

        [ForeignKey("GuardLoginId")]
        public GuardLogin GuardLogin { get; set; }

      

        [ForeignKey("SmartWandId")]
        public ClientSiteSmartWand SmartWand { get; set; }

    
    }
}
