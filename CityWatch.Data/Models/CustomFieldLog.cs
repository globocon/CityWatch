using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class CustomFieldLog
    {
        [Key]
        public int Id { get; set; }

        public int CustomFieldId { get; set; }

        public int ClientSiteLogBookId { get; set; }

        public string DayValue { get; set; }

        [ForeignKey("CustomFieldId")]
        public ClientSiteCustomField ClientSiteCustomField { get; set; }

        [ForeignKey("ClientSiteLogBookId")]
        public ClientSiteLogBook ClientSiteLogBook { get; set; }

    }
}
