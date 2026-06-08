using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cafe.Models
{
    public class CustomerDashboardViewModel
    {
                public List<Order> ActiveOrders { get; set; } = new List<Order>();

        public List<Reservation> UpcomingReservations { get; set; } = new List<Reservation>();

        public int CartCount { get; set; }
    }
}