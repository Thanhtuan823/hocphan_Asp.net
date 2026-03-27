using lab2.Data;
using lab2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace lab2.Controllers
{
    [Authorize]
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
            // Lấy toàn bộ đơn hàng từ database
            var orders = _context.Orders.ToList();

            var model = new AdminDashboard
            {
                TotalUsers = _userManager.Users.Count(),
                TotalProducts = _context.Products.Count(),
                TotalOrders = orders.Count,

                // CHỐT: Chỉ tính tiền những đơn hàng có trạng thái Success
                TotalRevenue = orders.Where(o => o.Status == OrderStatus.Success).Sum(o => o.TotalAmount),

                // Serialize dữ liệu để đẩy sang JavaScript xử lý Filter/Sort theo ngày
                RawOrdersJson = JsonConvert.SerializeObject(orders.Select(o => new {
                    Date = o.OrderPlaced.ToString("yyyy-MM-dd"),
                    Amount = o.TotalAmount,
                    Status = o.Status.ToString() // Trả về "Success", "Cancelled", v.v.
                }))
            };

            return View(model);
        }
    }
}