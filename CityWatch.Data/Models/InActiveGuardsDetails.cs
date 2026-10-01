using CityWatch.Data.Providers;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace CityWatch.Data.Models
{
    public  class InActiveGuardsDetails
    {
        [Key]
        public int Id { get; set; }
        public DateTime? LastWorkingDate { get; set; }
        public int GuardId { get; set; }

        [ForeignKey("GuardId")]
        public Guard Guard { get; set; }
    }
}
