using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class RadioCheckLogbookSiteDetails
    {
        [Key]
        public int Id { get; set; }

        public int ClientSiteId { get; set; }

        public DateTime CrDate { get; set; }


    }
}
