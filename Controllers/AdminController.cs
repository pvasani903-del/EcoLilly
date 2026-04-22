using System;
using System.Linq;
using EcoLilly.Data;
using EcoLilly.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EcoLilly.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================= LOGIN =================
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Email and password are required.";
                return View();
            }

            // 🔐 Get admin by email
            var admin = _context.Admins
                .AsNoTracking()
                .FirstOrDefault(a => a.Email == email);

            // ❌ FIXED: Standard matching without BCrypt or PasswordHash
            if (admin == null || admin.Password != password)
            {
                ViewBag.Error = "Invalid credentials.";
                return View();
            }

            // ✅ SET SESSION (IMPORTANT)
            HttpContext.Session.SetString("Role", "Admin");
            HttpContext.Session.SetString("AdminEmail", admin.Email);
            HttpContext.Session.SetString("UserName",
                string.IsNullOrEmpty(admin.Name) ? admin.Username : admin.Name);

            return RedirectToAction(nameof(Index));
        }

        // ================= DASHBOARD =================
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin")
                return RedirectToAction("Login", "Account");

            var model = new DashboardViewModel
            {
                TotalProducts = _context.Products.Count(),
                TotalOrders = _context.Orders.Count(),
                TotalUsers = _context.Users.Count(),

                 // ✔ FIXED CART + WISHLIST
                CartCount = _context.CartItems.Count(),
                WishlistCount = _context.Wishlists.Count(),

                // ✔ SAFE REVENUE
                TotalRevenue = _context.Orders
                    .Select(o => (decimal?)o.TotalAmount)
                    .Sum() ?? 0,

                RecentOrders = _context.Orders
                    .OrderByDescending(o => o.OrderDate)
                    .Take(10)
                    .ToList(),

                AdminName = HttpContext.Session.GetString("UserName") ?? "Admin"
            };

            // ✅ LAST 7 DAYS DATA
            for (int i = 6; i >= 0; i--)
            {
                var day = DateTime.Today.AddDays(-i);

                model.OrdersByDayLabels.Add(day.ToString("dd MMM"));

                var sum = _context.Orders
                    .Where(o => o.OrderDate.Date == day.Date)
                    .Select(o => (decimal?)o.TotalAmount)
                    .Sum() ?? 0;

                model.OrdersByDayTotals.Add(sum);
            }

            return View(model);
        }

        // ================= LOGOUT =================
        public IActionResult Logout()
        {
            // Clear all user and admin sessions entirely
            HttpContext.Session.Clear();
            
            // Redirect smoothly to the main Account Login page
            return RedirectToAction("Login", "Account");
        }

        // ================= CHANGE PASSWORD =================
        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            return View();
        }

        [HttpPost]
        public IActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var adminEmail = HttpContext.Session.GetString("AdminEmail");

            if (adminEmail == null || HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            var admin = _context.Admins.FirstOrDefault(a => a.Email == adminEmail);

            if (admin == null)
                return RedirectToAction("Login", "Account");

            if (admin.Password != currentPassword)
            {
                ViewBag.Error = "Current password is incorrect.";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "New passwords do not match.";
                return View();
            }

            admin.Password = newPassword;
            _context.Update(admin);
            _context.SaveChanges();

            ViewBag.Success = "Admin password changed successfully!";
            return View();
        }
    }
}