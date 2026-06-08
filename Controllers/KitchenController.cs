using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using cafe.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace cafe.Controllers
{
    [RoleAuthorize("Kitchen")]
    public class KitchenController : Controller
    {
        private readonly CafeManagementContext _context;

        public KitchenController(CafeManagementContext context)
        {
            _context = context;
        }

        // GET: /Kitchen/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var stats = new KitchenStats
            {
                PendingCount = await _context.Orders.CountAsync(o => o.OrderStatus == "Pending"),
                PreparingCount = await _context.Orders.CountAsync(o => o.OrderStatus == "Preparing"),
                ReadyCount = await _context.Orders.CountAsync(o => o.OrderStatus == "Ready"),
                ServedCount = await _context.Orders.CountAsync(o => o.OrderStatus == "Served"),
                CompletedCount = await _context.Orders.CountAsync(o => o.OrderStatus == "Completed"),
                TotalToday = await _context.Orders
                    .Where(o => o.OrderTime.Date == DateTime.Today)
                    .CountAsync(),
                TotalRevenueToday = await _context.Orders
                    .Where(o => o.OrderTime.Date == DateTime.Today && o.TotalAmount.HasValue)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0
            };

            // Recent orders for dashboard
            var recentOrders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .OrderByDescending(o => o.OrderTime)
                .Take(5)
                .Select(o => new RecentOrderViewModel
                {
                    OrderId = o.OrderId,
                    CustomerName = o.Customer != null ? o.Customer.Name : "Guest",
                    TableNumber = o.Table != null ? o.Table.TableNumber : 0,
                    OrderStatus = o.OrderStatus,
                    OrderTime = o.OrderTime,
                    TotalAmount = o.TotalAmount
                })
                .ToListAsync();

            var dashboardViewModel = new DashboardViewModel
            {
                Stats = stats,
                RecentOrders = recentOrders
            };

            return View(dashboardViewModel);
        }

        // GET: /Kitchen/NewOrders
        public async Task<IActionResult> NewOrders()
        {
            var orders = await GetOrdersByStatus("Pending");
            return View(orders);
        }

        // GET: /Kitchen/Preparing
        public async Task<IActionResult> Preparing()
        {
            var orders = await GetOrdersByStatus("Preparing");
            return View(orders);
        }

        // GET: /Kitchen/Ready
        public async Task<IActionResult> Ready()
        {
            var orders = await GetOrdersByStatus("Ready");
            return View(orders);
        }

        // GET: /Kitchen/Completed
        public async Task<IActionResult> Completed()
        {
            var orders = await _context.Orders
                .Where(o => o.OrderStatus == "Served" || o.OrderStatus == "Completed")
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .Select(o => new OrderViewModel
                {
                    OrderId = o.OrderId,
                    CustomerName = o.Customer != null ? o.Customer.Name : "Guest",
                    TableNumber = o.Table != null ? o.Table.TableNumber : 0,
                    OrderTime = o.OrderTime,
                    OrderStatus = o.OrderStatus,
                    TotalAmount = o.TotalAmount,
                    Items = string.Join(", ", o.OrderItems.Select(oi => oi.MenuItem.ItemName))
                })
                .OrderByDescending(o => o.OrderTime)
                .ToListAsync();
            return View(orders);
        }

        // GET: /Kitchen/History
        public async Task<IActionResult> History(int page = 1, int pageSize = 10)
        {
            var orders = await _context.Orders
                .Where(o => o.OrderStatus == "Served" || o.OrderStatus == "Completed")
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .OrderByDescending(o => o.OrderTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OrderViewModel
                {
                    OrderId = o.OrderId,
                    CustomerName = o.Customer != null ? o.Customer.Name : "Guest",
                    TableNumber = o.Table != null ? o.Table.TableNumber : 0,
                    OrderTime = o.OrderTime,
                    OrderStatus = o.OrderStatus,
                    TotalAmount = o.TotalAmount,
                    Items = string.Join(", ", o.OrderItems.Select(oi => oi.MenuItem.ItemName))
                })
                .ToListAsync();

            var totalCount = await _context.Orders
                .CountAsync(o => o.OrderStatus == "Served" || o.OrderStatus == "Completed");

            var pagedModel = new PagedOrderViewModel
            {
                Orders = orders,
                TotalCount = totalCount,
                CurrentPage = page,
                PageSize = pageSize
            };

            return View(pagedModel);
        }

        // POST: Update order status
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, string newStatus)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null) return NotFound();

                // Validate status transition
                var validTransitions = new Dictionary<string, string[]>
                {
                    ["Pending"] = new[] { "Preparing" },
                    ["Preparing"] = new[] { "Ready" },
                    ["Ready"] = new[] { "Served", "Completed" }
                };

                if (!validTransitions.ContainsKey(order.OrderStatus) ||
                    !validTransitions[order.OrderStatus].Contains(newStatus))
                {
                    TempData["Error"] = "Invalid status transition";
                    return RedirectToAction("NewOrders");
                }

                // Update status
                order.OrderStatus = newStatus;
                await _context.SaveChangesAsync();

                // Log activity
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var log = new ActivityLog
                {
                    UserId = currentUserId,
                    Action = $"Updated order {orderId} status to {newStatus}",
                    Entity = "Orders"
                };
                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();

                // Notify Cashier when order is Ready
                if (newStatus == "Ready")
                {
                    await NotifyCashier(orderId);
                }

                TempData["Success"] = $"Order status updated to {newStatus}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to update order status: " + ex.Message;
            }

            return RedirectToAction(GetRedirectAction(newStatus));
        }

        // GET: /Kitchen/OrderDetails/{id}
        public async Task<IActionResult> OrderDetails(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null) return NotFound();

            var viewModel = new OrderDetailsViewModel
            {
                OrderId = order.OrderId,
                CustomerName = order.Customer?.Name ?? "Guest",
                TableNumber = order.Table?.TableNumber ?? 0,
                OrderTime = order.OrderTime,
                OrderStatus = order.OrderStatus,
                TotalAmount = order.TotalAmount,
                Items = order.OrderItems.Select(oi => new OrderItemViewModel
                {
                    Quantity = oi.Quantity ?? 0,
                    Price = oi.Price ?? 0,
                    ItemName = oi.MenuItem.ItemName,
                    Description = oi.MenuItem.Description
                }).ToList()
            };

            return View(viewModel);
        }

        private async Task<List<OrderViewModel>> GetOrdersByStatus(string status)
        {
            return await _context.Orders
                .Where(o => o.OrderStatus == status)
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .OrderByDescending(o => o.OrderTime)
                .Select(o => new OrderViewModel
                {
                    OrderId = o.OrderId,
                    CustomerName = o.Customer != null ? o.Customer.Name : "Guest",
                    TableNumber = o.Table != null ? o.Table.TableNumber : 0,
                    OrderTime = o.OrderTime,
                    OrderStatus = o.OrderStatus,
                    TotalAmount = o.TotalAmount,
                    Items = string.Join(", ", o.OrderItems.Select(oi => oi.MenuItem.ItemName))
                })
                .ToListAsync();
        }

        private async Task NotifyCashier(int orderId)
        {
            // SignalR notification simulation
            var log = new ActivityLog
            {
                UserId = 6, // Cashier user ID
                Action = $"KITCHEN: Order {orderId} is READY for pickup!",
                Entity = "Orders"
            };
            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        private string GetRedirectAction(string status)
        {
            return status switch
            {
                "Preparing" => "Preparing",
                "Ready" => "Ready",
                "Served" or "Completed" => "Completed",
                _ => "NewOrders"
            };
        }
    }
}