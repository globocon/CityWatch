using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CityWatch.Data.Tests.UnitTests.Model
{
    [TestClass]
    public class RosterPayHelperTests
    {
        // $40 guard / $42 sell — the PrixCar rate on the live shifts that showed the bug.
        private static PayRate Rate() => new PayRate { GuardPayRate = 40m, SellRateToClient = 42m };

        [TestMethod]
        public void NormalGuardShift_KeepsItsRate()
        {
            var shift = new RosterSchedule { GuardId = 1518, ProviderName = "Oxford Group", PayRate = Rate() };

            Assert.AreEqual(40m, RosterPayHelper.GetGuardPayRate(shift));
            Assert.AreEqual(42m, RosterPayHelper.GetSellRate(shift));
        }

        [TestMethod]
        public void CompanyOnlyRelief_CostsNothing()
        {
            // Shift 11401: Tim Sell relieved by GroupOne with no relief guard -> was $280, must be $0.
            var shift = new RosterSchedule { GuardId = 1518, PayRate = Rate(), ReliefProviderName = "GroupOne", ReliefReason = "Other" };

            Assert.IsTrue(RosterPayHelper.IsCompanyOnlyRelief(shift));
            Assert.AreEqual(0m, RosterPayHelper.GetGuardPayRate(shift));
            Assert.AreEqual(0m, RosterPayHelper.GetSellRate(shift));
            Assert.AreEqual(0m, RosterPayHelper.GetRate(shift, "guard"));
            Assert.AreEqual(0m, RosterPayHelper.GetRate(shift, "sell"));
        }

        [TestMethod]
        public void NamedReliefGuard_KeepsTheRate()
        {
            // Picking a relief guard auto-fills ReliefProviderName with the guard's company.
            var shift = new RosterSchedule { GuardId = 1518, PayRate = Rate(), ReliefGuardId = 1553, ReliefProviderName = "Oxford Group" };

            Assert.IsFalse(RosterPayHelper.IsCompanyOnlyRelief(shift));
            Assert.AreEqual(40m, RosterPayHelper.GetGuardPayRate(shift));
            Assert.AreEqual(42m, RosterPayHelper.GetSellRate(shift));
        }

        [TestMethod]
        public void PricedCompanyShiftWithoutRelief_KeepsItsRate()
        {
            // Live examples: "Fox" / "CWS (In-House)" provider shifts are deliberately priced.
            var shift = new RosterSchedule { ProviderName = "Fox", PayRate = Rate() };

            Assert.AreEqual(40m, RosterPayHelper.GetGuardPayRate(shift));
            Assert.AreEqual(42m, RosterPayHelper.GetSellRate(shift));
        }

        [TestMethod]
        public void BlankReliefProviderName_IsNotARelief()
        {
            var shift = new RosterSchedule { GuardId = 1, PayRate = Rate(), ReliefProviderName = "  " };

            Assert.IsFalse(RosterPayHelper.IsCompanyOnlyRelief(shift));
            Assert.AreEqual(40m, RosterPayHelper.GetGuardPayRate(shift));
        }

        [TestMethod]
        public void NoPayRateOrNullShift_IsZero()
        {
            Assert.AreEqual(0m, RosterPayHelper.GetGuardPayRate(new RosterSchedule { ProviderName = "GroupOne" }));
            Assert.AreEqual(0m, RosterPayHelper.GetSellRate(new RosterSchedule()));
            Assert.AreEqual(0m, RosterPayHelper.GetRate(null, "sell"));
        }

        [TestMethod]
        public void RateType_PicksGuardUnlessSell()
        {
            var shift = new RosterSchedule { GuardId = 1, PayRate = Rate() };

            Assert.AreEqual(42m, RosterPayHelper.GetRate(shift, "sell"));
            Assert.AreEqual(40m, RosterPayHelper.GetRate(shift, "guard"));
            Assert.AreEqual(40m, RosterPayHelper.GetRate(shift, null)); // same default as the old inline code
        }
    }
}
