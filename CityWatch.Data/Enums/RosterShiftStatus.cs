using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Enums
{
    public enum RosterShiftStatus
    {
        [Display(Name = "Pushed")]
        Pushed = 0,

        [Display(Name = "Accepted")]
        Accepted = 1,

        [Display(Name = "Declined")]
        Declined = 2
    }
}
