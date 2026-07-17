using AHC.Sandbox.Application.Orders.Dtos;
using AHC.Sandbox.Application.Orders.Interfaces;
using AHC.Sandbox.Domain.Entities;

namespace AHC.Sandbox.Application.Orders.Services
{

    public class OrderService : IOrderService
    {
        private readonly IOrderReadRepository _orderReadRepository;

        public OrderService(IOrderReadRepository orderReadRepository)
        {
            _orderReadRepository = orderReadRepository;
        }

        public async Task<IReadOnlyCollection<OrderDto>> GetOrdersAsync(int? customerId = null, CancellationToken cancellationToken = default)
        {
            var orders = await _orderReadRepository.GetAllAsync(customerId, cancellationToken);

            return orders
                .Select(MapOrder)
                .ToArray();
        }

        public async Task<OrderWithLinesDto?> GetOrderByIdAsync(int orderId, CancellationToken cancellationToken = default)
        {
            var order = await _orderReadRepository.GetByIdAsync(orderId, cancellationToken);

            return order is null ? null : MapOrderWithLines(order);
        }

        private static OrderDto MapOrder(Order order)
        {
            return new OrderDto
            {
                OrderId = order.OrderId,
                OrderNumber = order.OrderNumber,
                CustomerId = order.CustomerId,
                OrderDate = order.OrderDate,
                DueDate = order.DueDate,
                ShipDate = order.ShipDate,
                Status = order.Status,
                PurchaseOrderNumber = order.PurchaseOrderNumber,
                AccountNumber = order.AccountNumber,
                ShipToAddressId = order.ShipToAddressId,
                BillToAddressId = order.BillToAddressId,
                ShipMethod = order.ShipMethod,
                SubTotal = order.SubTotal,
                TaxAmount = order.TaxAmount,
                FreightAmount = order.FreightAmount,
                TotalDue = order.TotalDue,
                TrackingNumber = order.TrackingNumber,
                Comment = order.Comment,
                IsShipped = order.IsShipped
            };
        }

        private static OrderWithLinesDto MapOrderWithLines(Order order)
        {
            return new OrderWithLinesDto
            {
                OrderId = order.OrderId,
                OrderNumber = order.OrderNumber,
                CustomerId = order.CustomerId,
                OrderDate = order.OrderDate,
                DueDate = order.DueDate,
                ShipDate = order.ShipDate,
                Status = order.Status,
                PurchaseOrderNumber = order.PurchaseOrderNumber,
                AccountNumber = order.AccountNumber,
                ShipToAddressId = order.ShipToAddressId,
                BillToAddressId = order.BillToAddressId,
                ShipMethod = order.ShipMethod,
                SubTotal = order.SubTotal,
                TaxAmount = order.TaxAmount,
                FreightAmount = order.FreightAmount,
                TotalDue = order.TotalDue,
                TrackingNumber = order.TrackingNumber,
                Comment = order.Comment,
                IsShipped = order.IsShipped,
                Lines = order.Lines
                    .Select(MapLine)
                    .ToArray()
            };
        }

        private static OrderLineDto MapLine(OrderLine line)
        {
            return new OrderLineDto
            {
                OrderLineId = line.OrderLineId,
                ProductId = line.ProductId,
                OrderQty = line.OrderQty,
                UnitPrice = line.UnitPrice,
                UnitPriceDiscount = line.UnitPriceDiscount,
                LineTotal = line.LineTotal
            };
        }
    }

}
