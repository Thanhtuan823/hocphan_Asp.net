using lab2.Models;
using lab2.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;

namespace lab2.Controllers
{
    public class OrderController : Controller
    {
        private readonly IOrderRepository repository;
        private readonly SessionCart cart;

        public OrderController(IOrderRepository _repository, SessionCart _cart)
        {
            repository = _repository;
            cart = _cart;
        }
        [HttpGet]
        public IActionResult Completed()
        {
            return View();
        }

        // Hiển thị form Checkout
        [HttpGet]
        public IActionResult Checkout()
        {
            ViewBag.Cart = cart; // truyền giỏ hàng sang view
            return View(new Order());
        }

        [HttpPost]
        public IActionResult Checkout(Order order)
        {
            if (!cart.Items.Any())
            {
                ModelState.AddModelError("", "Giỏ hàng của bạn đang trống");
            }

            if (ModelState.IsValid)
            {
              
                order.OrderPlaced = DateTime.Now;


                // Tạo OrderDetails từ giỏ hàng
                foreach (var item in cart.Items)
                {
                    order.OrderDetails.Add(new OrderDetail
                    {
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Price = item.Price,
                        Quantity = item.Quantity
                    });
                }

                // Lưu đơn hàng
                repository.SaveOrder(order);

                // Xóa giỏ hàng
                cart.Clear();
                cart.Save(HttpContext.RequestServices);

                // Chuyển sang Completed
                return RedirectToAction("Completed");
            }

            // Nếu có lỗi thì quay lại Checkout
            ViewBag.Cart = cart;
            return View(order);
        }
    }
}