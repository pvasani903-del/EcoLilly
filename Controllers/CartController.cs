using EcoLilly.Data;
using EcoLilly.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace EcoLilly.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================= CART PAGE =================
        public IActionResult Index()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItems = _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserEmail == userEmail)
                .ToList();

            return View(cartItems);
        }

        // ================= ADD TO CART =================
        public IActionResult AddToCart(int id)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(userEmail))
                return RedirectToAction("Login", "Account");

            var item = _context.CartItems
                .FirstOrDefault(c => c.ProductId == id && c.UserEmail == userEmail);

            if (item != null)
                item.Quantity++;
            else
            {
                _context.CartItems.Add(new CartItem
                {
                    ProductId = id,
                    Quantity = 1,
                    UserEmail = userEmail
                });
            }

            _context.SaveChanges();

            // ✅ UPDATE COUNT
            var count = _context.CartItems.Count(c => c.UserEmail == userEmail);
            HttpContext.Session.SetInt32("CartCount", count);

            return RedirectToAction("Index");
        }
        // ================= INCREASE =================
        public IActionResult Increase(int id)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = _context.CartItems
                .FirstOrDefault(c => c.Id == id && c.UserEmail == userEmail);

            if (cart != null)
            {
                cart.Quantity++;
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        // ================= DECREASE =================
        public IActionResult Decrease(int id)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = _context.CartItems
                .FirstOrDefault(c => c.Id == id && c.UserEmail == userEmail);

            if (cart != null && cart.Quantity > 1)
            {
                cart.Quantity--;
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        // ================= REMOVE =================
        public IActionResult Remove(int id)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            var cart = _context.CartItems
                .FirstOrDefault(c => c.Id == id && c.UserEmail == userEmail);

            if (cart != null)
            {
                _context.CartItems.Remove(cart);
                _context.SaveChanges();
            }

            // ✅ UPDATE COUNT
            var count = _context.CartItems.Count(c => c.UserEmail == userEmail);
            HttpContext.Session.SetInt32("CartCount", count);

            return RedirectToAction("Index");
        }

        // ================= CHECKOUT =================
        public IActionResult Checkout()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItems = _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserEmail == userEmail)
                .ToList();

            ViewBag.Total = cartItems.Sum(i => i.Product.Price * i.Quantity);

            return View(cartItems);
        }

        // ================= PLACE ORDER =================
        [HttpPost]
        public IActionResult PlaceOrder(string name, string phone, string address, string paymentMethod)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItems = _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserEmail == userEmail)
                .ToList();

            if (!cartItems.Any())
                return RedirectToAction("Index");

            decimal total = cartItems.Sum(i => i.Product.Price * i.Quantity);

            var order = new Order
            {
                CustomerName = name,
                Address = address,
                Phone = phone,
                UserEmail = userEmail,
                TotalAmount = total,
                OrderDate = DateTime.Now,
                PaymentMethod = paymentMethod,
                PaymentStatus = paymentMethod == "Online" ? "Paid" : "Pending"
            };

            _context.Orders.Add(order);

            foreach (var item in cartItems)
            {
                _context.OrderItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Product.Price,
                    Order = order
                });
            }

            // ✅ Clear cart
            _context.CartItems.RemoveRange(cartItems);

            _context.SaveChanges();

            return RedirectToAction("Success");
        }

        // ================= SUCCESS PAGE =================
        public IActionResult Success()
        {
            return View();
        }
    }
}