using AHC.Sandbox.Data.Context;
using AHC.Sandbox.Data.Repositories;
using AHC.Sandbox.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AHC.Sandbox.IntegrationTests.Customers;

/// <summary>
/// Exercises <see cref="CustomerReadRepository"/> against the real local AdventureWorksLT
/// database. Read-only — uses stable seed rows and must not mutate any data.
///
/// Known seed data this file relies on (see <c>ReadMe-IntegrationTests.md</c> /
/// <c>docs/database-schema.md</c> for how to re-derive these if the local database changes):
/// - CustomerID 1 (Orlando Gee) exists and has no orders.
/// - CustomerID 29485 exists and has exactly one order: SalesOrderID 71782 (SO71782).
/// - SalesOrderID 71774 belongs to a different customer (29847), giving a customer/order
///   mismatch pair.
/// - CustomerID 999999 is unknown (max CustomerID in the seed data is 30118).
/// </summary>
public class CustomerReadRepositoryTests
{
    private const int KnownCustomerIdWithoutOrders = 1;
    private const int KnownCustomerIdWithOrders = 29485;
    private const int KnownOrderId = 71782;
    private const int MismatchedOrderId = 71774;
    private const int UnknownCustomerId = 999999;

    private AdventureWorksLtDbContext _dbContext = null!;
    private CustomerReadRepository _repository = null!;

    [SetUp]
    public void Setup()
    {
        _dbContext = DbContextTestFactory.Create();
        _repository = new CustomerReadRepository(_dbContext);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    // --- GetAllAsync -----------------------------------------------------------------------

    [Test]
    public async Task GetAllAsync_ReturnsCustomersOrderedByLastNameThenFirstName()
    {
        var customers = await _repository.GetAllAsync();

        Assert.That(customers, Is.Not.Empty);

        // Re-deriving "expected" order via a client-side OrderBy isn't reliable here: SQL
        // Server's default collation (case-insensitive, accent-insensitive) doesn't match any
        // single .NET StringComparer exactly once names contain diacritics/punctuation. Instead,
        // compare against the ground truth of an independent raw-SQL query using the same
        // ORDER BY, which exercises the same collation the repository's query does.
        var expectedOrder = await GetCustomerIdsOrderedByLastNameThenFirstNameAsync();

        Assert.That(customers.Select(c => c.CustomerId).ToArray(), Is.EqualTo(expectedOrder));
    }

    private async Task<int[]> GetCustomerIdsOrderedByLastNameThenFirstNameAsync()
    {
        const string sql = """
            SELECT CustomerID
            FROM SalesLT.Customer
            ORDER BY LastName, FirstName;
            """;

        var connection = _dbContext.Database.GetDbConnection();
        await connection.OpenAsync();

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;

            await using var reader = await command.ExecuteReaderAsync();

            var customerIds = new List<int>();

            while (await reader.ReadAsync())
            {
                customerIds.Add(reader.GetInt32(0));
            }

            return customerIds.ToArray();
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    // --- GetByIdAsync ----------------------------------------------------------------------

    [Test]
    public async Task GetByIdAsync_KnownCustomer_ReturnsMappedCustomer()
    {
        var customer = await _repository.GetByIdAsync(KnownCustomerIdWithoutOrders);

        Assert.That(customer, Is.Not.Null);
        Assert.That(customer!.CustomerId, Is.EqualTo(KnownCustomerIdWithoutOrders));
        Assert.That(customer.FirstName, Is.EqualTo("Orlando"));
        Assert.That(customer.LastName, Is.EqualTo("Gee"));
    }

    [Test]
    public async Task GetByIdAsync_UnknownCustomer_ReturnsNull()
    {
        var customer = await _repository.GetByIdAsync(UnknownCustomerId);

        Assert.That(customer, Is.Null);
    }

    // --- GetOrdersByCustomerIdAsync ----------------------------------------------------------

    [Test]
    public async Task GetOrdersByCustomerIdAsync_CustomerWithOrders_ReturnsOrders()
    {
        var orders = await _repository.GetOrdersByCustomerIdAsync(KnownCustomerIdWithOrders);

        Assert.That(orders, Has.Count.EqualTo(1));
        var order = orders.Single();
        Assert.That(order.OrderId, Is.EqualTo(KnownOrderId));
        Assert.That(order.CustomerId, Is.EqualTo(KnownCustomerIdWithOrders));
        Assert.That(order.OrderNumber, Is.EqualTo("SO71782"));
    }

    [Test]
    public async Task GetOrdersByCustomerIdAsync_CustomerWithoutOrders_ReturnsEmptyCollection()
    {
        var orders = await _repository.GetOrdersByCustomerIdAsync(KnownCustomerIdWithoutOrders);

        Assert.That(orders, Is.Empty);
    }

    // --- GetOrderByIdAsync -------------------------------------------------------------------

    [Test]
    public async Task GetOrderByIdAsync_MatchingCustomerAndOrder_ReturnsOrder()
    {
        var order = await _repository.GetOrderByIdAsync(KnownCustomerIdWithOrders, KnownOrderId);

        Assert.That(order, Is.Not.Null);
        Assert.That(order!.OrderId, Is.EqualTo(KnownOrderId));
        Assert.That(order.OrderNumber, Is.EqualTo("SO71782"));
        Assert.That(order.SubTotal, Is.EqualTo(39785.3304m));
        Assert.That(order.TaxAmount, Is.EqualTo(3182.8264m));
        Assert.That(order.FreightAmount, Is.EqualTo(994.6333m));
        Assert.That(order.TotalDue, Is.EqualTo(43962.7901m));
    }

    [Test]
    public async Task GetOrderByIdAsync_MismatchedCustomerAndOrder_ReturnsNull()
    {
        // MismatchedOrderId genuinely exists, but belongs to a different customer, so this must
        // not match.
        var order = await _repository.GetOrderByIdAsync(KnownCustomerIdWithOrders, MismatchedOrderId);

        Assert.That(order, Is.Null);
    }

    // --- GetSummaryAsync ---------------------------------------------------------------------

    [Test]
    public async Task GetSummaryAsync_CustomerWithOrders_ReturnsAggregates()
    {
        var summary = await _repository.GetSummaryAsync(KnownCustomerIdWithOrders);

        Assert.That(summary, Is.Not.Null);
        Assert.That(summary!.Customer.CustomerId, Is.EqualTo(KnownCustomerIdWithOrders));
        Assert.That(summary.OrderCount, Is.EqualTo(1));
        Assert.That(summary.TotalOrderValue, Is.EqualTo(43962.7901m));
        Assert.That(summary.MostRecentOrderDate, Is.EqualTo(new DateTime(2008, 6, 1)));
    }

    [Test]
    public async Task GetSummaryAsync_UnknownCustomer_ReturnsNull()
    {
        var summary = await _repository.GetSummaryAsync(UnknownCustomerId);

        Assert.That(summary, Is.Null);
    }

    // --- GetRecentOrdersAsync ------------------------------------------------------------------

    [Test]
    public async Task GetRecentOrdersAsync_RespectsCountAndOrdering()
    {
        // The local AdventureWorksLT seed data only has a single order per customer, so this
        // can't exercise real truncation of multiple rows down to `count` — it confirms the
        // TOP(@count) parameterized query still returns the right (single) order for a customer
        // that has one.
        var orders = await _repository.GetRecentOrdersAsync(KnownCustomerIdWithOrders, count: 5);

        Assert.That(orders, Has.Count.EqualTo(1));
        Assert.That(orders.Single().OrderId, Is.EqualTo(KnownOrderId));
    }

    [Test]
    public async Task GetRecentOrdersAsync_CustomerWithoutOrders_ReturnsEmptyCollection()
    {
        var orders = await _repository.GetRecentOrdersAsync(KnownCustomerIdWithoutOrders, count: 5);

        Assert.That(orders, Is.Empty);
    }

    // --- GetOrderSummaryAsync ------------------------------------------------------------------

    [Test]
    public async Task GetOrderSummaryAsync_CustomerWithOrders_ReturnsAggregates()
    {
        var summary = await _repository.GetOrderSummaryAsync(KnownCustomerIdWithOrders);

        Assert.That(summary, Is.Not.Null);
        Assert.That(summary!.CustomerId, Is.EqualTo(KnownCustomerIdWithOrders));
        Assert.That(summary.OrderCount, Is.EqualTo(1));
        Assert.That(summary.SubTotal, Is.EqualTo(39785.3304m));
        Assert.That(summary.TaxAmount, Is.EqualTo(3182.8264m));
        Assert.That(summary.FreightAmount, Is.EqualTo(994.6333m));
        Assert.That(summary.TotalDue, Is.EqualTo(43962.7901m));
        Assert.That(summary.FirstOrderDate, Is.EqualTo(new DateTime(2008, 6, 1)));
        Assert.That(summary.MostRecentOrderDate, Is.EqualTo(new DateTime(2008, 6, 1)));
    }

    [Test]
    public async Task GetOrderSummaryAsync_UnknownCustomer_ReturnsNull()
    {
        var summary = await _repository.GetOrderSummaryAsync(UnknownCustomerId);

        Assert.That(summary, Is.Null);
    }
}
