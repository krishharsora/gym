using System;
using System.Linq;
using cafe.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies; // ✅ REQUIRED
using Microsoft.AspNetCore.Http; // ✅ REQUIRED

namespace cafe.Controllers
{
    public class AccountController : Controller
    {
        private readonly EmailService _emailService;
        private readonly CafeManagementContext db;
        private readonly ILogger<AccountController> _logger;
        public AccountController(CafeManagementContext context, EmailService emailService, ILogger<AccountController> logger)
        {
            db = context;
            _emailService = emailService;
            _logger = logger;

        }

        // ================= LOGIN =================

        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("user_id") != null)
            {
                var role = HttpContext.Session.GetString("role");

                if (role == "Admin")
                    return RedirectToAction("Dashboard", "Admin");

                if (role == "Manager")
                    return RedirectToAction("Dashboard", "Manager");

                if (role == "Kitchen")
                    return RedirectToAction("Dashboard", "Kitchen");

                if (role == "Cashier")
                    return RedirectToAction("Dashboard", "Cashier");
            }

            return View();
        }

        [HttpPost]
public IActionResult Login(string email, string password)
{
    var user = db.Users.FirstOrDefault(x => x.Email == email && x.Status == true);

    if (user != null)
    {
        var hasher = new PasswordHasher<User>();
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, password);

        if (result == PasswordVerificationResult.Success)
        {
            // ✅ 1. SET USER SESSION
            HttpContext.Session.SetString("user_id", user.UserId.ToString());
            HttpContext.Session.SetString("role", user.Role);
            HttpContext.Session.SetString("name", user.FullName);
            HttpContext.Session.SetString("email", user.Email); // ✅ ADD EMAIL

            // ✅ 2. CLEAR OLD CUSTOMER SESSION
            HttpContext.Session.Remove("CustomerId");

            // ✅ 3. CREATE/LINK CUSTOMER
            var customer = db.Customers.FirstOrDefault(c => c.Email == user.Email);
            int customerId;
            
            if (customer == null)
            {
                customer = new Customer
                {
                    Name = user.FullName,
                    Email = user.Email,
                    Phone = user.Phone ?? "9999999999",
                    CreatedAt = DateTime.Now
                };
                db.Customers.Add(customer);
                db.SaveChanges();
                customerId = customer.CustomerId;
                _logger.LogInformation("🆕 Created customer {CustomerId} for user {Email}", customerId, user.Email);
            }
            else
            {
                customerId = customer.CustomerId;
                _logger.LogInformation("✅ Found customer {CustomerId} for user {Email}", customerId, user.Email);
            }
            
            // ✅ 4. SET CUSTOMER SESSION
            HttpContext.Session.SetString("CustomerId", customerId.ToString());
              HttpContext.Session.Remove($"CustomerCart_{customerId}");
            // ✅ 5. REDIRECT BY ROLE
            switch (user.Role)
            {
                case "Admin": return RedirectToAction("Dashboard", "Admin");
                case "Manager": return RedirectToAction("Dashboard", "Manager");
                case "Kitchen": return RedirectToAction("Dashboard", "Kitchen");
                case "Cashier": return RedirectToAction("Dashboard", "Cashier");
                case "Customer": return RedirectToAction("Dashboard", "Customer");
                default: return RedirectToAction("Dashboard", "Customer");
            }
        }
    }

    ViewBag.error = "Invalid Email or Password";
    return View();
}

        // ================= REGISTER =================

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(string name, string email, string password, string phone)
        {
            Random rnd = new Random();
            int otp = rnd.Next(100000, 999999);

            HttpContext.Session.SetString("reg_otp", otp.ToString());
            HttpContext.Session.SetString("reg_name", name);
            HttpContext.Session.SetString("reg_email", email);
            HttpContext.Session.SetString("reg_password", password);
            HttpContext.Session.SetString("reg_phone", phone);   // ✅ SAVE PHONE IN SESSION

            string subject = "Cafe System OTP";
            string body = "<h3>Your OTP is: " + otp + "</h3>";

            _emailService.SendEmail(email, subject, body);

            return RedirectToAction("VerifyRegisterOTP");
        }

        public IActionResult VerifyRegisterOTP()
        {
            return View();
        }

        [HttpPost]
        public IActionResult VerifyRegisterOTP(string otp)
        {
            var sessionOtp = HttpContext.Session.GetString("reg_otp");

            if (otp == sessionOtp)
            {
                string plainPassword = HttpContext.Session.GetString("reg_password");

                var hasher = new PasswordHasher<User>();
                string hashedPassword = hasher.HashPassword(null, plainPassword);

                User user = new User
                {
                    FullName = HttpContext.Session.GetString("reg_name"),
                    Email = HttpContext.Session.GetString("reg_email"),
                    Phone = HttpContext.Session.GetString("reg_phone"),  // ✅ INSERT PHONE
                    PasswordHash = hashedPassword,
                    Role = "Customer",
                    Status = true
                };
                Customer customer = new Customer
                {
                    Name = user.FullName,
                    Email = user.Email,
                    Phone = user.Phone,  // ✅ INSERT PHONE

                };

                db.Users.Add(user);
                db.SaveChanges();
                db.Customers.Add(customer);
                db.SaveChanges();
                return RedirectToAction("Login");
            }

            ViewBag.error = "Invalid OTP";
            return View();
        }
        // ================= FORGOT PASSWORD =================

        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            var user = db.Users.FirstOrDefault(x => x.Email == email);

            if (user == null)
            {
                ViewBag.error = "Email not found";
                return View();
            }

            Random rnd = new Random();
            int otp = rnd.Next(100000, 999999);

            HttpContext.Session.SetString("reset_otp", otp.ToString());
            HttpContext.Session.SetString("reset_email", email);

            string subject = "Password Reset OTP";
            string body = "<h3>Your Password Reset OTP: " + otp + "</h3>";

            _emailService.SendEmail(email, subject, body);

            return RedirectToAction("VerifyResetOTP");
        }

        public IActionResult VerifyResetOTP()
        {
            return View();
        }

        [HttpPost]
        public IActionResult VerifyResetOTP(string otp)
        {
            string sessionOtp = HttpContext.Session.GetString("reset_otp");

            if (otp == sessionOtp)
            {
                return RedirectToAction("ResetPassword");
            }

            ViewBag.error = "Invalid OTP";
            return View();
        }

        // ================= RESET PASSWORD =================

        public IActionResult ResetPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ResetPassword(string password)
        {
            string email = HttpContext.Session.GetString("reset_email");

            var user = db.Users.FirstOrDefault(x => x.Email == email);

            if (user == null)
                return RedirectToAction("Login");

            var hasher = new PasswordHasher<User>();

            user.PasswordHash = hasher.HashPassword(user, password);

            db.SaveChanges();

            HttpContext.Session.Remove("reset_otp");
            HttpContext.Session.Remove("reset_email");

            TempData["msg"] = "Password Reset Successful";

            return RedirectToAction("Login");
        }

        // ================= LOGOUT =================

      public IActionResult Logout()
{
    // 1. ✅ DESTROY ALL SESSION DATA
    HttpContext.Session.Clear();  // ✅ This clears EVERYTHING instantly

    // 2. ✅ NO NEED for individual removes - Session.Clear() handles it
    // Remove these hardcoded lines:
    // HttpContext.Session.Remove("CustomerCart_13"); ❌ DELETE
    // HttpContext.Session.Remove("CustomerCart_26"); ❌ DELETE
    // HttpContext.Session.Remove("Cart_13");         ❌ DELETE
    // HttpContext.Session.Remove("Cart_26");         ❌ DELETE

    // 3. ✅ CACHE BUSTING HEADERS
    Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate, max-age=0";
    Response.Headers["Pragma"] = "no-cache";
    Response.Headers["Expires"] = "Thu, 01 Jan 1970 00:00:00 GMT";

    // 4. ✅ DELETE SESSION COOKIE
    Response.Cookies.Delete(".AspNetCore.Session");

    // 5. ✅ CACHE-BUST REDIRECT
    return RedirectToAction("Login", "Account", new { t = DateTime.Now.Ticks });
}
    }
}