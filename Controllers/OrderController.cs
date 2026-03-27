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

        // ================= USER AREA (ADDRESS & CHECKOUT) =================

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Address(Order model)
        {
            ModelState.Remove("UserId");
            ModelState.Remove("CancelReason");
            ModelState.Remove("OrderDetails");

            if (ModelState.IsValid)
            {
                TempData["Name"] = model.Name;
                TempData["Phone"] = model.PhoneNumber;
                TempData["Add1"] = model.Address1;
                TempData["Add2"] = model.Address2;
                TempData["City"] = model.City;
                TempData["Zip"] = model.Zip;

                return RedirectToAction(nameof(Checkout));
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await userManager.FindByIdAsync(userId);
            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);
            ViewBag.CartItems = cart?.Items.ToList() ?? new List<CartItem>();

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
            else if (currentUser != null)
            {
                order.PhoneNumber = currentUser.PhoneNumber;
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(Order order, string? discountCode)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            order.UserId = userId;

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.Items.Any())
                return RedirectToAction("Index", "Cart");

            ModelState.Remove("UserId");
            ModelState.Remove("CancelReason");
            ModelState.Remove("OrderDetails");
            ModelState.Remove("discountCode");

            if (ModelState.IsValid)
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        order.OrderPlaced = DateTime.Now;
                        order.Status = OrderStatus.Pending;

                        decimal subTotal = cart.Items.Sum(i => i.Price * i.Quantity);
                        decimal vat = subTotal * 0.1m;
                        decimal totalWithVat = subTotal + vat;

                        if (!string.IsNullOrEmpty(discountCode) && discountCode.Trim().ToLower() == "khachhangtiemnang")
                        {
                            totalWithVat = totalWithVat * 0.8m;
                        }

                        order.TotalAmount = totalWithVat;
                        order.OrderDetails = new List<OrderDetail>();

                        foreach (var item in cart.Items)
                        {
                            var product = await _context.Products.FindAsync(item.ProductId);
                            if (product == null || product.Quantity < item.Quantity)
                            {
                                ModelState.AddModelError("", $"Sản phẩm '{item.ProductName}' không đủ tồn kho.");
                                ViewBag.CartItems = cart.Items.ToList();
                                return View(order);
                            }

                            // Trừ số lượng tồn kho
                            product.Quantity -= item.Quantity;

                            // =========================================================
                            // THÊM MỚI: Cộng dồn số lượng đã bán
                            // =========================================================
                            product.SoldQuantity += item.Quantity;
                            order.OrderDetails.Add(new OrderDetail
                            {
                                ProductId = item.ProductId,
                                ProductName = item.ProductName,
                                Price = item.Price,
                                Quantity = item.Quantity
                            });
                        }

                        _context.Orders.Add(order);
                        _context.CartItems.RemoveRange(cart.Items);

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        var user = await userManager.GetUserAsync(User);
                        if (user != null)
                        {
                            string htmlContent = GetOrderHtmlEmail(order);
                            await _emailSender.SendEmailAsync(user.Email, $"[Life & Trees] Xác nhận đơn hàng #{order.OrderId}", htmlContent);
                        }
                        return RedirectToAction(nameof(Completed));
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                        ModelState.AddModelError("", "Lỗi hệ thống: " + msg);
                    }
                }
            }
            ViewBag.CartItems = cart.Items.ToList();
            return View(order);
        }

        [HttpGet]
        public IActionResult Completed() => View();

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestCancel(int orderId, string reason)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);

            if (order != null && order.Status == OrderStatus.Pending)
            {
                order.Status = OrderStatus.CancelRequested;
                order.CancelReason = reason;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã gửi yêu cầu hủy đơn hàng.";
            }
            return RedirectToAction(nameof(History));
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
.Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (order == null) return NotFound();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("Admin") && order.UserId != userId) return Forbid();

            return View(order);
        }

        private string GetOrderHtmlEmail(Order order)
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
                <p style='color: #e2e8f0; margin-top: 5px;'>Xác nhận đơn hàng thành công</p>
            </div>
            <div style='padding: 30px;'>
                <h2 style='color: #2d3748; font-size: 20px; margin-top: 0;'>Chào {order.Name},</h2>
                <p style='color: #4a5568; line-height: 1.6;'>Cảm ơn bạn đã tin tưởng lựa chọn <b>Life & Trees</b>. Đơn hàng <b>#{order.OrderId}</b> của bạn đã được hệ thống tiếp nhận và đang chờ xử lý.</p>
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
                    <p style='margin: 0; color: #718096;'>Tổng giá trị đơn hàng:</p>
                    <h2 style='margin: 5px 0; color: #dc3545; font-size: 28px;'>{order.TotalAmount:N0}đ</h2>
                </div>
                <div style='margin-top: 30px; padding: 20px; background: #f8f9fa; border-radius: 8px;'>
                    <h4 style='margin: 0 0 10px 0; color: #2d3748;'>📍 Thông tin nhận hàng</h4>
                    <p style='margin: 0; color: #4a5568; font-size: 14px;'>{order.Address1}, {order.City}</p>
                </div>
                <div style='margin-top: 30px; text-align: center;'>
                    <p style='font-size: 12px; color: #a0aec0;'>Đây là email tự động, vui lòng không phản hồi email này.</p>
                </div>
            </div>
        </div>
    </div>";
        }
    }
}