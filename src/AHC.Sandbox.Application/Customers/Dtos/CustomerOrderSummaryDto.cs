namespace AHC.Sandbox.Application.Customers.Dtos
{
    public class CustomerOrderSummaryDto
    {
        public int CustomerId { get; init; }
        public int OrderCount { get; init; }
        public decimal SubTotal { get; init; }
        public decimal TaxAmount { get; init; }
        public decimal FreightAmount { get; init; }
        public decimal TotalDue { get; init; }
        public DateTime? FirstOrderDate { get; init; }
        public DateTime? MostRecentOrderDate { get; init; }
    }
}