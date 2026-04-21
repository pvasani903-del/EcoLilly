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

        // GET: /Review/Create
        [HttpGet]
        public IActionResult Create(int productId)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            var reviewModel = new Review
            {
                ProductId = productId,
                User = userEmail,
                Date = DateTime.Now
            };

            return View(reviewModel);
        }

        // POST: /Review/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Review model)
        {
            model.Date = DateTime.Now;

            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (!string.IsNullOrEmpty(userEmail))
            {
                model.User = userEmail;
            }

            // Remove ModelState validation for navigational properties that might trip it up
            ModelState.Remove("Product");

            if (ModelState.IsValid)
            {
                _db.Reviews.Add(model);
                await _db.SaveChangesAsync(); // Make sure you ran migrations!

                TempData["SuccessMessage"] = "Thank you for your review!";
                return RedirectToAction("MyOrders", "Account");
            }

            return View(model);
        }
    }
}