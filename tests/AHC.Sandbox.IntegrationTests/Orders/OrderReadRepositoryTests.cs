using AHC.Sandbox.Data.Context;
using AHC.Sandbox.Data.Repositories;
using AHC.Sandbox.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AHC.Sandbox.IntegrationTests.Orders;

/// <summary>
/// Exercises <see cref="OrderReadRepository"/> against the real local AdventureWorksLT
/// database. Read-only — uses stable seed rows and must not mutate any data.
///
/// Known seed data this file relies on (re-derive against the local database if it changes):
/// - 32 seeded orders, SalesOrderIDs 71774–71946, and every one shares the single OrderDate
///   2008-06-01 — so the SalesOrderID tiebreaker carries the list ordering entirely.
/// - SalesOrderID 71774 (customer 29847, SO71774) has exactly 2 lines (110562, 110563) and a
///   fully populated header: PO348186287, account 10-4020-000609, ship/bill address 1092,
///   CARGO TRANSPORT 5, SubTotal 880.3484, TaxAmt 70.4279, Freight 22.0087, computed TotalDue
///   972.7850, TrackingNumber 324A3AD5-F7B6-4CE0. Comment is null on every seed order.
/// - SalesOrderID 71782 has 43 lines; the first by SalesOrderDetailID is 110667
///   (qty 3, product 714, UnitPrice 29.9940, no discount, computed LineTotal 89.982000).
/// - SalesOrderID 999999 is unknown.
/// - No seed order has a null ShipDate, so IsShipped's false branch is covered by unit tests.
/// </summary>
public class OrderReadRepositoryTests
{
    private const int KnownOrderId = 71774;
    private const int KnownOrderIdWithManyLines = 71782;
    private const int UnknownOrderId = 999999;

    private AdventureWorksLtDbContext _dbContext = null!;
    private OrderReadRepository _repository = null!;

    [SetUp]
    public void Setup()
    {
        _dbContext = DbContextTestFactory.Create();
        _repository = new OrderReadRepository(_dbContext);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    // --- GetAllAsync -----------------------------------------------------------------------

    [Test]
    public async Task GetAllAsync_ReturnsOrdersNewestFirst()
    {
        var orders = await _repository.GetAllAsync();

        Assert.That(orders, Is.Not.Empty);

        // Same technique as the Customer/Product read tests: compare against an independent
        // raw-SQL query using the same ORDER BY, rather than re-deriving the order client-side.
        var expectedOrder = await GetOrderIdsNewestFirstAsync();

        Assert.That(orders.Select(o => o.OrderId).ToArray(), Is.EqualTo(expectedOrder));
    }

    [Test]
    public async Task GetAllAsync_ReturnsHeadersWithEmptyLines()
    {
        var orders = await _repository.GetAllAsync();

        Assert.That(orders, Is.Not.Empty);
        Assert.That(orders.All(o => o.Lines.Count == 0), Is.True,
            "GetAllAsync is a headers-only read; no order should carry lines.");
    }

    private async Task<int[]> GetOrderIdsNewestFirstAsync()
    {
        const string sql = """
            SELECT SalesOrderID
            FROM SalesLT.SalesOrderHeader
            ORDER BY OrderDate DESC, SalesOrderID;
            """;

        var connection = _dbContext.Database.GetDbConnection();
        await connection.OpenAsync();

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;

            await using var reader = await command.ExecuteReaderAsync();

            var orderIds = new List<int>();

            while (await reader.ReadAsync())
            {
                orderIds.Add(reader.GetInt32(0));
            }

            return orderIds.ToArray();
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    // --- GetByIdAsync ----------------------------------------------------------------------

    // Pins the header mapping column-for-column against a fully-populated seed row, including
    // the two database-computed columns (SalesOrderNumber, TotalDue) and the non-standard
    // varchar(18) TrackingNumber.
    [Test]
    public async Task GetByIdAsync_KnownOrder_MapsEveryHeaderColumn()
    {
        var order = await _repository.GetByIdAsync(KnownOrderId);

        Assert.That(order, Is.Not.Null);
        Assert.That(order!.OrderId, Is.EqualTo(71774));
        Assert.That(order.OrderNumber, Is.EqualTo("SO71774"));
        Assert.That(order.CustomerId, Is.EqualTo(29847));
        Assert.That(order.OrderDate, Is.EqualTo(new DateTime(2008, 6, 1)));
        Assert.That(order.DueDate, Is.EqualTo(new DateTime(2008, 6, 13)));
        Assert.That(order.ShipDate, Is.EqualTo(new DateTime(2008, 6, 8)));
        Assert.That(order.Status, Is.EqualTo(5));
        Assert.That(order.PurchaseOrderNumber, Is.EqualTo("PO348186287"));
        Assert.That(order.AccountNumber, Is.EqualTo("10-4020-000609"));
        Assert.That(order.ShipToAddressId, Is.EqualTo(1092));
        Assert.That(order.BillToAddressId, Is.EqualTo(1092));
        Assert.That(order.ShipMethod, Is.EqualTo("CARGO TRANSPORT 5"));
        Assert.That(order.SubTotal, Is.EqualTo(880.3484m));
        Assert.That(order.TaxAmount, Is.EqualTo(70.4279m));
        Assert.That(order.FreightAmount, Is.EqualTo(22.0087m));
        Assert.That(order.TotalDue, Is.EqualTo(972.7850m));
        Assert.That(order.TrackingNumber, Is.EqualTo("324A3AD5-F7B6-4CE0"));
        Assert.That(order.Comment, Is.Null);
        Assert.That(order.IsShipped, Is.True);

        Assert.That(order.Lines, Has.Count.EqualTo(2));
        Assert.That(order.Lines.Select(l => l.OrderLineId), Is.EqualTo(new[] { 110562, 110563 }));
    }

    // Pins the line mapping — including the numeric(38,6) computed LineTotal — via a seed
    // order with enough lines to make the SalesOrderDetailID sort observable.
    [Test]
    public async Task GetByIdAsync_KnownOrder_MapsLinesOrderedByLineId()
    {
        var order = await _repository.GetByIdAsync(KnownOrderIdWithManyLines);

        Assert.That(order, Is.Not.Null);
        Assert.That(order!.Lines, Has.Count.EqualTo(43));
        Assert.That(order.Lines.Select(l => l.OrderLineId).ToArray(),
            Is.EqualTo(order.Lines.Select(l => l.OrderLineId).OrderBy(id => id).ToArray()));

        var firstLine = order.Lines.First();
        Assert.That(firstLine.OrderLineId, Is.EqualTo(110667));
        Assert.That(firstLine.OrderQty, Is.EqualTo(3));
        Assert.That(firstLine.ProductId, Is.EqualTo(714));
        Assert.That(firstLine.UnitPrice, Is.EqualTo(29.9940m));
        Assert.That(firstLine.UnitPriceDiscount, Is.EqualTo(0m));
        Assert.That(firstLine.LineTotal, Is.EqualTo(89.982000m));
    }

    [Test]
    public async Task GetByIdAsync_UnknownOrder_ReturnsNull()
    {
        var order = await _repository.GetByIdAsync(UnknownOrderId);

        Assert.That(order, Is.Null);
    }
}
