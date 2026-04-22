using EcoLilly.Data;
using EcoLilly.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace EcoLilly.Controllers
{
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ReviewController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ✅ GET: Review/Create
        [HttpGet]
        public IActionResult Create(int productId)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            var review = new Review
            {
                ProductId = productId,
                User = userEmail
            };

            return View(review);
        }

        // ✅ POST: Review/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Review model)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            model.User = userEmail;
            model.Date = DateTime.Now;

            // ❗ Fix validation issue
            ModelState.Remove("Product");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            _db.Reviews.Add(model);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Review submitted successfully!";

            // ✅ Redirect to Orders page
            return RedirectToAction("Index", "Order");
        }
    }
}