using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cafe.Models
{
    public class CartViewModel
    {
                public List<CartItemViewModel> CartItems { get; set; }
        public decimal? TotalAmount { get; set; }
    }
}