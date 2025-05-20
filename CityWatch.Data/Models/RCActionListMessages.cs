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
    public class RCActionListMessages
    {
        [Key]
        public int Id { get; set; }
        

        public string Notifications { get; set; }
        public string Subject { get; set; }

        public string AlarmKeypadCode { get; set; }

        public string Action1 { get; set; }

        public string Physicalkey { get; set; }

        public string Action2 { get; set; }

        public string SiteCombinationLook { get; set; }

        public string Action4 { get; set; }

        public string Action3 { get; set; }

        public string Action5 { get; set; }

        public string CommentsForControlRoomOperator { get; set; }

        public DateTime messagetime { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsState { get; set; }
        public bool IsNational { get; set; }
        public bool IsClientType { get; set; }
        public bool IsSMSPersonal { get; set; }
        public bool IsSMSSmartWand { get; set; }
        public bool IsPersonalEmail { get; set; }



    }
}
