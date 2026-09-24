using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class LoginUserHistory
    {
        [Key]
        public int Id { get; set; }
        public int LoginUserId { get; set; }
        
        public DateTime? LoginTime { get; set; }

        public string IPAddress { get; set; }

        [NotMapped]
        public string FormattedLastLoginDate
        {
            get
            {
                return LoginTime.HasValue
                    ? LoginTime.Value.ToString("dd/MM/yyyy HH:mm:ss")
                    : null;
            }
        }
        public int GuardId { get; set; }
        public int ClientSiteId { get; set; }
        [NotMapped]
        public string guard { get; set; }

        [NotMapped]
        public string SiteName { get; set; }

        [NotMapped]
        public string loginType { get; set; }
    }
}
