using System;
using System.Collections.Generic;

namespace cafe.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int? OrderId { get; set; }

    public string? PaymentMethod { get; set; } = "Stripe";

    public decimal? Amount { get; set; }
 public string? StripeSessionId { get; set; }        // ✅ Add

    public string PaymentStatus { get; set; } = "Paid";

    public DateTime? PaidAt { get; set; }

    public virtual Order? Order { get; set; }
}
