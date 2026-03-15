using lab2.Data;
using lab2.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace lab2.Controllers.Management
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AdminController(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Dashboard()
        {
            var model = new AdminDashboard
            {
                TotalUsers = _userManager.Users.Count(),
                TotalOrders = _context.Orders.Count(),
                TotalRevenue = _context.Orders.Sum(o => o.TotalAmount)
            };

            return View(model);
        }
    }
}