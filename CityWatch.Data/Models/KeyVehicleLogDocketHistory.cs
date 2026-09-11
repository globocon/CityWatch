using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace CityWatch.Data.Models
{
    public class KeyVehicleLogDocketHistory
    {
        [Key]
        public int Id { get; set; }
        public string DocketSerialNo { get; set; }
        public string FileName { get; set; }
        public string DocketReason { get; set; }
        public int KeyVehicleLogId { get; set; }
        [ForeignKey("KeyVehicleLogId")]
        public KeyVehicleLog KeyVehicleLog { get; set; }
    }
}
