using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Enums
{
    public enum PatrolTouringMode
    {
        [Display(Name = "Standard (STND)")]
        STND = 0,

        [Display(Name = "Patrol Car (PCAR)")]
        PCAR = 1,

        [Display(Name = "Inspector (INSP)")]
        INSP = 2
    }
}
