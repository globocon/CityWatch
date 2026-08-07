using CityWatch.Tracking.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CityWatch.Tracking.Tests
{
    /// <summary>
    /// Two key spaces share the UnitId column. This test exists because patrol officers log
    /// in WITHOUT a SmartWand — every observed PCAR login has SmartWandId NULL — so the car
    /// (its login Position) must be able to be the unit, and position ids (10-24) would
    /// otherwise collide head-on with wand ids (which run past 140).
    ///
    /// The offset here MUST match the mobile app's PositionUnitOffset constant.
    /// </summary>
    [TestClass]
    public class TrackingUnitKeyTests
    {
        [TestMethod]
        public void PositionAndWandKeySpaces_CannotCollide()
        {
            // Real values: positions 10-24, wands seen past 140.
            for (var position = 1; position <= 100; position++)
                Assert.IsTrue(TrackingUnitKey.FromPosition(position) > 1_000_000,
                    "Every car key must sit far above any plausible wand id.");

            for (var wand = 1; wand <= 100_000; wand++)
                if (wand % 9_999 == 0)
                    Assert.IsFalse(TrackingUnitKey.IsPosition(wand),
                        $"Wand {wand} must never be mistaken for a car.");
        }

        [TestMethod]
        public void CarKey_IsReadable_AndRoundTrips()
        {
            // "Mobile Patrols (Car) M1" is position 10 -> 2,000,010.
            var m1 = TrackingUnitKey.FromPosition(10);

            Assert.AreEqual(2_000_010, m1);
            Assert.IsTrue(TrackingUnitKey.IsPosition(m1));
            Assert.AreEqual(10, TrackingUnitKey.ToPositionId(m1));
        }

        [TestMethod]
        public void WandKey_IsNotAPosition_AndHasNoPositionId()
        {
            Assert.IsFalse(TrackingUnitKey.IsPosition(140));
            Assert.IsNull(TrackingUnitKey.ToPositionId(140));
        }

        [TestMethod]
        public void EveryRealPatrolCarPosition_MapsToItsDocumentedUnitId()
        {
            // The 12 patrol car positions in the live data.
            var expected = new (int Position, int Unit)[]
            {
                (10, 2000010), (14, 2000014), (15, 2000015), (16, 2000016),
                (17, 2000017), (18, 2000018), (19, 2000019), (20, 2000020),
                (21, 2000021), (22, 2000022), (23, 2000023), (24, 2000024)
            };

            foreach (var (position, unit) in expected)
                Assert.AreEqual(unit, TrackingUnitKey.FromPosition(position));
        }
    }
}
