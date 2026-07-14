namespace AHC.Sandbox.Application.Customers.Dtos
{
    public class CustomerOrderDto
    {
        public int OrderId { get; init; }
        public int CustomerId { get; init; }
        public string OrderNumber { get; init; } = string.Empty;
        public DateTime OrderDate { get; init; }
        public DateTime? ShipDate { get; init; }
        public decimal SubTotal { get; init; }
        public decimal TaxAmount { get; init; }
        public decimal FreightAmount { get; init; }
        public decimal TotalDue { get; init; }
    }
}