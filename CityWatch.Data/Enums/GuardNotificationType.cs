using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Enums
{
    /// <summary>
    /// Ids match the seeded rows in dbo.NotificationTypes (DbScript/377).
    /// </summary>
    public enum GuardNotificationType
    {
        [Display(Name = "Training Course")]
        TrainingCourse = 1,

        [Display(Name = "Site Message")]
        SiteMessage = 2
    }

    /// <summary>
    /// Who a notification is addressed to. Derived from which of GuardId / ClientSiteId is
    /// set on the row rather than stored, so the two can never contradict each other.
    /// </summary>
    public enum GuardNotificationTarget
    {
        [Display(Name = "Guard")]
        Guard = 1,

        [Display(Name = "Site")]
        Site = 2
    }

    /// <summary>
    /// Ids of dbo.TrainingCourseStatus (DbScript/282 and 290). A course counts as outstanding
    /// until it reaches <see cref="Completed"/> — "Certificate Hold" still needs the guard to
    /// sit a practical, which is why the rest of the app also tests for != 4 rather than
    /// treating anything past "Progress" as done.
    /// </summary>
    public enum TrainingCourseStatusType
    {
        [Display(Name = "Assigned")]
        Assigned = 1,

        [Display(Name = "Progress")]
        Progress = 2,

        [Display(Name = "Certificate Hold")]
        CertificateHold = 3,

        [Display(Name = "Completed")]
        Completed = 4
    }
}
