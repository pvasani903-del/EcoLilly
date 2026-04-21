using EcoLilly.Data;
using EcoLilly.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace EcoLilly.Controllers
{
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ContactController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Contact
        [HttpGet]
        public IActionResult Index()
        {
            // return an empty model so asp-for bindings always have a model
            return View(new Contact());
        }

        // POST: /Contact
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(Contact model)
        {
            if (!ModelState.IsValid)
            {
                // validation failed — show errors on the same page
                return View(model);
            }

            // save to DB
            _context.Contacts.Add(model);
            await _context.SaveChangesAsync();

            // use TempData so message survives the redirect
            TempData["ContactSuccess"] = "Message sent successfully!";

            // Redirect to GET to prevent double-post and to clear model state
            return RedirectToAction(nameof(Index));
        }
    }
}