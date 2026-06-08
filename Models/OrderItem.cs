using System;
using System.Collections.Generic;

namespace cafe.Models;

public partial class OrderItem
{
    public int OrderItemId { get; set; }

    public int? OrderId { get; set; }

    public int? ItemId { get; set; }

    public int? Quantity { get; set; }

    public decimal? Price { get; set; }

    public decimal? Subtotal { get; set; }

    public virtual MenuItem? MenuItem { get; set; }

    public virtual Order? Order { get; set; }
}
