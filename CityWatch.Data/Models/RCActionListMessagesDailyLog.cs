using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using static Dropbox.Api.Sharing.RequestedLinkAccessLevel;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CityWatch.Data.Models
{
    public class RCActionListMessagesDailyLog
    {
        [Required]
        public int Id { get; set; }
        public int RCActionListMessagesId { get; set; }
        public DateTime SentDate { get; set; }
    }
}

