using CityWatch.Data.Models;
using CityWatch.Web.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace CityWatch.Web.Tests
{
    [TestClass]
    public class TagHistoryReportGeneratorTests
    {
        private static TagHistoryReportRequest Request(string sortBy = null) => new TagHistoryReportRequest
        {
            LogFromDate = new DateTime(2026, 10, 14),
            LogToDate = new DateTime(2026, 10, 20),
            SortBy = sortBy
        };

        [TestMethod]
        public void GeneratePdf_WithRows_ReturnsPdf()
        {
            var rows = new List<TagHistoryReportViewModel>
            {
                new() { Id = 1, SiteName = "VISY-VIC-Coolaroo", Action = "Changed", Note = "label changed from 'A' to 'B'", TagUId = "53F6072F620001",
                        TagType = "NFC", UserName = "Visy vic", AppType = "Android", GuardName = "Sam Smith", GuardInitial = "S.S", GuardSecurityNo = "556-998-3333",
                        ChangedOn = new DateTime(2026, 10, 15, 12, 0, 0) },
                new() { Id = 2, SiteName = null, Action = "Deleted", ChangedBy = null, ChangedOn = new DateTime(2026, 10, 16, 8, 30, 0) },
            };

            var pdf = new TagHistoryReportGenerator().GeneratePdf(rows, Request());

            Assert.IsTrue(pdf.Length > 0);
            Assert.AreEqual("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
        }

        [TestMethod]
        public void GeneratePdf_NoRows_StillReturnsPdf()
        {
            var pdf = new TagHistoryReportGenerator().GeneratePdf(new List<TagHistoryReportViewModel>(), Request(TagHistorySortBy.Site));

            Assert.AreEqual("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
        }
    }
}
