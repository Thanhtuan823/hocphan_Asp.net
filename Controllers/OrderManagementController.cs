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
        [Authorize(Roles = "Admin")] // Chỉ Admin mới được quyền đổi trạng thái
        public IActionResult UpdateStatus(int orderId, OrderStatus status)
        {
            // Tìm đơn hàng theo ID (Lưu ý: dùng 'orderId' cho khớp với View)
            var order = _context.Orders.Find(orderId);

            if (order == null)
            {
                return NotFound();
            }

            // Cập nhật trạng thái mới (Pending, Shipping, Success, Cancelled)
            order.Status = status;

            // Lưu thay đổi vào Database
            _context.SaveChanges();

            // Thông báo cho Admin (Tùy chọn)
            TempData["message"] = $"Đã cập nhật đơn hàng #{order.OrderId} thành công.";

            return RedirectToAction("Index");
        }
    }
}