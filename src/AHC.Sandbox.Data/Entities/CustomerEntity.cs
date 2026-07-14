using System;
using System.Collections.Generic;
using System.Text;

namespace AHC.Sandbox.Data.Entities;

public class CustomerEntity
{
    public int CustomerId { get; set; }
    public string? Title { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string EmailAddress { get; set; } = string.Empty;
    public string? Phone { get; set; }
}