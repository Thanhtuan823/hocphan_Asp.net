using System.ComponentModel.DataAnnotations;

namespace lab2.Models
{
    public enum OrderStatus
    {
        [Display(Name = "Đang xử lý")]
        Pending,

        [Display(Name = "Đang giao hàng")]
        Shipping,

        [Display(Name = "Đã giao hàng")]
        Success,

        [Display(Name = "Xác nhận hủy")]
        Cancelled,
        [Display(Name = "Hủy thành công")]
        CancelRequested

    }
}