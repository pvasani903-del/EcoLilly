using Microsoft.AspNetCore.Mvc;
using EcoLilly.Data;
using EcoLilly.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;

namespace EcoLilly.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public ProductController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // Index with search
        public async Task<IActionResult> Index(string q)
        {
            var products = _db.Products.AsQueryable();
            if (!string.IsNullOrEmpty(q))
                products = products.Where(p => p.Name != null && p.Name.Contains(q));
            return View(await products.OrderByDescending(p => p.Id).ToListAsync());
        }

        public IActionResult Create()
        {
            return View(new Product());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product model, IFormFile? image)
        {
            if (!ModelState.IsValid) return View(model);

            // Ensure EcoFeatures is not null (DB column is non-nullable)
            model.EcoFeatures = model.EcoFeatures ?? string.Empty;

            if (image != null && image.Length > 0)
            {
                var uploads = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);
                var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
                var filePath = Path.Combine(uploads, fileName);
                using var fs = new FileStream(filePath, FileMode.Create);
                await image.CopyToAsync(fs);
                model.Image = "/uploads/" + fileName;
            }

            _db.Products.Add(model);
            await _db.SaveChangesAsync();
            TempData["success"] = "Product added";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var p = await _db.Products.FindAsync(id);
            if (p == null) return NotFound();
            return View(p);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product model, IFormFile? image)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var existing = await _db.Products.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Name = model.Name;
            existing.Price = model.Price;
            existing.Description = model.Description;
            existing.Category = model.Category;
            existing.InStock = model.InStock;
            // Keep existing value if model.EcoFeatures is null; otherwise update (and guard against null)
            existing.EcoFeatures = model.EcoFeatures ?? existing.EcoFeatures ?? string.Empty;

            if (image != null && image.Length > 0)
            {
                var uploads = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);
                var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
                var filePath = Path.Combine(uploads, fileName);
                using var fs = new FileStream(filePath, FileMode.Create);
                await image.CopyToAsync(fs);
                existing.Image = "/uploads/" + fileName;
            }

            _db.Products.Update(existing);
            await _db.SaveChangesAsync();
            TempData["success"] = "Product updated";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var p = await _db.Products.FindAsync(id);
            if (p != null)
            {
                _db.Products.Remove(p);
                await _db.SaveChangesAsync();
                TempData["success"] = "Product deleted";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}