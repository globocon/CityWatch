﻿using Microsoft.AspNetCore.DataProtection;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;

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
        public string HomePageMessage2 { get; set; }
        public string MessageBarColour { get; set; }
        public DateTime HomePageMessageUploadedOn { get; set; }
        public string BannerLogoPath { get; set; }
        public string BannerMessage { get; set; }
        public string Hyperlink { get; set; }
        //p1-225 Core Settings-start
        public string HyperlinkLabel { get; set; }
        public string HyperlinkColour { get; set; }
        public string LogoHyperlink { get; set; }
        //p1-225 Core Settings-end
        public DateTime BannerMessageUploadedOn { get; set; }

        public string EmailMessage { get; set; }
        public DateTime EmailMessageUploadedOn { get; set; }

        public string ApiProvider { get; set; }
        public string ApiSecretkey { get; set; }

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
        public string IRMail { get; set; }
        public string KPIMail { get; set; }
        public string FusionMail { get; set; }
        public string TimesheetsMail { get; set; }
        public string ApiProviderIR { get; set; }
        public string ApiSecretkeyIR { get; set; }
    }
}
