using Microsoft.AspNetCore.Mvc;
using EcoLilly.Data;
using System.Linq;

namespace EcoLilly.Controllers
{
    public class ShopController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShopController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Added q parameter to support search
        public IActionResult Index(decimal? minPrice, decimal? maxPrice, string category, string q)
        {
            var products = _context.Products.AsQueryable();

            if (minPrice.HasValue)
                products = products.Where(p => p.Price >= minPrice);

            if (maxPrice.HasValue)
                products = products.Where(p => p.Price <= maxPrice);

            if (!string.IsNullOrEmpty(category) && category != "all")
                products = products.Where(p => p.Category == category);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLowerInvariant();
                products = products.Where(p => p.Name.ToLower().Contains(term) || (p.Description != null && p.Description.ToLower().Contains(term)));
                ViewBag.SearchQuery = q;
            }

            ViewBag.Categories = _context.Products
                .Select(p => p.Category)
                .Distinct()
                .ToList();

            return View(products.ToList());
        }

        public IActionResult Details(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            if (product == null) return NotFound();

            return View(product);
        }
    }
}