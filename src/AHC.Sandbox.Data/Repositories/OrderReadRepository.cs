using AHC.Sandbox.Application.Orders.Interfaces;
using AHC.Sandbox.Data.Context;
using AHC.Sandbox.Data.Entities;
using AHC.Sandbox.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AHC.Sandbox.Data.Repositories;

public class OrderReadRepository : IOrderReadRepository
{
    private readonly AdventureWorksLtDbContext _dbContext;

    public OrderReadRepository(AdventureWorksLtDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<Order>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.SalesOrderHeaders
            .AsNoTracking()
            // Newest first. Every seed order shares a single OrderDate, so the SalesOrderId
            // tiebreaker is what actually makes this ordering deterministic today.
            .OrderByDescending(o => o.OrderDate)
            .ThenBy(o => o.SalesOrderId)
            .ToArrayAsync(cancellationToken);

        return entities
            .Select(e => ToDomain(e, includeLines: false))
            .ToArray();
    }

    public async Task<Order?> GetByIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.SalesOrderHeaders
            .AsNoTracking()
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.SalesOrderId == orderId, cancellationToken);

        return entity is null ? null : ToDomain(entity, includeLines: true);
    }

    private static Order ToDomain(SalesOrderHeaderEntity entity, bool includeLines)
    {
        return new Order
        {
            OrderId = entity.SalesOrderId,
            OrderNumber = entity.SalesOrderNumber,
            CustomerId = entity.CustomerId,
            OrderDate = entity.OrderDate,
            DueDate = entity.DueDate,
            ShipDate = entity.ShipDate,
            Status = entity.Status,
            PurchaseOrderNumber = entity.PurchaseOrderNumber,
            AccountNumber = entity.AccountNumber,
            ShipToAddressId = entity.ShipToAddressId,
            BillToAddressId = entity.BillToAddressId,
            ShipMethod = entity.ShipMethod,
            SubTotal = entity.SubTotal,
            TaxAmount = entity.TaxAmt,
            FreightAmount = entity.Freight,
            TotalDue = entity.TotalDue,
            TrackingNumber = entity.TrackingNumber,
            Comment = entity.Comment,
            Lines = includeLines
                ? entity.Details
                    .OrderBy(d => d.SalesOrderDetailId)
                    .Select(ToDomainLine)
                    .ToArray()
                : Array.Empty<OrderLine>()
        };
    }

    private static OrderLine ToDomainLine(SalesOrderDetailEntity entity)
    {
        return new OrderLine
        {
            OrderLineId = entity.SalesOrderDetailId,
            ProductId = entity.ProductId,
            OrderQty = entity.OrderQty,
            UnitPrice = entity.UnitPrice,
            UnitPriceDiscount = entity.UnitPriceDiscount,
            LineTotal = entity.LineTotal
        };
    }
}
