using System.ComponentModel.DataAnnotations;

namespace lab2.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; } // yêu cầu

        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        [MaxLength(500)]
        public string ProductName { get; set; } // yêu cầu

        [Required(ErrorMessage = "Vui lòng nhập mô tả sản phẩm")]
        [MaxLength(1000)]
        public string Description { get; set; } // yêu cầu, tối đa 1000 ký tự

        [Required(ErrorMessage = "Vui lòng nhập giá sản phẩm")]
        [Range(1, int.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        public int Price { get; set; } // yêu cầu, giá > 0

        [Required(ErrorMessage = "Vui lòng nhập số lượng sản phẩm")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng phải >= 0")]
        public int Quantity { get; set; } // yêu cầu, >= 0

        public int SoldQuantity { get; set; } = 0; // số lượng đã bán, mặc định 0

        [Required(ErrorMessage = "Vui lòng thêm hình ảnh sản phẩm")]
        public string ImageUrl { get; set; } // yêu cầu

        [Required(ErrorMessage = "Vui lòng nhập danh mục sản phẩm")]
        public int CategoryId { get; set; } // yêu cầu

        public Categories? Category { get; set; } // navigation property

    }
}
