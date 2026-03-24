using lab2.Data;
using lab2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lab2.Controllers.Management
{
    [Authorize]
    public class OrderManagementController : Controller
    {
        private readonly AppDbContext _context;

        public OrderManagementController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var orders = _context.Orders.Include(o => o.OrderDetails).ToList();
            return View("~/Views/Management/OrderManagement/Index.cshtml", orders);
        }

        public IActionResult Details(int id)
        {
            var order = _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefault(o => o.OrderId == id);

            if (order == null) return NotFound();
            return View("~/Views/Management/OrderManagement/Details.cshtml", order);
        }

        [HttpPost]
        public IActionResult UpdateStatus(int id, bool shipped)
        {
            var order = _context.Orders.Find(id);
            if (order == null) return NotFound();

            order.Shipped = shipped;
            _context.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}