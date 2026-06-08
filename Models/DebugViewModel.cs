using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stripe.Checkout;  // ✅ For Session type
namespace cafe.Models
{
    public class DebugViewModel
    {
         public string SessionId { get; set; }
    public int CustomerId { get; set; }
    public string CartKey { get; set; }
    public string CartJson { get; set; }
    public List<CartItem> CartItems { get; set; } = new();
    public List<string> AllSessionKeys { get; set; } = new();
    public List<Order> RecentOrders { get; set; } = new();
    public Session StripeSession { get; set; }
    public string Error { get; set; }
    }
}