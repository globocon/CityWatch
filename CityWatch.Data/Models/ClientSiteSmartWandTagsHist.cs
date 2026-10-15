using System;
using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Models
{
    /// <summary>
    /// One recorded change to a wand tag.
    ///
    /// Written by ClientSiteWandDataProvider as part of making the change, so every row also
    /// says which application it came from and what version that application was running -
    /// which a database trigger could not have known. ClientSiteWandDataProvider is the only
    /// thing that writes a tag, so every route into one is covered: the Kpi admin screen and
    /// the mobile app. A change made directly against the table - a support script, or someone
    /// editing it by hand - leaves no row, which the earlier trigger-based version did catch.
    ///
    /// Nothing else should insert here, or the history stops being a faithful record of what
    /// happened to the tag.
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

        /// <summary>Added, Changed, Deleted (soft) or Restored.</summary>
        public string Action { get; set; }

        public DateTime ChangedOn { get; set; }

        /// <summary>Null when the caller did not say who was acting.</summary>
        public int? ChangedByUserId { get; set; }

        /// <summary>Null when the caller did not say who was acting.</summary>
        public int? ChangedByGuardId { get; set; }

        /// <summary>
        /// Readable actor - the user or guard name. Null when the id given matched nobody, or
        /// when no id was given at all; the ids above are then the only record of who acted.
        /// </summary>
        public string ChangedBy { get; set; }

        /// <summary>Which application the change came from: see <see cref="WandTagAppType"/>.</summary>
        public string AppType { get; set; }

        /// <summary>
        /// The build that application was running, e.g. "1.54.2". For the mobile app this is
        /// whatever it last reported - see WandTagChange. Null when it is not known.
        /// </summary>
        public string AppVersion { get; set; }

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
