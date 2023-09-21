using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Models
{
    public enum KvlFieldType
    {
        [Display(Name = "Vehicle Config")]
        VehicleConfig = 1,

        [Display(Name = "Trailer Type")]
        TrailerType,

        [Display(Name = "Type of Individual")]
        IndividualType,

        [Display(Name = "Entry Reason")]
        EntryReason,

        [Display(Name = "Product")]
        ProductType,

        [Display(Name = "ID / Plate")]
        Plate,

        [Display(Name = "Docket Reasons")]
        DocketReasons,
        [Display(Name = "Person of Interest (POI)")]
        PersonOfInterest,
    }

    public class KeyVehcileLogField
    {
        [Key]
        public int Id { get; set; }

        public KvlFieldType TypeId { get; set; }

        public string Name { get; set; }

        public bool IsDeleted { get; set; }
    }
}
