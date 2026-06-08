using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using cafe.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace cafe.Controllers
{
    [RoleAuthorize("Cashier")]
    public class CashierController : Controller
    {
        private readonly CafeManagementContext _context;
        private readonly ILogger<CashierController> _logger;
        public CashierController(CafeManagementContext context, ILogger<CashierController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Cashier/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var today = DateTime.Today;

            var stats = new
            {
                PendingBills = await _context.Payments
                    .Where(p => p.PaymentStatus == "Pending")
                    .CountAsync(),
                TodaySales = await _context.Payments
                    .Where(p => p.PaymentStatus == "Paid" &&
                               p.PaidAt.HasValue &&
                               p.PaidAt.Value.Date == today &&
                               p.Amount.HasValue)
                    .SumAsync(p => p.Amount!.Value),
                TotalTables = await _context.CafeTables.CountAsync(),
                OccupiedTables = await _context.CafeTables
                    .CountAsync(t => t.Status != "Available")
            };

            ViewBag.Stats = stats;
            return View();
        }

        // GET: /Cashier/PendingBills
        public async Task<IActionResult> PendingBills()
        {
            var pendingBills = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .Where(o => o.OrderStatus == "Ready" || o.OrderStatus == "Served")
                .OrderByDescending(o => o.OrderTime)
                .ToListAsync();

            return View(pendingBills);
        }

        // GET: /Cashier/Bills/Pending (Workflow Step 1)
        public async Task<IActionResult> BillsPending()
        {
            var readyOrders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .Where(o => o.OrderStatus == "Ready")
                .OrderBy(o => o.OrderTime)
                .ToListAsync();

            return View(readyOrders);
        }

        // GET: /Cashier/Bills/Generate (Workflow Step 2)
        // GET: /Cashier/BillsGenerate?orderId=1
        public async Task<IActionResult> BillsGenerate(int orderId)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.Table)
                    .Include(o => o.OrderItems)!
                        .ThenInclude(oi => oi.MenuItem)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                {
                    TempData["Error"] = "Order not found";
                    return RedirectToAction("BillsPending");
                }

                if (order.OrderStatus != "Ready")
                {
                    TempData["Error"] = $"Order #{orderId} is {order.OrderStatus}, not ready for billing";
                    return RedirectToAction("BillsPending");
                }

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BillsGenerate error: OrderId={OrderId}", orderId);
                TempData["Error"] = "Error loading bill";
                return RedirectToAction("BillsPending");
            }
        }

        // POST: Generate Bill (Workflow Step 2)
        // 🔥 FIXED: GenerateBill POST - Works with your view's JavaScript
        [HttpPost]
        public async Task<IActionResult> GenerateBill([FromBody] int orderId)
        {
            try
            {
                _logger.LogInformation("🔄 GenerateBill: OrderId={OrderId}", orderId);

                // Get order
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                // Check existing payment
                var existingPayment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.OrderId == orderId);

                if (existingPayment != null)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Bill already generated",
                        paymentId = existingPayment.PaymentId
                    });
                }

                // Create payment
                var payment = new Payment
                {
                    OrderId = orderId,
                    Amount = order.TotalAmount ?? 0m,
                    PaymentMethod = "Cash",
                    PaymentStatus = "Pending"
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Payment created: {PaymentId} for Order {OrderId}",
                    payment.PaymentId, orderId);

                return Json(new
                {
                    success = true,
                    paymentId = payment.PaymentId,
                    orderId = orderId,
                    message = "Bill generated successfully!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GenerateBill FAILED: {OrderId}", orderId);
                return Json(new { success = false, message = "Server error" });
            }
        }
        // GET: /Cashier/Payments
        public async Task<IActionResult> Payments()
        {
            var payments = await _context.Payments
                .Include(p => p.Order)
                    .ThenInclude(o => o.Customer)
                .Include(p => p.Order.Table)
                .Where(p => p.PaymentStatus == "Pending")
                .OrderByDescending(p => p.Order.OrderTime)
                .ToListAsync();

            return View(payments);
        }

        // POST: Process Payment (Workflow Step 3)
        [HttpPost]
        public async Task<IActionResult> ProcessPayment(int paymentId, string paymentMethod)
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                    .ThenInclude(o => o.Table)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            if (payment == null || payment.PaymentStatus != "Pending")
            {
                return Json(new { success = false, message = "Payment not found or already processed" });
            }

            // Update Payment Status: PAID
            payment.PaymentMethod = paymentMethod;
            payment.PaymentStatus = "Paid";
            payment.PaidAt = DateTime.Now;

            // Update Order Status: COMPLETED
            payment.Order.OrderStatus = "Completed";

            // Update Table Status: AVAILABLE
            if (payment.Order.Table != null)
            {
                payment.Order.Table.Status = "Available";
            }

            await _context.SaveChangesAsync();

            // Log activity
            // await LogActivity("Processed payment", "Payments", paymentId);

            TempData["Success"] = "Payment processed successfully!";
            return Json(new { success = true });
        }

        // GET: /Cashier/CompletedPayments
        public async Task<IActionResult> CompletedPayments(DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Payments
                .Include(p => p.Order)
                    .ThenInclude(o => o.Customer)
                .Where(p => p.PaymentStatus == "Paid");

            if (fromDate.HasValue)
                query = query.Where(p => p.PaidAt >= fromDate);
            if (toDate.HasValue)
                query = query.Where(p => p.PaidAt <= toDate.Value.AddDays(1));

            var payments = await query.OrderByDescending(p => p.PaidAt).ToListAsync();

            // ✅ FIX: Create cycle-safe DTO for JSON serialization
            var safePayments = payments.Select(p => new
            {
                PaymentId = p.PaymentId,
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod,
                PaidAt = p.PaidAt,
                OrderId = p.Order?.OrderId,
                CustomerName = p.Order?.Customer?.Name,
                TableNumber = p.Order?.Table?.TableNumber  // Fixed property name
            }).ToList();

            ViewBag.SafePaymentsJson = JsonSerializer.Serialize(safePayments, new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return View(payments); // Original model for Razor view
        }

        // GET: /Cashier/DailySales
        // FIXED DailySales Action
        public async Task<IActionResult> DailySales(DateTime? date)
        {
            date ??= DateTime.Today;

            // Create a proper view model instead of anonymous type
            var salesData = await _context.Payments
                .Include(p => p.Order)
                    .ThenInclude(o => o.Customer)
                .Where(p => p.PaymentStatus == "Paid" &&
                           p.PaidAt.HasValue &&
                           p.PaidAt.Value.Date == date.Value.Date &&
                           p.Amount.HasValue)
                .GroupBy(p => p.PaidAt.Value.Hour)
                .Select(g => new DailySalesViewModel
                {
                    Hour = g.Key,
                    TotalAmount = g.Sum(p => p.Amount!.Value),
                    Count = g.Count(),
                    Payments = g.ToList()
                })
                .OrderBy(x => x.Hour)
                .ToListAsync();

            ViewBag.SelectedDate = date;
            return View(salesData);
        }

        private async Task LogActivity(string action, string entity, int entityId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var log = new ActivityLog
            {
                UserId = userId,
                Action = action,
                Entity = entity,
                CreatedAt = DateTime.Now
            };
            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}