﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class DuressAppField
    {
        [Key]
        public int Id { get; set; }

        public int TypeId { get; set; }

        public string Name { get; set; }
        public string Label { get; set; }
    }
}
