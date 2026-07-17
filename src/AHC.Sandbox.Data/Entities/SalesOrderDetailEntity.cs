using System;
using System.Collections.Generic;
using System.Text;

namespace AHC.Sandbox.Data.Entities;

public class SalesOrderDetailEntity
{
    public int SalesOrderId { get; set; }
    public int SalesOrderDetailId { get; set; }
    public short OrderQty { get; set; }
    public int ProductId { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal UnitPriceDiscount { get; set; }
    public decimal LineTotal { get; set; }
}
