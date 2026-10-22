using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class GeneralFeeds
    {
        public int Id { get; set; }
        public string Brand { get; set; }
        public string APIStrings { get; set; }
        public string keys { get; set; }
    }
}
