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

        // GET: /Admin/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Admin/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Email and password are required.";
                return View();
            }

            var admin = _context.Admins
                .AsNoTracking()
                .FirstOrDefault(a => a.Email == email && a.Password == password);

            if (admin == null)
            {
                ViewBag.Error = "Invalid credentials.";
                return View();
            }

            HttpContext.Session.SetString("AdminEmail", admin.Email);
            HttpContext.Session.SetString("AdminName", string.IsNullOrEmpty(admin.Name) ? admin.Username : admin.Name);

            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Index
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
                TotalRevenue = _context.Orders.Sum(o => o.TotalAmount),
                RecentOrders = _context.Orders
                    .OrderByDescending(o => o.OrderDate)
                    .Take(10)
                    .ToList(),
                AdminName = HttpContext.Session.GetString("UserName") ?? "Admin"
            };

            // last 7 days
            for (int i = 6; i >= 0; i--)
            {
                var day = DateTime.Today.AddDays(-i);
                model.OrdersByDayLabels.Add(day.ToString("dd MMM"));

                var sum = _context.Orders
                    .Where(o => o.OrderDate.Date == day.Date)
                    .Sum(o => (decimal?)o.TotalAmount) ?? 0m;

                model.OrdersByDayTotals.Add(sum);
            }

            return View(model);
        }


        // GET: /Admin/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }
    }
}