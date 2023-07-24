using CityWatch.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CityWatch.Common.Tests
{
    [TestClass]
    public class FileNameHelper_UnitTests
    {
        [TestMethod]
        public void GetSanitizedFileNamePart_WithValidFileName_ReturnsSameFileName()
        {
            var fileName = @"Hello World";

            var result = FileNameHelper.GetSanitizedFileNamePart(fileName);

            Assert.AreEqual(fileName, result);
        }

        [TestMethod]
        public void GetSanitizedFileNamePart_WithValidInFileName_ReturnsSanitizedFileName()
        {
            const string fileName = @"W*o'r\l.d";
            const string expectedFileName = @"W_o'r_l.d";

            var result = FileNameHelper.GetSanitizedFileNamePart(fileName);

            Assert.AreEqual(expectedFileName, result);
        }

        [TestMethod]
        public void GetSanitizedFileNamePart_WithNoFileName_ReturnsNull()
        {
            var result = FileNameHelper.GetSanitizedFileNamePart(null);

            Assert.IsNull(result);
        }
    }
}
