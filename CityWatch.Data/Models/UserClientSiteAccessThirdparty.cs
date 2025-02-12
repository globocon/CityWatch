using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class UserClientSiteAccessThirdparty
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        public int ClientSiteId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        [ForeignKey("ClientSiteId")]
        public ClientSite ClientSite { get; set; }
    }
}
