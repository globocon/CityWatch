using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Enums
{
    public enum HrGroup
    {
        [Display(Name = "HR 1 (C4i)")]
        HR1 = 1,

        [Display(Name = "HR 2 (Client)")]
        HR2,

        [Display(Name = "HR 3 (Special)")]
        HR3
    }
}
