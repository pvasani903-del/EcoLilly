using EcoLilly.Data;
using EcoLilly.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace EcoLilly.Controllers
{
    public class AdminCartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminCartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Show list of users with carts (grouped) and item counts
        public IActionResult Index(string search)
        {
            var carts = _context.CartItems
                .Include(c => c.Product)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                carts = carts.Where(c => c.Product != null && c.Product.Name.Contains(search));
            }

            var grouped = carts
                .GroupBy(c => c.UserEmail)
                .Select(g => new
                {
                    UserEmail = g.Key,
                    ItemsCount = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(g => g.ItemsCount)
                .ToList();

            var vm = grouped.Select(g => new AdminCartUserViewModel
            {
                UserEmail = g.UserEmail ?? "guest",
                ItemsCount = g.ItemsCount
            }).ToList();

            return View(vm);
        }

        // Show all cart items for a specific user, also provide products for Add form
        public IActionResult Details(string userEmail)
        {
            if (string.IsNullOrEmpty(userEmail))
                return RedirectToAction("Index");

            var items = _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserEmail == userEmail)
                .ToList();

            ViewBag.UserEmail = userEmail;
            ViewBag.Products = _context.Products.OrderBy(p => p.Name).Select(p => new { p.Id, p.Name, p.Price }).ToList();

            return View(items);
        }

        // GET: /AdminCart/Edit/5
        public IActionResult Edit(int id)
        {
            var item = _context.CartItems
                .Include(c => c.Product)
                .FirstOrDefault(c => c.Id == id);

            if (item == null)
                return NotFound();

            ViewBag.Products = _context.Products.OrderBy(p => p.Name).Select(p => new { p.Id, p.Name, p.Price }).ToList();
            return View(item);
        }

        // POST: /AdminCart/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, int productId, int quantity)
        {
            if (quantity < 1)
            {
                ModelState.AddModelError(nameof(quantity), "Quantity must be at least 1.");
            }

            var item = _context.CartItems.Find(id);
            if (item == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Products = _context.Products.OrderBy(p => p.Name).Select(p => new { p.Id, p.Name, p.Price }).ToList();
                var vm = _context.CartItems.Include(c => c.Product).FirstOrDefault(c => c.Id == id);
                return View(vm);
            }

            // update product reference and quantity
            item.ProductId = productId;
            item.Quantity = quantity;
            _context.CartItems.Update(item);
            _context.SaveChanges();

            TempData["success"] = "Cart item updated.";
            return RedirectToAction("Details", new { userEmail = item.UserEmail });
        }

        // Add item to user's cart (admin action)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddItem(string userEmail, int productId, int quantity = 1)
        {
            if (string.IsNullOrEmpty(userEmail) || quantity < 1)
                return RedirectToAction("Details", new { userEmail });

            var product = _context.Products.Find(productId);
            if (product == null)
                return RedirectToAction("Details", new { userEmail });

            var existing = _context.CartItems.FirstOrDefault(c => c.UserEmail == userEmail && c.ProductId == productId);
            if (existing != null)
            {
                existing.Quantity += quantity;
                _context.CartItems.Update(existing);
            }
            else
            {
                _context.CartItems.Add(new CartItem
                {
                    UserEmail = userEmail,
                    ProductId = productId,
                    Quantity = quantity
                });
            }

            _context.SaveChanges();
            TempData["success"] = "Item added/updated";
            return RedirectToAction("Details", new { userEmail });
        }

        // Update quantity (admin)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateQuantity(int id, int quantity)
        {
            var item = _context.CartItems.Find(id);
            if (item != null)
            {
                if (quantity <= 0)
                {
                    _context.CartItems.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                    _context.CartItems.Update(item);
                }
                _context.SaveChanges();
                TempData["success"] = "Quantity updated";
                return RedirectToAction("Details", new { userEmail = item.UserEmail });
            }

            return RedirectToAction("Index");
        }

        // Increase by 1 (admin)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult IncreaseItem(int id)
        {
            var item = _context.CartItems.Find(id);
            if (item != null)
            {
                item.Quantity++;
                _context.SaveChanges();
                TempData["success"] = "Quantity increased";
                return RedirectToAction("Details", new { userEmail = item.UserEmail });
            }
            return RedirectToAction("Index");
        }

        // Decrease by 1 (admin)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DecreaseItem(int id)
        {
            var item = _context.CartItems.Find(id);
            if (item != null)
            {
                if (item.Quantity > 1)
                {
                    item.Quantity--;
                    _context.SaveChanges();
                    TempData["success"] = "Quantity decreased";
                }
                else
                {
                    // remove if quantity would go to zero
                    var email = item.UserEmail;
                    _context.CartItems.Remove(item);
                    _context.SaveChanges();
                    TempData["success"] = "Item removed";
                    return RedirectToAction("Details", new { userEmail = email });
                }

                return RedirectToAction("Details", new { userEmail = item.UserEmail });
            }
            return RedirectToAction("Index");
        }

        // Remove a single cart item (admin) - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var cart = _context.CartItems.Find(id);

            if (cart != null)
            {
                var email = cart.UserEmail;
                _context.CartItems.Remove(cart);
                _context.SaveChanges();
                TempData["success"] = "Item removed from cart";
                return RedirectToAction("Details", new { userEmail = email });
            }

            return RedirectToAction("Index");
        }

        // Clear entire cart for a user (admin action)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Clear(string userEmail)
        {
            if (string.IsNullOrEmpty(userEmail))
                return RedirectToAction("Index");

            var items = _context.CartItems.Where(c => c.UserEmail == userEmail).ToList();
            if (items.Any())
            {
                _context.CartItems.RemoveRange(items);
                _context.SaveChanges();
                TempData["success"] = $"Cleared {items.Count} items for {userEmail}";
            }

            return RedirectToAction("Index");
        }
    }

    // small view model used by AdminCart/Index
    public class AdminCartUserViewModel
    {
        public string UserEmail { get; set; } = string.Empty;
        public int ItemsCount { get; set; }
    }
}