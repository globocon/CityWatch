using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
   
        [Table("DailyWandFq")]
        public class DailyWandFq
        {
            [Key]
            public int Id { get; set; }

            public int ClientSiteId { get; set; }

            // DB column name is "Fq"
            public int Fq { get; set; }

            public DateTime FqDate { get; set; }

            public DateTime CreatedAt { get; set; }

            public DateTime UpdatedAt { get; set; }

            // not-mapped helpers for UI if needed
            [NotMapped]
            public string ClientSiteName { get; set; }
        }




    

}
