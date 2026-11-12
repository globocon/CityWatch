using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class RadioCheckPushMessages
    {
        public int Id { get; set; }
        public int ClientSiteId { get; set; }
        public int LogBookId { get; set; }
        public string Notes { get; set; }
        public int EntryType { get; set; }
        public DateTime Date { get; set; }
        public int IsAcknowledged { get; set; }
        // Project 4 , Task 48, Audio notification, Added By Binoy
        public bool? PlayNotificationSound { get; set; } = true;
        public int IsDuress { get; set; }
        

    }
}
