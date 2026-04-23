using EcoLilly.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace EcoLilly.Controllers
{
    public class AdminReviewController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminReviewController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            var reviews = _context.Reviews
                .Include(r => r.Product)
                .OrderByDescending(r => r.Date)
                .ToList();

            return View(reviews);
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var review = _context.Reviews.Find(id);
            if (review != null)
            {
                _context.Reviews.Remove(review);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}