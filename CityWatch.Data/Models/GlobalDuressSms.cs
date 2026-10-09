using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class GlobalDuressSms
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string SmsNumber { get; set; }
        public int RecordCreateUserId { get; set; }
    }
}
