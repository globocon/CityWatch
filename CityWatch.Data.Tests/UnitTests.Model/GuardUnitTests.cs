using CityWatch.Data.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CityWatch.Data.Tests.UnitTests.Model
{
    [TestClass]
    public class GuardUnitTests
    {
        [DataTestMethod]        
        [DataRow("975324")]
        [DataRow("12346789")]
        [DataRow("4561382")]
        [DataRow("000222886")]
        [DataRow("922-343-70S")]
        [DataRow("569-829-21S")]
        public void Guard_WithValidSecurityNo_ReturnsIsValid(string securityNo)
        {
            var guard = GetGuard();
            guard.SecurityNo = securityNo;

            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(guard, new ValidationContext(guard), results, true);

            Assert.IsTrue(isValid);
        }

        [DataTestMethod]
        [DataRow("0")]
        [DataRow("9")]        
        [DataRow("000")]
        [DataRow("890")]
        [DataRow("09876543210")]
        [DataRow("0987654321")]
        [DataRow("321")]
        [DataRow("654")]
        [DataRow("999")]
        [DataRow("0000")]
        [DataRow("1234")]
        [DataRow("0987")]        
        [DataRow("9999")]
        [DataRow("1234567890")]
        [DataRow("0987654321")]
        [DataRow("0000000000")]
        [DataRow("9999999999")]
        public void Guard_WithPattenSecurityNo_ReturnsValidationError(string securityNo)
        {
            var guard = GetGuard();
            guard.SecurityNo = securityNo;

            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(guard, new ValidationContext(guard), results, true);

            Assert.IsFalse(isValid);
            Assert.IsTrue(results.Select(z => z.ErrorMessage).Contains("Invalid Security Number"));
        }

        private Guard GetGuard()
        { 
            return new Guard()
            {
                Id = 1,
                Name = "Guard A",
                IsActive = true,
                Initial = "G.A",
                SecurityNo = "975324"
            };
        }
    }
}
