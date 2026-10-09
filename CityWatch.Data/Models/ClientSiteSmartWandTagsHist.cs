using System;
using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Models
{
    /// <summary>
    /// One recorded change to a wand tag.
    ///
    /// Written by the trigger on ClientSiteSmartWandTags, not by this application, so a change
    /// made by any route is captured - the Kpi admin screen, the mobile app, a support script, or
    /// someone editing the table by hand. The application's only part is telling the trigger who
    /// is acting, which ClientSiteWandDataProvider does immediately before it saves.
    ///
    /// Read-only from code: nothing should ever insert into this table directly, or the history
    /// stops being a faithful record of what happened to the tag.
    /// </summary>
    public class ClientSiteSmartWandTagsHist
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// The tag the change was made to. Deliberately not a foreign key - the history has to
        /// outlive the tag row if it is ever hard-deleted.
        /// </summary>
        public int TagId { get; set; }

        /// <summary>Added, Changed, Deleted (soft), Restored, or Removed (hard delete).</summary>
        public string Action { get; set; }

        public DateTime ChangedOn { get; set; }

        /// <summary>Null when the change was not made through the application.</summary>
        public int? ChangedByUserId { get; set; }

        /// <summary>Null when the change was not made through the application.</summary>
        public int? ChangedByGuardId { get; set; }

        /// <summary>
        /// Readable actor - the user or guard name, or the SQL login prefixed with
        /// "(direct database change)" when the change came from outside the application.
        /// </summary>
        public string ChangedBy { get; set; }

        /// <summary>
        /// The change in a sentence, e.g. "label changed from 'Close' to 'Locked (NEW)'". The
        /// Old/New pairs below are the same information in queryable form.
        /// </summary>
        public string Note { get; set; }

        public int? OldClientSiteId { get; set; }
        public int? NewClientSiteId { get; set; }

        public string OldUId { get; set; }
        public string NewUId { get; set; }

        public int? OldTagsTypeId { get; set; }
        public int? NewTagsTypeId { get; set; }

        public string OldLabelDescription { get; set; }
        public string NewLabelDescription { get; set; }

        public bool? OldIsDeleted { get; set; }
        public bool? NewIsDeleted { get; set; }

        public bool? OldFqBypass { get; set; }
        public bool? NewFqBypass { get; set; }
    }
}
