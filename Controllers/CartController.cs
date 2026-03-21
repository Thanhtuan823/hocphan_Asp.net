using lab2.Data;
using lab2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;

namespace lab2.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext context;

        public CartController(AppDbContext _context)
        {
            context = _context;
        }

        // Hiển thị giỏ hàng
        public IActionResult Index()
        {
            var cart = SessionCart.GetCart(HttpContext.RequestServices);
            return View(cart.Items);
        }

        // Thêm sản phẩm vào giỏ
        [HttpPost]
        public IActionResult AddToCart(int productId, int quantity = 1)
        {
            var cart = SessionCart.GetCart(HttpContext.RequestServices);

            var product = context.Products.FirstOrDefault(p => p.ProductId == productId);
            if (product != null)
            {
                var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
                if (existingItem == null)
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
                else
                {
                    existingItem.Quantity += quantity;
                }
            }

            cart.Save(HttpContext.RequestServices);

            return Json(new { success = true, count = cart.TotalQuantity });
        }

        // Xóa sản phẩm khỏi giỏ
        [HttpPost]
        public IActionResult RemoveFromCart(int productId)
        {
            var cart = SessionCart.GetCart(HttpContext.RequestServices);

            var item = cart.Items.FirstOrDefault(c => c.ProductId == productId);
            if (item != null)
            {
                cart.Items.Remove(item);
                cart.Save(HttpContext.RequestServices);
            }

            return Json(new { success = true, count = cart.TotalQuantity, totalPrice = cart.TotalPrice });
        }

        // Cập nhật số lượng
        [HttpPost]
        public IActionResult UpdateQuantity([FromBody] CartItemUpdate update)
        {
            var cart = SessionCart.GetCart(HttpContext.RequestServices);

            var item = cart.Items.FirstOrDefault(c => c.ProductId == update.ProductId);
            if (item != null)
            {
                item.Quantity = update.Quantity;
                cart.Save(HttpContext.RequestServices);
            }

            return Json(new { success = true, count = cart.TotalQuantity });
        }
    }

    // DTO để cập nhật số lượng
    public class CartItemUpdate
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}