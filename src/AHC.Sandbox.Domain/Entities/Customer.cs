using System;
using System.Collections.Generic;
using System.Text;

namespace AHC.Sandbox.Domain.Entities
{
    public class Customer
    {
        public int CustomerId { get; init; }
        public string FirstName { get; init; } = string.Empty;
        public string? MiddleName { get; init; }
        public string LastName { get; init; } = string.Empty;
        public string? CompanyName { get; init; }
        public string EmailAddress { get; init; } = string.Empty;

        public string FullName =>
            string.IsNullOrWhiteSpace(MiddleName)
                ? $"{FirstName} {LastName}"
                : $"{FirstName} {MiddleName} {LastName}";
    }

}
