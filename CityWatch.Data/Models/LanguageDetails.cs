using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class LanguageDetails
    {
        [Key]
        public int Id { get; set; }
        public int GuardId { get; set; }
        [ForeignKey("GuardId")]
        public Guard Guard { get; set; }
        public int LanguageID { get; set; }
        [ForeignKey("LanguageID")]
        public LanguageMaster LanguageMaster { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsDeleted { get; set; }
    }
}
