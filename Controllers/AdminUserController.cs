using EcoLilly.Data;
using EcoLilly.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace EcoLilly.Controllers
{
    public class AdminUserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminUserController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /AdminUser
        public async Task<IActionResult> Index(string search)
        {
            var users = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                users = users.Where(u => u.Name.Contains(search) || u.Email.Contains(search));
            }

            var list = await users.OrderByDescending(u => u.Id).ToListAsync();
            return View(list);
        }

        // GET: /AdminUser/Create
        public IActionResult Create()
        {
            return View(new User());
        }

        // POST: /AdminUser/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Check duplicate email
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email already exists.");
                return View(model);
            }

            _context.Users.Add(model);
            await _context.SaveChangesAsync();

            TempData["success"] = "User added successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /AdminUser/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        // POST: /AdminUser/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User model)
        {
            if (id != model.Id) return BadRequest();

            // Password is required on model but not editable here.
            // Use fully-qualified nameof to avoid ambiguity with Controller.User (ClaimsPrincipal).
            ModelState.Remove(nameof(EcoLilly.Models.User.Password));

            if (!ModelState.IsValid) return View(model);

            var existing = await _context.Users.FindAsync(id);
            if (existing == null) return NotFound();

            // update fields (do not overwrite password unless provided)
            existing.Name = model.Name;
            existing.Email = model.Email;
            existing.Phone = model.Phone;
            existing.Address = model.Address;
            existing.ProfileImage = model.ProfileImage;

            _context.Users.Update(existing);
            await _context.SaveChangesAsync();

            TempData["success"] = "User updated.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /AdminUser/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                TempData["success"] = "User deleted.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}