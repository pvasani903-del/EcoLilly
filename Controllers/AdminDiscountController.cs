using EcoLilly.Data;
using EcoLilly.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace EcoLilly.Controllers
{
    public class AdminDiscountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminDiscountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /AdminDiscount
        public async Task<IActionResult> Index(string search)
        {
            var discounts = _context.Discounts.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                discounts = discounts.Where(d => d.Code.Contains(search));
            }

            return View(await discounts.OrderByDescending(d => d.Id).ToListAsync());
        }

        // GET: /AdminDiscount/Create
        public IActionResult Create()
        {
            return View(new Discount { IsActive = true });
        }

        // POST: /AdminDiscount/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Discount model)
        {
            if (!ModelState.IsValid)
                return View(model);

            _context.Discounts.Add(model);
            await _context.SaveChangesAsync();

            TempData["success"] = "Discount created";
            return RedirectToAction(nameof(Index));
        }

        // GET: /AdminDiscount/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var disc = await _context.Discounts.FindAsync(id);
            if (disc == null) return NotFound();
            return View(disc);
        }

        // POST: /AdminDiscount/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Discount model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var existing = await _context.Discounts.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Code = model.Code;
            existing.Percentage = model.Percentage;
            existing.ExpiryDate = model.ExpiryDate;
            existing.IsActive = model.IsActive;

            _context.Discounts.Update(existing);
            await _context.SaveChangesAsync();

            TempData["success"] = "Discount updated";
            return RedirectToAction(nameof(Index));
        }

        // POST: /AdminDiscount/Toggle/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id)
        {
            var disc = await _context.Discounts.FindAsync(id);
            if (disc != null)
            {
                disc.IsActive = !disc.IsActive;
                _context.Discounts.Update(disc);
                await _context.SaveChangesAsync();
                TempData["success"] = disc.IsActive ? "Discount activated" : "Discount deactivated";
            }
            return RedirectToAction(nameof(Index));
        }

        // DELETE discount - now POST with anti-forgery
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var disc = await _context.Discounts.FindAsync(id);
            if (disc != null)
            {
                _context.Discounts.Remove(disc);
                await _context.SaveChangesAsync();
                TempData["success"] = "Discount deleted";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}