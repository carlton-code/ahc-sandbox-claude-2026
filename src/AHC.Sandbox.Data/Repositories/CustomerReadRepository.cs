using AHC.Sandbox.Application.Customers.Dtos;
using AHC.Sandbox.Application.Customers.Interfaces;
using AHC.Sandbox.Data.Context;
using AHC.Sandbox.Data.Entities;
using AHC.Sandbox.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;

namespace AHC.Sandbox.Data.Repositories;

public class CustomerReadRepository : ICustomerReadRepository
{
    private readonly AdventureWorksLtDbContext _dbContext;

    public CustomerReadRepository(AdventureWorksLtDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.Customers
            .AsNoTracking()
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .ToArrayAsync(cancellationToken);

        return entities
            .Select(MapCustomer)
            .ToArray();
    }

    public async Task<Customer?> GetByIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);

        return entity is null ? null : MapCustomer(entity);
    }

    public Task<IReadOnlyCollection<CustomerOrderDto>> GetOrdersByCustomerIdAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                SalesOrderID,
                CustomerID,
                SalesOrderNumber,
                OrderDate,
                ShipDate,
                SubTotal,
                TaxAmt,
                Freight,
                TotalDue
            FROM SalesLT.SalesOrderHeader
            WHERE CustomerID = @customerId
            ORDER BY OrderDate DESC, SalesOrderID DESC;
            """;

        return ExecuteOrderQueryAsync(
            sql,
            command => AddParameter(command, "@customerId", customerId),
            cancellationToken);
    }

    public async Task<CustomerOrderDto?> GetOrderByIdAsync(
        int customerId,
        int orderId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                SalesOrderID,
                CustomerID,
                SalesOrderNumber,
                OrderDate,
                ShipDate,
                SubTotal,
                TaxAmt,
                Freight,
                TotalDue
            FROM SalesLT.SalesOrderHeader
            WHERE CustomerID = @customerId
                AND SalesOrderID = @orderId;
            """;

        var orders = await ExecuteOrderQueryAsync(
            sql,
            command =>
            {
                AddParameter(command, "@customerId", customerId);
                AddParameter(command, "@orderId", orderId);
            },
            cancellationToken);

        return orders.FirstOrDefault();
    }

    public async Task<CustomerSummaryDto?> GetSummaryAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        var customer = await GetByIdAsync(customerId, cancellationToken);

        if (customer is null)
        {
            return null;
        }

        var orderSummary = await GetOrderSummaryAsync(customerId, cancellationToken);

        return new CustomerSummaryDto
        {
            Customer = MapCustomerDto(customer),
            OrderCount = orderSummary?.OrderCount ?? 0,
            TotalOrderValue = orderSummary?.TotalDue ?? 0,
            MostRecentOrderDate = orderSummary?.MostRecentOrderDate
        };
    }

    public Task<IReadOnlyCollection<CustomerOrderDto>> GetRecentOrdersAsync(
        int customerId,
        int count = 5,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP (@count)
                SalesOrderID,
                CustomerID,
                SalesOrderNumber,
                OrderDate,
                ShipDate,
                SubTotal,
                TaxAmt,
                Freight,
                TotalDue
            FROM SalesLT.SalesOrderHeader
            WHERE CustomerID = @customerId
            ORDER BY OrderDate DESC, SalesOrderID DESC;
            """;

        return ExecuteOrderQueryAsync(
            sql,
            command =>
            {
                AddParameter(command, "@customerId", customerId);
                AddParameter(command, "@count", Math.Max(count, 1));
            },
            cancellationToken);
    }

    public async Task<CustomerOrderSummaryDto?> GetOrderSummaryAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        var customerExists = await _dbContext.Customers
            .AsNoTracking()
            .AnyAsync(c => c.CustomerId == customerId, cancellationToken);

        if (!customerExists)
        {
            return null;
        }

        const string sql = """
            SELECT
                COUNT(1) AS OrderCount,
                COALESCE(SUM(SubTotal), 0) AS SubTotal,
                COALESCE(SUM(TaxAmt), 0) AS TaxAmount,
                COALESCE(SUM(Freight), 0) AS FreightAmount,
                COALESCE(SUM(TotalDue), 0) AS TotalDue,
                MIN(OrderDate) AS FirstOrderDate,
                MAX(OrderDate) AS MostRecentOrderDate
            FROM SalesLT.SalesOrderHeader
            WHERE CustomerID = @customerId;
            """;

        var connection = _dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;

        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@customerId", customerId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return new CustomerOrderSummaryDto
                {
                    CustomerId = customerId
                };
            }

            return new CustomerOrderSummaryDto
            {
                CustomerId = customerId,
                OrderCount = Convert.ToInt32(reader["OrderCount"]),
                SubTotal = Convert.ToDecimal(reader["SubTotal"]),
                TaxAmount = Convert.ToDecimal(reader["TaxAmount"]),
                FreightAmount = Convert.ToDecimal(reader["FreightAmount"]),
                TotalDue = Convert.ToDecimal(reader["TotalDue"]),
                FirstOrderDate = reader["FirstOrderDate"] is DBNull ? null : Convert.ToDateTime(reader["FirstOrderDate"]),
                MostRecentOrderDate = reader["MostRecentOrderDate"] is DBNull ? null : Convert.ToDateTime(reader["MostRecentOrderDate"])
            };
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    public async Task<CustomerRewardsDto?> GetRewardsAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        // Unlike GetOrderSummaryAsync, no separate existence probe is needed: this LEFT JOINs
        // *from* SalesLT.Customer, so zero rows means the customer doesn't exist, while one row
        // with null tier columns means the customer exists but has no tier assigned (about a
        // third of customers). An aggregate can't make that distinction; this join can.
        const string sql = """
            SELECT
                c.CustomerID,
                crl.RewardsLevelId,
                rl.RewardsLevelName,
                rl.DiscountPercent
            FROM SalesLT.Customer AS c
                LEFT JOIN Rewards.CustomerRewardsLevel AS crl ON crl.CustomerId = c.CustomerID
                LEFT JOIN Rewards.RewardsLevel AS rl ON rl.RewardsLevelId = crl.RewardsLevelId
            WHERE c.CustomerID = @customerId;
            """;

        var connection = _dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;

        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@customerId", customerId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            var rewards = new CustomerRewardsDto
            {
                CustomerId = Convert.ToInt32(reader["CustomerID"]),
                RewardsLevelId = reader["RewardsLevelId"] is DBNull ? null : Convert.ToInt32(reader["RewardsLevelId"]),
                RewardsLevelName = reader["RewardsLevelName"] is DBNull ? null : Convert.ToString(reader["RewardsLevelName"]),
                DiscountPercent = reader["DiscountPercent"] is DBNull ? null : Convert.ToDecimal(reader["DiscountPercent"])
            };

            // PK_CustomerRewardsLevel (ADR-0008) makes a second row impossible, so reaching here
            // means that constraint is gone — most likely the database was re-provisioned from a
            // fresh restore, which drops it. Throw rather than silently returning whichever tier
            // the server happened to order first: a wrong tier is worse than a failed request.
            if (await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException(
                    $"Customer {customerId} has more than one rewards tier row, which " +
                    "PK_CustomerRewardsLevel should prevent. The constraint is missing — see " +
                    "docs/adr/0008-one-rewards-tier-per-customer.md to restore it.");
            }

            return rewards;
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    private async Task<IReadOnlyCollection<CustomerOrderDto>> ExecuteOrderQueryAsync(
        string sql,
        Action<DbCommand> configureCommand,
        CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;

        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            configureCommand(command);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var orders = new List<CustomerOrderDto>();

            while (await reader.ReadAsync(cancellationToken))
            {
                orders.Add(new CustomerOrderDto
                {
                    OrderId = Convert.ToInt32(reader["SalesOrderID"]),
                    CustomerId = Convert.ToInt32(reader["CustomerID"]),
                    OrderNumber = Convert.ToString(reader["SalesOrderNumber"]) ?? string.Empty,
                    OrderDate = Convert.ToDateTime(reader["OrderDate"]),
                    ShipDate = reader["ShipDate"] is DBNull ? null : Convert.ToDateTime(reader["ShipDate"]),
                    SubTotal = Convert.ToDecimal(reader["SubTotal"]),
                    TaxAmount = Convert.ToDecimal(reader["TaxAmt"]),
                    FreightAmount = Convert.ToDecimal(reader["Freight"]),
                    TotalDue = Convert.ToDecimal(reader["TotalDue"])
                });
            }

            return orders;
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static Customer MapCustomer(CustomerEntity entity)
    {
        return new Customer
        {
            CustomerId = entity.CustomerId,
            FirstName = entity.FirstName,
            MiddleName = entity.MiddleName,
            LastName = entity.LastName,
            CompanyName = entity.CompanyName,
            EmailAddress = entity.EmailAddress
        };
    }

    private static CustomerDto MapCustomerDto(Customer customer)
    {
        return new CustomerDto
        {
            CustomerId = customer.CustomerId,
            FirstName = customer.FirstName,
            MiddleName = customer.MiddleName,
            LastName = customer.LastName,
            FullName = customer.FullName,
            CompanyName = customer.CompanyName,
            EmailAddress = customer.EmailAddress
        };
    }
}

