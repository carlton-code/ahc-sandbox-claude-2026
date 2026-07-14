using System;
using System.Collections.Generic;
using System.Text;

namespace AHC.Sandbox.Application.Customers.Dtos
{
    public class CustomerDto
    {
        public int CustomerId { get; init; }
        public string FirstName { get; init; } = string.Empty;
        public string? MiddleName { get; init; }
        public string LastName { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string? CompanyName { get; init; }
        public string EmailAddress { get; init; } = string.Empty;
    }
}
