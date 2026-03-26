using lab2.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace lab2.ViewComponents
{
    public class CartCountViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public CartCountViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            int cartCount = 0;

            // 1. Lấy ID của người dùng hiện tại
            var claimsPrincipal = User as ClaimsPrincipal;
            var userId = claimsPrincipal?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId != null)
            {
                // 2. Tính tổng số lượng sản phẩm trong giỏ hàng từ Database
                cartCount = await _context.CartItems
                    .Where(ci => ci.Cart.UserId == userId)
                    .SumAsync(ci => (int?)ci.Quantity) ?? 0;
            }

            // 3. Trả về View (thường là Default.cshtml)
            return View(cartCount);
        }
    }
}