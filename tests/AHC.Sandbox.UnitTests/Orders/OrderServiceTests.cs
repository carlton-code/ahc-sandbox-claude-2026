using AHC.Sandbox.Application.Orders.Services;
using AHC.Sandbox.Domain.Entities;
using AHC.Sandbox.UnitTests.Orders.Fakes;

namespace AHC.Sandbox.UnitTests.Orders
{
    public class OrderServiceTests
    {
        private FakeOrderReadRepository _readRepository = null!;
        private OrderService _service = null!;

        [SetUp]
        public void Setup()
        {
            _readRepository = new FakeOrderReadRepository();
            _service = new OrderService(_readRepository);
        }

        // Seed-shaped defaults: order 71774 (customer 29847), the reference row the integration
        // tests pin against the real database.
        private static Order CreateOrder(
            int orderId,
            string orderNumber = "SO71774",
            int customerId = 29847,
            DateTime? shipDate = null,
            IReadOnlyCollection<OrderLine>? lines = null)
        {
            return new Order
            {
                OrderId = orderId,
                OrderNumber = orderNumber,
                CustomerId = customerId,
                OrderDate = new DateTime(2008, 6, 1),
                DueDate = new DateTime(2008, 6, 13),
                ShipDate = shipDate,
                Status = 5,
                PurchaseOrderNumber = "PO348186287",
                AccountNumber = "10-4020-000609",
                ShipToAddressId = 1092,
                BillToAddressId = 1092,
                ShipMethod = "CARGO TRANSPORT 5",
                SubTotal = 880.3484m,
                TaxAmount = 70.4279m,
                FreightAmount = 22.0087m,
                TotalDue = 972.7850m,
                TrackingNumber = "324A3AD5-F7B6-4CE0",
                Comment = null,
                Lines = lines ?? Array.Empty<OrderLine>()
            };
        }

        // --- GetOrdersAsync -------------------------------------------------------------------

        [Test]
        public async Task GetOrdersAsync_MapsAllRepositoryOrdersToDtos()
        {
            _readRepository.Orders.Add(CreateOrder(71774));
            _readRepository.Orders.Add(CreateOrder(71776, orderNumber: "SO71776", customerId: 30072));

            var result = await _service.GetOrdersAsync();

            Assert.That(result, Has.Count.EqualTo(2));

            var first = result.Single(o => o.OrderId == 71774);
            Assert.That(first.OrderNumber, Is.EqualTo("SO71774"));
            Assert.That(first.CustomerId, Is.EqualTo(29847));

            var second = result.Single(o => o.OrderId == 71776);
            Assert.That(second.OrderNumber, Is.EqualTo("SO71776"));
            Assert.That(second.CustomerId, Is.EqualTo(30072));
        }

        [Test]
        public async Task GetOrdersAsync_EmptyRepository_ReturnsEmptyNotNull()
        {
            var result = await _service.GetOrdersAsync();

            Assert.That(result, Is.Empty);
        }

        // --- GetOrderByIdAsync ----------------------------------------------------------------

        // The mapping is the only real logic in this service, so this pins every header field
        // and every line field once.
        [Test]
        public async Task GetOrderByIdAsync_KnownOrder_MapsEveryFieldToTheDto()
        {
            var lines = new[]
            {
                new OrderLine
                {
                    OrderLineId = 110562,
                    ProductId = 836,
                    OrderQty = 1,
                    UnitPrice = 356.898m,
                    UnitPriceDiscount = 0m,
                    LineTotal = 356.898000m
                },
                new OrderLine
                {
                    OrderLineId = 110563,
                    ProductId = 822,
                    OrderQty = 1,
                    UnitPrice = 356.898m,
                    UnitPriceDiscount = 0.05m,
                    LineTotal = 339.053100m
                }
            };

            _readRepository.Orders.Add(CreateOrder(71774, shipDate: new DateTime(2008, 6, 8), lines: lines));

            var result = await _service.GetOrderByIdAsync(71774);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.OrderId, Is.EqualTo(71774));
            Assert.That(result.OrderNumber, Is.EqualTo("SO71774"));
            Assert.That(result.CustomerId, Is.EqualTo(29847));
            Assert.That(result.OrderDate, Is.EqualTo(new DateTime(2008, 6, 1)));
            Assert.That(result.DueDate, Is.EqualTo(new DateTime(2008, 6, 13)));
            Assert.That(result.ShipDate, Is.EqualTo(new DateTime(2008, 6, 8)));
            Assert.That(result.Status, Is.EqualTo(5));
            Assert.That(result.PurchaseOrderNumber, Is.EqualTo("PO348186287"));
            Assert.That(result.AccountNumber, Is.EqualTo("10-4020-000609"));
            Assert.That(result.ShipToAddressId, Is.EqualTo(1092));
            Assert.That(result.BillToAddressId, Is.EqualTo(1092));
            Assert.That(result.ShipMethod, Is.EqualTo("CARGO TRANSPORT 5"));
            Assert.That(result.SubTotal, Is.EqualTo(880.3484m));
            Assert.That(result.TaxAmount, Is.EqualTo(70.4279m));
            Assert.That(result.FreightAmount, Is.EqualTo(22.0087m));
            Assert.That(result.TotalDue, Is.EqualTo(972.7850m));
            Assert.That(result.TrackingNumber, Is.EqualTo("324A3AD5-F7B6-4CE0"));
            Assert.That(result.Comment, Is.Null);
            Assert.That(result.IsShipped, Is.True);

            Assert.That(result.Lines, Has.Count.EqualTo(2));
            var firstLine = result.Lines.First();
            Assert.That(firstLine.OrderLineId, Is.EqualTo(110562));
            Assert.That(firstLine.ProductId, Is.EqualTo(836));
            Assert.That(firstLine.OrderQty, Is.EqualTo(1));
            Assert.That(firstLine.UnitPrice, Is.EqualTo(356.898m));
            Assert.That(firstLine.UnitPriceDiscount, Is.EqualTo(0m));
            Assert.That(firstLine.LineTotal, Is.EqualTo(356.898000m));

            var secondLine = result.Lines.Last();
            Assert.That(secondLine.OrderLineId, Is.EqualTo(110563));
            Assert.That(secondLine.UnitPriceDiscount, Is.EqualTo(0.05m));
            Assert.That(secondLine.LineTotal, Is.EqualTo(339.053100m));
        }

        [Test]
        public async Task GetOrderByIdAsync_UnshippedOrder_IsShippedIsFalse()
        {
            _readRepository.Orders.Add(CreateOrder(71774, shipDate: null));

            var result = await _service.GetOrderByIdAsync(71774);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.ShipDate, Is.Null);
            Assert.That(result.IsShipped, Is.False);
        }

        [Test]
        public async Task GetOrderByIdAsync_OrderWithoutLines_ReturnsEmptyLinesNotNull()
        {
            _readRepository.Orders.Add(CreateOrder(71774));

            var result = await _service.GetOrderByIdAsync(71774);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Lines, Is.Empty);
        }

        [Test]
        public async Task GetOrderByIdAsync_UnknownOrder_ReturnsNull()
        {
            var result = await _service.GetOrderByIdAsync(999999);

            Assert.That(result, Is.Null);
        }
    }
}
