namespace AHC.Sandbox.Application.Customers.Dtos
{
    public class CustomerSummaryDto
    {
        public CustomerDto Customer { get; init; } = new();
        public int OrderCount { get; init; }
        public decimal TotalOrderValue { get; init; }
        public DateTime? MostRecentOrderDate { get; init; }
    }
}