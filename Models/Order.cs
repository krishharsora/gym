using System;
using System.Collections.Generic;

namespace cafe.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int? CustomerId { get; set; }

    public int? TableId { get; set; }
    public string? StripePaymentIntentId { get; set; } = null;
    public DateTime OrderTime { get; set; } = DateTime.Now;

    public string? OrderStatus { get; set; } = "Pending";

    public decimal? TotalAmount { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public string? StripeSessionId { get; set; }
    public virtual CafeTable? Table { get; set; }
}
