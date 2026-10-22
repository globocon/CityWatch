using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class LoginUserRCHistory
    {
        [Key]
        public int Id { get; set; }
        public DateTime? LoginTime { get; set; }

        public string IPAddress { get; set; }

        [NotMapped]
        public string FormattedLastLoginDate
        {
            get
            {
                return LoginTime.HasValue
                    ? LoginTime.Value.ToString("ddd, dd MMM yyyy")
                    : null;
            }
        }
        [NotMapped]
        public string FormattedLastLoginTime
        {
            get
            {
                return LoginTime.HasValue
                    ? LoginTime.Value.ToString("HH:mm tt")
                    : null;
            }
        }
        public int GuardId { get; set; }

        [ForeignKey("GuardId")]
        public Guard Guard { get; set; }

        

        
    }
    public class GuardRCLoginDetail
    {
        public string GuardName { get; set; }
        public string License { get; set; }
        public List<LoginUserRCHistory> Logins { get; set; }
    }
}
