using System.Linq;
using CityWatch.Tracking.Data;
using CityWatch.Tracking.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CityWatch.Tracking.Tests
{
    /// <summary>
    /// The schema's source of truth is DbScript/360; this context merely reads it. These tests
    /// pin the mapping to the script so a drift between the two is a test failure, not a
    /// production surprise. No database connection is needed — building the model is enough.
    /// </summary>
    [TestClass]
    public class TrackingDbContextTests
    {
        private static TrackingDbContext Build()
        {
            var options = new DbContextOptionsBuilder<TrackingDbContext>()
                .UseSqlServer("Server=unused;Database=unused;")   // provider only; never connects
                .Options;
            return new TrackingDbContext(options);
        }

        [TestMethod]
        public void Model_Builds_WithAllSixEntities()
        {
            using var context = Build();
            var entityNames = context.Model.GetEntityTypes().Select(e => e.ClrType.Name).OrderBy(n => n).ToList();

            CollectionAssert.AreEqual(new[]
            {
                nameof(TrackingAccessAudit),
                nameof(TrackingModeCommand),
                nameof(TrackingSession),
                nameof(TrackingUnitEnrolment),
                nameof(TrackPoint),
                nameof(TrackSegment)
            }, entityNames);
        }

        [TestMethod]
        public void TableNames_MatchDbScript360()
        {
            using var context = Build();

            foreach (var entity in context.Model.GetEntityTypes())
            {
                Assert.AreEqual(entity.ClrType.Name, entity.GetTableName(),
                    $"Entity {entity.ClrType.Name} must map to the singular table name used in DbScript/360.");
            }
        }

        [TestMethod]
        public void TrackPoint_HasNoForeignKeys_ByDesign()
        {
            using var context = Build();
            var trackPoint = context.Model.FindEntityType(typeof(TrackPoint))!;

            Assert.AreEqual(0, trackPoint.GetForeignKeys().Count(),
                "D7: no FKs on TrackPoint — required for insert rate and clean Level-4 rollback.");
        }

        [TestMethod]
        public void TrackPoint_DedupeIndex_IsUnique()
        {
            using var context = Build();
            var trackPoint = context.Model.FindEntityType(typeof(TrackPoint))!;
            var dedupe = trackPoint.GetIndexes().Single(i => i.GetDatabaseName() == "UX_TrackPoint_Dedupe");

            Assert.IsTrue(dedupe.IsUnique);
            CollectionAssert.AreEqual(new[] { "UnitId", "SessionId", "Seq" },
                dedupe.Properties.Select(p => p.Name).ToArray());
        }

        [TestMethod]
        public void Coordinates_AreDecimal9_6()
        {
            using var context = Build();
            var trackPoint = context.Model.FindEntityType(typeof(TrackPoint))!;

            Assert.AreEqual("decimal(9,6)", trackPoint.FindProperty(nameof(TrackPoint.Latitude))!.GetColumnType());
            Assert.AreEqual("decimal(9,6)", trackPoint.FindProperty(nameof(TrackPoint.Longitude))!.GetColumnType());
        }

        [TestMethod]
        public void Enrolment_UnitId_IsNeverDatabaseGenerated()
        {
            // UnitId IS ClientSiteSmartWand.Id — the store must never invent one.
            using var context = Build();
            var enrolment = context.Model.FindEntityType(typeof(TrackingUnitEnrolment))!;

            Assert.AreEqual(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never,
                enrolment.FindProperty(nameof(TrackingUnitEnrolment.UnitId))!.ValueGenerated);
        }
    }
}
