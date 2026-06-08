using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using cafe.Models;
namespace cafe.Controllers
{

    public class ProfileController : Controller
    {
        private readonly CafeManagementContext db;

        public ProfileController(CafeManagementContext context)
        {
            db = context;
        }

        public IActionResult Index()
        {
            var id = HttpContext.Session.GetString("user_id");

            if (id == null)
                return RedirectToAction("Login", "Account");

            int userId = Convert.ToInt32(id);

            var user = db.Users.Find(userId);

            return View(user);
        }

        [HttpPost]
        public IActionResult Index(User u)
        {
            var user = db.Users.Find(u.UserId);

            user.FullName = u.FullName;
            user.Email = u.Email;
            user.Phone = u.Phone;

            db.SaveChanges();

            HttpContext.Session.SetString("name", user.FullName);

            ViewBag.msg = "Profile Updated Successfully";

            return View(user);
        }
        // ================= CHANGE PASSWORD =================

        [HttpPost]
        public IActionResult ChangePassword(string oldPassword, string newPassword)
        {
            int id = Convert.ToInt32(HttpContext.Session.GetString("user_id"));

            var user = db.Users.Find(id);

            var hasher = new PasswordHasher<User>();

            // Assuming user.Password contains the stored hash
            var result = hasher.VerifyHashedPassword(null, user.PasswordHash, oldPassword);

            if (result == PasswordVerificationResult.Success)
            {
                user.PasswordHash = hasher.HashPassword(null, newPassword);

                db.SaveChanges();

                ViewBag.passmsg = "Password Changed Successfully";
            }
            else
            {
                ViewBag.passmsg = "Old Password Incorrect";
            }

            return View("Index", user);
        }
    }
}