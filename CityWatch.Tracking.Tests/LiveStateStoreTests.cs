using System;
using System.Linq;
using CityWatch.Tracking.Contracts;
using CityWatch.Tracking.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CityWatch.Tracking.Tests
{
    [TestClass]
    public class LiveStateStoreTests
    {
        private static UnitLiveState State(int unitId, DateTime recordedUtc, decimal lat = -33.865143m)
            => new()
            {
                UnitId = unitId,
                SessionId = Guid.NewGuid(),
                Lat = lat,
                Lon = 151.209900m,
                Mode = TrackingMode.Transit,
                Source = TrackPointSource.Transit,
                RecordedUtc = recordedUtc,
                ReceivedUtc = DateTime.UtcNow
            };

        [TestMethod]
        public void Update_ThenGet_ReturnsLatest()
        {
            var store = new InMemoryLiveStateStore();
            var now = DateTime.UtcNow;

            store.Update(State(42, now));
            var result = store.Get(42);

            Assert.IsNotNull(result);
            Assert.AreEqual(42, result!.UnitId);
            Assert.AreEqual(now, result.RecordedUtc);
        }

        [TestMethod]
        public void Update_OlderFix_DoesNotRegressTheLivePicture()
        {
            // The backfill scenario: a device replays an hour of offline history while also
            // sending live points. The marker must never move backwards.
            var store = new InMemoryLiveStateStore();
            var now = DateTime.UtcNow;

            store.Update(State(42, now, lat: -33.9m));
            store.Update(State(42, now.AddMinutes(-30), lat: -34.5m)); // stale backfill

            Assert.AreEqual(-33.9m, store.Get(42)!.Lat, "A stale point must not overwrite a fresher one.");
        }

        [TestMethod]
        public void ChangedSince_ReturnsOnlyUnitsChangedAfterTheCursor()
        {
            var store = new InMemoryLiveStateStore();
            var now = DateTime.UtcNow;

            store.Update(State(1, now));
            store.Update(State(2, now));
            var (_, cursor) = store.ChangedSince(0);

            store.Update(State(2, now.AddSeconds(1)));
            var (changed, nextCursor) = store.ChangedSince(cursor);

            Assert.AreEqual(1, changed.Count, "Only the unit that moved is in the diff.");
            Assert.AreEqual(2, changed[0].UnitId);
            Assert.IsTrue(nextCursor > cursor);
        }

        [TestMethod]
        public void ChangedSince_EmptyDiff_WhenNothingMoved()
        {
            var store = new InMemoryLiveStateStore();
            store.Update(State(1, DateTime.UtcNow));
            var (_, cursor) = store.ChangedSince(0);

            var (changed, _) = store.ChangedSince(cursor);

            Assert.AreEqual(0, changed.Count,
                "An idle fleet must produce empty frames — this is what makes 1 Hz broadcast cheap.");
        }

        [TestMethod]
        public void Remove_TakesUnitOffTheMap_AndAdvancesTheCursor()
        {
            var store = new InMemoryLiveStateStore();
            store.Update(State(1, DateTime.UtcNow));
            var (_, cursor) = store.ChangedSince(0);

            store.Remove(1);

            Assert.IsNull(store.Get(1));
            Assert.AreEqual(0, store.Snapshot().Count);
            var (_, nextCursor) = store.ChangedSince(cursor);
            Assert.IsTrue(nextCursor > cursor, "Removal must advance the version so pollers notice.");
        }

        [TestMethod]
        public void Snapshot_IsAPointInTimeCopy()
        {
            var store = new InMemoryLiveStateStore();
            store.Update(State(1, DateTime.UtcNow));
            var snapshot = store.Snapshot();

            store.Update(State(2, DateTime.UtcNow));

            Assert.AreEqual(1, snapshot.Count, "A snapshot taken earlier must not grow later.");
        }

        [TestMethod]
        public void ConcurrentUpdates_LoseNothing_AndKeepMonotonicVersions()
        {
            var store = new InMemoryLiveStateStore();
            var now = DateTime.UtcNow;

            System.Threading.Tasks.Parallel.For(0, 1000, i =>
                store.Update(State(i % 50, now.AddMilliseconds(i))));

            var snapshot = store.Snapshot();
            Assert.AreEqual(50, snapshot.Count);
            var versions = snapshot.Select(s => s.Version).ToList();
            Assert.AreEqual(versions.Count, versions.Distinct().Count(),
                "Every stored state carries a unique version stamp.");
        }
    }
}
