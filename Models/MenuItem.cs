using System;
using System.Collections.Generic;

namespace cafe.Models;

public partial class MenuItem
{
    public int ItemId { get; set; }

    public int? CategoryId { get; set; }

    public string? ItemName { get; set; }

    public string? Description { get; set; }

    public decimal? Price { get; set; }

    public bool IsAvailable { get; set; }= true;

    public string? ImageUrl { get; set; }

    public virtual MenuCategory? Category { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
