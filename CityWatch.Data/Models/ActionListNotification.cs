using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class ActionListNotification
    {
        public int ID { get; set; }
        public int ClientSiteID { get; set; }
        public string AlarmKeypadCode { get; set; }
        public string Physicalkey { get; set; }
        public string CombinationLook { get; set; }
        public string Action1 { get; set; }
        public string Action2 { get; set; }
        public string Action3 { get; set; }
        public string Action4 { get; set; }
        public string Action5 { get; set; }
        public string SiteAlarm { get; set; }
        public string CommentsForControlRoomOperator { get; set; }
        public string Message { get; set; }
    }

}
