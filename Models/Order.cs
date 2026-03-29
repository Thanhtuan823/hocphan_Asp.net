using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace lab2.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        // THÊM DÒNG NÀY ĐỂ HẾT LỖI
        [Required]
        public string UserId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên người nhận")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
        public string Address1 { get; set; }

        public string? Address2 { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập thành phố")]
        public string City { get; set; }
        public string? CancelReason { get; set; }

     
        [Display(Name = "Số điện thoại")]
        [DataType(DataType.PhoneNumber)]
        public string PhoneNumber { get; set; } // Thêm dòng này vào

        public string? Zip { get; set; }

        public DateTime OrderPlaced { get; set; } = DateTime.Now;

        // Trạng thái dùng Enum tiếng Việt chúng ta đã tạo
        public OrderStatus Status { get; set; } // OrderStatus là một enum

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        // Thêm vào trong class Order
        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFee { get; set; } // Lưu phí vận chuyển thực tế của đơn hàng

        [Display(Name = "Mã giảm giá ")]
        public string? DiscountCode { get; set; }

        // Danh sách chi tiết món hàng trong đơn
        public List<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}