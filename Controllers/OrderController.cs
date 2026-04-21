using EcoLilly.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore; // Added for .Include()

namespace EcoLilly.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            // Include OrderItems, Product, and Reviews to display them in the view
            var orders = _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Reviews)
                .Where(o => o.UserEmail == userEmail)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }


        // APPLY COUPON
        [HttpPost]
        public IActionResult ApplyCoupon([FromBody] CouponRequest request)
        {
            var discount = _context.Discounts
                .FirstOrDefault(d =>
                    d.Code == request.code &&
                    d.IsActive &&
                    d.ExpiryDate >= DateTime.Now);

            if (discount == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Invalid or Expired Coupon"
                });
            }

            decimal discountAmount = request.subtotal * discount.Percentage / 100;
            decimal finalAmount = request.subtotal - discountAmount;

            return Json(new
            {
                success = true,
                percentage = discount.Percentage,
                discountAmount = discountAmount,
                finalAmount = finalAmount
            });
        }


        // MODEL FOR AJAX REQUEST
        public class CouponRequest
        {
            public string code { get; set; }
            public decimal subtotal { get; set; }
        }
    }
}