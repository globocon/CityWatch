using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Enums
{
    public enum PcarVisitStatusEnum
    {
        [Display(Name = "InProgress")]
        InProgress = 1,

        [Display(Name = "Completed")]
        Completed = 2,

        [Display(Name = "Pushed To PCAR")]
        PushedToPcar = 3
    }
}
