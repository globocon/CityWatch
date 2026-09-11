using System;
using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Models
{
    public class GuardReminderEmailLog
    {
        [Key]
        public int Id { get; set; }
        public int GuardId { get; set; }
        public string GuardName { get; set; }
        public string SentTo { get; set; }
        public string CcTo { get; set; }
        public string MessageBody { get; set; }
        public DateTime SentDate { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}
