using CityWatch.Tracking.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CityWatch.Tracking.Data
{
    /// <summary>
    /// The feature pack's own context — its DbSets run against the same physical database as
    /// CityWatchDbContext, sharing nothing with it (D1, §3.3). The 214-DbSet platform context
    /// is a shared surface across three applications; this one is paid for only where the
    /// pack is enabled, and an accidental .Include() from platform code into tracking tables
    /// is structurally impossible.
    ///
    /// Schema is deployed by numbered DbScript files (360–368), never by migrations — the
    /// platform has no EF migrations and this pack follows the house convention (§1.2, §8.5).
    /// </summary>
    public class TrackingDbContext : DbContext
    {
        public TrackingDbContext(DbContextOptions<TrackingDbContext> options) : base(options)
        {
        }

        public DbSet<TrackPoint> TrackPoints { get; set; } = null!;

        /// <summary>Read-only platform projections (§13.3 boundary). Never written.</summary>
        public DbSet<PlatformSmartWand> PlatformSmartWands { get; set; } = null!;
        public DbSet<PlatformGuard> PlatformGuards { get; set; } = null!;
        public DbSet<PlatformGuardAppVersion> PlatformGuardAppVersions { get; set; } = null!;
        public DbSet<PlatformClientSite> PlatformClientSites { get; set; } = null!;
        public DbSet<PlatformWandScan> PlatformWandScans { get; set; } = null!;
        public DbSet<PlatformWandTag> PlatformWandTags { get; set; } = null!;
        public DbSet<PlatformSiteKpi> PlatformSiteKpis { get; set; } = null!;
        public DbSet<PlatformDailyWandFq> PlatformDailyWandFqs { get; set; } = null!;
        public DbSet<PlatformWandRound> PlatformWandRounds { get; set; } = null!;
        public DbSet<PlatformPosition> PlatformPositions { get; set; } = null!;
        public DbSet<PlatformClientSiteDuress> PlatformClientSiteDuress { get; set; } = null!;
        public DbSet<TrackSegment> TrackSegments { get; set; } = null!;
        public DbSet<TrackingSession> TrackingSessions { get; set; } = null!;
        public DbSet<TrackingUnitEnrolment> TrackingUnitEnrolments { get; set; } = null!;
        public DbSet<TrackingModeCommand> TrackingModeCommands { get; set; } = null!;
        public DbSet<TrackingAccessAudit> TrackingAccessAudits { get; set; } = null!;
        public DbSet<GeocodeCache> GeocodeCacheEntries { get; set; } = null!;
        public DbSet<TrackingDeviceToken> TrackingDeviceTokens { get; set; } = null!;
        public DbSet<TrackingSiteVisit> TrackingSiteVisits { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            /* Mirrors DbScript/402 exactly. The scripts are the source of truth for the
               schema; this mapping is how the pack reads it. */

            modelBuilder.Entity<TrackPoint>(e =>
            {
                // Matches the script: PK nonclustered, clustered index on (UnitId, RecordedUtc).
                e.HasIndex(p => new { p.UnitId, p.RecordedUtc }).HasDatabaseName("CX_TrackPoint_Unit_Time");
                e.HasIndex(p => new { p.UnitId, p.SessionId, p.Seq }).IsUnique().HasDatabaseName("UX_TrackPoint_Dedupe");
            });

            modelBuilder.Entity<TrackSegment>(e =>
            {
                e.HasIndex(s => new { s.UnitId, s.StartUtc }).HasDatabaseName("IX_TrackSegment_Unit_Start");
                e.HasIndex(s => s.SessionId).HasDatabaseName("IX_TrackSegment_Session");
            });

            modelBuilder.Entity<TrackingSession>(e =>
            {
                e.Property(s => s.Id).ValueGeneratedNever();   // Guid supplied by the opener
                e.HasIndex(s => new { s.UnitId, s.Status }).HasDatabaseName("IX_TrackingSession_Unit_Status");
                e.HasIndex(s => s.GuardId).HasDatabaseName("IX_TrackingSession_Guard");
            });

            modelBuilder.Entity<TrackingModeCommand>(e =>
            {
                e.HasIndex(c => new { c.UnitId, c.CommandSeq }).IsUnique().HasDatabaseName("UX_TrackingModeCommand_Unit_Seq");
                e.HasIndex(c => new { c.UnitId, c.Status }).HasDatabaseName("IX_TrackingModeCommand_Unit_Status");
            });

            modelBuilder.Entity<TrackingAccessAudit>(e =>
            {
                e.HasIndex(a => new { a.UserId, a.AccessedUtc }).HasDatabaseName("IX_TrackingAccessAudit_User_Time");
                e.HasIndex(a => new { a.UnitId, a.AccessedUtc }).HasDatabaseName("IX_TrackingAccessAudit_Unit_Time");
            });

            modelBuilder.Entity<GeocodeCache>(e =>
            {
                e.ToTable("GeocodeCache");
                // Matches DbScript/368. Unique: one answer per cell; concurrent misses race
                // the index and the loser's write is discarded (see GeocodeService).
                e.HasIndex(c => new { c.CellLat, c.CellLon }).IsUnique().HasDatabaseName("UX_GeocodeCache_Cell");
            });

            modelBuilder.Entity<TrackingDeviceToken>(e =>
            {
                e.ToTable("TrackingDeviceToken");
                // Matches DbScript/369: one row per physical token, re-homed across units.
                e.HasIndex(t => t.FcmToken).IsUnique().HasDatabaseName("UX_TrackingDeviceToken_Token");
                e.HasIndex(t => new { t.UnitId, t.IsActive }).HasDatabaseName("IX_TrackingDeviceToken_Unit_Active");
                e.Property(t => t.FcmToken).HasMaxLength(512);
                e.Property(t => t.Platform).HasMaxLength(20);
            });

            modelBuilder.Entity<TrackingSiteVisit>(e =>
            {
                e.ToTable("TrackingSiteVisit");
                // Matches DbScript/370. ConfirmedUtc leads the feed index: the bell reads
                // "confirmed since X" far more often than anything else touches this table.
                e.HasIndex(v => v.ConfirmedUtc).HasDatabaseName("IX_TrackingSiteVisit_Confirmed");
                e.HasIndex(v => new { v.SessionId, v.ExitedUtc }).HasDatabaseName("IX_TrackingSiteVisit_Session_Open");
                e.HasIndex(v => new { v.UnitId, v.EnteredUtc }).HasDatabaseName("IX_TrackingSiteVisit_Unit_Entered");
                e.Property(v => v.SiteName).HasMaxLength(200);
                e.Property(v => v.Source).HasMaxLength(10);
                e.Property(v => v.EnteredLat).HasPrecision(9, 6);
                e.Property(v => v.EnteredLon).HasPrecision(9, 6);
            });
        }
    }
}

