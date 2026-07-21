using System.ComponentModel.DataAnnotations;

namespace AHC.Sandbox.Application.Addresses.Dtos
{
    /// <summary>
    /// <para>Body for <c>POST /api/v1/customers/{customerId}/addresses</c>.</para>
    /// <para>
    /// The lengths mirror the real column widths mapped in <c>AdventureWorksLtDbContext</c>, for the
    /// same reason as <c>CreateCustomerDto</c>: EF's <c>HasMaxLength</c> is a mapping hint, not a
    /// client-side check, so without these a too-long value reaches SQL Server and comes back as an
    /// unhandled 500 rather than a 400.
    /// </para>
    /// <para>
    /// <c>AddressType</c> is here — and not on <see cref="UpdateAddressDto"/> — because creating an
    /// address writes the <c>SalesLT.CustomerAddress</c> link row as well as the address itself.
    /// </para>
    /// </summary>
    public class CreateCustomerAddressDto
    {
        [Required]
        [StringLength(60)]
        public string AddressLine1 { get; init; } = string.Empty;

        [StringLength(60)]
        public string? AddressLine2 { get; init; }

        [Required]
        [StringLength(30)]
        public string City { get; init; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string StateProvince { get; init; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string CountryRegion { get; init; } = string.Empty;

        [Required]
        [StringLength(15)]
        public string PostalCode { get; init; } = string.Empty;

        /// <summary>
        /// Only ever <c>Main Office</c> or <c>Shipping</c> in this data, but not constrained to
        /// those here — the column is a plain <c>Name</c> alias type with no check constraint, so
        /// rejecting anything else would be this API inventing a rule the database doesn't have.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string AddressType { get; init; } = string.Empty;
    }
}
