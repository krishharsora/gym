using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cafe.Models
{
    public class DashboardViewModel
    {
         public KitchenStats Stats { get; set; }
    public List<RecentOrderViewModel> RecentOrders { get; set; }
    }
}