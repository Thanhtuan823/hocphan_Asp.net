using lab2.Data;
using lab2.Models;
using Microsoft.AspNetCore.Identity.UI.Services; // Thêm thư viện này
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lab2.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IEmailSender _emailSender; // Khai báo EmailSender

        // Tiêm IEmailSender vào constructor
        public HomeController(AppDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        public IActionResult Index()
        {
            var products = _context.Products.Include(p => p.Category).ToList();
            ViewBag.SuccessMessage = TempData["SuccessMessage"];
            return View(products);
        }

        public IActionResult About()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Contact()
        {
            return View();
        }

        // HÀM XỬ LÝ KHI KHÁCH HÀNG BẤM GỬI LIÊN HỆ
        [HttpPost]
        public async Task<IActionResult> Contact(string name, string email, string message)
        {
            // Địa chỉ email của BẠN - Nơi sẽ nhận được tin nhắn
            string adminEmail = "thanhnguyen10988@gmail.com";

            // Tiêu đề email
            string subject = $"[LifeTrees - Liên hệ mới] Từ khách hàng: {name}";

            // Nội dung email gửi cho bạn (Định dạng HTML)
            string htmlMessage = $@"
                <div style='font-family: Arial, sans-serif; line-height: 1.6;'>
                    <h2 style='color: #2e7d32;'>Có một tin nhắn mới từ trang Liên hệ</h2>
                    <p><strong>Tên khách hàng:</strong> {name}</p>
                    <p><strong>Email khách hàng:</strong> <a href='mailto:{email}'>{email}</a> <em>(Bạn có thể bấm trực tiếp vào đây để phản hồi)</em></p>
                    <hr/>
                    <h3>Nội dung lời nhắn:</h3>
                    <p style='background-color: #f8f9fa; padding: 15px; border-left: 4px solid #2e7d32; white-space: pre-wrap;'>{message}</p>
                </div>";

            // Gọi dịch vụ gửi mail
            await _emailSender.SendEmailAsync(adminEmail, subject, htmlMessage);

            // Báo thành công ra ngoài View
            TempData["SuccessMessage"] = "Cảm ơn bạn! Lời nhắn của bạn đã được gửi thành công, chúng tôi sẽ phản hồi sớm nhất qua email.";

            return RedirectToAction("Contact");
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                                        .Include(p => p.Category)
                                        .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
    }
}