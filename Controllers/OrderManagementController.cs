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

                // Nếu trạng thái đơn hàng chuyển sang Thành công (Success)
                if (status == OrderStatus.Success)
                {
                    // 1. Tìm thông tin khách hàng dựa vào UserId lưu trong đơn hàng
                    var customer = await userManager.FindByIdAsync(order.UserId);

                    if (customer != null)
                    {
                        // 2. Tạo nội dung email
                        string htmlContent = GetCompletedOrderHtmlEmail(order);

                        // 3. Gửi mail trực tiếp cho khách hàng (customer.Email)
                        await _emailSender.SendEmailAsync(
                            customer.Email,
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
                    <td style='padding: 12px 0; border-bottom: 1px solid #edf2f7;'>
                        <span style='display: block; font-weight: 600; color: #2d3748;'>{item.ProductName}</span>
                    </td>
                    <td style='padding: 12px 8px; border-bottom: 1px solid #edf2f7; text-align: center; color: #718096;'>x{item.Quantity}</td>
                    <td style='padding: 12px 0; border-bottom: 1px solid #edf2f7; text-align: right; font-weight: 600; color: #2d3748;'>{item.Price:N0}đ</td>
                </tr>";
            }

            return $@"
            <div style='background-color: #f7fafc; padding: 40px 10px; font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;'>
                <div style='max-width: 600px; margin: auto; background: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.05);'>
                    <div style='background: #198754; padding: 30px; text-align: center;'>
                        <h1 style='color: #ffffff; margin: 0; font-size: 24px; letter-spacing: 1px;'>LIFE & TREES</h1>
                        <p style='color: #e2e8f0; margin-top: 5px; text-transform: uppercase; font-weight: bold;'>Đơn hàng đã hoàn tất</p>
                    </div>
                    <div style='padding: 30px;'>
                        <h2 style='color: #2d3748; font-size: 20px; margin-top: 0;'>Thông báo cho Admin,</h2>
                        <p style='color: #4a5568; line-height: 1.6;'>Đơn hàng <b>#{order.OrderId}</b> của khách hàng <b>{order.Name}</b> đã được giao thành công và chuyển sang trạng thái hoàn tất.</p>
                
                        <table style='width: 100%; border-collapse: collapse; margin-top: 20px;'>
                            <thead>
                                <tr>
                                    <th align='left' style='padding-bottom: 10px; border-bottom: 2px solid #198754; color: #198754; font-size: 14px; text-transform: uppercase;'>Sản phẩm</th>
                                    <th style='padding-bottom: 10px; border-bottom: 2px solid #198754; color: #198754; font-size: 14px; text-transform: uppercase;'>SL</th>
                                    <th align='right' style='padding-bottom: 10px; border-bottom: 2px solid #198754; color: #198754; font-size: 14px; text-transform: uppercase;'>Giá</th>
                                </tr>
                            </thead>
                            <tbody>
                                {rows}
                            </tbody>
                        </table>

                        <div style='margin-top: 20px; text-align: right;'>
                            <p style='margin: 0; color: #718096;'>Doanh thu đơn hàng:</p>
                            <h2 style='margin: 5px 0; color: #198754; font-size: 28px;'>{order.TotalAmount:N0}đ</h2>
                        </div>

                        <div style='margin-top: 30px; padding: 20px; background: #f8f9fa; border-radius: 8px;'>
                            <h4 style='margin: 0 0 10px 0; color: #2d3748;'>📍 Thông tin khách hàng</h4>
                            <p style='margin: 0; color: #4a5568; font-size: 14px;'>Khách hàng: {order.Name}</p>
                            <p style='margin: 5px 0 0 0; color: #4a5568; font-size: 14px;'>Địa chỉ: {order.Address1}, {order.City}</p>
                        </div>
                
                        <div style='margin-top: 30px; text-align: center;'>
                            <p style='font-size: 12px; color: #a0aec0;'>Hệ thống quản lý Life & Trees</p>
                        </div>
                    </div>
                </div>
            </div>";
        }
        private string GetCancelOrderHtmlEmail(Order order)
        {
            // Tận dụng lại danh sách sản phẩm để khách biết đơn nào bị hủy
            var rows = "";
            foreach (var item in order.OrderDetails)
            {
                rows += $@"
                <tr>
                    <td style='padding: 12px 0; border-bottom: 1px solid #edf2f7;'>
                        <span style='display: block; font-weight: 600; color: #2d3748; text-decoration: line-through;'>{item.ProductName}</span>
                    </td>
                    <td style='padding: 12px 8px; border-bottom: 1px solid #edf2f7; text-align: center; color: #718096;'>x{item.Quantity}</td>
                    <td style='padding: 12px 0; border-bottom: 1px solid #edf2f7; text-align: right; font-weight: 600; color: #718096;'>{item.Price:N0}đ</td>
                </tr>";
            }

            return $@"
            <div style='background-color: #fff5f5; padding: 40px 10px; font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;'>
                <div style='max-width: 600px; margin: auto; background: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.05); border-top: 5px solid #dc3545;'>
                    <div style='padding: 30px; text-align: center;'>
                        <h1 style='color: #dc3545; margin: 0; font-size: 24px; letter-spacing: 1px;'>LIFE & TREES</h1>
                        <p style='color: #718096; margin-top: 5px; text-transform: uppercase; font-weight: bold;'>Xác nhận hủy đơn hàng</p>
                    </div>
                    <div style='padding: 30px;'>
                        <h2 style='color: #2d3748; font-size: 18px; margin-top: 0;'>Chào {order.Name},</h2>
                        <p style='color: #4a5568; line-height: 1.6;'>
                            Chúng tôi xác nhận đơn hàng <b>#{order.OrderId}</b> của bạn đã được <b>HỦY</b> thành công trên hệ thống.
                        </p>

                        <div style='margin: 20px 0; padding: 20px; background: #fff5f5; border-left: 4px solid #dc3545; border-radius: 4px;'>
                            <p style='margin: 0; color: #2d3748; font-weight: bold;'>Lý do hủy:</p>
                            <p style='margin: 5px 0 0 0; color: #e53e3e;'>{(string.IsNullOrEmpty(order.CancelReason) ? "Theo yêu cầu hoặc thay đổi từ hệ thống." : order.CancelReason)}</p>
                        </div>

                        <h4 style='color: #2d3748; border-bottom: 1px solid #edf2f7; padding-bottom: 10px;'>Chi tiết đơn hàng đã hủy:</h4>
                        <table style='width: 100%; border-collapse: collapse;'>
                            <tbody>
                                {rows}
                            </tbody>
                        </table>

                        <div style='margin-top: 20px; text-align: right; opacity: 0.7;'>
                            <p style='margin: 0; color: #718096;'>Tổng giá trị hoàn lại:</p>
                            <h2 style='margin: 5px 0; color: #2d3748; font-size: 24px; text-decoration: line-through;'>{order.TotalAmount:N0}đ</h2>
                        </div>

                        <p style='color: #718096; font-size: 14px; margin-top: 30px; line-height: 1.6;'>
                            Nếu bạn không thực hiện yêu cầu này hoặc có thắc mắc, vui lòng liên hệ hotline hỗ trợ của chúng tôi ngay lập tức.
                        </p>

                        <div style='margin-top: 30px; text-align: center;'>
                            <a href='#' style='display: inline-block; background: #198754; color: white; padding: 12px 25px; text-decoration: none; border-radius: 8px; font-weight: bold;'>Quay lại cửa hàng</a>
                        </div>
                    </div>
                </div>
            </div>";
        }
    }
}