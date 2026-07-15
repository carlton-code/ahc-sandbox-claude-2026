using System.ComponentModel.DataAnnotations;

namespace AHC.Sandbox.Application.Customers.Dtos
{
    /// <summary>
    /// Body for <c>PUT /api/v1/customers/{customerId}</c>. A PUT replaces the whole record, so the
    /// same fields are required here as on create — see <see cref="CreateCustomerDto"/> for why the
    /// lengths and <c>[Required]</c> markers are load-bearing rather than decorative.
    /// </summary>
    public class UpdateCustomerDto
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; init; } = string.Empty;

        [StringLength(50)]
        public string? MiddleName { get; init; }

        [Required]
        [StringLength(50)]
        public string LastName { get; init; } = string.Empty;

        [StringLength(128)]
        public string? CompanyName { get; init; }

        [Required]
        [StringLength(50)]
        [EmailAddress]
        public string EmailAddress { get; init; } = string.Empty;
    }
}
