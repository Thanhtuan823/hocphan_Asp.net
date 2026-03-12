using System.ComponentModel.DataAnnotations;
namespace lab2.Models
{
    public class Categories
    {

        [Key]
        public int CategoryId {  get; set; }
        [Required]

        [StringLength(50,ErrorMessage = "Vui lòng thêm danh mục sản phẩm")]
        public string CategoryName {  get; set; }

        public ICollection<Product>? Products { get; set; }

    }
}
