using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CityWatch.Data.Models
{
    public class RCLinkedDuressMaster
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string GroupName { get; set; }
        public ICollection<RCLinkedDuressClientSites> RCLinkedDuressClientSites { get; set; }


    }
}
