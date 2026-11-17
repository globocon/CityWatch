using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Enums
{
    public enum PcarVisitStatusEnum
    {
        [Display(Name = "Assigned")]
        Assigned = 1,

        [Display(Name = "Accepted")]
        Accepted = 2,

        [Display(Name = "InProgress")]
        InProgress = 3,

        [Display(Name = "Completed")]
        Completed = 4,

        [Display(Name = "Cancelled or Delegated")]
        CancelledOrDelegated = 5
    }
}
