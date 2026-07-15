using System.ComponentModel.DataAnnotations;

namespace AHC.Sandbox.Application.Customers.Dtos
{
    /// <summary>
    /// <para>Body for <c>POST /api/v1/customers</c>.</para>
    /// <para>
    /// The lengths mirror the real column widths mapped in <c>AdventureWorksLtDbContext</c>. They
    /// aren't belt-and-braces: EF's <c>HasMaxLength</c> is a mapping hint, not a client-side check,
    /// so without these a too-long value reaches SQL Server and comes back as an unhandled 500
    /// rather than a 400.
    /// </para>
    /// <para>
    /// <c>[Required]</c> on a <c>string</c> rejects empty and whitespace as well as null, which is
    /// the point — EF's <c>IsRequired()</c> only rejects null, so before these attributes existed a
    /// body of <c>{}</c> created a customer with an empty first name, last name, and email and
    /// returned 201.
    /// </para>
    /// </summary>
    public class CreateCustomerDto
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
