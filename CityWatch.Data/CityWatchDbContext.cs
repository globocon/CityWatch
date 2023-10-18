using CityWatch.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace CityWatch.Data
{
    public class CityWatchDbContext : DbContext
    {
        public CityWatchDbContext(DbContextOptions<CityWatchDbContext> options) : base(options)
        {

        }

        public DbSet<User> Users { get; set; }
        public DbSet<ClientType> ClientTypes { get; set; }
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


    }
}
