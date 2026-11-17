using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    /// <summary>
    /// A notification shown in the mobile app's Notifications tab (DbScript/377).
    ///
    /// Rows are reconciled from their source — see
    /// <see cref="Providers.IGuardNotificationDataProvider.SyncNotifications"/> — rather than
    /// written by hand: the source decides whether a notification should exist.
    ///
    /// Addressed either to one guard (<see cref="GuardId"/>) or to a site
    /// (<see cref="ClientSiteId"/>), never both. Read state is NOT here: a site-targeted row
    /// is seen by many guards, so it lives per-guard in <see cref="GuardNotificationRead"/>.
    /// </summary>
    public class GuardNotification
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Set when the notification is for one guard. Null for site notifications.</summary>
        public int? GuardId { get; set; }

        /// <summary>Set when the notification is for everyone at a site. Null for guard notifications.</summary>
        public int? ClientSiteId { get; set; }

        public int NotificationTypeId { get; set; }

        /// <summary>
        /// Id of the row this was derived from, in the table <see cref="NotificationTypeId"/>
        /// names — for <see cref="Enums.GuardNotificationType.TrainingCourse"/> that is
        /// <see cref="GuardTrainingAndAssessment.Id"/>. Zero when there is no derived source.
        /// Not a foreign key: the types point at different tables.
        /// </summary>
        public int ReferenceId { get; set; }

        public string Title { get; set; }

        public string Message { get; set; }

        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// False once the source is resolved (the course was completed, or an operator
        /// withdrew a site message). The notification leaves the tab but the row is kept so
        /// the read history survives.
        /// </summary>
        public bool IsActive { get; set; }

        public DateTime? DeactivatedOn { get; set; }

        [ForeignKey("GuardId")]
        public Guard Guard { get; set; }

        [ForeignKey("ClientSiteId")]
        public ClientSite ClientSite { get; set; }

        [ForeignKey("NotificationTypeId")]
        public NotificationType NotificationType { get; set; }

        public ICollection<GuardNotificationRead> Reads { get; set; }

        /* Populated per reading guard when the provider builds the tab. Read state is
           per-guard (a site notification is read by each guard separately), so it cannot be a
           column — it is resolved against GuardNotificationReads for the guard asking. */

        [NotMapped]
        public bool IsRead { get; set; }

        [NotMapped]
        public DateTime? ReadOn { get; set; }

        /// <summary>Guard name or site name — what the card header shows.</summary>
        [NotMapped]
        public string TargetName { get; set; }

        /// <summary>
        /// Derived from which id is set rather than stored, so the two cannot contradict.
        /// </summary>
        [NotMapped]
        public Enums.GuardNotificationTarget Target =>
            GuardId.HasValue ? Enums.GuardNotificationTarget.Guard : Enums.GuardNotificationTarget.Site;
    }

    /// <summary>
    /// One guard has read one notification. The row's existence IS "read" — marking something
    /// unread deletes it, so there is no third state to reason about.
    /// </summary>
    public class GuardNotificationRead
    {
        [Key]
        public int Id { get; set; }

        public int GuardNotificationId { get; set; }

        public int GuardId { get; set; }

        public DateTime ReadOn { get; set; }

        [ForeignKey("GuardNotificationId")]
        public GuardNotification GuardNotification { get; set; }

        [ForeignKey("GuardId")]
        public Guard Guard { get; set; }
    }

    public class NotificationType
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
