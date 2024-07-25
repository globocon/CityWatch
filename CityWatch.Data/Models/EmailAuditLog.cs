using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class EmailAuditLog
    {
        public int ID { get; set; }
        public int UserID { get; set; }
        public int GuardID { get; set; }
        public string IPAddress { get; set; }      
        public string ToAddress { get; set; }
        public string BCCAddress { get; set; }
        public string Module { get; set; }
        public string Type { get; set; }
        public string EmailSubject { get; set; }
        public string AttachmentFileName { get; set; }
        public DateTime SendingDate { get; set; }
        public bool SendStatus { get; set; }
    }
}
