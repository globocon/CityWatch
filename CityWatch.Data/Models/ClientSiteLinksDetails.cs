using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CityWatch.Data.Models
{

    public class ClientSiteLinksDetails
    {
        [Key]
        public int Id { get; set; }
        public int ClientSiteLinksTypeId { get; set; }
        [ForeignKey("ClientSiteLinksTypeId")]
        public string Title { get; set; }
        public string Hyperlink { get; set; }
        public string State { get; set; }
        [NotMapped]
        public int typeId { get; set; }

    }
    
}
