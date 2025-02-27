using CityWatch.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Linq;
using System.Reflection.Metadata;

namespace CityWatch.Data
{
    public class CityWatchDbContext : DbContext
    {
        public CityWatchDbContext(DbContextOptions<CityWatchDbContext> options) : base(options)
        {
            Database.SetCommandTimeout(TimeSpan.FromMinutes(5));
        }


        public DbSet<User> Users { get; set; }
        public DbSet<ClientType> ClientTypes { get; set; }
        public DbSet<GuardAccess> GuardAccess { get; set; }
        public DbSet<ClientSite> ClientSites { get; set; }
        public DbSet<ClientSiteKpiSetting> ClientSiteKpiSettings { get; set; }
        public DbSet<ClientSiteDayKpiSetting> ClientSiteDayKpiSettings { get; set; }
        public DbSet<FeedbackTemplate> FeedbackTemplates { get; set; }
        public DbSet<FeedbackType> FeedbackType { get; set; }
        public DbSet<ReportTemplate> ReportTemplates { get; set; }
        public DbSet<StaffDocument> StaffDocuments { get; set; }
        public DbSet<IncidentReport> IncidentReports { get; set; }
        public DbSet<UserClientSiteAccess> UserClientSiteAccess { get; set; }
        public DbSet<DailyClientSiteKpi> DailyClientSiteKpis { get; set; }
        public DbSet<KpiDataImportJob> KpiDataImportJobs { get; set; }
        public DbSet<IncidentReportField> IncidentReportFields { get; set; }
        public DbSet<KpiSendSchedule> KpiSendSchedules { get; set; }
        public DbSet<KpiSendScheduleClientSite> KpiSendScheduleClientSites { get; set; }
        public DbSet<KpiSendScheduleJob> KpiSendScheduleJobs { get; set; }
        public DbSet<KpiSendScheduleJobsTimeSheet> KpiSendScheduleJobsTimeSheet { get; set; }
        public DbSet<AppConfiguration> Appconfigurations { get; set; }
        public DbSet<ClientSiteLogBook> ClientSiteLogBooks { get; set; }
        public DbSet<GuardLog> GuardLogs { get; set; }
        public DbSet<Guard> Guards { get; set; }
        public DbSet<ClientSiteKpiNote> ClientSiteKpiNotes { get; set; }
        public DbSet<RCActionList> RCActionList { get; set; }
        public DbSet<ClientSiteSmartWand> ClientSiteSmartWands { get; set; }
        public DbSet<GuardLogin> GuardLogins { get; set; }
        public DbSet<IncidentReportPosition> IncidentReportPositions { get; set; }
        public DbSet<IncidentReportPSPF> IncidentReportPSPF { get; set; }
        public DbSet<KpiSendScheduleSummaryNote> KpiSendScheduleSummaryNotes { get; set; }
        public DbSet<IncidentReportEventType> IncidentReportEventTypes { get; set; }
        public DbSet<KeyVehicleLog> KeyVehicleLogs { get; set; }
        public DbSet<ClientSitePatrolCar> ClientSitePatrolCars { get; set; }
        public DbSet<PatrolCarLog> PatrolCarLogs { get; set; }
        public DbSet<KpiSendScheduleSummaryImage> KpiSendScheduleSummaryImages { get; set; }
        public DbSet<ClientSiteCustomField> ClientSiteCustomFields { get; set; }
        public DbSet<CustomFieldLog> CustomFieldLogs { get; set; }
        public DbSet<ClientSitePoc> ClientSitePocs { get; set; }
        public DbSet<ClientSiteLocation> ClientSiteLocations { get; set; }
        public DbSet<KeyVehcileLogField> KeyVehcileLogFields { get; set; }
        public DbSet<ClientSiteKey> ClientSiteKeys { get; set; }
        public DbSet<KeyVehicleLogAuditHistory> KeyVehicleLogAuditHistory { get; set; }
        public DbSet<ClientSiteRadioStatus> ClientSiteRadioStatus { get; set; }
        public DbSet<KeyVehicleLogProfile> KeyVehicleLogVisitorProfiles { get; set; }
        public DbSet<KeyVehicleLogVisitorPersonalDetail> KeyVehicleLogVisitorPersonalDetails { get; set; }
        public DbSet<GuardLicense> GuardLicenses { get; set; }
        public DbSet<GuardComplianceAndLicense> GuardComplianceLicense { get; set; }
        public DbSet<GuardCompliance> GuardCompliances { get; set; }
        public DbSet<ClientSiteActivityStatus> ClientSiteActivityStatus { get; set; }
        public DbSet<ClientSiteRadioCheck> ClientSiteRadioChecks { get; set; }
        public DbSet<CompanyDetails> CompanyDetails { get; set; }
        public DbSet<ClientSiteManningKpiSetting> ClientSiteManningKpiSettings { get; set; }
        public DbSet<ClientSiteManningKpiSettingADHOC> ClientSiteManningKpiSettingsADHOC { get; set; }
        public DbSet<IncidentReportsPlatesLoaded> IncidentReportsPlatesLoaded { get; set; }
        public DbSet<ClientSiteDuress> ClientSiteDuress { get; set; }
        public DbSet<ClientSiteLinksPageType> ClientSiteLinksPageType { get; set; }
        public DbSet<ClientSiteLinksDetails> ClientSiteLinksDetails { get; set; }
        public DbSet<ClientSiteRadioChecksActivityStatus> ClientSiteRadioChecksActivityStatus { get; set; }
        public DbSet<RadioCheckListGuardData> RadioCheckListGuardData { get; set; }
        public DbSet<RadioCheckListInActiveGuardData> RadioCheckListInActiveGuardData { get; set; }
        public DbSet<RadioCheckListGuardLoginData> RadioCheckListGuardLoginData { get; set; }
        public DbSet<RadioCheckListNotAvailableGuardData> RadioCheckListNotAvailableGuardData { get; set; }
        public DbSet<RadioCheckListGuardKeyVehicleData> RadioCheckListGuardKeyVehicleData { get; set; }
        public DbSet<RadioCheckListGuardIncidentReportData> RadioCheckListGuardIncidentReportData { get; set; }
        public DbSet<RadioCheckDuress> RadioCheckDuress { get; set; }
        public DbSet<RadioCheckStatusColor> RadioCheckStatusColor { get; set; }
        public DbSet<RadioCheckStatus> RadioCheckStatus { get; set; }
        public DbSet<BroadcastBannerLiveEvents> BroadcastBannerLiveEvents { get; set; }
        public DbSet<BroadcastBannerCalendarEvents> BroadcastBannerCalendarEvents { get; set; }

        public DbSet<RadioCheckLogbookSiteDetails> RadioCheckLogbookSiteDetails { get; set; }
        public DbSet<DosAndDontsField> DosAndDontsField { get; set; }

        public DbSet<ActionListNotification> ActionListNotification { get; set; }


        public DbSet<RadioCheckPushMessages> RadioCheckPushMessages { get; set; }

        public DbSet<RadioChecksSmartWandScanResults> RadioChecksSmartWandScanResults { get; set; }

        public DbSet<RadioCheckListSWReadData> RadioCheckListSWReadData { get; set; }
        public DbSet<GlobalDuressEmail> GlobalDuressEmail { get; set; }

        public DbSet<SiteEventLog> SiteEventLog { get; set; }

        public DbSet<HRGroups> HRGroups { get; set; }
        public DbSet<ReferenceNoNumbers> ReferenceNoNumbers { get; set; }
        public DbSet<ReferenceNoAlphabets> ReferenceNoAlphabets { get; set; }
        public DbSet<HrSettings> HrSettings { get; set; }
        public DbSet<LicenseTypes> LicenseTypes { get; set; }


        public DbSet<TrailerDeatilsViewModel> TrailerDeatilsViewModel { get; set; }

        public DbSet<SmartWandScanGuardHistory> SmartWandScanGuardHistory { get; set; }

        public DbSet<GlobalComplianceAlertEmail> GlobalComplianceAlertEmail { get; set; }

        public DbSet<KPIScheduleDeafultMailbox> KPIScheduleDeafultMailbox { get; set; }
        public DbSet<CriticalDocuments> CriticalDocuments { get; set; }
        public DbSet<CriticalDocumentsClientSites> CriticalDocumentsClientSites { get; set; }

        public DbSet<ClientSiteKpiSettingsCustomDropboxFolder> ClientSiteKpiSettingsCustomDropboxFolder { get; set; }
        public DbSet<CriticalDocumentDescriptions> CriticalDocumentDescriptions { get; set; }
        public DbSet<DropboxDirectory> DropboxDirectory { get; set; }

        public DbSet<ClientSiteRadioChecksActivityStatus_History> ClientSiteRadioChecksActivityStatus_History { get; set; }
        public DbSet<TimeSheet> TimeSheet { get; set; }

        public DbSet<AudioRecordingLog> AudioRecordingLog { get; set; }

        public DbSet<SiteLogUploadHistory> SiteLogUploadHistory { get; set; }


        public DbSet<KpiSendTimesheetSchedules> KpiSendTimesheetSchedules { get; set; }
        public DbSet<KpiSendTimesheetClientSites> KpiSendTimesheetClientSites { get; set; }

        public DbSet<LoginUserHistory> LoginUserHistory { get; set; }
        public DbSet<ANPR> ANPR { get; set; }

        public DbSet<SubDomain> SubDomain { get; set; }
        public DbSet<HrSettingsLockedClientSites> HrSettingsLockedClientSites { get; set; }
        public DbSet<LanguageMaster> LanguageMaster { get; set; }
        public DbSet<LanguageDetails> LanguageDetails { get; set; }
        public DbSet<GuardTrainingAndAssessment> GuardTrainingAndAssessment { get; set; }


        public DbSet<GuardHoursByQuarterViewModel> GuardHoursByQuarterViewModel { get; set; }

        public DbSet<GuardTwoHourNoActivityNotificationLog> GuardTwoHourNoActivityNotificationLog { get; set; }

        public DbSet<KPITelematicsField> KPITelematicsField { get; set; }
        public DbSet<HyperLinks> HyperLinks { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GuardLog>()
                .ToTable(tb => tb.HasTrigger("Insert_GuardLogs"));
            base.OnModelCreating(modelBuilder);
          
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Conventions.Add(_ => new BlankTriggerAddingConvention());
        }
        //SW Channels-start
        public DbSet<SWChannels> SWChannel { get; set; }
        //SW Channels-end
        //General Feeds-start
        public DbSet<GeneralFeeds> GeneralFeeds { get; set; }
        //General Feeds-end
        public DbSet<SmsChannel> SmsChannel { get; set; }
        public DbSet<GlobalDuressSms> GlobalDuressSms { get; set; }


        //public DbSet<SiteEventLog> SiteEventLog { get; set; }
        //for toggle areas - start 
        public DbSet<ClientSiteToggle> ClientSiteToggle { get; set; }
        //for toggle areas - end 
        public DbSet<RCLinkedDuressMaster> RCLinkedDuressMaster { get; set; }
        public DbSet<RCLinkedDuressClientSites> RCLinkedDuressClientSites { get; set; }
        public DbSet<HrSettingsClientSites> HrSettingsClientSites { get; set; }

        public DbSet<HrSettingsClientStates> HrSettingsClientStates { get; set; }
        public DbSet<FileDownloadAuditLogs> FileDownloadAuditLogs { get; set; }
        public DbSet<GuardLogsDocumentImages> GuardLogsDocumentImages { get; set; }
        public DbSet<TrainingTQNumbers> TrainingTQNumbers { get; set; }
        public DbSet<TrainingTestQuestionNumbers> TrainingTestQuestionNumbers { get; set; }
        public DbSet<TrainingCourses> TrainingCourses { get; set; }
        public DbSet<TrainingCourseDuration> TrainingCourseDuration { get; set; }
        public DbSet<TrainingTestDuration> TrainingTestDuration { get; set; }
        public DbSet<TrainingTestPassMark> TrainingTestPassMark { get; set; }
        public DbSet<TrainingTestAttempts> TrainingTestAttempts { get; set; }
        public DbSet<TrainingCertificateExpiryYears> TrainingCertificateExpiryYears { get; set; }

        public DbSet<TrainingTestQuestionSettings> TrainingTestQuestionSettings { get; set; }
        public DbSet<TrainingTestQuestions> TrainingTestQuestions { get; set; }
        public DbSet<TrainingTestQuestionsAnswers> TrainingTestQuestionsAnswers { get; set; }
        public DbSet<TrainingTestFeedbackQuestions> TrainingTestFeedbackQuestions { get; set; }
        public DbSet<TrainingTestFeedbackQuestionsAnswers> TrainingTestFeedbackQuestionsAnswers { get; set; }

        public DbSet<TrainingInstructor> TrainingInstructor { get; set; }

        public DbSet<TrainingCourseInstructor> TrainingCourseInstructor { get; set; }
        public DbSet<TrainingCourseCertificate> TrainingCourseCertificate { get; set; }

        public DbSet<TrainingCourseStatusColor> TrainingCourseStatusColor { get; set; }
        public DbSet<TrainingCourseStatus> TrainingCourseStatus { get; set; }
        public DbSet<TrainingLocation> TrainingLocation { get; set; }
        public DbSet<TrainingCourseCertificateRPL> TrainingCourseCertificateRPL { get; set; }

        public DbSet<GuardTrainingAttendedQuestionsAndAnswers> GuardTrainingAttendedQuestionsAndAnswers { get; set; }
        public DbSet<GuardTrainingAndAssessmentScore> GuardTrainingAndAssessmentScore { get; set; }


        public DbSet<UserInput> UserInput { get; set; }
        public DbSet<GuardTrainingStartTest> GuardTrainingStartTest { get; set; }
        public DbSet<GuardTrainingAttendedFeedbackQuestionsAndAnswers> GuardTrainingAttendedFeedbackQuestionsAndAnswers { get; set; }


    }
    /* 07022024 dileep to solve the trigger in table not allowed in enity framework 7.0
     issue save changes because the target table has database triggers
     */
    public class BlankTriggerAddingConvention : IModelFinalizingConvention
    {
        public virtual void ProcessModelFinalizing(
            IConventionModelBuilder modelBuilder,
            IConventionContext<IConventionModelBuilder> context)
        {
            foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
            {
                var table = StoreObjectIdentifier.Create(entityType, StoreObjectType.Table);
                if (table != null
                    && entityType.GetDeclaredTriggers().All(t => t.GetDatabaseName(table.Value) == null)
                    && (entityType.BaseType == null
                        || entityType.GetMappingStrategy() != RelationalAnnotationNames.TphMappingStrategy))
                {
                    entityType.Builder.HasTrigger(table.Value.Name + "_Trigger");
                }

                foreach (var fragment in entityType.GetMappingFragments(StoreObjectType.Table))
                {
                    if (entityType.GetDeclaredTriggers().All(t => t.GetDatabaseName(fragment.StoreObject) == null))
                    {
                        entityType.Builder.HasTrigger(fragment.StoreObject.Name + "_Trigger");
                    }
                }
            }
        }
    }
}
