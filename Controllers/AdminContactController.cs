using EcoLilly.Data;
using EcoLilly.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;

namespace EcoLilly.Controllers
{
    public class AdminContactController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminContactController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /AdminContact
        public async Task<IActionResult> Index(string search)
        {
            var query = _context.Contacts.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var term = search.Trim();
                query = query.Where(c => c.Name.Contains(term) || c.Email.Contains(term) || c.Subject.Contains(term));
            }

            var list = await query.OrderByDescending(c => c.Id).ToListAsync();
            return View(list);
        }

        // GET: /AdminContact/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var contact = await _context.Contacts.FindAsync(id);
            if (contact == null) return NotFound();
            return View(contact);
        }

        // GET: /AdminContact/Create
        public IActionResult Create()
        {
            return View(new Contact());
        }

        // POST: /AdminContact/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Contact model)
        {
            if (!ModelState.IsValid)
                return View(model);

            _context.Contacts.Add(model);
            await _context.SaveChangesAsync();

            TempData["success"] = "Message created.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /AdminContact/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var contact = await _context.Contacts.FindAsync(id);
            if (contact == null) return NotFound();
            return View(contact);
        }

        // POST: /AdminContact/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Contact model)
        {
            if (id != model.Id) return BadRequest();

            if (!ModelState.IsValid)
                return View(model);

            var existing = await _context.Contacts.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Name = model.Name;
            existing.Email = model.Email;
            existing.Subject = model.Subject;
            existing.Message = model.Message;

            _context.Contacts.Update(existing);
            await _context.SaveChangesAsync();

            TempData["success"] = "Message updated.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /AdminContact/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var contact = await _context.Contacts.FindAsync(id);
            if (contact != null)
            {
                _context.Contacts.Remove(contact);
                await _context.SaveChangesAsync();
                TempData["success"] = "Message deleted.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}