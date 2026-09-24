using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class RadioCheckStatus
    {
        public int Id { get; set; }
        public string ReferenceNo { get; set; }
        public string Name { get; set; }
        public string RadioCheckStatusColorName { get; set; }
        public int? RadioCheckStatusColorId { get; set; }
        [ForeignKey("RadioCheckStatusColorId")]
        public RadioCheckStatusColor RadioCheckStatusColor { get; set; }
    }
}
