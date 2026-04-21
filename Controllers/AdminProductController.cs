using EcoLilly.Data;
using EcoLilly.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EcoLilly.Controllers
{
    public class AdminProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminProductController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: /AdminProduct
        public async Task<IActionResult> Index(string search)
        {
            var products = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                products = products.Where(p => p.Name != null && p.Name.Contains(search));
            }

            var list = await products.OrderByDescending(p => p.Id).ToListAsync();
            return View(list);
        }

        // GET: /AdminProduct/Create
        public IActionResult Create()
        {
            return View(new Product());
        }

        // POST: /AdminProduct/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product model, IFormFile? ImageFile)
        {
            if (!ModelState.IsValid)
                return View(model);

            model.EcoFeatures = model.EcoFeatures ?? string.Empty;

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var uploads = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                var path = Path.Combine(uploads, fileName);
                await using var stream = new FileStream(path, FileMode.Create);
                await ImageFile.CopyToAsync(stream);
                model.Image = "/uploads/" + fileName;
            }

            _context.Products.Add(model);
            await _context.SaveChangesAsync();

            TempData["success"] = "Product added";
            return RedirectToAction(nameof(Index));
        }

        // GET: /AdminProduct/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var p = await _context.Products.FindAsync(id);
            if (p == null) return NotFound();
            return View(p);
        }

        // POST: /AdminProduct/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product model, IFormFile? ImageFile)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var existing = await _context.Products.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Name = model.Name;
            existing.Price = model.Price;
            existing.Description = model.Description;
            existing.Category = model.Category;
            existing.InStock = model.InStock;
            existing.EcoFeatures = model.EcoFeatures ?? existing.EcoFeatures ?? string.Empty;

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var uploads = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                var path = Path.Combine(uploads, fileName);
                await using var stream = new FileStream(path, FileMode.Create);
                await ImageFile.CopyToAsync(stream);
                existing.Image = "/uploads/" + fileName;
            }

            _context.Products.Update(existing);
            await _context.SaveChangesAsync();

            TempData["success"] = "Product updated";
            return RedirectToAction(nameof(Index));
        }

        // GET: /AdminProduct/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var p = await _context.Products.FindAsync(id);
            if (p != null)
            {
                _context.Products.Remove(p);
                await _context.SaveChangesAsync();
                TempData["success"] = "Product deleted";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}