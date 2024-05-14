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
        public DbSet<GuardCompliance> GuardCompliances { get; set; }
        public DbSet<GuardComplianceAndLicense> GuardComplianceLicense { get; set; }
        public DbSet<ClientSiteActivityStatus> ClientSiteActivityStatus { get; set;}
        public DbSet<ClientSiteRadioCheck> ClientSiteRadioChecks { get; set; }
        public DbSet<CompanyDetails> CompanyDetails { get; set; }
        public DbSet<ClientSiteManningKpiSetting> ClientSiteManningKpiSettings { get; set; }         
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
