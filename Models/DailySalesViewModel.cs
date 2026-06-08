using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cafe.Models
{
    public class DailySalesViewModel
    {
         public int Hour { get; set; }
    public decimal TotalAmount { get; set; }
    public int Count { get; set; }
    public List<Payment> Payments { get; set; } = new();
    }
}