using EcoLilly.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace EcoLilly.Controllers
{
    public class AdminWishlistController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminWishlistController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Index with optional search by product name or user email
        public IActionResult Index(string search)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            var wishlists = _context.Wishlists
                .Include(w => w.Product)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                wishlists = wishlists.Where(w =>
                    (w.Product != null && w.Product.Name.Contains(search)) ||
                    (w.UserEmail != null && w.UserEmail.Contains(search))
                );
            }

            var list = wishlists.ToList();
            return View(list);
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var wishlist = _context.Wishlists.Find(id);
            if (wishlist != null)
            {
                _context.Wishlists.Remove(wishlist);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}