using CityWatch.Data.Enums;
using CityWatch.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace CityWatch.Web.Models
{
    public class GuardLogViewModel
    {
        public GuardLogViewModel(GuardLog guardLog)
        {
            ClientSiteId = guardLog.GuardLogin?.ClientSiteId;
            EventDateTime = guardLog.EventDateTime;
            Notes = guardLog.Notes;
            GuardInitials = guardLog.GuardLogin?.Guard.Initial;
            IrEntryType = guardLog.IrEntryType;
            GuardId = guardLog.GuardLogin?.Guard.Id;
        }

        public GuardLogViewModel(IEnumerable<PatrolCarLog> patrolCarLogs)
        {
            var logBook = patrolCarLogs.First().ClientSiteLogBook;

            ClientSiteId = logBook.ClientSiteId;
            EventDateTime = new DateTime(logBook.Date.Year, logBook.Date.Month, logBook.Date.Day, 0, 1, 0);

            var notes = new StringBuilder();
            foreach (var patrolCarLog in patrolCarLogs)
            {
                notes.AppendFormat("{0} - {1}: {2} KM \r\n", 
                    patrolCarLog.ClientSitePatrolCar.Model, 
                    patrolCarLog.ClientSitePatrolCar.Rego,
                    patrolCarLog.MileageText);                
            }

            Notes = notes.ToString();
            GuardInitials = "N/A";
        }

        public GuardLogViewModel(IEnumerable<CustomFieldLog> customFieldLogs)
        {
            var logBook = customFieldLogs.First().ClientSiteLogBook;
            EventDateTime = new DateTime(logBook.Date.Year, logBook.Date.Month, logBook.Date.Day, 0, 1, 0);

            var notes = new StringBuilder();
            foreach (var customFieldLog in customFieldLogs.GroupBy(x => x.ClientSiteCustomField.TimeSlot))
            {
                notes.AppendFormat("{0} - ", customFieldLog.Key);
                foreach (var fieldLog in customFieldLog)
                {
                    notes.AppendFormat("{0} : {1}, ", fieldLog.ClientSiteCustomField.Name, fieldLog.DayValue);
                }
                notes.AppendFormat("\n");
            }

            Notes = notes.ToString();
            GuardInitials = "N/A";
        }
        public int? ClientSiteId { get; set; }

        public DateTime EventDateTime { get; set; }

        public string Notes { get; set; }

        [JsonPropertyName("Date")]
        public string Date { get { return EventDateTime.ToString("dd MMM yyyy"); } }

        public string GuardInitials { get; set; }

        public IrEntryType? IrEntryType { get; set; }

        public int? GuardId { get; set; }
    }
}
