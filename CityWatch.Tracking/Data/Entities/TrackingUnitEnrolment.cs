using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Tracking.Data.Entities
{
    /// <summary>
    /// Per-unit enablement and the consent record. A unit is tracked only when the module is
    /// enabled AND the unit is enrolled AND consent is recorded — the last of those is a
    /// structural guarantee, not a checkbox (§13.5): ingest rejects points from any unit whose
    /// enrolment lacks a consent timestamp.
    ///
    /// Enabling a customer is inserting rows here, never a deployment (§3.4). Disabling a unit
    /// is Level-2 rollback and takes effect on its next batch.
    /// </summary>
    [Table("TrackingUnitEnrolment")]
    public class TrackingUnitEnrolment
    {
        /// <summary>ClientSiteSmartWand.Id. Primary key — one enrolment per unit.</summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int UnitId { get; set; }

        public bool IsEnabled { get; set; }

        public DateTime EnrolledUtc { get; set; }

        /// <summary>User.Id of the administrator who enrolled the unit.</summary>
        public int EnrolledByUserId { get; set; }

        /// <summary>
        /// When the officer's written notice/consent was recorded. Null means consent is not
        /// on file and ingest will refuse the unit even if IsEnabled is true. State
        /// workplace-surveillance law is why this is a column and not a document (§13.8).
        /// </summary>
        public DateTime? ConsentRecordedUtc { get; set; }

        /// <summary>Free-text reference to the consent artefact (register entry, HR record).</summary>
        [MaxLength(200)]
        public string? ConsentReference { get; set; }

        public DateTime? DisabledUtc { get; set; }

        [MaxLength(200)]
        public string? Notes { get; set; }
    }
}
