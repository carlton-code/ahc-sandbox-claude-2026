using AHC.Sandbox.Domain.Entities;

namespace AHC.Sandbox.UnitTests.Domain
{
    public class OrderTests
    {
        [Test]
        public void IsShipped_WithoutShipDate_IsFalse()
        {
            var order = new Order
            {
                OrderId = 71774,
                OrderNumber = "SO71774",
                CustomerId = 29847,
                OrderDate = new DateTime(2008, 6, 1),
                DueDate = new DateTime(2008, 6, 13),
                ShipDate = null
            };

            Assert.That(order.IsShipped, Is.False);
        }

        [Test]
        public void IsShipped_WithShipDate_IsTrue()
        {
            var order = new Order
            {
                OrderId = 71774,
                OrderNumber = "SO71774",
                CustomerId = 29847,
                OrderDate = new DateTime(2008, 6, 1),
                DueDate = new DateTime(2008, 6, 13),
                ShipDate = new DateTime(2008, 6, 8)
            };

            Assert.That(order.IsShipped, Is.True);
        }
    }
}
