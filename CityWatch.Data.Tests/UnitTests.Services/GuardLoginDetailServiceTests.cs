using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Data.Tests.UnitTests.Services
{
    [TestClass]
    public class GuardLoginDetailServiceTests
    {
        public Mock<IGuardDataProvider> _guardDataProvider;
        public GuardLoginDetailService _guardLoginDetailService;

        [TestInitialize]
        public void Setup()
        {
            _guardDataProvider = new Mock<IGuardDataProvider>();
            _guardLoginDetailService = new GuardLoginDetailService(_guardDataProvider.Object);
        }

        [TestMethod]
        public void GetGuardDetailsByLogBookId_ForReportBeforeMidnight_ReturnOffDutyTrim()
        {
            var guardLogins = GetGuardLoginsWorkingInSequence_Set1().ToList();
            _guardDataProvider.Setup(x => x.GetGuardLoginsByLogBookId(It.IsAny<int>())).Returns(guardLogins);

            var guardDetails = _guardLoginDetailService.GetGuardDetailsByLogBookId(1);

            Assert.AreEqual(1, guardDetails.Count());
            Assert.AreEqual(2, guardDetails.First().Count());

            var guard2 = guardDetails.Single().Single(z => z.GuardName == "PQR");
            Assert.AreEqual(DateTime.Parse("2023-01-26 17:00:00"), guard2.OnDuty);
            Assert.AreEqual(DateTime.Parse("2023-01-26 23:59:00"), guard2.OffDuty);
        }

        [TestMethod]
        public void GetGuardDetailsByLogBookId_ForReportAfterMidnight_ReturnBothOnDutyOffDutyTrim()
        {
            var guardLogins = GetGuardLoginsWorkingInSequence_Set2().ToList();
            _guardDataProvider.Setup(x => x.GetGuardLoginsByLogBookId(It.IsAny<int>())).Returns(guardLogins);

            var guardDetails = _guardLoginDetailService.GetGuardDetailsByLogBookId(2);

            Assert.AreEqual(1, guardDetails.Count());
            Assert.AreEqual(4, guardDetails.First().Count());

            var guardMorningShift = guardDetails.Single().Where(z => z.GuardName == "PQR").First();
            Assert.AreEqual(DateTime.Parse("2023-01-27 00:01:00"), guardMorningShift.OnDuty);
            Assert.AreEqual(DateTime.Parse("2023-01-27 01:00:00"), guardMorningShift.OffDuty);

            var guardEveningShift = guardDetails.Single().Where(z => z.GuardName == "PQR").Last();
            Assert.AreEqual(DateTime.Parse("2023-01-27 17:00:00"), guardMorningShift.OnDuty);
            Assert.AreEqual(DateTime.Parse("2023-01-27 23:59:00"), guardMorningShift.OffDuty);
        }

        [TestMethod]
        public void GetGuardDetailsByLogBookId_ForReportBeforeMidnight_ParallelDuty_ReturnOffDutyTrim()
        {
            var guardLogins = GetGuardLoginsWorkingParallel_Set1().ToList();
            _guardDataProvider.Setup(x => x.GetGuardLoginsByLogBookId(It.IsAny<int>())).Returns(guardLogins);

            var guardDetails = _guardLoginDetailService.GetGuardDetailsByLogBookId(1);

            Assert.AreEqual(2, guardDetails.Count());
            Assert.AreEqual(1, guardDetails.First().Count());

            var guard1 = guardDetails.Single(z => z.Key == "Wand 1").Single(z => z.GuardName == "ABC");
            Assert.AreEqual(DateTime.Parse("2023-01-26 17:00:00"), guard1.OnDuty);
            Assert.AreEqual(DateTime.Parse("2023-01-26 23:59:00"), guard1.OffDuty);
        }

        private static IEnumerable<GuardLogin> GetGuardLoginsWorkingInSequence_Set1()
        {
            var guardLogins = new List<GuardLogin>()
            {
                new GuardLogin()
                {
                    Guard = new Guard() { Name = "ABC" },
                    SmartWand = new ClientSiteSmartWand() { SmartWandId = "Wand 1" },
                    OnDuty = DateTime.Parse("2023-01-26 09:00:00"),
                    OffDuty = DateTime.Parse("2023-01-26 17:00:00"),
                    ClientSiteLogBookId = 1,
                    ClientSiteLogBook = new ClientSiteLogBook(){ Id = 1, Date = DateTime.Parse("2023-01-26 12:00:00"), }
                },
                new GuardLogin()
                {
                    Guard = new Guard() { Name = "PQR" },
                    SmartWand = new ClientSiteSmartWand() { SmartWandId = "Wand 1" },
                    OnDuty = DateTime.Parse("2023-01-26 17:00:00"),
                    OffDuty = DateTime.Parse("2023-01-27 01:00:00"),
                    ClientSiteLogBookId = 1,
                    ClientSiteLogBook = new ClientSiteLogBook(){ Id = 1, Date = DateTime.Parse("2023-01-26 12:00:00") }
                }
            };

            return guardLogins;
        }

        private IEnumerable<GuardLogin> GetGuardLoginsWorkingInSequence_Set2()
        {
            var guardLogins = new List<GuardLogin>()
            {
                new GuardLogin()
                {
                    Guard = new Guard() { Name = "PQR" },
                    SmartWand = new ClientSiteSmartWand() { SmartWandId = "Wand 1" },
                    OnDuty = DateTime.Parse("2023-01-27 00:01:00"),
                    OffDuty = DateTime.Parse("2023-01-27 01:00:00"),
                    ClientSiteLogBookId = 2,
                    ClientSiteLogBook = new ClientSiteLogBook(){ Id = 2, Date = DateTime.Parse("2023-01-27 12:00:00") }
                },
                new GuardLogin()
                {
                    Guard = new Guard() { Name = "XYZ" },
                    SmartWand = new ClientSiteSmartWand() { SmartWandId = "Wand 1" },
                    OnDuty = DateTime.Parse("2023-01-27 01:00:00"),
                    OffDuty = DateTime.Parse("2023-01-27 09:00:00"),
                    ClientSiteLogBookId = 2,
                    ClientSiteLogBook = new ClientSiteLogBook(){ Id = 2, Date = DateTime.Parse("2023-01-27 12:00:00") }
                },
                new GuardLogin()
                {
                    Guard = new Guard() { Name = "ABC" },
                    SmartWand = new ClientSiteSmartWand() { SmartWandId = "Wand 1" },
                    OnDuty = DateTime.Parse("2023-01-27 09:00:00"),
                    OffDuty = DateTime.Parse("2023-01-27 17:00:00"),
                    ClientSiteLogBookId = 2,
                    ClientSiteLogBook = new ClientSiteLogBook(){ Id = 2, Date = DateTime.Parse("2023-01-27 12:00:00"), }
                },
                new GuardLogin()
                {
                    Guard = new Guard() { Name = "PQR" },
                    SmartWand = new ClientSiteSmartWand() { SmartWandId = "Wand 1" },
                    OnDuty = DateTime.Parse("2023-01-27 17:00:00"),
                    OffDuty = DateTime.Parse("2023-01-28 01:00:00"),
                    ClientSiteLogBookId = 2,
                    ClientSiteLogBook = new ClientSiteLogBook(){ Id = 2, Date = DateTime.Parse("2023-01-27 12:00:00") }
                }
            };

            return guardLogins;
        }

        private static IEnumerable<GuardLogin> GetGuardLoginsWorkingParallel_Set1()
        {
            var guardLogins = new List<GuardLogin>()
            {
                new GuardLogin()
                {
                    Guard = new Guard() { Name = "ABC" },
                    SmartWand = new ClientSiteSmartWand() { SmartWandId = "Wand 1" },
                     OnDuty = DateTime.Parse("2023-01-26 17:00:00"),
                    OffDuty = DateTime.Parse("2023-01-27 01:00:00"),
                    ClientSiteLogBookId = 1,
                    ClientSiteLogBook = new ClientSiteLogBook(){ Id = 1, Date = DateTime.Parse("2023-01-26 12:00:00"), }
                },
                new GuardLogin()
                {
                    Guard = new Guard() { Name = "PQR" },
                    SmartWand = new ClientSiteSmartWand() { SmartWandId = "Wand 2" },
                    OnDuty = DateTime.Parse("2023-01-26 17:00:00"),
                    OffDuty = DateTime.Parse("2023-01-27 01:00:00"),
                    ClientSiteLogBookId = 1,
                    ClientSiteLogBook = new ClientSiteLogBook(){ Id = 1, Date = DateTime.Parse("2023-01-26 12:00:00") }
                }
            };

            return guardLogins;
        }
    }
}
