using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class CompanyDetails
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Domain { get; set; }
        public DateTime LastUploaded { get; set; }
        public DateTime PrimaryLogoUploadedOn { get; set; }
        public string PrimaryLogoPath { get; set; }
        public string HomePageMessage { get; set; }
        public string MessageBarColour { get; set; }
        public DateTime HomePageMessageUploadedOn { get; set; }
        public string BannerLogoPath { get; set; }
        public string BannerMessage { get; set; }
        public string Hyperlink { get; set; }
        public DateTime BannerMessageUploadedOn { get; set; }

        public string EmailMessage { get; set; }
        public DateTime EmailMessageUploadedOn { get; set; }

        [NotMapped]
        public string FormattedLastUploaded { get { return LastUploaded.ToString("dd MMM yyyy @ HH:mm"); } }
        [NotMapped]
        public string FormattedPrimaryLogoUploaded { get { return PrimaryLogoUploadedOn.ToString("dd MMM yyyy @ HH:mm"); } }
        [NotMapped]
        public string FormattedHomePageMessageUploaded { get { return HomePageMessageUploadedOn.ToString("dd MMM yyyy @ HH:mm"); } }
        [NotMapped]
        public string FormattedBannerMessageUploaded { get { return BannerMessageUploadedOn.ToString("dd MMM yyyy @ HH:mm"); } }
        [NotMapped]
        public string FormattedEmailMessageUploaded { get { return BannerMessageUploadedOn.ToString("dd MMM yyyy @ HH:mm"); } }
    }
}
