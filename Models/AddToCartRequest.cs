using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cafe.Models
{
    public class AddToCartRequest
    {
          public int ItemId { get; set; }
        public int Quantity { get; set; }
    }
}