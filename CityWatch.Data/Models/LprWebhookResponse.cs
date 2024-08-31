using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class LprWebhookResponse
    {
        [Key]
        public int Id { get; set; }
        public string license_plate_number { get; set; }
        public string created { get; set; }
        public string camera_id { get; set; }
        public string webhook_id { get; set; }
        public DateTime CrDateTime { get; set; }
        public int ReadStatus { get; set; }
    }
}
