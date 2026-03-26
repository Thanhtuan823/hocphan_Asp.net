using System.ComponentModel.DataAnnotations;

namespace lab2.Models
{
    public class Categories
    {
        [Key]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        [StringLength(100)]
        [Display(Name = "Tên danh mục")]
        public string CategoryName { get; set; }

        // THÊM: Mô tả ngắn để hiển thị trên Card cho User xem
        [StringLength(500)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        // THÊM: Ảnh đại diện cho danh mục (giúp giao diện User bắt mắt hơn)
        [Display(Name = "Hình ảnh")]
        public string? CategoryImageUrl { get; set; }

        // Quan hệ 1 - Nhiều với Product
        public ICollection<Product>? Products { get; set; }
    }
}