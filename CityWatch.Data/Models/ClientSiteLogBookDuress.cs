using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class ClientSiteLogBookDuress
    {
        [Key]
        public int Id { get; set; }

        public int ClientSiteId { get; set; }

        public bool IsActive { get; set; }

        public int? ActivatedBy { get; set; }

        public DateTime? ActivatedAt { get; set; }
    }
}
