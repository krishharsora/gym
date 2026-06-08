using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cafe.Models
{
    public class PlaceOrderViewModel
    {
          public int? TableId { get; set; }
        public string PaymentIntentId { get; set; }
    }
}