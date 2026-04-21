using EcoLilly.Data;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace EcoLilly.Controllers
{
    public class AdminOrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminOrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(string search)
        {
            var orders = _context.Orders.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                orders = orders.Where(o => o.Id.ToString().Contains(search));
            }

            return View(orders.ToList());
        }

        public IActionResult Delete(int id)
        {
            var order = _context.Orders.Find(id);

            if (order != null)
            {
                _context.Orders.Remove(order);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }
    }
}