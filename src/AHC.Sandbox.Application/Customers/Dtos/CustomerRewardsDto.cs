namespace AHC.Sandbox.Application.Customers.Dtos
{
    public class CustomerRewardsDto
    {
        public int CustomerId { get; init; }

        // Null across all three tier fields means the customer exists but has no rewards tier
        // assigned — a third of customers today. RewardsLevelId is nullable rather than int
        // because Gold is RewardsLevelId 0, which default(int) would be indistinguishable from.
        public int? RewardsLevelId { get; init; }
        public string? RewardsLevelName { get; init; }
        public decimal? DiscountPercent { get; init; }
    }
}
