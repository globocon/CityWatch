using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class HandoverNotes
    {
        [Key]
        public int Id { get; set; }
        public string Notes { get; set; }
        public int ClientSiteID { get; set; }
    }
}
