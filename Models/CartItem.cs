using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace lab2.Models
{
    public class CartItem
    {
        [Key]
        public int Id { get; set; }

        // Khóa ngoại liên kết tới bảng Cart
        public int CartId { get; set; }
        [ForeignKey("CartId")]
        public virtual Cart? Cart { get; set; }

        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ImageUrl { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int Quantity { get; set; }
        // Thêm thuộc tính này vào class CartItem
        [NotMapped]
        public int StockQuantity { get; set; }

        // Thuộc tính tính toán nhanh (không lưu xuống DB)
        [NotMapped]
        public decimal Total => Price * Quantity;
    }
}