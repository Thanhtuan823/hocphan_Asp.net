using System.Collections.Generic;
using System.Linq;

namespace lab2.Models
{
    public class Cart
    {
        private List<CartItem> items = new List<CartItem>();

        public IEnumerable<CartItem> Items => items;

        // Thêm sản phẩm vào giỏ
        public void AddItem(Product product, int quantity)
        {
            var item = items.FirstOrDefault(i => i.ProductId == product.ProductId);
            if (item == null)
            {
                items.Add(new CartItem
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,   // lấy từ Product
                    ImageUrl = product.ImageUrl,         // lấy từ Product
                    Price = product.Price,               // lấy từ Product
                    Quantity = quantity
                });
            }
            else
            {
                item.Quantity += quantity;
            }
        }

        // Xóa sản phẩm khỏi giỏ
        public void RemoveItem(int productId)
        {
            items.RemoveAll(i => i.ProductId == productId);
        }

        // Tính tổng tiền giỏ hàng
        public decimal ComputeTotalValue()
        {
            return items.Sum(i => i.Total);
        }

        // Xóa toàn bộ giỏ hàng
        public void Clear()
        {
            items.Clear();
        }
    }
}