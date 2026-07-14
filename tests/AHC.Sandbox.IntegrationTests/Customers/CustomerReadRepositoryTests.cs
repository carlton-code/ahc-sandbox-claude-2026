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
/// - CustomerID 1 (Orlando Gee) exists and has no orders, and has no Rewards tier assigned.
/// - CustomerID 29485 exists and has exactly one order: SalesOrderID 71782 (SO71782).
/// - SalesOrderID 71774 belongs to a different customer (29847), giving a customer/order
///   mismatch pair.
/// - CustomerID 999999 is unknown (max CustomerID in the seed data is 30118).
/// - CustomerID 3 is assigned Rewards tier Silver (RewardsLevelId 1, DiscountPercent 0.0009).
///   Rewards.RewardsLevel holds exactly three tiers: Gold (0), Silver (1), Bronze (2). 552 of
///   847 customers have a tier; the other 295 have none, so both branches are real seed states.
/// - Exactly two customers are named "Orlando Gee": CustomerIDs 1 and 29773.
/// - Exactly two are named "Roger Van Houten": CustomerIDs 635 and 30102. The surname contains a
///   space, which is what makes it the search test that matters — see
///   <see cref="CustomerReadRepository.SearchByNameAsync"/>.
/// - No customer's name contains a LIKE metacharacter (%, _, [), so a search for one must return
///   nothing rather than everything.
/// </summary>
public class CustomerReadRepositoryTests
{
    private const int KnownCustomerIdWithoutOrders = 1;
    private const int KnownCustomerIdWithOrders = 29485;
    private const int KnownOrderId = 71782;
    private const int MismatchedOrderId = 71774;
    private const int UnknownCustomerId = 999999;
    private const int KnownCustomerIdWithRewardsTier = 3;
    private const int KnownCustomerIdWithoutRewardsTier = 1;

    // "Orlando Gee" — first name, last name, and the two together all resolve to these two rows.
    private static readonly int[] OrlandoGeeCustomerIds = [1, 29773];

    // "Roger Van Houten" — surname contains a space.
    private static readonly int[] RogerVanHoutenCustomerIds = [635, 30102];

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
            ORDER BY LastName, FirstName, CustomerID;
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

    // Same distinction as GetOrderSummaryAsync_CustomerWithNoOrders_ReturnsZeroedSummaryNotNull:
    // a customer with no orders still has a summary (their details, zeroed totals), and only an
    // unknown customer is null. Here the zeros come from GetSummaryAsync's own `?? 0` folding.
    [Test]
    public async Task GetSummaryAsync_CustomerWithNoOrders_ReturnsCustomerWithZeroedTotals()
    {
        var summary = await _repository.GetSummaryAsync(KnownCustomerIdWithoutOrders);

        Assert.That(summary, Is.Not.Null);
        Assert.That(summary!.Customer.CustomerId, Is.EqualTo(KnownCustomerIdWithoutOrders));
        Assert.That(summary.OrderCount, Is.EqualTo(0));
        Assert.That(summary.TotalOrderValue, Is.EqualTo(0m));
        Assert.That(summary.MostRecentOrderDate, Is.Null);
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

    // The dominant case — 815 of 847 customers have no orders — and the one the other two tests
    // don't distinguish between. A customer with no orders is not the same as no such customer:
    // this returns a zeroed summary, while GetOrderSummaryAsync_UnknownCustomer_ReturnsNull below
    // returns null (which the controller turns into a 404). The zeros come from the query's
    // COALESCE(SUM(...), 0) and the null dates from MIN/MAX over no rows; the AnyAsync existence
    // probe is what keeps the two cases apart, since the aggregate itself always returns one row.
    [Test]
    public async Task GetOrderSummaryAsync_CustomerWithNoOrders_ReturnsZeroedSummaryNotNull()
    {
        var summary = await _repository.GetOrderSummaryAsync(KnownCustomerIdWithoutOrders);

        Assert.That(summary, Is.Not.Null);
        Assert.That(summary!.CustomerId, Is.EqualTo(KnownCustomerIdWithoutOrders));
        Assert.That(summary.OrderCount, Is.EqualTo(0));
        Assert.That(summary.SubTotal, Is.EqualTo(0m));
        Assert.That(summary.TaxAmount, Is.EqualTo(0m));
        Assert.That(summary.FreightAmount, Is.EqualTo(0m));
        Assert.That(summary.TotalDue, Is.EqualTo(0m));
        Assert.That(summary.FirstOrderDate, Is.Null);
        Assert.That(summary.MostRecentOrderDate, Is.Null);
    }

    [Test]
    public async Task GetOrderSummaryAsync_UnknownCustomer_ReturnsNull()
    {
        var summary = await _repository.GetOrderSummaryAsync(UnknownCustomerId);

        Assert.That(summary, Is.Null);
    }

    // --- GetRewardsAsync -----------------------------------------------------------------------

    [Test]
    public async Task GetRewardsAsync_CustomerWithTier_ReturnsTier()
    {
        var rewards = await _repository.GetRewardsAsync(KnownCustomerIdWithRewardsTier);

        Assert.That(rewards, Is.Not.Null);
        Assert.That(rewards!.CustomerId, Is.EqualTo(KnownCustomerIdWithRewardsTier));
        Assert.That(rewards.RewardsLevelId, Is.EqualTo(1));
        Assert.That(rewards.RewardsLevelName, Is.EqualTo("Silver"));
        Assert.That(rewards.DiscountPercent, Is.EqualTo(0.0009m));
    }

    // The 35%-of-customers case, and the whole reason the query LEFT JOINs from SalesLT.Customer:
    // an unenrolled customer must still come back as a customer, not as a 404.
    [Test]
    public async Task GetRewardsAsync_CustomerWithoutTier_ReturnsDtoWithNullTier()
    {
        var rewards = await _repository.GetRewardsAsync(KnownCustomerIdWithoutRewardsTier);

        Assert.That(rewards, Is.Not.Null);
        Assert.That(rewards!.CustomerId, Is.EqualTo(KnownCustomerIdWithoutRewardsTier));
        Assert.That(rewards.RewardsLevelId, Is.Null);
        Assert.That(rewards.RewardsLevelName, Is.Null);
        Assert.That(rewards.DiscountPercent, Is.Null);
    }

    [Test]
    public async Task GetRewardsAsync_UnknownCustomer_ReturnsNull()
    {
        var rewards = await _repository.GetRewardsAsync(UnknownCustomerId);

        Assert.That(rewards, Is.Null);
    }

    // --- SearchByNameAsync ---------------------------------------------------------------------

    [Test]
    public async Task SearchByNameAsync_FirstNameOnly_ReturnsMatches()
    {
        var customers = await _repository.SearchByNameAsync("Orlando");

        Assert.That(customers.Select(c => c.CustomerId), Is.EqualTo(OrlandoGeeCustomerIds));
    }

    [Test]
    public async Task SearchByNameAsync_LastNameOnly_ReturnsMatches()
    {
        var customers = await _repository.SearchByNameAsync("Gee");

        Assert.That(customers.Select(c => c.CustomerId), Is.EqualTo(OrlandoGeeCustomerIds));
    }

    [Test]
    public async Task SearchByNameAsync_FirstAndLastName_ReturnsMatches()
    {
        var customers = await _repository.SearchByNameAsync("Orlando Gee");

        Assert.That(customers.Select(c => c.CustomerId), Is.EqualTo(OrlandoGeeCustomerIds));
    }

    // The case that rules out splitting the term into first/last parts: "Van Houten" is the
    // surname, so a naive split on the first space would search for the surname "Van" and find
    // nothing.
    [Test]
    public async Task SearchByNameAsync_MultiWordSurname_ReturnsMatches()
    {
        var customers = await _repository.SearchByNameAsync("Roger Van Houten");

        Assert.That(customers.Select(c => c.CustomerId), Is.EqualTo(RogerVanHoutenCustomerIds));
    }

    // Pins a dependency on the database's default case-insensitive collation rather than assuming
    // it: if the collation ever changed, the endpoint would quietly stop matching.
    [Test]
    public async Task SearchByNameAsync_IsCaseInsensitive()
    {
        var lower = await _repository.SearchByNameAsync("orlando gee");
        var upper = await _repository.SearchByNameAsync("ORLANDO GEE");

        Assert.That(lower.Select(c => c.CustomerId), Is.EqualTo(OrlandoGeeCustomerIds));
        Assert.That(upper.Select(c => c.CustomerId), Is.EqualTo(OrlandoGeeCustomerIds));
    }

    // Without wildcard escaping these return every customer instead of none — the difference
    // between a search and a table dump.
    [TestCase("%")]
    [TestCase("_")]
    [TestCase("[")]
    public async Task SearchByNameAsync_LikeWildcardInTerm_IsTreatedLiterally(string term)
    {
        var customers = await _repository.SearchByNameAsync(term);

        Assert.That(customers, Is.Empty);
    }

    [Test]
    public async Task SearchByNameAsync_NoMatch_ReturnsEmpty()
    {
        var customers = await _repository.SearchByNameAsync("zzzznomatch");

        Assert.That(customers, Is.Empty);
    }

    [Test]
    public async Task SearchByNameAsync_TrimsSurroundingWhitespace()
    {
        var customers = await _repository.SearchByNameAsync("  Orlando Gee  ");

        Assert.That(customers.Select(c => c.CustomerId), Is.EqualTo(OrlandoGeeCustomerIds));
    }

    // Two things at once, both against a term that matches most of the table.
    //
    // Ordering: a client-side OrderBy can't reproduce SQL Server's collation once names carry
    // diacritics/punctuation (see GetAllAsync_ReturnsCustomersOrderedByLastNameThenFirstName), so
    // this compares against an independent hand-written query instead.
    //
    // Matching: the SQL below deliberately keeps the naive four-clause form — matching FirstName
    // and LastName separately as well as the two concatenations — while the repository only
    // matches the concatenations. So this isn't restating the production predicate back at itself;
    // it pins that dropping those two clauses really is equivalent, which is the assumption the
    // simplification rests on.
    [Test]
    public async Task SearchByNameAsync_MatchesAndOrdersTheSameAsEquivalentSql()
    {
        var customers = await _repository.SearchByNameAsync("a");

        Assert.That(customers, Is.Not.Empty);

        var expected = await GetSearchMatchedCustomerIdsAsync("a");

        Assert.That(customers.Select(c => c.CustomerId).ToArray(), Is.EqualTo(expected));
    }

    private async Task<int[]> GetSearchMatchedCustomerIdsAsync(string term)
    {
        const string sql = """
            SELECT CustomerID
            FROM SalesLT.Customer
            WHERE FirstName LIKE @pattern ESCAPE '\'
               OR LastName LIKE @pattern ESCAPE '\'
               OR (FirstName + ' ' + LastName) LIKE @pattern ESCAPE '\'
               OR (FirstName + ' ' + ISNULL(MiddleName + ' ', '') + LastName) LIKE @pattern ESCAPE '\'
            ORDER BY LastName, FirstName, CustomerID;
            """;

        var connection = _dbContext.Database.GetDbConnection();
        await connection.OpenAsync();

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@pattern";
            parameter.Value = $"%{term}%";
            command.Parameters.Add(parameter);

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
}
