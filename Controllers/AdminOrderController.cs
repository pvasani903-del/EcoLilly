using EcoLilly.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        // list orders with optional search (id, customer name, email, phone)
        public IActionResult Index(string search)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            var q = _context.Orders.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                // allow search by Id, CustomerName, UserEmail or Phone
                q = q.Where(o =>
                    o.Id.ToString().Contains(search) ||
                    (o.CustomerName != null && o.CustomerName.Contains(search)) ||
                    (o.UserEmail != null && o.UserEmail.Contains(search)) ||
                    (o.Phone != null && o.Phone.Contains(search))
                );
            }

            // Sort by Id descending so order numbers appear in numeric order (#latest first)
            var orders = q.OrderByDescending(o => o.Id).ToList();
            return View(orders);
        }

        [HttpPost]
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