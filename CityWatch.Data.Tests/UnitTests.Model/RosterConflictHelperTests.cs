using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace CityWatch.Data.Tests.UnitTests.Model
{
    [TestClass]
    public class RosterConflictHelperTests
    {
        private const int Abhi = 717;
        private const int Mohit = 675;

        private static bool IsWorkedBy(RosterSchedule shift, int guardId) =>
            RosterConflictHelper.IsWorkedBy(guardId).Compile()(shift);

        [TestMethod]
        public void RosteredGuard_NoRelief_IsWorking()
        {
            var shift = new RosterSchedule { GuardId = Abhi };

            Assert.IsTrue(IsWorkedBy(shift, Abhi));
        }

        [TestMethod]
        public void Issue83_CompanyOnlyRelief_FreesTheRosteredGuard()
        {
            // ABHI's Cropley 06:00-14:00 released to "Oxford" with no guard named.
            var shift = new RosterSchedule { GuardId = Abhi, ReliefProviderName = "Oxford Group", ReliefReason = "Other" };

            Assert.IsFalse(IsWorkedBy(shift, Abhi));
        }

        [TestMethod]
        public void NamedReliefGuard_FreesRosteredGuard_AndBooksTheReliefGuard()
        {
            // Unchanged behaviour: picking a relief guard also fills ReliefProviderName with their company.
            var shift = new RosterSchedule { GuardId = Abhi, ReliefGuardId = Mohit, ReliefProviderName = "Oxford Group" };

            Assert.IsFalse(IsWorkedBy(shift, Abhi));
            Assert.IsTrue(IsWorkedBy(shift, Mohit));
        }

        [TestMethod]
        public void BlankReliefProviderName_IsNotARelief()
        {
            Assert.IsTrue(IsWorkedBy(new RosterSchedule { GuardId = Abhi, ReliefProviderName = "" }, Abhi));
            Assert.IsTrue(IsWorkedBy(new RosterSchedule { GuardId = Abhi, ReliefProviderName = "  " }, Abhi));
        }

        [TestMethod]
        public void OtherGuardsAndCompanyShifts_AreNotWorkedByThisGuard()
        {
            Assert.IsFalse(IsWorkedBy(new RosterSchedule { GuardId = Mohit }, Abhi));
            Assert.IsFalse(IsWorkedBy(new RosterSchedule { ProviderName = "GroupOne" }, Abhi));
        }

        [TestMethod]
        public void Rule_TranslatesToSqlServer()
        {
            // The rule runs inside EF queries; prove it converts to SQL (no database needed).
            var options = new DbContextOptionsBuilder<CityWatchDbContext>()
                .UseSqlServer("Server=localhost;Database=translation-only;Trusted_Connection=True;TrustServerCertificate=True")
                .Options;
            using var context = new CityWatchDbContext(options, null);

            var sql = context.RosterSchedules.Where(RosterConflictHelper.IsWorkedBy(Abhi)).ToQueryString();

            StringAssert.Contains(sql, "[ReliefGuardId]");
            StringAssert.Contains(sql, "[ReliefProviderName]");
        }
    }
}
