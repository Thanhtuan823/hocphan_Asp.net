using lab2.Data;
using lab2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace lab2.Controllers
{
    [Authorize] // Bắt buộc đăng nhập để dùng giỏ hàng
    public class CartController : Controller
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        // 1. Trong hàm Index, nạp thêm số lượng tồn kho từ bảng Product
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart != null)
            {
                foreach (var item in cart.Items)
                {
                    var product = await _context.Products.AsNoTracking()
                        .FirstOrDefaultAsync(p => p.ProductId == item.ProductId);
                    item.StockQuantity = product?.Quantity ?? 0;
                }
                return View(cart.Items);
            }
            return View(new List<CartItem>());
        }

        // 2. Cập nhật hàm UpdateQuantity để chặn khi vượt kho
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity([FromBody] CartItemUpdate update)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.ProductId == update.ProductId && ci.Cart.UserId == userId);

            if (cartItem == null) return Json(new { success = false, message = "Không tìm thấy sản phẩm" });

            var product = await _context.Products.FindAsync(update.ProductId);

            // Kiểm tra kho thực tế
            if (update.Quantity > product.Quantity)
            {
                return Json(new
                {
                    success = false,
                    message = $"Kho chỉ còn {product.Quantity} sản phẩm.",
                    currentStock = product.Quantity
                });
            }

            cartItem.Quantity = update.Quantity;
            await _context.SaveChangesAsync();

            var cart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId);
            return Json(new { success = true, count = cart?.Items.Sum(i => i.Quantity) ?? 0 });
        }

        // 2. Thêm sản phẩm vào giỏ (Lưu trực tiếp vào DB)
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var product = await _context.Products.FindAsync(productId);

            if (product == null) return Json(new { success = false, message = "Sản phẩm không tồn tại" });

            // Kiểm tra tồn kho trước khi cho vào giỏ
            if (product.Quantity <= 0) return Json(new { success = false, message = "Sản phẩm đã hết hàng" });

            // Tìm hoặc tạo mới Giỏ hàng của User
            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (cartItem == null)
            {
                cartItem = new CartItem
                {
                    CartId = cart.CartId,
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    ImageUrl = product.ImageUrl,
                    Price = product.Price,
                    Quantity = quantity
                };
                _context.CartItems.Add(cartItem);
            }
            else
            {
                cartItem.Quantity += quantity;
            }

            await _context.SaveChangesAsync();

            // Tính tổng số lượng để cập nhật Badge trên giao diện
            var totalCount = cart.Items.Sum(i => i.Quantity);
            return Json(new { success = true, count = totalCount });
        }

        // 3. Xóa sản phẩm khỏi giỏ trong DB
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.ProductId == productId && ci.Cart.UserId == userId);

            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
            }

            // Lấy lại giỏ hàng để tính toán lại tổng tiền/số lượng trả về cho Ajax
            var cart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId);
            var totalCount = cart?.Items.Sum(i => i.Quantity) ?? 0;
            var totalPrice = cart?.Items.Sum(i => i.Price * i.Quantity) ?? 0;

            return Json(new { success = true, count = totalCount, totalPrice = totalPrice });
        }
        
    }

    public class CartItemUpdate
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}