using System;
using System.Collections.Generic;
using System.Text;

namespace AHC.Sandbox.Application.Orders.Dtos
{
    public class OrderWithLinesDto
    {
        public int OrderId { get; init; }
        public string OrderNumber { get; init; } = string.Empty;
        public int CustomerId { get; init; }
        public DateTime OrderDate { get; init; }
        public DateTime DueDate { get; init; }
        public DateTime? ShipDate { get; init; }
        public byte Status { get; init; }
        public string? PurchaseOrderNumber { get; init; }
        public string? AccountNumber { get; init; }
        public int? ShipToAddressId { get; init; }
        public int? BillToAddressId { get; init; }
        public string ShipMethod { get; init; } = string.Empty;
        public decimal SubTotal { get; init; }
        public decimal TaxAmount { get; init; }
        public decimal FreightAmount { get; init; }
        public decimal TotalDue { get; init; }
        public string TrackingNumber { get; init; } = string.Empty;
        public string? Comment { get; init; }
        public bool IsShipped { get; init; }
        public IReadOnlyCollection<OrderLineDto> Lines { get; init; } = Array.Empty<OrderLineDto>();
    }
}
