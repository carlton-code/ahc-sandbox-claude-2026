namespace AHC.Sandbox.Application.Customers.Dtos
{
    public class UpdateCustomerDto
    {
        public string FirstName { get; init; } = string.Empty;
        public string? MiddleName { get; init; }
        public string LastName { get; init; } = string.Empty;
        public string? CompanyName { get; init; }
        public string EmailAddress { get; init; } = string.Empty;
    }
}