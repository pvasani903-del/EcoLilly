using Microsoft.AspNetCore.Mvc;
using EcoLilly.Data;
using EcoLilly.Models;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace EcoLilly.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // HOME PAGE
        // =========================
        public IActionResult Index(string search)
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "User")
                return RedirectToAction("Login", "Account");

            var email = HttpContext.Session.GetString("UserEmail");
            ViewBag.UserEmail = email;

            var productsQuery = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                productsQuery = productsQuery
                                .Where(p => p.Name.Contains(search));
            }

            var products = productsQuery.ToList();

            return View(products);
        }

        // =========================
        // ABOUT PAGE
        // =========================
        [HttpGet]
        public IActionResult About(bool preview = false)
        {
            ViewBag.IsPreview = preview;
            return View();
        }


        // =========================
        // CONTACT PAGE (GET)
        // =========================
        [HttpGet]
        public IActionResult Contact()
        {
            return View();
        }


        // =========================
        // CONTACT PAGE (POST)
        // =========================
        [HttpPost]
        public IActionResult Contact(Contact model)
        {
            if (ModelState.IsValid)
            {
                _context.Contacts.Add(model);
                _context.SaveChanges();

                ViewBag.Success = "Message sent successfully!";
                ModelState.Clear();
            }

            return View(model);
        }

    }
}