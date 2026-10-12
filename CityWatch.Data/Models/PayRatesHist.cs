using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    /// <summary>
    /// One recorded change to a pay rate (Admin > Settings > HR Settings > Renumeration - Pay Rates).
    ///
    /// Added 12 Oct 2026 after "Prixcar - Sat (2026)" Guard Pay was silently set to $0.00 on live
    /// (10 Oct) and nobody could tell who changed it or when - see DbScript 383 for the full story.
    ///
    /// Written by the trigger on PayRates, not by this application, so a change made by any route
    /// is captured. The application's only part is telling the trigger who is acting, which
    /// ConfigDataProvider.SavePayRate / DeletePayRate do immediately before they save.
    ///
    /// Read-only from code: nothing should ever insert into this table directly.
    /// </summary>
    [Table("PayRatesHist")]
    public class PayRatesHist
    {
        [Key]
        public int Id { get; set; }

        /// <summary>The pay rate the change was made to. Deliberately not a foreign key.</summary>
        public int PayRateId { get; set; }

        /// <summary>Added, Changed, Deleted (soft), Restored, or Removed (hard delete).</summary>
        public string Action { get; set; }

        public DateTime ChangedOn { get; set; }

        /// <summary>Null when the change was not made through the application.</summary>
        public int? ChangedByUserId { get; set; }

        /// <summary>
        /// Readable actor - the user name, or the SQL login prefixed with
        /// "(direct database change)" when the change was made outside the application.
        /// </summary>
        public string ChangedBy { get; set; }

        /// <summary>The change in a sentence, e.g. "Guard Pay changed from 41.00 to 0.00".</summary>
        public string Note { get; set; }

        public string OldDescription { get; set; }
        public string NewDescription { get; set; }
        public int? OldPayRateGroupId { get; set; }
        public int? NewPayRateGroupId { get; set; }
        public decimal? OldSellRateToClient { get; set; }
        public decimal? NewSellRateToClient { get; set; }
        public decimal? OldComms1 { get; set; }
        public decimal? NewComms1 { get; set; }
        public decimal? OldComms2 { get; set; }
        public decimal? NewComms2 { get; set; }
        public decimal? OldGuardPayRate { get; set; }
        public decimal? NewGuardPayRate { get; set; }
        public string OldCurrency { get; set; }
        public string NewCurrency { get; set; }
        public bool? OldIsDeleted { get; set; }
        public bool? NewIsDeleted { get; set; }
    }
}
