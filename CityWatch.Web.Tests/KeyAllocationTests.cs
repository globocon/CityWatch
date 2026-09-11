using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Common.Services;
using CityWatch.Web.Helpers;
using CityWatch.Web.Models;
using CityWatch.Web.Pages.Guard;
using CityWatch.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;

namespace CityWatch.Web.Tests
{
    /// <summary>
    /// Covers KeyVehicleLogModel.OnGetIsKeyAllocated, the check behind the "Key is already out"
    /// warning. KeyNo holds a "; " joined list of keys, and the handler used to test it with a raw
    /// Contains(), so key "1" matched an allocated key "12". Now that the warning is a hard stop
    /// with no override, that false positive would block a guard from issuing an available key.
    ///
    /// Pure unit tests - IViewDataService is mocked, so no database is involved.
    /// </summary>
    [TestClass]
    public class KeyAllocationTests
    {
        private const int LogBookId = 1234;

        /// <summary>
        /// Builds the page model with every dependency mocked; only IViewDataService is set up,
        /// since that is all this handler reads.
        /// </summary>
        private static KeyVehicleLogModel CreateModel(params string[] allocatedKeyNoValues)
        {
            var openLogs = new List<KeyVehicleLogViewModel>();
            foreach (var keyNo in allocatedKeyNoValues)
            {
                openLogs.Add(new KeyVehicleLogViewModel(
                    new KeyVehicleLog { KeyNo = keyNo },
                    new List<KeyVehcileLogField>()));
            }

            var viewDataService = new Mock<IViewDataService>();
            viewDataService
                .Setup(z => z.GetKeyVehicleLogs(LogBookId, KvlStatusFilter.Open))
                .Returns(openLogs);

            var webHostEnvironment = new Mock<IWebHostEnvironment>();
            webHostEnvironment.SetupGet(z => z.WebRootPath).Returns(@"C:\wwwroot");

            return new KeyVehicleLogModel(
                webHostEnvironment.Object,
                Mock.Of<IGuardLogDataProvider>(),
                Mock.Of<IClientDataProvider>(),
                Mock.Of<IGuardDataProvider>(),
                viewDataService.Object,
                Mock.Of<IKeyVehicleLogDocketGenerator>(),
                Options.Create(new EmailOptions()),
                Options.Create(new Settings()),
                Mock.Of<IDropboxService>(),
                Mock.Of<ILogger<KeyVehicleLogModel>>(),
                Mock.Of<IAppConfigurationProvider>(),
                Mock.Of<ISiteEventLogDataProvider>(),
                Mock.Of<ISmsSenderProvider>(),
                Mock.Of<IConfigDataProvider>());
        }

        private static bool IsAllocated(KeyVehicleLogModel model, string keyNo)
        {
            return (bool)((JsonResult)model.OnGetIsKeyAllocated(LogBookId, keyNo)).Value;
        }

        [TestMethod]
        public void KeyOnAnOpenEntry_IsReportedAllocated()
        {
            var model = CreateModel("12");

            Assert.IsTrue(IsAllocated(model, "12"));
        }

        [TestMethod]
        public void KeyNotOnAnyOpenEntry_IsReportedAvailable()
        {
            var model = CreateModel("12");

            Assert.IsFalse(IsAllocated(model, "99"));
        }

        /// <summary>The regression: "1" must not match an allocated "12".</summary>
        [TestMethod]
        public void KeyThatIsOnlyASubstringOfAnAllocatedKey_IsReportedAvailable()
        {
            var model = CreateModel("12");

            Assert.IsFalse(IsAllocated(model, "1"), "Key 1 is available; only key 12 is signed out.");
            Assert.IsFalse(IsAllocated(model, "2"), "Key 2 is available; only key 12 is signed out.");
        }

        [TestMethod]
        public void KeyInAJoinedList_IsMatchedOnItsOwn()
        {
            // One entry holding three keys, the format the UI writes.
            var model = CreateModel("K1; K2; K10");

            Assert.IsTrue(IsAllocated(model, "K1"));
            Assert.IsTrue(IsAllocated(model, "K2"));
            Assert.IsTrue(IsAllocated(model, "K10"));
            Assert.IsFalse(IsAllocated(model, "K3"));
        }

        [TestMethod]
        public void AllocatedKeyIsFound_AcrossMultipleOpenEntries()
        {
            var model = CreateModel("A1", "B2; B3");

            Assert.IsTrue(IsAllocated(model, "B3"));
            Assert.IsFalse(IsAllocated(model, "B4"));
        }

        [TestMethod]
        public void MatchIgnoresSurroundingWhitespaceAndCase()
        {
            var model = CreateModel("  a1  ;  a2 ");

            Assert.IsTrue(IsAllocated(model, "A1"));
            Assert.IsTrue(IsAllocated(model, " a2 "));
        }

        [TestMethod]
        public void NoOpenEntries_MeansNothingIsAllocated()
        {
            var model = CreateModel();

            Assert.IsFalse(IsAllocated(model, "12"));
        }

        [TestMethod]
        public void EmptyOrMissingKeyNo_IsNotTreatedAsAllocated()
        {
            // An empty request must not match, and an entry with no keys must not be scanned.
            var model = CreateModel("", "12");

            Assert.IsFalse(IsAllocated(model, ""));
            Assert.IsFalse(IsAllocated(model, "   "));
            Assert.IsFalse(IsAllocated(model, null));
        }
    }
}
