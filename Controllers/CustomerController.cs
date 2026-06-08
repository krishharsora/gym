using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stripe;
using System.Text.Json;
// ✅ REQUIRED
using Stripe.Checkout;
using cafe.Models;
using System.Globalization; // ✅ ADD THIS LINE
using System.Text; // ✅ For Encoding.UTF8
using Microsoft.AspNetCore.Authentication.Cookies;
namespace cafe.Controllers
{
    // Fixed: Use standard Authorize, handle roles in filter
    [RoleAuthorize("Customer")]
    public class CustomerController : Controller
    {
        private readonly CafeManagementContext _context;
        private readonly ILogger<CustomerController> _logger;

        private const string STRIPE_SECRET_KEY = "sk_test_51TBctuQuyAg2bqwwUv1pvz9bVqbqjGfc31kb0AJIwVQVQVo4xyKZLcc9oObdXOj3KiVUAPCmZd3AttSIGcBhgJQE00tMQaOiU9";
        private const string STRIPE_PUBLISHABLE_KEY = "pk_test_51TBctuQuyAg2bqwwsEo7CwidoqM4gv47lngP5FJ8ZFLxfWGHalA9HCMjuBR0jNYUL5DSItKAoxey7JKw3Lt7VGxO00g0Y5ydkA";





        public CustomerController(CafeManagementContext context, ILogger<CustomerController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            StripeConfiguration.ApiKey = STRIPE_SECRET_KEY;

        }

        // Dashboard ✅ FIXED
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var customerId = GetOrCreateCustomerId();
                var model = new CustomerDashboardViewModel
                {
                    ActiveOrders = await _context.Orders
                        .Where(o => o.CustomerId == customerId && o.OrderStatus != "Completed")
                        .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.MenuItem) // FIXED: MenuItem not MenuItems
                        .Include(o => o.Table)
                        .OrderByDescending(o => o.OrderTime)
                        .Take(5)
                        .ToListAsync(),

                    UpcomingReservations = await _context.Reservations
.Where(r => r.CustomerId == customerId && r.ReservationDate >= DateOnly.FromDateTime(DateTime.Today)).Include(r => r.Table)
                        .OrderBy(r => r.ReservationDate)
                        .Take(3)
                        .ToListAsync(),

                    CartCount = GetCartCount()
                };
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer dashboard");
                return View(new CustomerDashboardViewModel());
            }
        }

        // Menu ✅ FIXED
        public async Task<IActionResult> Menu(string category = null)
        {
            try
            {
                var query = _context.MenuItems
                    .Include(m => m.Category)
                    .Where(m => m.IsAvailable == true);

                if (!string.IsNullOrEmpty(category))
                    query = query.Where(m => m.Category.CategoryName == category);

                var model = new MenuViewModel
                {
                    MenuItems = await query.OrderBy(m => m.CategoryId).ThenBy(m => m.ItemName).ToListAsync(),
                    Categories = await _context.MenuCategories.OrderBy(c => c.CategoryName).ToListAsync(),
                    CartCount = GetCartCount()
                };
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading menu");
                return View(new MenuViewModel());
            }
        }



        // Cart page ✅ FIXED
        public IActionResult Cart()
        {
            try
            {
                var cart = GetCartFromSession();
                var itemIds = cart.Select(c => c.ItemId).ToList();

                var menuItems = _context.MenuItems
                    .Include(m => m.Category)
                    .Where(m => itemIds.Contains(m.ItemId))
                    .ToList();

                var model = new CartViewModel
                {
                    CartItems = cart.Select(c => new CartItemViewModel
                    {
                        ItemId = c.ItemId,
                        MenuItem = menuItems.FirstOrDefault(m => m.ItemId == c.ItemId),
                        Quantity = c.Quantity
                    }).Where(x => x.MenuItem != null).ToList(),

                    TotalAmount = cart.Sum(c =>
  {
      var item = menuItems.FirstOrDefault(m => m.ItemId == c.ItemId);
      return item == null ? 0 : c.Quantity * item.Price;
  })
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cart");
                return View(new CartViewModel());
            }
        }

        // Stripe Payment Intent ✅ FIXED
        [HttpPost]
        public async Task<IActionResult> CreatePaymentIntent(decimal amount)
        {
            try
            {
                if (amount <= 0)
                    return Json(new { success = false, message = "Invalid amount" });

                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(amount * 100), // paise
                    Currency = "inr",
                    PaymentMethodTypes = new List<string> { "card" }
                };

                var service = new PaymentIntentService();
                var intent = await service.CreateAsync(options);

                return Json(new
                {
                    success = true,
                    clientSecret = intent.ClientSecret,
                    publishableKey = STRIPE_PUBLISHABLE_KEY
                });
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error");
                return Json(new { success = false, message = "Payment setup failed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment intent error");
                return Json(new { success = false, message = "Server error" });
            }
        }

        // Place Order ✅ FIXED
        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderViewModel model)
        {
            try
            {
                var customerId = GetOrCreateCustomerId();
                var cart = GetCartFromSession(); // ✅ FIXED

                if (!cart.Any())
                    return Json(new { success = false, message = "Cart is empty" });

                var total = 0m;
                foreach (var c in cart)
                {
                    var item = await _context.MenuItems.FindAsync(c.ItemId); // ✅ ASYNC
                    if (item != null)
                        total += c.Quantity * (item.Price ?? 0);
                }

                var order = new Order
                {
                    CustomerId = customerId,
                    TableId = model?.TableId,
                    TotalAmount = total,
                    StripePaymentIntentId = model?.PaymentIntentId,
                    OrderStatus = "Pending", // ✅ EXPLICIT
                    OrderTime = DateTime.Now  // ✅ EXPLICIT
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Add order items...
                foreach (var item in cart)
                {
                    var menuItem = await _context.MenuItems.FindAsync(item.ItemId);
                    if (menuItem != null)
                    {
                        _context.OrderItems.Add(new OrderItem
                        {
                            OrderId = order.OrderId,
                            ItemId = item.ItemId,
                            Quantity = item.Quantity,
                            Price = menuItem.Price,
                            Subtotal = item.Quantity * menuItem.Price
                        });
                    }
                }

                await _context.SaveChangesAsync(); // ✅ SINGLE SAVE
                SaveCartToSession(new List<CartItem>()); // ✅ CLEAR CART

                _logger.LogInformation("✅ Order {OrderId} CREATED - Status: {Status}", order.OrderId, order.OrderStatus);
                return Json(new { success = true, orderId = order.OrderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PlaceOrder FAILED");
                return Json(new { success = false, message = "Order failed" });
            }
        }

        // Orders ✅ FIXED
        public async Task<IActionResult> Orders()
        {
            try
            {
                var customerId = GetOrCreateCustomerId();
                var orders = await _context.Orders
                    .Where(o => o.CustomerId == customerId)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem) // FIXED: MenuItem
                    .ThenInclude(mi => mi.Category)
                    .Include(o => o.Table)
                    .OrderByDescending(o => o.OrderTime)
                    .ToListAsync();
                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders");
                return View(new List<Order>());
            }
        }

        // Reservation ✅ FIXED
        public async Task<IActionResult> Reservation()
        {
            try
            {
                ViewBag.Tables = await _context.CafeTables
                    .Where(t => t.Status == "Available")
                    .ToListAsync();
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reservation page");
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReserveTable([FromBody] Reservation model)
        {
            try
            {
                model.CustomerId = GetOrCreateCustomerId();
                model.Status = "Pending";

                // Check invalid date
                if (model.ReservationDate.HasValue &&
                    model.ReservationDate.Value < DateOnly.FromDateTime(DateTime.Today))
                {
                    return Json(new { success = false, message = "Invalid date" });
                }

                // 🔹 Check if table already reserved
                var exists = await _context.Reservations
            .AnyAsync(r => r.TableId == model.TableId &&
                           r.ReservationDate == model.ReservationDate &&
                           r.ReservationTime == model.ReservationTime);
                if (exists)
                    return Json(new { success = false, message = "Table already reserved" });

                // Save reservation
                _context.Reservations.Add(model);

                var table = await _context.CafeTables.FindAsync(model.TableId);
                if (table != null)
                    table.Status = "Reserved";

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Table reserved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reservation error");
                return Json(new { success = false });
            }
        }

        // Add these actions to your CustomerController

        // ✅ NEW: Feedback Page
        public async Task<IActionResult> Feedback()
        {
            try
            {
                var customerId = GetOrCreateCustomerId();
                var model = new CustomerFeedbackViewModel
                {
                    RecentFeedbacks = await _context.Feedbacks
                        .Where(f => f.CustomerId == customerId)
                        .OrderByDescending(f => f.CreatedAt)
                        .Take(5)
                        .ToListAsync(),
                    CartCount = GetCartCount()
                };
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading feedback page");
                return View(new CustomerFeedbackViewModel());
            }
        }
        // In CustomerController or create a ViewHelper class
        public static class ViewHelper
        {
            public static string FormatDateTime(DateTime dateTime)
            {
                return dateTime.ToString("MMM dd, yyyy hh:mm tt", CultureInfo.InvariantCulture);
            }
        }
        // ✅ FIXED: Updated AddToCart with better error handling & async
        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                if (request?.ItemId <= 0 || request.Quantity <= 0)
                    return Json(new { success = false, message = "Invalid request data" });

                // Load item
                var item = await _context.MenuItems.FirstOrDefaultAsync(m => m.ItemId == request.ItemId);
                if (item == null)
                    return Json(new { success = false, message = "Item not found" });

                if (!item.IsAvailable)
                    return Json(new { success = false, message = "Item is currently unavailable" });

                var cart = GetCartFromSession();
                var existing = cart.FirstOrDefault(x => x.ItemId == request.ItemId);

                if (existing != null)
                {
                    existing.Quantity += request.Quantity;
                }
                else
                {
                    cart.Add(new CartItem { ItemId = request.ItemId, Quantity = request.Quantity });
                }

                SaveCartToSession(cart);
                var totalItems = cart.Sum(x => x.Quantity);

                return Json(new
                {
                    success = true,
                    cartCount = totalItems,
                    message = $"{request.Quantity} x {item.ItemName} added to cart!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to cart: {@Request}", request);
                return Json(new { success = false, message = "Server error. Please try again." });
            }
        }
        [HttpPost]
        public IActionResult UpdateCartItem([FromBody] UpdateCartItemRequest? request)
        {
            try
            {
                if (request == null || request.ItemId <= 0)
                    return Json(new { success = false, message = "Invalid request" });

                var cart = GetCartFromSession();
                var cartItem = cart.FirstOrDefault(x => x.ItemId == request.ItemId);

                if (cartItem == null)
                    return Json(new { success = false, message = "Item not found in cart" });

                if (request.Quantity <= 0)
                {
                    cart.Remove(cartItem);
                }
                else
                {
                    cartItem.Quantity = request.Quantity;
                }

                SaveCartToSession(cart);

                return Json(new
                {
                    success = true,
                    message = $"Cart updated successfully",
                    cartCount = cart.Sum(x => x.Quantity),
                    totalItems = cart.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateCartItem error: {@Request}", request);
                return Json(new { success = false, message = "Server error" });
            }
        }

        [HttpPost]
        public IActionResult RemoveCartItem([FromBody] RemoveCartItemRequest? request)
        {
            try
            {
                if (request == null || request.ItemId <= 0)
                    return Json(new { success = false, message = "Invalid request" });

                var cart = GetCartFromSession();
                var removed = cart.RemoveAll(x => x.ItemId == request.ItemId) > 0;

                SaveCartToSession(cart);

                return Json(new
                {
                    success = true,
                    message = "Item removed successfully",
                    cartCount = cart.Sum(x => x.Quantity)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RemoveCartItem error: {@Request}", request);
                return Json(new { success = false, message = "Server error" });
            }
        }

        [HttpPost]
        public IActionResult ClearCart()
        {
            try
            {
                HttpContext.Session.Remove("CustomerCart");
                return Json(new { success = true, message = "Cart cleared" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ClearCart error");
                return Json(new { success = false, message = "Server error" });
            }
        }
        // ✅ UPDATED: Feedback submission with OrderId support
        [HttpPost]
        public async Task<IActionResult> SubmitFeedback([FromBody] Feedback model)
        {
            try
            {
                if (model == null || model.Rating < 1 || model.Rating > 5)
                    return Json(new { success = false, message = "Invalid rating (1-5 stars required)" });

                model.CustomerId = GetOrCreateCustomerId();
                model.CreatedAt = DateTime.Now;

                _context.Feedbacks.Add(model);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Thank you for your feedback! ⭐"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Feedback submission error: {@Feedback}", model);
                return Json(new { success = false, message = "Failed to submit feedback" });
            }
        }
        // ✅ NEW: Checkout Session (ADD THIS to CustomerController)

        
       [HttpPost]
public async Task<IActionResult> CreateCheckoutSession([FromForm] decimal amount)
{
    try
    {
        var customerId = GetOrCreateCustomerId();
        
        // ✅ DEBUG LOG
        _logger.LogInformation("🛒 Checkout for Customer {CustomerId} | ₹{Amount}", customerId, amount);
        
        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        var successUrl = $"{baseUrl}/Customer/OrderSuccess?session_id={{CHECKOUT_SESSION_ID}}&customerId={customerId}";
        var cancelUrl = $"{baseUrl}/Customer/Cart";

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "inr",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Cafe Order",
                            Description = $"Customer #{customerId} - {HttpContext.Session.GetString("name") ?? "Guest"}"
                        },
                        UnitAmount = (long)(amount * 100)
                    },
                    Quantity = 1
                }
            },
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                { "customerId", customerId.ToString() },
                { "totalAmount", amount.ToString("F2", CultureInfo.InvariantCulture) },
                { "userEmail", HttpContext.Session.GetString("email") ?? "" }
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        _logger.LogInformation("✅ Session {SessionId} | Customer {CustomerId} | User: {Email}", 
            session.Id, customerId, HttpContext.Session.GetString("email"));

        return Json(new { 
            success = true, 
            sessionId = session.Id,
            redirectUrl = session.Url 
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Checkout FAILED");
        return Json(new { success = false, message = "Server error" });
    }
}
        [HttpGet]
        public IActionResult TestCheckoutUrls()
        {
            var customerId = GetOrCreateCustomerId();
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

            return Json(new
            {
                customerId,
                baseUrl,
                successUrl = $"{baseUrl}/Customer/OrderSuccess?session_id={{CHECKOUT_SESSION_ID}}&customerId={customerId}",
                cancelUrl = $"{baseUrl}/Customer/Cart",
                testAmount = 429.00m
            });
        }
       
        [HttpGet]
        public IActionResult DebugUrls()
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            return Json(new
            {
                baseUrl,
                successUrl = $"{baseUrl}/Customer/OrderSuccess?session_id={{CHECKOUT_SESSION_ID}}",
                cancelUrl = $"{baseUrl}/Customer/Cart"
            });
        }
        [HttpGet]
        public async Task<IActionResult> DebugOrderStatus(string session_id)
        {
            var customerId = GetOrCreateCustomerId();
            var model = new
            {
                CustomerId = customerId,
                SessionId = session_id,
                Cart = GetCartFromSession(),
                CartKey = $"CustomerCart_{customerId}",
                HasCartJson = !string.IsNullOrEmpty(HttpContext.Session.GetString($"CustomerCart_{customerId}")),
                RecentOrders = await _context.Orders.Where(o => o.CustomerId == customerId).CountAsync()
            };
            return Json(model);
        }
        // 🔥 WEBHOOK: Handles successful payments
        [HttpPost]
        public async Task<IActionResult> StripeWebhook()
        {
            try
            {
                var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                var stripeEvent = EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], "whsec_your_webhook_secret");

                if (stripeEvent.Type == "checkout.session.completed")  // ✅ Correct
                {
                    var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                    await HandleSuccessfulPayment(session);
                }

                return Ok();
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe webhook error");
                return BadRequest();
            }
        }

        private async Task HandleSuccessfulPayment(Stripe.Checkout.Session session)
        {
            try
            {
                if (session?.PaymentStatus != "paid") return;

                if (!int.TryParse(session.Metadata.GetValueOrDefault("customerId", "0"), out int customerId))
                {
                    _logger.LogError("❌ No customerId in webhook metadata");
                    return;
                }

                // ✅ CHECK DUPLICATE FIRST
                if (await _context.Orders.AnyAsync(o => o.StripeSessionId == session.Id))
                {
                    _logger.LogInformation("✅ Webhook duplicate ignored: {SessionId}", session.Id);
                    return;
                }

                var cart = GetCartFromSession();
                if (!cart.Any())
                {
                    _logger.LogWarning("❌ Empty cart in webhook for {CustomerId}", customerId);
                    return;
                }

                decimal totalAmount = session.AmountTotal.HasValue ? session.AmountTotal.Value / 100m : 0m;

                var order = new Order
                {
                    CustomerId = customerId,
                    TotalAmount = totalAmount,
                    StripePaymentIntentId = session.PaymentIntentId,
                    StripeSessionId = session.Id,
                    OrderStatus = "Pending", // ✅ EXPLICIT
                    OrderTime = DateTime.Now   // ✅ EXPLICIT
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Add order items + payment (same as before)
                // ... rest of method

                await _context.SaveChangesAsync();
                ClearCustomerCart();

                _logger.LogInformation("✅ WEBHOOK Order {OrderId} - Status: {Status}", order.OrderId, order.OrderStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Webhook FAILED");
            }
        }
        // ✅ ADD Order Success page
        // ✅ FIXED: OrderSuccess - Single source of truth
        public async Task<IActionResult> OrderSuccess(string session_id, string customerId = null)
        {
            ViewBag.SessionId = session_id;
            ViewBag.QueryCustomerId = customerId;

            try
            {
                if (string.IsNullOrEmpty(session_id))
                {
                    ViewBag.Error = "No session ID";
                    return View();
                }

                var service = new SessionService();
                var session = await service.GetAsync(session_id);

                // ✅ CHECK IF ORDER EXISTS FIRST
                var existingOrder = await _context.Orders
                    .Include(o => o.OrderItems)
                    .Include(o => o.Payments)
                    .FirstOrDefaultAsync(o => o.StripeSessionId == session_id);

                if (existingOrder != null)
                {
                    _logger.LogInformation("✅ Order exists: {OrderId} for session {SessionId}",
                        existingOrder.OrderId, session_id);

                    ViewBag.Success = true;
                    ViewBag.OrderId = existingOrder.OrderId;
                    ViewBag.TotalAmount = existingOrder.TotalAmount;
                    ViewBag.ItemCount = existingOrder.OrderItems.Count;
                    ViewBag.PaymentCount = existingOrder.Payments.Count;
                    return View();
                }

                // Only create if no existing order
                if (session?.PaymentStatus == "paid")
                {
                    int parsedCustomerId = int.Parse(customerId ?? session.Metadata?["customerId"] ?? "0");
                    var created = await CreateOrderFromSuccessfulPayment(session, parsedCustomerId);

                    if (created)
                    {
                        ViewBag.Success = true;
                        ViewBag.Message = "Order created successfully!";
                    }
                    else
                    {
                        ViewBag.Error = "Order creation failed";
                    }
                }
                else
                {
                    ViewBag.Error = $"Payment status: {session?.PaymentStatus}";
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                _logger.LogError(ex, "OrderSuccess error");
            }

            return View();
        }

        // ✅ FIXED: CreateOrderFromSuccessfulPayment - Handles NO CART scenario
        private async Task<bool> CreateOrderFromSuccessfulPayment(Session session, int customerId)
        {
            try
            {
                _logger.LogInformation("🚀 Order creation START - Customer: {CustomerId}", customerId);

                // ✅ CRITICAL: Get cart BEFORE anything
                var cart = GetCartFromSession();
                if (!cart.Any())
                {
                    _logger.LogError("❌ NO CART - cannot create order");
                    return false;
                }

                decimal totalAmount = 0m;
                foreach (var item in cart)
                {
                    var menuItem = await _context.MenuItems.FindAsync(item.ItemId);
                    if (menuItem?.Price.HasValue == true)
                        totalAmount += item.Quantity * menuItem.Price.Value;
                }

                // ✅ BULLETPROOF ORDER CREATION
                var order = new Order
                {
                    CustomerId = customerId,
                    TotalAmount = totalAmount > 0 ? totalAmount : 100m, // Minimum
                    StripePaymentIntentId = session.PaymentIntentId,
                    StripeSessionId = session.Id,
                    OrderStatus = "Pending",           // ✅ EXPLICIT
                    OrderTime = DateTime.UtcNow          // ✅ EXPLICIT - FIXES NULL
                };

                _logger.LogInformation("📝 Creating order: ID=?, Customer={CustomerId}, Status={Status}, Time={Time}, Total=₹{Total}",
                    "NEW", customerId, order.OrderStatus, order.OrderTime, order.TotalAmount);

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ SAVED Order {OrderId} - Status: {Status}", order.OrderId, order.OrderStatus);

                // Add order items
                foreach (var cartItem in cart)
                {
                    var menuItem = await _context.MenuItems.FindAsync(cartItem.ItemId);
                    if (menuItem != null)
                    {
                        _context.OrderItems.Add(new OrderItem
                        {
                            OrderId = order.OrderId,
                            ItemId = cartItem.ItemId,
                            Quantity = cartItem.Quantity,
                            Price = menuItem.Price ?? 0m,
                            Subtotal = cartItem.Quantity * (menuItem.Price ?? 0m)
                        });
                    }
                }

                // Payment record
                _context.Payments.Add(new Payment
                {
                    OrderId = order.OrderId,
                    StripeSessionId = session.Id,
                    Amount = order.TotalAmount,
                    PaymentStatus = "Paid",
                    PaidAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                ClearCustomerCart();

                _logger.LogInformation("🎉 FINAL Order {OrderId}: Status={Status}, Items={Count}, ₹{Total}",
                    order.OrderId, order.OrderStatus, cart.Count, order.TotalAmount);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Order creation FAILED for {CustomerId}", customerId);
                return false;
            }
        }

        // ✅ CRITICAL: Recover cart before it's cleared
        private List<CartItem> RecoverCartItems(int customerId)
        {
            var key = $"CustomerCart_{customerId}";
            var json = HttpContext.Session.GetString(key);

            if (string.IsNullOrEmpty(json))
            {
                _logger.LogWarning("No cart session for customer {CustomerId}", customerId);
                return new List<CartItem>();
            }

            try
            {
                var cart = JsonSerializer.Deserialize<List<CartItem>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<CartItem>();

                _logger.LogInformation("✅ Recovered {Count} cart items for customer {CustomerId}",
                    cart.Count, customerId);
                return cart;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cart recovery failed for customer {CustomerId}", customerId);
                return new List<CartItem>();
            }
        }

       
        private decimal GetTotalFromStripeSession(Session session)
        {
            // Priority 1: AmountTotal (most reliable)
            if (session.AmountTotal.HasValue && session.AmountTotal > 0)
                return session.AmountTotal.Value / 100m;

            // Priority 2: Metadata
            if (session.Metadata?.TryGetValue("totalAmount", out var metaTotal) == true &&
                decimal.TryParse(metaTotal, NumberStyles.Float, CultureInfo.InvariantCulture, out var total))
                return total;

            // Priority 3: Cart fallback
            var customerCart = GetCartFromSession();
            return customerCart.Sum(c =>
            {
                var item = _context.MenuItems.Find(c.ItemId);
                return item?.Price.HasValue == true ? c.Quantity * item.Price.Value : 0m;
            });
        }

        // ✅ NEW HELPER: Add order items
        private async Task AddOrderItems(int orderId, int customerId)
        {
            var cartKey = $"CustomerCart_{customerId}";
            var cartJson = HttpContext.Session.GetString(cartKey);

            if (string.IsNullOrEmpty(cartJson))
            {
                _logger.LogWarning("No cart JSON for order {OrderId}", orderId);
                return;
            }

            List<CartItem> cartItems;
            try
            {
                cartItems = JsonSerializer.Deserialize<List<CartItem>>(cartJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<CartItem>();
            }
            catch
            {
                _logger.LogWarning("Invalid cart JSON for order {OrderId}", orderId);
                return;
            }

            int added = 0;
            foreach (var cartItem in cartItems)
            {
                var menuItem = await _context.MenuItems.FindAsync(cartItem.ItemId);
                if (menuItem?.IsAvailable == true)
                {
                    _context.OrderItems.Add(new OrderItem
                    {
                        OrderId = orderId,
                        ItemId = cartItem.ItemId,
                        Quantity = cartItem.Quantity,
                        Price = menuItem.Price ?? 0m,
                        Subtotal = cartItem.Quantity * (menuItem.Price ?? 0m)
                    });
                    added++;
                }
            }

            if (added > 0)
                _logger.LogInformation("✅ Added {Count} items to order {OrderId}", added, orderId);
        }

        // ✅ NEW HELPER: Add payment record
        private async Task AddPaymentRecord(int orderId, Session session, decimal amount)
        {
            // ✅ FIXED: Direct DbSet usage (NO ??=)
            var payment = new Payment
            {
                OrderId = orderId,
                StripeSessionId = session.Id,
                Amount = amount,
                PaymentStatus = session.PaymentStatus ?? "paid",
                PaidAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment); // ✅ DbSet.Add()
        }
        // ✅ FIXED: CreateOrderFromSession (SINGLE SOURCE OF TRUTH)
        private async Task<bool> CreateOrderFromSession(Session session, int customerId)
        {
            try
            {
                _logger.LogInformation("🚀 === CreateOrderFromSession START ===");
                _logger.LogInformation("📋 Session: {SessionId}, Customer: {CustomerId}, Status: {Status}",
                    session.Id, customerId, session.PaymentStatus);

                // 1. ✅ FIX: Use customer-specific cart key
                var cartKey = $"CustomerCart_{customerId}";
                var cartJson = HttpContext.Session.GetString(cartKey);
                _logger.LogInformation("📦 Cart key: {Key}, JSON length: {Length}", cartKey, cartJson?.Length ?? 0);

                if (string.IsNullOrEmpty(cartJson))
                {
                    _logger.LogWarning("❌ NO CART JSON found for {CustomerId}", customerId);
                    return false;
                }

                var cart = JsonSerializer.Deserialize<List<CartItem>>(cartJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (cart == null || !cart.Any())
                {
                    _logger.LogWarning("❌ Cart deserialized but empty for {CustomerId}. Count: {Count}",
                        customerId, cart?.Count ?? 0);
                    return false;
                }

                _logger.LogInformation("🛒 Cart loaded: {Count} items", cart.Count);

                // 2. Calculate total from ACTUAL cart items
                decimal totalAmount = 0;
                var validItems = new List<CartItem>();

                foreach (var cartItem in cart)
                {
                    var menuItem = await _context.MenuItems.FindAsync(cartItem.ItemId);
                    if (menuItem?.Price.HasValue == true && menuItem.IsAvailable == true)
                    {
                        totalAmount += cartItem.Quantity * menuItem.Price.Value;
                        validItems.Add(cartItem);
                        _logger.LogInformation("✅ Item {ItemId}: {Qty}x₹{Price} = ₹{Subtotal}",
                            cartItem.ItemId, cartItem.Quantity, menuItem.Price,
                            cartItem.Quantity * menuItem.Price.Value);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Invalid item {ItemId} (not found/unavailable)", cartItem.ItemId);
                    }
                }

                if (!validItems.Any())
                {
                    _logger.LogError("❌ No valid cart items for {CustomerId}", customerId);
                    return false;
                }

                _logger.LogInformation("💰 Calculated total: ₹{Total:F2} from {Count} valid items", totalAmount, validItems.Count);

                // 3. Create Order
                var order = new Order
                {
                    CustomerId = customerId,
                    TotalAmount = totalAmount,
                    StripePaymentIntentId = session.PaymentIntentId,
                    StripeSessionId = session.Id,
                    OrderStatus = "Pending",
                    OrderTime = DateTime.UtcNow
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                _logger.LogInformation("✅ Order SAVED: ID={OrderId}", order.OrderId);

                // 4. Add Order Items
                int savedItems = 0;
                foreach (var cartItem in validItems)
                {
                    var menuItem = await _context.MenuItems.FindAsync(cartItem.ItemId);
                    if (menuItem != null)
                    {
                        var orderItem = new OrderItem
                        {
                            OrderId = order.OrderId,
                            ItemId = cartItem.ItemId,
                            Quantity = cartItem.Quantity,
                            Price = menuItem.Price ?? 0m,
                            Subtotal = cartItem.Quantity * (menuItem.Price ?? 0m)
                        };
                        _context.OrderItems.Add(orderItem);
                        savedItems++;
                    }
                }

                // 5. Add Payment Record
                _context.Payments.Add(new Payment
                {
                    OrderId = order.OrderId,
                    StripeSessionId = session.Id,

                    Amount = totalAmount,
                    PaymentStatus = "paid",
                    PaidAt = DateTime.UtcNow
                });

                // 6. Clear Cart
                HttpContext.Session.Remove(cartKey);

                await _context.SaveChangesAsync();

                _logger.LogInformation("🎉 === SUCCESS === Order {OrderId}: {Items} items, ₹{Total:F2}, Cart cleared",
                    order.OrderId, savedItems, totalAmount);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 CreateOrderFromSession FAILED for customer {CustomerId}: {Message}",
                    customerId, ex.Message);
                return false;
            }
        }
        [HttpGet]
        public async Task<IActionResult> DebugCartAndOrder(string session_id = null)
        {
            var model = new DebugViewModel
            {
                SessionId = session_id,
                CustomerId = GetOrCreateCustomerId(),
                CartKey = $"CustomerCart_{GetOrCreateCustomerId()}",
                CartJson = HttpContext.Session.GetString($"CustomerCart_{GetOrCreateCustomerId()}"),
                CartItems = GetCartFromSession(),
                AllSessionKeys = HttpContext.Session.Keys.ToList(),
                RecentOrders = await _context.Orders
                    .Where(o => o.CustomerId == GetOrCreateCustomerId())
                    .OrderByDescending(o => o.OrderTime)
                    .Take(5)
                    .ToListAsync()
            };

            // Try to get Stripe session
            if (!string.IsNullOrEmpty(session_id))
            {
                try
                {
                    var service = new SessionService();
                    model.StripeSession = await service.GetAsync(session_id);
                }
                catch (Exception ex)
                {
                    model.Error = $"Stripe session error: {ex.Message}";
                }
            }

            return View(model);
        }



        // ✅ FIXED Session Helper Methods
       private int GetOrCreateCustomerId()
{
    const string SESSION_KEY = "CustomerId";
    
    // 1. ✅ PRIORITY 1: Session CustomerId (most reliable)
    if (HttpContext.Session.TryGetValue(SESSION_KEY, out var bytes) && 
        int.TryParse(Encoding.UTF8.GetString(bytes), out int sessionCustomerId))
    {
        if (_context.Customers.Find(sessionCustomerId) != null)
        {
            _logger.LogInformation("✅ Using session CustomerId: {CustomerId}", sessionCustomerId);
            return sessionCustomerId;
        }
    }
    
    // 2. ✅ PRIORITY 2: Link to logged-in user
    var userEmail = HttpContext.Session.GetString("email");
    if (!string.IsNullOrEmpty(userEmail))
    {
        var customer = _context.Customers.FirstOrDefault(c => c.Email == userEmail);
        if (customer != null)
        {
            HttpContext.Session.SetString(SESSION_KEY, customer.CustomerId.ToString());
            _logger.LogInformation("✅ Linked user {Email} → Customer {CustomerId}", userEmail, customer.CustomerId);
            return customer.CustomerId;
        }
    }
    
    // 3. ✅ PRIORITY 3: Fresh guest (should rarely happen for logged-in users)
    var guest = new cafe.Models.Customer
    {
        Name = HttpContext.Session.GetString("name") ?? "Guest",
        Email = userEmail ?? $"guest_{Guid.NewGuid():N[8]}@cafe.local",
        Phone = HttpContext.Session.GetString("phone") ?? "9999999999",
        CreatedAt = DateTime.Now
    };
    
    _context.Customers.Add(guest);
    _context.SaveChanges();
    HttpContext.Session.SetString(SESSION_KEY, guest.CustomerId.ToString());
    
    _logger.LogInformation("🆕 Created guest CustomerId: {CustomerId}", guest.CustomerId);
    return guest.CustomerId;
}

       // ✅ FIXED: CustomerController
private List<CartItem> GetCartFromSession()
{
    var customerId = GetOrCreateCustomerId();
    var key = $"CustomerCart_{customerId}";  // ✅ "CustomerCart_28"
    var json = HttpContext.Session.GetString(key);

    if (string.IsNullOrEmpty(json)) return new List<CartItem>();

    try
    {
        return JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
    }
    catch
    {
        HttpContext.Session.Remove(key);
        return new List<CartItem>();
    }
}

private void SaveCartToSession(List<CartItem> cart)
{
    var customerId = GetOrCreateCustomerId();
    var key = $"CustomerCart_{customerId}";  // ✅ "CustomerCart_28"
    HttpContext.Session.SetString(key, JsonSerializer.Serialize(cart));
}

private void ClearCustomerCart()
{
    var customerId = GetOrCreateCustomerId();
    HttpContext.Session.Remove($"CustomerCart_{customerId}");
}

       
        private int GetCartCount()
        {
            return GetCartFromSession().Sum(x => x.Quantity);
        }
    }
}







