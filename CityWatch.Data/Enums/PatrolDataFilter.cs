using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Enums
{
    public enum PatrolDataFilter
    {
        [Display(Name = "All IRs")]
        All = 0,

        [Display(Name = "Patrol Cars Only")]
        PatrolOnly = 1,

        [Display(Name = "Custom Batch (Other)")]
        Custom = 2
    }
}
