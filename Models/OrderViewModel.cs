using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cafe.Models
{
    public class OrderViewModel
    {
         public int OrderId { get; set; }
        public string CustomerName { get; set; }
        public int TableNumber { get; set; }
        public DateTime OrderTime { get; set; }
        public string OrderStatus { get; set; }
        public decimal? TotalAmount { get; set; }
        public string Items { get; set; }
    }
}