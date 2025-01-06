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
        public TrainingCourseDuration CourseDuration { get; set; }

        [ForeignKey("TestDurationId")]
        public TrainingTestDuration TestDuration { get; set; }
        [ForeignKey("PassMarkId")]
        public TrainingTestPassMark PassMark { get; set; }
        [ForeignKey("AttemptsId")]
        public TrainingTestAttempts Attempts { get; set; }
        [ForeignKey("CertificateExpiryId")]
        public TrainingCertificateExpiryYears CertificateExpiryYears { get; set; }
        [ForeignKey("HRSettingsId")]
        public HrSettings HrSettings { get; set; }

    }
}
