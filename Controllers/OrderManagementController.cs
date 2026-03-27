using lab2.Data;
using lab2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lab2.Controllers
{
    [Authorize(Roles = "Admin")]
    public class OrderManagementController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> userManager;
        private readonly IEmailSender _emailSender;

        public OrderManagementController(AppDbContext context,
                                        UserManager<IdentityUser> userManager,
                                        IEmailSender emailSender)
        {
            _context = context;
            this.userManager = userManager;
            _emailSender = emailSender;
        }

        // Trang danh sách đơn hàng cho Admin
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product) // Thêm dòng này nếu muốn lọc theo tên sản phẩm
                .OrderByDescending(o => o.OrderPlaced)
                .ToListAsync();

            return View("~/Views/Management/OrderManagement/Index.cshtml", orders);
        }

        // Trang chi tiết đơn hàng cho Admin
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (order == null) return NotFound();

            return View("~/Views/Management/OrderManagement/Details.cshtml", order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, OrderStatus status)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order != null)
            {
                order.Status = status;
                await _context.SaveChangesAsync();

                if (status == OrderStatus.Success)
                {
                    var adminUsers = await userManager.GetUsersInRoleAsync("Admin");
                    foreach (var admin in adminUsers)
                    {
                        string htmlContent = GetCompletedOrderHtmlEmail(order);
                        await _emailSender.SendEmailAsync(
                            admin.Email,
                            $"[Life & Trees] Đơn hàng #{order.OrderId} đã hoàn tất",
                            htmlContent
                        );
                    }
                }
            }
            TempData["message"] = $"Đã cập nhật trạng thái đơn hàng #{orderId}";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmCancel(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order != null && order.Status == OrderStatus.CancelRequested)
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        order.Status = OrderStatus.Cancelled;
                        foreach (var item in order.OrderDetails)
                        {
                            var product = await _context.Products.FindAsync(item.ProductId);
                            if (product != null)
                            {
                                product.Quantity += item.Quantity;
                                product.SoldQuantity -= item.Quantity;
                            }
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        try
                        {
                            var customer = await userManager.FindByIdAsync(order.UserId);
                            if (customer != null)
                            {
                                string htmlContent = GetCancelOrderHtmlEmail(order);
                                await _emailSender.SendEmailAsync(customer.Email, $"[Life & Trees] Xác nhận hủy đơn hàng #{order.OrderId}", htmlContent);
                            }
                        }
                        catch { }

                        TempData["SuccessMessage"] = "Đã xác nhận hủy đơn hàng thành công.";
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        TempData["ErrorMessage"] = "Có lỗi xảy ra khi xử lý dữ liệu.";
                    }
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // ================= EMAIL HELPERS FOR ADMIN =================

        private string GetCompletedOrderHtmlEmail(Order order)
        {
            var rows = "";
            foreach (var item in order.OrderDetails)
            {
                rows += $@"
        <tr>
            <td>{item.ProductName}</td>
            <td>x{item.Quantity}</td>
            <td>{item.Price:N0}đ</td>
        </tr>";
            }

            return $@"
    <div style='font-family: Segoe UI, sans-serif;'>
        <h2 style='color: #198754;'>Thông báo đơn hàng hoàn tất</h2>
        <p>Đơn hàng <b>#{order.OrderId}</b> của khách hàng <b>{order.Name}</b> đã được xử lý thành công.</p>
        <table style='width:100%; border-collapse: collapse; margin-top: 20px;'>
            <thead>
                <tr>
                    <th>Sản phẩm</th>
                    <th>SL</th>
                    <th>Giá</th>
                </tr>
            </thead>
            <tbody>
                {rows}
            </tbody>
        </table>
        <p style='margin-top: 20px;'>Tổng giá trị: <b>{order.TotalAmount:N0}đ</b></p>
    </div>";
        }

        private string GetCancelOrderHtmlEmail(Order order)
        {
            return $@"
    <div style='background-color: #fff5f5; padding: 40px 10px; font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;'>
        <div style='max-width: 600px; margin: auto; background: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.05); border-top: 5px solid #dc3545;'>
            <div style='padding: 30px; text-align: center;'>
                <h1 style='color: #dc3545; margin: 0; font-size: 24px; letter-spacing: 1px;'>LIFE & TREES</h1>
                <p style='color: #718096; margin-top: 5px; text-transform: uppercase; font-weight: bold;'>Thông báo hủy đơn hàng</p>
            </div>
            <div style='padding: 30px; border-top: 1px solid #edf2f7;'>
                <h2 style='color: #2d3748; font-size: 18px; margin-top: 0;'>Chào {order.Name},</h2>
                <p style='color: #4a5568; line-height: 1.6;'>
                    Đơn hàng <b>#{order.OrderId}</b> của bạn đã được hệ thống xác nhận <b>HỦY</b>.
                </p>
                <div style='margin: 20px 0; padding: 20px; background: #f8f9fa; border-left: 4px solid #dc3545; border-radius: 4px;'>
                    <p style='margin: 0; color: #2d3748; font-weight: bold;'>Lý do hủy:</p>
                    <p style='margin: 5px 0 0 0; color: #4a5568;'>{(string.IsNullOrEmpty(order.CancelReason) ? "Theo yêu cầu của khách hàng hoặc shop." : order.CancelReason)}</p>
                </div>
                <p style='color: #4a5568; line-height: 1.6;'>
                    Số tiền và sản phẩm của đơn hàng này đã được hoàn lại vào kho. Nếu bạn có bất kỳ thắc mắc nào hoặc muốn đặt lại sản phẩm khác, đừng ngần ngại liên hệ với chúng tôi.
                </p>
                <div style='margin-top: 30px; text-align: center;'>
                    <a href='#' style='background: #198754; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Tiếp tục mua sắm</a>
                </div>
            </div>
        </div>
    </div>";
        }
    }
}