using lab2.Data;
using lab2.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace lab2.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext context;
        private readonly SessionCart cart;

        public CartController(AppDbContext _context, SessionCart _cart)
        {
            context = _context;
            cart = _cart;
        }

        // Hiển thị giỏ hàng
        public IActionResult Index()
        {
            return View(cart.Items);
        }

        // Thêm sản phẩm vào giỏ
        [HttpPost]
        public IActionResult AddToCart(int productId, int quantity = 1)
        {
            var product = context.Products.FirstOrDefault(p => p.ProductId == productId);
            if (product == null) return NotFound();

            var existingItem = cart.Items.FirstOrDefault(c => c.ProductId == productId);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Items.Add(new CartItem
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    ImageUrl = product.ImageUrl,
                    Price = product.Price,
                    Quantity = quantity
                });
            }

            cart.Save(HttpContext.RequestServices);
            return Json(new { success = true, count = cart.Items.Sum(i => i.Quantity) });


        }

        // Xóa sản phẩm khỏi giỏ
        public IActionResult RemoveFromCart(int productId)
        {
            var item = cart.Items.FirstOrDefault(c => c.ProductId == productId);
            if (item != null)
            {
                cart.Items.Remove(item);
                cart.Save(HttpContext.RequestServices);
            }
            return Json(new { success = true, count = cart.Items.Sum(i => i.Quantity) });


        }

        // Cập nhật số lượng
        [HttpPost]
        public IActionResult UpdateQuantity([FromBody] CartItemUpdate update)
        {
            var item = cart.Items.FirstOrDefault(c => c.ProductId == update.ProductId);
            if (item != null)
            {
                item.Quantity = update.Quantity;
                cart.Save(HttpContext.RequestServices);
            }
            return Ok();
        }
    }

    // DTO để cập nhật số lượng
    public class CartItemUpdate
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}