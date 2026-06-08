using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cafe.Models
{
    public class CartItemViewModel
    {
         public int ItemId { get; set; }
        public MenuItem MenuItem { get; set; }
        public int Quantity { get; set; }
    }
}