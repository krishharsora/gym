using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cafe.Models
{
    public class DashboardViewModel2
    {
        public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalTables { get; set; }
    public int AvailableTables { get; set; }
    public int TotalReservations { get; set; }
    public List<Order> RecentOrders { get; set; }
    public List<Feedback> RecentFeedback { get; set; }
    }
}