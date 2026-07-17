using System;
using System.Collections.Generic;
using System.Text;

namespace AHC.Sandbox.Application.Orders.Dtos
{
    public class OrderLineDto
    {
        public int OrderLineId { get; init; }
        public int ProductId { get; init; }
        public short OrderQty { get; init; }
        public decimal UnitPrice { get; init; }
        public decimal UnitPriceDiscount { get; init; }
        public decimal LineTotal { get; init; }
    }
}
