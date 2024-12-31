using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class TestQuestionSettings
    {
        [Key]
        public int Id { get; set; }
        public int CourseDurationId { get; set; }
        public int TestDurationId { get; set; }
        public int PassMarkId { get; set; }
        public int AttemptsId { get; set; }
        public int CertificateExpiryId { get; set; }
        public int HRSettingsId { get; set; }
        public bool IsCertificateExpiry { get; set; }

        public bool IsCertificateWithQAndADump { get; set; }
        public bool IsCertificateHoldUntilPracticalTaken { get; set; }
        public bool IsAnonymousFeedback { get; set; }
        public bool IsDeleted { get; set; }

        [ForeignKey("CourseDurationId")]
        public CourseDuration CourseDuration { get; set; }

        [ForeignKey("TestDurationId")]
        public TestDuration TestDuration { get; set; }
        [ForeignKey("PassMarkId")]
        public PassMark PassMark { get; set; }
        [ForeignKey("AttemptsId")]
        public Attempts Attempts { get; set; }
        [ForeignKey("CertificateExpiryId")]
        public CertificateExpiryYears CertificateExpiryYears { get; set; }
        [ForeignKey("HRSettingsId")]
        public HrSettings HrSettings { get; set; }

    }
}
