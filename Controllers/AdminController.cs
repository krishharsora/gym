using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using cafe.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims; // ✅ This provides ClaimTypes


namespace cafe.Controllers
{
    [RoleAuthorize("Admin")]
    public class AdminController : Controller
    {
        private readonly CafeManagementContext db;

        public AdminController(CafeManagementContext db)
        {
            this.db = db;
        }
        private int CurrentUserId
        {
            get
            {
                return int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId)
                    ? userId
                    : 1; // Default to admin user ID 1
            }
        }


        // ======================
        // DASHBOARD
        // ======================

        public IActionResult Dashboard()
        {
            // Staff (Users with staff roles)
            ViewBag.TotalStaff = db.Users.Count(u => u.Role != "Customer");
            ViewBag.TotalCustomers = db.Customers.Count();
            ViewBag.TotalMenuItems = db.MenuItems.Count(mi => mi.IsAvailable);
            ViewBag.TotalOrders = db.Orders.Count();

            // Today's real data
            var today = DateTime.Today;
            ViewBag.TodayOrders = db.Orders.Count(o => o.OrderTime.Date == today);
            ViewBag.TodayRevenue = db.Orders
                .Where(o => o.OrderTime.Date == today)
                .Sum(o => o.TotalAmount ?? 0);

            // Table stats (using your exact schema)
            ViewBag.ActiveTables = db.CafeTables.Count(t => t.Status == "Available" || t.Status == "Occupied");
            ViewBag.TotalTables = db.CafeTables.Count();
            ViewBag.ReservedTables = db.CafeTables.Count(t => t.Status == "Reserved");

            // Order status stats
            ViewBag.PendingOrders = db.Orders.Count(o => o.OrderStatus == "Pending");
            ViewBag.PreparingOrders = db.Orders.Count(o => o.OrderStatus == "Preparing");

            // Menu items stats
            ViewBag.AvailableItems = db.MenuItems.Count(mi => mi.IsAvailable);
            ViewBag.UnavailableItems = db.MenuItems.Count(mi => !mi.IsAvailable);

            // Reservations
            ViewBag.TotalReservations = db.Reservations.Count();
            ViewBag.PendingReservations = db.Reservations.Count(r => r.Status == "Pending");

            // Payments
            ViewBag.TotalRevenue = db.Payments.Where(p => p.PaymentStatus == "Paid").Sum(p => p.Amount);
            ViewBag.PendingPayments = db.Payments.Count(p => p.PaymentStatus == "Pending");

            // Feedback & Activity
            ViewBag.TotalFeedback = db.Feedbacks.Count();
            ViewBag.AverageRating = db.Feedbacks.Average(f => f.Rating) ?? 0;
            ViewBag.RecentActivity = db.ActivityLogs.OrderByDescending(a => a.CreatedAt).Take(5).ToList();

            // Monthly revenue (last 6 months)
            var monthlyRevenue = db.Payments
                .Where(p => p.PaidAt >= DateTime.Now.AddMonths(-6) && p.PaymentStatus == "Paid")
                .GroupBy(p => new
                {
                    Year = p.PaidAt!.Value.Year,
                    Month = p.PaidAt!.Value.Month
                })
                .Select(g => new
                {
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    Revenue = g.Sum(p => p.Amount)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList();
            ViewBag.MonthlyRevenue = monthlyRevenue;

            // Recent orders
            ViewBag.RecentOrders = db.Orders
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .OrderByDescending(o => o.OrderTime)
                .Take(5)
                .ToList();

            return View();
        }

        // ======================
        // STAFF LIST
        // ======================

        public IActionResult StaffList()
        {
            var staff = db.Users
                .Where(u => u.Role != "Customer")
                .ToList();

            return View(staff);
        }

        // ======================
        // CATEGORIES
        // ======================

        // CATEGORIES CRUD
        public IActionResult Categories()
        {
            var categories = db.MenuCategories
                .OrderBy(c => c.CategoryName)
                .Select(c => new CategoryViewModel
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    Description = c.Description,

                    ItemCount = db.MenuItems.Count(m => m.CategoryId == c.CategoryId) // ✅ Calculate here
                })
                .ToList();

            return View(categories);
        }

        public IActionResult AddCategory()
        {
            return View(new MenuCategory());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddCategory(MenuCategory category)
        {
            if (ModelState.IsValid)
            {
                if (db.MenuCategories.Any(c => c.CategoryName == category.CategoryName))
                {
                    ModelState.AddModelError("CategoryName", "Category already exists");
                    return View(category);
                }
                db.MenuCategories.Add(category);
                db.SaveChanges();
                TempData["Success"] = "Category added successfully!";
                return RedirectToAction("Categories");
            }
            return View(category);
        }

        public IActionResult EditCategory(int id)
        {
            var category = db.MenuCategories.Find(id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditCategory(MenuCategory category)
        {
            if (ModelState.IsValid)
            {
                var existing = db.MenuCategories.Find(category.CategoryId);
                if (existing == null) return NotFound();
                if (db.MenuCategories.Any(c => c.CategoryId != category.CategoryId && c.CategoryName == category.CategoryName))
                {
                    ModelState.AddModelError("CategoryName", "Category name already exists");
                    return View(category);
                }
                existing.CategoryName = category.CategoryName;
                existing.Description = category.Description;


                db.SaveChanges();
                TempData["Success"] = "Category updated successfully!";
                return RedirectToAction("Categories");
            }
            return View(category);
        }


        public IActionResult DeleteCategory(int id)
        {
            var category = db.MenuCategories.Find(id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost, ActionName("DeleteCategory")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCategoryConfirmed(int id)
        {
            var category = db.MenuCategories.Find(id);
            if (category == null) return NotFound();
            if (db.MenuItems.Any(m => m.CategoryId == id))
            {
                TempData["Error"] = "Cannot delete category with menu items!";
                return RedirectToAction("Categories");
            }
            db.MenuCategories.Remove(category);
            db.SaveChanges();
            TempData["Success"] = "Category deleted successfully!";
            return RedirectToAction("Categories");
        }

        // ======================
        // MENU ITEMS
        // ======================

        // MENU ITEMS CRUD
        public IActionResult MenuItems()
        {
            var items = db.MenuItems.Include(m => m.Category)
                .OrderBy(m => m.Category.CategoryName).ThenBy(m => m.ItemName).ToList();
            ViewBag.Categories = new SelectList(db.MenuCategories, "CategoryId", "CategoryName");
            return View(items);
        }

        public IActionResult AddMenuItem()
        {
            ViewBag.Categories = new SelectList(db.MenuCategories, "CategoryId", "CategoryName");
            return View(new MenuItem());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddMenuItem(MenuItem menuItem, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                    menuItem.ImageUrl = ProcessUploadedFile(imageFile);
                menuItem.IsAvailable = true;

                db.MenuItems.Add(menuItem);
                db.SaveChanges();
                TempData["Success"] = "Menu item added successfully!";
                return RedirectToAction("MenuItems");
            }
            ViewBag.Categories = new SelectList(db.MenuCategories, "CategoryId", "CategoryName");
            return View(menuItem);
        }

        public IActionResult EditMenuItem(int id)
        {
            var item = db.MenuItems.Include(m => m.Category).FirstOrDefault(m => m.ItemId == id);
            if (item == null) return NotFound();
            ViewBag.Categories = new SelectList(db.MenuCategories, "CategoryId", "CategoryName");
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditMenuItem(MenuItem menuItem, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                var existingItem = db.MenuItems.FirstOrDefault(m => m.ItemId == menuItem.ItemId);
                if (existingItem == null) return NotFound();
                existingItem.ItemName = menuItem.ItemName;
                existingItem.Description = menuItem.Description;
                existingItem.Price = menuItem.Price;
                existingItem.CategoryId = menuItem.CategoryId;
                existingItem.IsAvailable = menuItem.IsAvailable;
                if (imageFile != null && imageFile.Length > 0)
                {
                    if (!string.IsNullOrEmpty(existingItem.ImageUrl))
                    {
                        var oldFilePath = Path.Combine("wwwroot/images", Path.GetFileName(existingItem.ImageUrl));
                        if (System.IO.File.Exists(oldFilePath)) System.IO.File.Delete(oldFilePath);
                    }
                    existingItem.ImageUrl = ProcessUploadedFile(imageFile);
                }

                db.SaveChanges();
                TempData["Success"] = "Menu item updated successfully!";
                return RedirectToAction("MenuItems");
            }
            ViewBag.Categories = new SelectList(db.MenuCategories, "CategoryId", "CategoryName");
            return View(menuItem);
        }

        public IActionResult DeleteMenuItem(int id)
        {
            var item = db.MenuItems.Include(m => m.Category).FirstOrDefault(m => m.ItemId == id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost, ActionName("DeleteMenuItem")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteMenuItemConfirmed(int id)
        {
            var item = db.MenuItems.FirstOrDefault(m => m.ItemId == id);
            if (item == null) return NotFound();
            if (!string.IsNullOrEmpty(item.ImageUrl))
            {
                var filePath = Path.Combine("wwwroot/images", Path.GetFileName(item.ImageUrl));
                if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
            }
            db.MenuItems.Remove(item);
            db.SaveChanges();
            TempData["Success"] = "Menu item deleted successfully!";
            return RedirectToAction("MenuItems");
        }

        // HELPER METHOD
        private string ProcessUploadedFile(IFormFile file)
        {
            var uploadsFolder = Path.Combine("wwwroot", "images");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(fileStream);
            }
            return $"/images/{uniqueFileName}";
        }

        // ======================
        // TABLE LIST
        // ======================

        public IActionResult TableList()
        {
            var tables = db.CafeTables.ToList();

            return View(tables);
        }



        [HttpGet]
        public IActionResult CreateTable()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CreateTable(CafeTable table)
        {
            if (ModelState.IsValid)
            {
                db.CafeTables.Add(table);
                db.SaveChanges();
                return RedirectToAction("TableList");
            }
            return View(table);
        }

        [HttpGet]
        public IActionResult EditTable(int id)
        {
            var table = db.CafeTables.Find(id);
            if (table == null)
            {
                return NotFound();
            }
            return View(table);
        }

        [HttpPost]
        public IActionResult EditTable(int id, CafeTable table)
        {
            if (id != table.TableId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    db.Update(table);
                    db.SaveChanges();
                    return RedirectToAction("TableList");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CafeTableExists(table.TableId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(table);
        }

        [HttpPost]
        public IActionResult DeleteTable(int id)
        {
            var table = db.CafeTables.Find(id);
            if (table != null)
            {
                db.CafeTables.Remove(table);
                db.SaveChanges();
            }
            return RedirectToAction("TableList");
        }

        private bool CafeTableExists(int id)
        {
            return db.CafeTables.Any(e => e.TableId == id);
        }

        // ======================
        // ORDERS
        // ======================

        public IActionResult Orders()
        {
            try
            {
                // ✅ Safe query - ignores missing columns
                var orders = db.Orders
                    .IgnoreQueryFilters() // If you have soft deletes
                    .OrderByDescending(o => o.OrderTime)
                    .ToList();
                return View(orders);
            }
            catch (Exception ex)
            {
                // Log error & show empty list
                TempData["Error"] = "Unable to load orders: " + ex.Message;
                return View(new List<Order>());
            }
        }
        // ======================
        // ADD STAFF
        // ======================

        public IActionResult AddStaff()
        {
            return View(new User());
        }

        [HttpPost]
        public IActionResult AddStaff(User user, string Status)
        {
            if (ModelState.IsValid)
            {
                var hasher = new PasswordHasher<User>();
                user.PasswordHash = hasher.HashPassword(user, user.PasswordHash);
                user.Status = Status == "true";
                user.CreatedAt = DateTime.Now;

                // Simple password (you can hash later)
                user.PasswordHash = user.PasswordHash;

                db.Users.Add(user);
                db.SaveChanges();

                return RedirectToAction("StaffList");
            }

            return View(user);
        }
        // ======================
        // EDIT STAFF
        // ======================

        public IActionResult EditStaff(int id)
        {
            var user = db.Users.Find(id);
            if (user == null) return NotFound();

            return View(user);
        }


        [HttpPost]
        public IActionResult EditStaff(User user)
        {
            if (ModelState.IsValid)
            {
                var existingUser = db.Users.Find(user.UserId);

                if (existingUser == null)
                    return NotFound();

                // ✅ update fields
                existingUser.FullName = user.FullName;
                existingUser.Email = user.Email;
                existingUser.Phone = user.Phone;
                existingUser.Role = user.Role;
                existingUser.Status = user.Status;

                db.SaveChanges();

                return RedirectToAction("StaffList");
            }


            return View(user);
        }
        // ======================
        // DELETE STAFF (CONFIRMATION PAGE)
        // ======================

        public IActionResult DeleteStaff(int id)
        {
            var user = db.Users.Find(id);
            if (user == null) return NotFound();

            return View(user); // Show confirmation page
        }

        [HttpPost, ActionName("DeleteStaff")]
        public IActionResult DeleteConfirmed(int id)
        {
            var user = db.Users.Find(id);
            if (user == null) return NotFound();

            db.Users.Remove(user);
            db.SaveChanges();

            return RedirectToAction("StaffList");
        }
        // ✅ PAYMENTS
        public IActionResult Payments()
        {
            var payments = db.Payments
                .Include(p => p.Order.Customer)
                .Include(p => p.Order)
                .OrderByDescending(p => p.PaidAt)
                .ToList();
            return View(payments);
        }

        // ✅ FEEDBACK
        public IActionResult Feedback()
        {
            var feedback = db.Feedbacks
                .Include(f => f.Customer)
                .Include(f => f.Order)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();
            return View(feedback);
        }

        // ✅ REPORTS (Activity Logs)
        public IActionResult Reports()
        {
            var logs = db.ActivityLogs
                .Include(l => l.User)
                .OrderByDescending(l => l.CreatedAt)
                .ToList();
            return View(logs);
        }

        [HttpGet]
        public async Task<IActionResult> Reservations()
        {
            var reservations = await db.Reservations
                .Include(r => r.Customer)
                .Include(r => r.Table)
                .OrderBy(r => r.ReservationDate)
                .ThenBy(r => r.ReservationTime)
                .ToListAsync();

            var customers = await db.Customers
                .Where(c => c.Name != "Guest Customer")
                .ToListAsync();

            var tables = await db.CafeTables
                 .Where(t => t.Status != "Occupied")  // String comparison
                 .ToListAsync();

            ViewBag.Customers = customers;
            ViewBag.Tables = tables;
            ViewBag.TotalReservations = reservations.Count;

            return View(reservations);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReservation(Reservation reservation)
        {
            if (ModelState.IsValid)
            {
                // ✅ FIX 1: Handle nullable DateOnly? and TimeOnly?
                if (!reservation.ReservationDate.HasValue || !reservation.ReservationTime.HasValue)
                {
                    ModelState.AddModelError("", "Date and time are required.");
                    return RedirectToAction(nameof(Reservations));
                }

                // ✅ FIX 2: Convert DateOnly? to DateTime for validation
                var reservationDateTime = reservation.ReservationDate.Value.ToDateTime(reservation.ReservationTime.Value);

                // Validate future date (minimum 1 hour from now)
                if (reservationDateTime <= DateTime.Now.AddHours(1))
                {
                    ModelState.AddModelError("", "Reservation must be at least 1 hour in the future.");
                    return RedirectToAction(nameof(Reservations));
                }

                // ✅ FIX 3: Handle nullable GuestCount and Capacity
                if (!reservation.GuestCount.HasValue || reservation.GuestCount <= 0)
                {
                    ModelState.AddModelError("", "Valid guest count is required.");
                    return RedirectToAction(nameof(Reservations));
                }

                // Check table capacity
                var table = await db.CafeTables.FindAsync(reservation.TableId);
                if (table == null || !table.Capacity.HasValue || reservation.GuestCount > table.Capacity)
                {
                    ModelState.AddModelError("", "Invalid table selection or guest count exceeds capacity.");
                    return RedirectToAction(nameof(Reservations));
                }

                // ✅ FIX 4: String status comparison (matches your DB schema)
                var conflictingReservation = await db.Reservations
                    .AnyAsync(r => r.TableId == reservation.TableId &&
                                  r.ReservationDate == reservation.ReservationDate &&
                                  r.ReservationTime == reservation.ReservationTime &&
                                  r.Status != "Cancelled");  // ✅ String comparison

                if (conflictingReservation)
                {
                    ModelState.AddModelError("", "Table is already reserved at this time.");
                    return RedirectToAction(nameof(Reservations));
                }

                // ✅ FIX 5: Set default string status
                reservation.Status ??= "Pending";

                db.Reservations.Add(reservation);
                await db.SaveChangesAsync();

                // ✅ FIX 6: Define CurrentUserId property at class level
                var currentUserId = 1; // Default or get from User.Identity

                // Log activity (if ActivityLogs table exists)
                if (db.ActivityLogs != null)
                {
                    db.ActivityLogs.Add(new ActivityLog
                    {
                        UserId = currentUserId,
                        Action = "Created new reservation",
                        Entity = "Reservations"
                    });
                    await db.SaveChangesAsync();
                }

                TempData["Success"] = "Reservation created successfully!";
            }
            // ✅ FIX 7: Pass model back on validation error for form repopulation
            else
            {
                // Repopulate ViewBag for form
                var customers = await db.Customers.Where(c => c.Name != "Guest Customer").ToListAsync();
                var tables = await db.CafeTables.Where(t => t.Status != "Occupied").ToListAsync();
                ViewBag.Customers = customers;
                ViewBag.Tables = tables;
                ViewBag.TotalReservations = await db.Reservations.CountAsync();
                return View("Reservations", reservation);
            }

            return RedirectToAction(nameof(Reservations));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int reservationId, string status) // ✅ string instead of enum
        {
            // ✅ Validate allowed status values
            var validStatuses = new[] { "Pending", "Confirmed", "Cancelled" };
            if (!validStatuses.Contains(status))
            {
                return Json(new { success = false, message = "Invalid status value" });
            }

            var reservation = await db.Reservations
                .Include(r => r.Table)
                .FirstOrDefaultAsync(r => r.ReservationId == reservationId);

            if (reservation == null)
                return Json(new { success = false, message = "Reservation not found" });

            // ✅ Update reservation status (string)
            reservation.Status = status;

            // ✅ Update table status based on reservation status
            if (status == "Confirmed")
            {
                reservation.Table.Status = "Reserved";
            }
            else if (status == "Cancelled")
            {
                reservation.Table.Status = "Available";
            }

            await db.SaveChangesAsync();

            // ✅ FIX: Use CurrentUserId property (define at class level)
            var currentUserId = CurrentUserId; // Helper method below

            // Log activity (safe null check)
            if (db.ActivityLogs != null)
            {
                db.ActivityLogs.Add(new ActivityLog
                {
                    UserId = currentUserId,
                    Action = $"Updated reservation #{reservationId} status to {status}",
                    Entity = "Reservations"
                });
                await db.SaveChangesAsync();
            }

            // ✅ Return JSON for AJAX success
            return Json(new
            {
                success = true,
                status = status,
                badgeClass = status == "Confirmed" ? "bg-success text-white" :
                            status == "Cancelled" ? "bg-danger text-white" :
                            "bg-warning text-dark"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelReservation(int reservationId)
        {
            var reservation = await db.Reservations
                .Include(r => r.Table)
                .FirstOrDefaultAsync(r => r.ReservationId == reservationId);

            if (reservation == null) return NotFound();

            reservation.Status = "Cancelled";
            reservation.Table.Status = "Available";

            await db.SaveChangesAsync();

            // Log activity
            db.ActivityLogs.Add(new ActivityLog
            {
                UserId = CurrentUserId,
                Action = "Cancelled reservation",
                Entity = "Reservations"
            });
            await db.SaveChangesAsync();

            TempData["Success"] = "Reservation cancelled successfully!";
            return Json(new { success = true });
        }

    }
}