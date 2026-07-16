using AHC.Sandbox.Domain.Entities;

namespace AHC.Sandbox.UnitTests.Domain
{
    public class ProductTests
    {
        [Test]
        public void IsDiscontinued_WithoutDiscontinuedDate_IsFalse()
        {
            var product = new Product
            {
                ProductId = 680,
                Name = "HL Road Frame - Black, 58",
                ProductNumber = "FR-R92B-58",
                SellStartDate = new DateTime(2002, 6, 1),
                DiscontinuedDate = null
            };

            Assert.That(product.IsDiscontinued, Is.False);
        }

        [Test]
        public void IsDiscontinued_WithDiscontinuedDate_IsTrue()
        {
            var product = new Product
            {
                ProductId = 680,
                Name = "HL Road Frame - Black, 58",
                ProductNumber = "FR-R92B-58",
                SellStartDate = new DateTime(2002, 6, 1),
                DiscontinuedDate = new DateTime(2006, 1, 1)
            };

            Assert.That(product.IsDiscontinued, Is.True);
        }
    }
}
