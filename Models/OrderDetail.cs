using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using lab2.Models;

namespace lab2.Models
{
    public class OrderDetail
    {
        [Key]
        public int OrderDetailId { get; set; }

        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; } // Liên kết ngược lại bảng Order

        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } // Để Admin có thể lấy ImageUrl, ProductName từ bảng Product

        public string ProductName { get; set; } // Lưu tên tại thời điểm mua

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; } // Lưu giá tại thời điểm mua

        public int Quantity { get; set; }
    }
}