using AHC.Sandbox.Domain.Entities;

namespace AHC.Sandbox.UnitTests.Domain
{
    public class AddressTests
    {
        private static Address CreateAddress(string? addressLine2 = null)
        {
            return new Address
            {
                AddressId = 541,
                AddressLine1 = "25981 College Street",
                AddressLine2 = addressLine2,
                City = "Montreal",
                StateProvince = "Quebec",
                CountryRegion = "Canada",
                PostalCode = "H1Y 2H5"
            };
        }

        [Test]
        public void SingleLineAddress_OmitsAddressLine2_WhenItIsNull()
        {
            var address = CreateAddress();

            Assert.That(
                address.SingleLineAddress,
                Is.EqualTo("25981 College Street, Montreal, Quebec, H1Y 2H5, Canada"));
        }

        [Test]
        public void SingleLineAddress_IncludesAddressLine2_WhenItIsPresent()
        {
            var address = CreateAddress(addressLine2: "Unit 4");

            Assert.That(
                address.SingleLineAddress,
                Is.EqualTo("25981 College Street, Unit 4, Montreal, Quebec, H1Y 2H5, Canada"));
        }

        // AddressLine2 is nullable in the database, but an empty or whitespace-only string is a
        // different value that would otherwise produce a stray ", , " in the middle of the line.
        [Test]
        public void SingleLineAddress_OmitsAddressLine2_WhenItIsWhitespace()
        {
            var address = CreateAddress(addressLine2: "   ");

            Assert.That(
                address.SingleLineAddress,
                Is.EqualTo("25981 College Street, Montreal, Quebec, H1Y 2H5, Canada"));
        }
    }
}
