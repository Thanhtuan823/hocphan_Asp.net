using lab2.Data;
using lab2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace lab2.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> userManager;
        private readonly IEmailSender _emailSender;

        public OrderController(AppDbContext context,
                                UserManager<IdentityUser> userManager,
                                IEmailSender emailSender)
        {
            _context = context;
            this.userManager = userManager;
            _emailSender = emailSender;
        }

        // ================= GET: Hiển thị trang nhập địa chỉ (Nếu cần) =================
        [HttpGet]
        public async Task<IActionResult> Address()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var lastOrder = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderPlaced)
                .FirstOrDefaultAsync();

            return View(lastOrder ?? new Order());
        }

        // ================= GET: Hiển thị trang Checkout (Tóm tắt đơn hàng) =================
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await userManager.FindByIdAsync(userId);

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.Items.Any()) return RedirectToAction("Index", "Cart");

            ViewBag.CartItems = cart.Items.ToList();

            var order = new Order();
            var lastOrder = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderPlaced)
                .FirstOrDefaultAsync();

            if (lastOrder != null)
            {
                order.Name = lastOrder.Name;
                order.PhoneNumber = currentUser?.PhoneNumber ?? lastOrder.PhoneNumber;
                order.Address1 = lastOrder.Address1;
                order.Address2 = lastOrder.Address2;
                order.City = lastOrder.City;
                order.Zip = lastOrder.Zip;
            }
            return View(order);
        }

        // ================= POST: Xử lý đặt hàng chính =================
        [HttpGet]
        public async Task<IActionResult> Completed(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return RedirectToAction("Index", "Home");

            return View(order); // Truyền Model sang View để hiển thị
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(Order order, string? discountCode, decimal ShippingFee, string PaymentMethod)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            order.UserId = userId;

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.Items.Any()) return RedirectToAction("Index", "Cart");

            // Loại bỏ các trường không lấy từ Form để ModelState hợp lệ
            ModelState.Remove("UserId");
            ModelState.Remove("CancelReason");
            ModelState.Remove("OrderDetails");
            ModelState.Remove("PaymentMethod");

            if (ModelState.IsValid)
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // 1. Thiết lập thông tin cơ bản
                        order.OrderPlaced = DateTime.Now;
                        order.Status = OrderStatus.Pending;
                        order.ShippingFee = ShippingFee;
                        order.DiscountCode = discountCode;

                        // 2. Tính toán tổng tiền
                        decimal subTotal = cart.Items.Sum(i => i.Price * i.Quantity);
                        decimal vat = subTotal * 0.1m;
                        decimal totalBeforeDiscount = subTotal + vat + ShippingFee;

                        // Áp mã giảm giá 20% nếu khớp mã
                        if (!string.IsNullOrEmpty(discountCode) && discountCode.Trim().ToLower() == "khachhangtiemnang")
                            order.TotalAmount = totalBeforeDiscount * 0.8m;
                        else
                            order.TotalAmount = totalBeforeDiscount;

                        order.OrderDetails = new List<OrderDetail>();

                        // 3. Trừ kho và tạo chi tiết đơn hàng
                        foreach (var item in cart.Items)
                        {
                            var product = await _context.Products.FindAsync(item.ProductId);
                            if (product == null || product.Quantity < item.Quantity)
                            {
                                ModelState.AddModelError("", $"Sản phẩm '{item.ProductName}' đã hết hàng hoặc không đủ tồn kho.");
                                ViewBag.CartItems = cart.Items.ToList();
                                return View(order);
                            }
                            product.Quantity -= item.Quantity;
                            product.SoldQuantity += item.Quantity;

                            order.OrderDetails.Add(new OrderDetail
                            {
                                ProductId = item.ProductId,
                                ProductName = item.ProductName,
                                Price = item.Price,
                                Quantity = item.Quantity
                            });
                        }

                        // 4. Lưu vào Database
                        _context.Orders.Add(order);
                        _context.CartItems.RemoveRange(cart.Items); // Xóa giỏ hàng sau khi đặt thành công
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        // 5. Gửi Email thông báo (Chạy ngầm hoặc try-catch để không làm treo trang)
                        try
                        {
                            var user = await userManager.GetUserAsync(User);
                            if (user != null)
                            {
                                string htmlEmail = GetOrderHtmlEmail(order);
                                await _emailSender.SendEmailAsync(user.Email, $"[Life & Trees] Xác nhận đơn hàng #{order.OrderId}", htmlEmail);
                            }
                        }
                        catch { /* Log lỗi gửi mail nếu cần */ }

                        // 6. Rẽ nhánh điều hướng
                        if (PaymentMethod == "BankTransfer")
                        {
                            // Đi tới trang quét mã QR
                            return RedirectToAction("CheckoutBank", new { id = order.OrderId });
                        }

                        return RedirectToAction(nameof(Completed), new { id = order.OrderId });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
                    }
                }
            }

            ViewBag.CartItems = cart?.Items.ToList();
            return View(order);
        }

        // ================= GET: Trang Ngân hàng (QR Code) =================
        [HttpGet]
        public async Task<IActionResult> CheckoutBank(int id)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == id);
            if (order == null) return NotFound();
            return View(order);
        }

        // ================= GET: Lịch sử đơn hàng =================
        public async Task<IActionResult> History()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderPlaced)
                .ToListAsync();
            return View(orders);
        }


        // ================= POST: Khách hàng gửi yêu cầu hủy đơn =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestCancel(int orderId, string cancelReason)
        {
      
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return NotFound();

         
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (order.UserId != userId) return Forbid();

         
            if (order.Status == OrderStatus.Success || order.Status == OrderStatus.Cancelled)
            {
                TempData["Error"] = "Đơn hàng này không thể gửi yêu cầu hủy nữa.";
                return RedirectToAction(nameof(History), new { id = orderId });
            }

     
            order.Status = OrderStatus.CancelRequested;
            order.CancelReason = cancelReason;

            _context.Update(order);
            await _context.SaveChangesAsync();

            // 5. Gửi Email thông báo yêu cầu hủy
            try
            {
                var user = await userManager.GetUserAsync(User);
                if (user != null)
                {
                    string htmlEmail = GetCancelRequestEmail(order);
                    await _emailSender.SendEmailAsync(
                        user.Email,
                        $"[Life & Trees] Xác nhận tiếp nhận yêu cầu hủy đơn #{order.OrderId}",
                        htmlEmail
                    );
                }
            }
            catch { /* Log lỗi gửi mail nếu cần */ }

            TempData["SuccessMessage"] = "Yêu cầu hủy đơn đã được gửi và đang chờ xét duyệt.";


            return RedirectToAction(nameof(History), new { id = orderId });
        }

        // ================= PRIVATE: Template Email xác nhận =================
        private string GetCancelRequestEmail(Order order)
        {
            var rows = "";
            foreach (var item in order.OrderDetails)
            {
                rows += $@"
        <tr>
            <td style='padding: 12px 0; border-bottom: 1px solid #eee;'>{item.ProductName}</td>
            <td style='padding: 12px 8px; border-bottom: 1px solid #eee; text-align: center;'>x{item.Quantity}</td>
            <td style='padding: 12px 0; border-bottom: 1px solid #eee; text-align: right;'>{item.Price:N0}đ</td>
        </tr>";
            }

            return $@"
    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; border: 1px solid #dee2e6; padding: 25px; border-top: 5px solid #dc3545;'>
        <h2 style='color: #dc3545; text-align: center;'>YÊU CẦU HỦY ĐƠN HÀNG</h2>
        <p>Chào <b>{order.Name}</b>,</p>
        <p>Chúng tôi đã nhận được yêu cầu hủy đơn hàng <b>#{order.OrderId}</b> của bạn.</p>
        
        <div style='background: #fff3cd; border: 1px solid #ffeeba; padding: 15px; margin: 20px 0; color: #856404;'>
            <h4 style='margin-top: 0;'>Chi tiết yêu cầu:</h4>
            <p style='margin-bottom: 5px;'><b>Lý do:</b> {order.CancelReason}</p>
            <p style='margin-bottom: 0;'><b>Trạng thái:</b> Đang chờ Admin xác nhận</p>
        </div>

        <table style='width: 100%; border-collapse: collapse;'>
            <thead>
                <tr style='background: #f8f9fa;'>
                    <th align='left' style='padding: 10px;'>Sản phẩm</th>
                    <th>SL</th>
                    <th align='right' style='padding: 10px;'>Giá</th>
                </tr>
            </thead>
            <tbody>{rows}</tbody>
        </table>

        <div style='text-align: right; margin-top: 20px;'>
            <h3 style='color: #dc3545;'>Tổng cộng: {order.TotalAmount:N0}đ</h3>
        </div>
        
        <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'/>
        <p style='font-size: 12px; color: #777; text-align: center;'>
            Chúng tôi sẽ kiểm tra tình trạng vận chuyển và phản hồi bạn trong thời gian sớm nhất.
        </p>
    </div>";
        }
    
        private string GetOrderHtmlEmail(Order order)
        {
            var rows = "";
            foreach (var item in order.OrderDetails)
            {
                rows += $@"
                <tr>
                    <td style='padding: 12px 0; border-bottom: 1px solid #eee;'>{item.ProductName}</td>
                    <td style='padding: 12px 8px; border-bottom: 1px solid #eee; text-align: center;'>x{item.Quantity}</td>
                    <td style='padding: 12px 0; border-bottom: 1px solid #eee; text-align: right;'>{item.Price:N0}đ</td>
                </tr>";
            }

            return $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; border: 1px solid #ddd; padding: 20px;'>
                <h2 style='color: #198754; text-align: center;'>CẢM ƠN BẠN ĐÃ ĐẶT HÀNG!</h2>
                <p>Chào <b>{order.Name}</b>, đơn hàng <b>#{order.OrderId}</b> của bạn đã được tiếp nhận.</p>
                <table style='width: 100%; border-collapse: collapse;'>
                    <thead>
                        <tr style='background: #f8f9fa;'>
                            <th align='left'>Sản phẩm</th>
                            <th>SL</th>
                            <th align='right'>Giá</th>
                        </tr>
                    </thead>
                    <tbody>{rows}</tbody>
                </table>
                <div style='text-align: right; margin-top: 20px;'>
                    <p>Phí vận chuyển: <b>{order.ShippingFee:N0}đ</b></p>
                    <h3 style='color: #dc3545;'>Tổng cộng: {order.TotalAmount:N0}đ</h3>
                </div>
                <hr/>
                <p style='font-size: 12px; color: #777;'>Địa chỉ nhận hàng: {order.Address1}, {order.City}</p>
            </div>";
        }
    }
}