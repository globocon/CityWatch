using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class ClientSiteRadioCheck
    {
        [Key]
        public int Id { get; set; }

        public int ClientSiteId { get; set; }

        public int GuardId { get; set; }

        public DateTime CheckedAt { get; set; }

        public string Status { get; set; }

        public bool Active { get; set; }
        public int? RadioCheckStatusId { get; set; }
        [ForeignKey("RadioCheckStatusId")]
        public RadioCheckStatus RadioCheckStatus { get; set; }


    }
}
