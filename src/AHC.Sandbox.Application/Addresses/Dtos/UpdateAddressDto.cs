using System.ComponentModel.DataAnnotations;

namespace AHC.Sandbox.Application.Addresses.Dtos
{
    /// <summary>
    /// <para>Body for <c>PUT /api/v1/addresses/{addressId}</c>. Replaces the whole address.</para>
    /// <para>
    /// No <c>AddressType</c>: that belongs to the customer↔address link rather than the address, and
    /// this route doesn't name a customer. There is no <c>PatchAddressDto</c> either — an address is
    /// a small record that's replaced wholesale, so a partial update earns little. See
    /// <c>.claude/rules/application-conventions.md</c> on not over-scaffolding DTOs.
    /// </para>
    /// </summary>
    public class UpdateAddressDto
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
    }
}
