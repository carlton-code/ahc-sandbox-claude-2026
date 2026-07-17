using System;
using System.Collections.Generic;
using System.Text;

namespace AHC.Sandbox.Data.Entities;

public class SalesOrderHeaderEntity
{
    public int SalesOrderId { get; set; }
    public string SalesOrderNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ShipDate { get; set; }
    public byte Status { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public string? AccountNumber { get; set; }
    public int? ShipToAddressId { get; set; }
    public int? BillToAddressId { get; set; }
    public string ShipMethod { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal TaxAmt { get; set; }
    public decimal Freight { get; set; }
    public decimal TotalDue { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public List<SalesOrderDetailEntity> Details { get; set; } = new();
}
