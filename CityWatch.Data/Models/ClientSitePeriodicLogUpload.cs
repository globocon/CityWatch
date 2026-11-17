using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    /// <summary>The dump periods that cover more than one day. Persisted as an int.</summary>
    public enum LogDumpPeriodType
    {
        [Display(Name = "Weekly")]
        Weekly = 1,

        [Display(Name = "Monthly")]
        Monthly = 2,
    }

    /// <summary>
    /// A weekly or monthly log dump that has been produced for a site.
    ///
    /// This is the periodic equivalent of ClientSiteLogBooks.DbxUploaded. The daily run marks the
    /// single log book it sent; a periodic run covers many log books and merges them into one
    /// document, so it has nothing of its own to mark - and marking the days it covered would tell
    /// the daily run those days were already sent. The presence of a row here is what stops the
    /// scheduler producing the same period twice.
    /// </summary>
    public class ClientSitePeriodicLogUpload
    {
        [Key]
        public int Id { get; set; }

        public int ClientSiteId { get; set; }

        [ForeignKey("ClientSiteId")]
        public ClientSite ClientSite { get; set; }

        public LogDumpPeriodType PeriodType { get; set; }

        /// <summary>Which report this document is - not necessarily the log book it was built from.</summary>
        public LogBookType LogBookType { get; set; }

        /// <summary>Monday of the week, or the first day of the month.</summary>
        public DateTime PeriodStartDate { get; set; }

        /// <summary>Sunday of the week, or the last day of the month.</summary>
        public DateTime PeriodEndDate { get; set; }

        public DateTime UploadedOn { get; set; }

        public string FileName { get; set; }

        /// <summary>Full Dropbox path the document was filed under.</summary>
        public string FilePath { get; set; }

        /// <summary>
        /// How many days of the period produced pages. A site that was quiet for part of the week
        /// still gets a dump, and this is what says so without opening the PDF.
        /// </summary>
        public int DaysIncluded { get; set; }

        /// <summary>
        /// Whether Dropbox accepted the document. The dump is recorded either way - it was produced
        /// and, where recipients are configured, emailed - so this distinguishes "not done" from
        /// "done but Dropbox refused it", with the reason in SchedulerTaskErrors.
        /// </summary>
        public bool DropboxUploaded { get; set; }

        public string EmailedTo { get; set; }
    }
}
