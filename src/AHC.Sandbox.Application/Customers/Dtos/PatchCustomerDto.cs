using System.ComponentModel.DataAnnotations;

namespace AHC.Sandbox.Application.Customers.Dtos
{
    /// <summary>
    /// <para>Body for <c>PATCH /api/v1/customers/{customerId}</c>. Every field is optional.</para>
    /// <para>
    /// Deliberately **no** <c>[Required]</c>, unlike <see cref="CreateCustomerDto"/>/<see
    /// cref="UpdateCustomerDto"/>: null here means "leave this field alone", which is what makes a
    /// partial update partial. <c>[StringLength]</c> still applies, because a supplied value has to
    /// fit the column whether or not it was optional.
    /// </para>
    /// <para>
    /// Consequence worth knowing: because null means "don't change", a PATCH cannot *clear*
    /// <c>MiddleName</c> or <c>CompanyName</c> back to null — use PUT to replace the whole record.
    /// </para>
    /// </summary>
    public class PatchCustomerDto
    {
        [StringLength(50)]
        public string? FirstName { get; init; }

        [StringLength(50)]
        public string? MiddleName { get; init; }

        [StringLength(50)]
        public string? LastName { get; init; }

        [StringLength(128)]
        public string? CompanyName { get; init; }

        [StringLength(50)]
        [EmailAddress]
        public string? EmailAddress { get; init; }
    }
}
