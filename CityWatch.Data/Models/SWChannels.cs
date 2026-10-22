using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class SWChannels
    {
        public int Id { get; set; }
        public string SWChannel { get; set; }
        public string keys { get; set; }
    }
}
