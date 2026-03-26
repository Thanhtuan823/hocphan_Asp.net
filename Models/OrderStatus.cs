using System.ComponentModel.DataAnnotations;

namespace lab2.Models
{
    public enum OrderStatus
    {
        [Display(Name = "Đang xử lý")]
        Pending,

        [Display(Name = "Đang giao hàng")]
        Shipping,

        [Display(Name = "Giao hàng thành công")]
        Success,

        [Display(Name = "Xác nhận hủy")]
        Cancelled,
        [Display(Name = "Hủy đơn hàng")]
        CancelRequested

    }
}