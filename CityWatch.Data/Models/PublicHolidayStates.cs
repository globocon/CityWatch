using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public  class PublicHolidayStates
    {
        public int Id { get; set; }
        public int CalendarEventId { get; set; }
        public string  State { get; set; }
        public bool IsDeleted { get; set; }
    }
}
