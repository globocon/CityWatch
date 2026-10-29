using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dropbox.Api.Sharing.RequestedLinkAccessLevel;

namespace CityWatch.Data.Models
{
    public class RCActionListMessagesClientsites
    {
        [Key]
        public int Id { get; set; }
        
        public int ClientSiteId { get; set; }
        public int RCActionListMessagesId { get; set; }
        public bool IsDeleted { get; set; }
    }
}
