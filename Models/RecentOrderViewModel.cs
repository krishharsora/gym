using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cafe.Models
{
    public class RecentOrderViewModel
    {
        public int OrderId { get; set; }
    public string CustomerName { get; set; }
    public int TableNumber { get; set; }
    public string OrderStatus { get; set; }
    public DateTime OrderTime { get; set; }
    public decimal? TotalAmount { get; set; }
    }
}