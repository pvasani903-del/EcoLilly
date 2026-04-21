using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcoLilly.Data;
using EcoLilly.Models;
using System.Collections.Generic;
using System.Linq;

namespace EcoLilly.Controllers
{
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WishlistController(ApplicationDbContext context)
        {
            _context = context;
        }

        // SHOW WISHLIST
        public IActionResult Index()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail") ?? "guest";

            var list = _context.Wishlists
                .Include(w => w.Product)
                .Where(w => w.UserEmail == userEmail)
                .ToList();

            var vm = list.Select(w => new WishlistItem
            {
                Id = w.Id,
                ProductId = w.ProductId,
                Name = w.Product.Name,
                Image = w.Product.Image,
                Category = w.Product.Category,
                Description = w.Product.Description,
                EcoFeatures = w.Product.EcoFeatures,
                Price = w.Product.Price
            }).ToList();

            HttpContext.Session.SetInt32("WishlistCount", vm.Count);

            return View(vm);
        }

        // ADD PRODUCT TO WISHLIST
        public IActionResult Add(int id)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(userEmail))
                return RedirectToAction("Login", "Account");

            var exists = _context.Wishlists
                .Any(w => w.ProductId == id && w.UserEmail == userEmail);

            if (!exists)
            {
                _context.Wishlists.Add(new Wishlist
                {
                    ProductId = id,
                    UserEmail = userEmail
                });

                _context.SaveChanges();
            }

            // ✅ UPDATE COUNT
            var count = _context.Wishlists.Count(w => w.UserEmail == userEmail);
            HttpContext.Session.SetInt32("WishlistCount", count);

            return RedirectToAction("Index");
        }

        // MOVE PRODUCT TO CART
        public IActionResult AddToCart(int id)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            var wishlistItem = _context.Wishlists
                .FirstOrDefault(w => w.ProductId == id && w.UserEmail == userEmail);

            if (wishlistItem == null)
                return RedirectToAction("Index");

            var existingCart = _context.CartItems
                .FirstOrDefault(c => c.ProductId == id && c.UserEmail == userEmail);

            if (existingCart != null)
            {
                existingCart.Quantity++;
            }
            else
            {
                var cartItem = new CartItem
                {
                    ProductId = id,
                    Quantity = 1,
                    UserEmail = userEmail
                };

                _context.CartItems.Add(cartItem);
            }

            _context.Wishlists.Remove(wishlistItem);

            _context.SaveChanges();

            return RedirectToAction("Index", "Cart");
        }

        // REMOVE FROM WISHLIST
        public IActionResult Remove(int id)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            var item = _context.Wishlists
                .FirstOrDefault(w => w.Id == id && w.UserEmail == userEmail);

            if (item != null)
            {
                _context.Wishlists.Remove(item);
                _context.SaveChanges();
            }

            // ✅ UPDATE COUNT
            var count = _context.Wishlists.Count(w => w.UserEmail == userEmail);
            HttpContext.Session.SetInt32("WishlistCount", count);

            return RedirectToAction("Index");
        }
    }
}