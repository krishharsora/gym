using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using cafe.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace cafe.Controllers
{
    [RoleAuthorize("Manager")]
    public class ManagerController : Controller
    {
        private readonly CafeManagementContext _context;

        public ManagerController(CafeManagementContext context)
        {
            _context = context;
        }
        public IActionResult Dashboard()
        {
            var dashboardData = new DashboardViewModel2
            {
                TotalOrders = _context.Orders.Count(),
                PendingOrders = _context.Orders.Count(o => o.OrderStatus == "Pending"),
                TotalRevenue = _context.Payments.Where(p => p.PaymentStatus == "Paid").Sum(p => p.Amount ?? 0),
                TotalTables = _context.CafeTables.Count(),
                AvailableTables = _context.CafeTables.Count(t => t.Status == "Available"),
                TotalReservations = _context.Reservations.Count(),
                RecentOrders = _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.Table)
                    .OrderByDescending(o => o.OrderTime)
                    .Take(5)
                    .ToList(),
                RecentFeedback = _context.Feedbacks
                    .Include(f => f.Customer)
                    .OrderByDescending(f => f.CreatedAt)
                    .Take(5)
                    .ToList()
            };
            return View(dashboardData);
        }

        // Orders Monitoring
        public IActionResult Orders()
        {
            var orders = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Payments)
                .OrderByDescending(o => o.OrderTime)
                .ToList();
            return View(orders);
        }

        // Table Status
        public IActionResult TableStatus()
        {
            var tables = _context.CafeTables.ToList();
            return View(tables);
        }

        // Reservations
        public IActionResult Reservations()
        {
            var reservations = _context.Reservations
                .Include(r => r.Customer)
                .Include(r => r.Table)
                .OrderByDescending(r => r.ReservationDate)
                .ToList();
            return View(reservations);
        }

        // Sales Reports
        public IActionResult SalesReports()
        {
            var salesData = _context.Payments
                .Include(p => p.Order)
                .ThenInclude(o => o.Customer)
                .Where(p => p.PaymentStatus == "Paid")
                .OrderByDescending(p => p.PaidAt)
                .ToList();
            return View(salesData);
        }

        // Customer Feedback
        public IActionResult Feedback()
        {
            var feedback = _context.Feedbacks
                .Include(f => f.Customer)
                .Include(f => f.Order)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();
            return View(feedback);
        }

        // Staff Activity
        public IActionResult StaffActivity()
        {
            var activities = _context.ActivityLogs
                .Include(a => a.User)
                .OrderByDescending(a => a.CreatedAt)
                .ToList();
            return View(activities);
        }
    }
}