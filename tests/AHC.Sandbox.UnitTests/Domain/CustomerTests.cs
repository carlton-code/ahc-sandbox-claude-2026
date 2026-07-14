using AHC.Sandbox.Domain.Entities;

namespace AHC.Sandbox.UnitTests.Domain
{
    public class CustomerTests
    {
        [Test]
        public void FullName_WithoutMiddleName_IsFirstAndLastNameOnly()
        {
            var customer = new Customer
            {
                CustomerId = 1,
                FirstName = "Jon",
                MiddleName = null,
                LastName = "Yang",
                EmailAddress = "jon.yang@example.com"
            };

            Assert.That(customer.FullName, Is.EqualTo("Jon Yang"));
        }

        [Test]
        public void FullName_WithWhitespaceMiddleName_IsFirstAndLastNameOnly()
        {
            var customer = new Customer
            {
                CustomerId = 1,
                FirstName = "Jon",
                MiddleName = "   ",
                LastName = "Yang",
                EmailAddress = "jon.yang@example.com"
            };

            Assert.That(customer.FullName, Is.EqualTo("Jon Yang"));
        }

        [Test]
        public void FullName_WithMiddleName_IncludesMiddleName()
        {
            var customer = new Customer
            {
                CustomerId = 1,
                FirstName = "Jon",
                MiddleName = "V",
                LastName = "Yang",
                EmailAddress = "jon.yang@example.com"
            };

            Assert.That(customer.FullName, Is.EqualTo("Jon V Yang"));
        }
    }
}
