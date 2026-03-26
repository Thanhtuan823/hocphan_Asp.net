using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace lab2.Models
{
    public class Cart
    {
        [Key]
        public int CartId { get; set; }

        [Required]
        public string UserId { get; set; } // Khóa để biết giỏ hàng này của ai

        // Dùng List để Entity Framework có thể theo dõi và lưu trữ
        public List<CartItem> Items { get; set; } = new List<CartItem>();

        // Các phương thức bổ trợ logic (vẫn giữ lại để dùng trong Controller)

        public decimal ComputeTotalValue()
        {
            // Lưu ý: Đảm bảo CartItem của bạn có thuộc tính Total (Price * Quantity)
            return Items.Sum(i => i.Price * i.Quantity);
        }

        public int TotalQuantity => Items.Sum(i => i.Quantity);

        public void Clear()
        {
            Items.Clear();
        }
    }
}