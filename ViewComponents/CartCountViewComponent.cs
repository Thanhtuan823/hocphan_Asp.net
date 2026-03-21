using Microsoft.AspNetCore.Mvc;
using lab2.Models;

namespace lab2.ViewComponents
{
    public class CartCountViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var cart = SessionCart.GetCart(HttpContext.RequestServices);
            int cartCount = cart.TotalQuantity;

            return View(cartCount);
        }
    }
}
