namespace lab2.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }       // Khóa tham chiếu tới Product
        public string ProductName { get; set; }  // Lấy từ Product.Name
        public string ImageUrl { get; set; }     // Lấy từ Product.ImageUrl
        public decimal Price { get; set; }       // Lấy từ Product.Price
        public int Quantity { get; set; }

        public decimal Total => Price * Quantity;
    }
}