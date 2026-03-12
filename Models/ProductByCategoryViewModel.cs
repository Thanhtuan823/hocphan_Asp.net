using System.Collections.Generic;

namespace lab2.Models
{
    public class ProductByCategoryViewModel
    {
        public int CategoryId { get; set; }          // ID của danh mục
        public string CategoryName { get; set; }     // Tên danh mục
        public List<Product> Products { get; set; }  // Danh sách sản phẩm thuộc danh mục
    }
}