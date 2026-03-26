using lab2.Data;
using lab2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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

        public OrderController(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            this.userManager = userManager;
        }

        // ================= USER AREA (ADDRESS & CHECKOUT) =================

        // 1. GET: Nhập địa chỉ giao hàng
        [HttpGet]
        public async Task<IActionResult> Address()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Gợi ý địa chỉ từ đơn hàng gần nhất (nếu có)
            var lastOrder = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderPlaced)
                .FirstOrDefaultAsync();

            return View(lastOrder ?? new Order());
        }

        // 2. POST: Lưu địa chỉ tạm thời vào TempData
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Address(Order model)
        {
            // Chỉ kiểm tra các trường địa chỉ, bỏ qua các trường hệ thống
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

            // 1. Lấy thông tin User để lấy SĐT (Identity)
            var currentUser = await userManager.FindByIdAsync(userId);

            // 2. Lấy Giỏ hàng
            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);
            ViewBag.CartItems = cart?.Items.ToList() ?? new List<CartItem>();

            var order = new Order();

            // 3. Lấy đơn hàng MỚI NHẤT (Cái mà bạn vừa cập nhật ở trang Hồ sơ)
            var lastOrder = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderPlaced)
                .FirstOrDefaultAsync();

            if (lastOrder != null)
            {
                // Lấy đúng Tên người nhận hàng (OrderAddress.Name cũ)
                order.Name = lastOrder.Name;

                // Ưu tiên SĐT từ Identity (nếu có), nếu không lấy từ đơn hàng
                order.PhoneNumber = currentUser?.PhoneNumber ?? lastOrder.PhoneNumber;

                // Các thông tin địa chỉ
                order.Address1 = lastOrder.Address1;
                order.Address2 = lastOrder.Address2;
                order.City = lastOrder.City;
                order.Zip = lastOrder.Zip;
            }
            else if (currentUser != null)
            {
                // Trường hợp User mới toanh, chưa có đơn hàng nào để lấy "Name" người nhận
                // Thì lúc này mới tạm lấy UserName hoặc để trống cho họ tự nhập
                order.PhoneNumber = currentUser.PhoneNumber;
            }

            return View(order);
        }
        // 4. POST: Thực hiện lưu đơn hàng và trừ kho
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(Order order)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            order.UserId = userId;

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.Items.Any())
                return RedirectToAction("Index", "Cart");

            // Xóa validation cho các trường tự động tạo
            ModelState.Remove("UserId");
            ModelState.Remove("CancelReason");
            ModelState.Remove("OrderDetails");

            if (ModelState.IsValid)
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        order.OrderPlaced = DateTime.Now;
                        order.Status = OrderStatus.Pending;
                        order.TotalAmount = cart.Items.Sum(i => i.Price * i.Quantity);
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

                            product.Quantity -= item.Quantity; // Cập nhật kho sản phẩm

                            order.OrderDetails.Add(new OrderDetail
                            {
                                ProductId = item.ProductId,
                                ProductName = item.ProductName,
                                Price = item.Price,
                                Quantity = item.Quantity
                            });
                        }

                        _context.Orders.Add(order);
                        _context.CartItems.RemoveRange(cart.Items); // Dọn dẹp giỏ hàng

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return RedirectToAction(nameof(Completed));
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        // Lấy lỗi chi tiết từ InnerException (thường là lỗi SQL)
                        var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                        ModelState.AddModelError("", "Lỗi lưu Database: " + msg);
                    }
                }
            }

            ViewBag.CartItems = cart.Items.ToList();
            return View(order);
        }

        [HttpGet]
        public IActionResult Completed() => View();

        // ================= USER AREA (HISTORY) =================

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

        // ================= ADMIN AREA =================

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .OrderByDescending(o => o.OrderPlaced)
                .ToListAsync();
            return View(orders);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, OrderStatus status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = status;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
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
                            if (product != null) product.Quantity += item.Quantity;
                        }
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                    catch { await transaction.RollbackAsync(); }
                }
            }
            return RedirectToAction(nameof(Index));
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
    }
}