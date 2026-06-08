using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cafe.Models
{
    public class OrderItemViewModel
    {
         public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string ItemName { get; set; }
        public string Description { get; set; }
    }
}