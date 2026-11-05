using CityWatch.Data.Enums;
using CityWatch.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Data.Providers
{
    public interface IGuardNotificationDataProvider
    {
        /// <summary>
        /// Brings dbo.GuardNotifications back in line with the sources it is derived from for
        /// one guard: raises notifications that are newly due, and deactivates the ones whose
        /// source has been resolved. Safe to call on every read.
        /// </summary>
        void SyncNotifications(int guardId);

        /// <summary>
        /// The guard's tab: notifications addressed to them, plus those addressed to the site
        /// they are currently signed in at. Read state is resolved for this guard.
        /// </summary>
        List<GuardNotification> GetNotifications(int guardId, int clientSiteId);

        int GetUnreadCount(int guardId, int clientSiteId);

        bool SetReadStatus(int notificationId, int guardId, int clientSiteId, bool isRead);

        int MarkAllAsRead(int guardId, int clientSiteId);

        /// <summary>
        /// Raises a notification that has no derived source — an operator addressing a site,
        /// or a one-off message to a guard. Exactly one of guardId/clientSiteId must be set.
        /// </summary>
        GuardNotification CreateNotification(int? guardId, int? clientSiteId, string title, string message);
    }

    public class GuardNotificationDataProvider : IGuardNotificationDataProvider
    {
        /// <summary>
        /// The wording the client asked for (P4 notifications spec 3b). The course name is
        /// prefixed so a guard carrying several outstanding courses can tell the cards apart.
        /// </summary>
        private const string TrainingCourseMessage =
            "YOU HAVE A NEW COURSE to complete - please access via web browser, and select " +
            "TOOLS > Training & Assessment, or by hitting the HR button within the LB/KV screen.";

        private const string TrainingCourseTitle = "New course to complete";

        private readonly CityWatchDbContext _context;

        public GuardNotificationDataProvider(CityWatchDbContext context)
        {
            _context = context;
        }

        public void SyncNotifications(int guardId)
        {
            if (guardId <= 0)
                return;

            SyncTrainingCourseNotifications(guardId);
        }

        /// <summary>
        /// Source of truth: a GuardTrainingAndAssessment row that has not reached status
        /// Completed and whose course has not been deleted — the same test
        /// <see cref="IGuardDataProvider.GetGuardTrainingAndAssessment"/> uses to decide what
        /// still shows in the guard's HR record, so the tab and the HR record cannot disagree.
        ///
        /// Courses are assigned to a person, so these are always guard-targeted.
        /// </summary>
        private void SyncTrainingCourseNotifications(int guardId)
        {
            const int typeId = (int)GuardNotificationType.TrainingCourse;
            const int completed = (int)TrainingCourseStatusType.Completed;

            /* One query for the outstanding courses, with the course name resolved in SQL.
               HrSettings.Description is the course title; the reference number in front of it
               is what the rest of the system calls the course (HrSettings.CertificateRecordName),
               so the card and a certificate name the same course the same way.

               The two reference lookups go through subqueries rather than the navigation
               properties on purpose. ReferenceNoNumberId/ReferenceNoAlphabetId are non-nullable
               ints, so navigating them makes EF emit an INNER JOIN — and a course with no
               reference number configured would then drop out of this query entirely and never
               raise a notification at all. HrSettings.CertificateRecordName exists precisely
               because that case occurs in this data. A subquery yields NULL instead, and
               BuildTrainingCourseMessage falls back to the bare description. */
            var outstanding = _context.GuardTrainingAndAssessment
                .Where(x => x.GuardId == guardId
                            && x.TrainingCourseStatusId != completed
                            && !x.TrainingCourses.IsDeleted)
                .Select(x => new
                {
                    x.Id,
                    x.TrainingCourses.HrSetting.Description,
                    ReferenceNumber = _context.ReferenceNoNumbers
                        .Where(r => r.Id == x.TrainingCourses.HrSetting.ReferenceNoNumberId)
                        .Select(r => r.Name)
                        .FirstOrDefault(),
                    ReferenceAlphabet = _context.ReferenceNoAlphabets
                        .Where(r => r.Id == x.TrainingCourses.HrSetting.ReferenceNoAlphabetId)
                        .Select(r => r.Name)
                        .FirstOrDefault()
                })
                .ToList();

            var existing = _context.GuardNotifications
                .Where(x => x.GuardId == guardId && x.NotificationTypeId == typeId)
                .ToList();

            var outstandingIds = outstanding.Select(x => x.Id).ToHashSet();
            var changed = false;

            // Raise: a course the guard has not been told about yet.
            foreach (var course in outstanding)
            {
                var notification = existing.FirstOrDefault(x => x.ReferenceId == course.Id);
                if (notification == null)
                {
                    _context.GuardNotifications.Add(new GuardNotification
                    {
                        GuardId = guardId,
                        ClientSiteId = null,
                        NotificationTypeId = typeId,
                        ReferenceId = course.Id,
                        Title = TrainingCourseTitle,
                        Message = BuildTrainingCourseMessage(course.ReferenceNumber, course.ReferenceAlphabet, course.Description),
                        CreatedOn = DateTime.Now,
                        IsActive = true
                    });
                    changed = true;
                }
                else if (!notification.IsActive)
                {
                    /* The course was completed and has been re-assigned (or re-opened from
                       Certificate Hold). Treat it as new again: dated now so it does not
                       surface at the bottom of the list with its original date, and with the
                       guard's old read mark cleared so it shows as unread. */
                    notification.IsActive = true;
                    notification.DeactivatedOn = null;
                    notification.CreatedOn = DateTime.Now;
                    ClearReads(notification.Id);
                    changed = true;
                }
            }

            // Retire: the course has been completed, or the course itself was deleted.
            foreach (var notification in existing.Where(x => x.IsActive && !outstandingIds.Contains(x.ReferenceId)))
            {
                notification.IsActive = false;
                notification.DeactivatedOn = DateTime.Now;
                changed = true;
            }

            if (!changed)
                return;

            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateException)
            {
                /* UX_GuardNotifications_Source rejected a duplicate insert: two devices synced
                   the same guard at the same moment and the other one won. The row the guard
                   needs now exists either way, so drop this attempt rather than failing the
                   fetch that triggered the sync. */
                foreach (var entry in _context.ChangeTracker.Entries<GuardNotification>().ToList())
                    entry.State = EntityState.Detached;
            }
        }

        private void ClearReads(int notificationId)
        {
            var reads = _context.GuardNotificationReads
                .Where(x => x.GuardNotificationId == notificationId)
                .ToList();

            if (reads.Count > 0)
                _context.GuardNotificationReads.RemoveRange(reads);
        }

        private static string BuildTrainingCourseMessage(string referenceNumber, string referenceAlphabet, string description)
        {
            var reference = (referenceNumber + referenceAlphabet)?.Trim();
            var courseName = string.IsNullOrEmpty(reference) ? description : reference + " " + description;

            return string.IsNullOrWhiteSpace(courseName)
                ? TrainingCourseMessage
                : $"{courseName} - {TrainingCourseMessage}";
        }

        /// <summary>
        /// One query for both targeting shapes, with the target's name projected so the card
        /// header can say who the notification is for, and the guard's own read row left-joined
        /// so "read" is answered per guard rather than per notification.
        /// </summary>
        private IQueryable<GuardNotification> QueryVisible(int guardId, int clientSiteId)
        {
            return _context.GuardNotifications
                .Where(x => x.IsActive
                            && ((x.GuardId.HasValue && x.GuardId.Value == guardId)
                                || (x.ClientSiteId.HasValue && clientSiteId > 0 && x.ClientSiteId.Value == clientSiteId)));
        }

        public List<GuardNotification> GetNotifications(int guardId, int clientSiteId)
        {
            if (guardId <= 0)
                return new List<GuardNotification>();

            var rows = QueryVisible(guardId, clientSiteId)
                .Select(x => new
                {
                    Notification = x,
                    TargetName = x.GuardId.HasValue ? x.Guard.Name : x.ClientSite.Name,
                    Read = x.Reads.FirstOrDefault(r => r.GuardId == guardId)
                })
                .AsNoTracking()
                .ToList();

            return rows
                .Select(row =>
                {
                    var notification = row.Notification;
                    notification.TargetName = row.TargetName;
                    notification.IsRead = row.Read != null;
                    notification.ReadOn = row.Read?.ReadOn;
                    return notification;
                })
                .OrderByDescending(x => x.CreatedOn)
                .ThenByDescending(x => x.Id)
                .ToList();
        }

        public int GetUnreadCount(int guardId, int clientSiteId)
        {
            if (guardId <= 0)
                return 0;

            return QueryVisible(guardId, clientSiteId)
                .Count(x => !x.Reads.Any(r => r.GuardId == guardId));
        }

        /// <summary>
        /// The guard and site are part of the lookup, not just the id: a guard must not be able
        /// to mark someone else's notification — or another site's — by guessing an id.
        /// </summary>
        public bool SetReadStatus(int notificationId, int guardId, int clientSiteId, bool isRead)
        {
            var visible = QueryVisible(guardId, clientSiteId).Any(x => x.Id == notificationId);
            if (!visible)
                return false;

            var read = _context.GuardNotificationReads
                .FirstOrDefault(x => x.GuardNotificationId == notificationId && x.GuardId == guardId);

            if (isRead)
            {
                if (read != null)
                    return true;

                _context.GuardNotificationReads.Add(new GuardNotificationRead
                {
                    GuardNotificationId = notificationId,
                    GuardId = guardId,
                    ReadOn = DateTime.Now
                });
            }
            else
            {
                if (read == null)
                    return true;

                _context.GuardNotificationReads.Remove(read);
            }

            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateException)
            {
                /* The guard marked the same notification read from two devices at once and
                   UX_GuardNotificationReads_Notification_Guard rejected the second row. The
                   state they asked for is the state that now holds, so this is a success. */
                foreach (var entry in _context.ChangeTracker.Entries<GuardNotificationRead>().ToList())
                    entry.State = EntityState.Detached;
            }

            return true;
        }

        public int MarkAllAsRead(int guardId, int clientSiteId)
        {
            if (guardId <= 0)
                return 0;

            var unreadIds = QueryVisible(guardId, clientSiteId)
                .Where(x => !x.Reads.Any(r => r.GuardId == guardId))
                .Select(x => x.Id)
                .ToList();

            if (unreadIds.Count == 0)
                return 0;

            var readOn = DateTime.Now;
            foreach (var notificationId in unreadIds)
            {
                _context.GuardNotificationReads.Add(new GuardNotificationRead
                {
                    GuardNotificationId = notificationId,
                    GuardId = guardId,
                    ReadOn = readOn
                });
            }

            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateException)
            {
                foreach (var entry in _context.ChangeTracker.Entries<GuardNotificationRead>().ToList())
                    entry.State = EntityState.Detached;
                return 0;
            }

            return unreadIds.Count;
        }

        public GuardNotification CreateNotification(int? guardId, int? clientSiteId, string title, string message)
        {
            var hasGuard = guardId.HasValue && guardId.Value > 0;
            var hasSite = clientSiteId.HasValue && clientSiteId.Value > 0;

            // Mirrors CK_GuardNotifications_Target: one target, and only one.
            if (hasGuard == hasSite)
                throw new ArgumentException("A notification must target either a guard or a site, not both and not neither.");

            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("A notification must have a message.", nameof(message));

            var notification = new GuardNotification
            {
                GuardId = hasGuard ? guardId : null,
                ClientSiteId = hasSite ? clientSiteId : null,
                NotificationTypeId = (int)GuardNotificationType.SiteMessage,
                ReferenceId = 0,
                Title = string.IsNullOrWhiteSpace(title) ? "Notification" : title.Trim(),
                Message = message.Trim(),
                CreatedOn = DateTime.Now,
                IsActive = true
            };

            _context.GuardNotifications.Add(notification);
            _context.SaveChanges();

            return notification;
        }
    }
}
