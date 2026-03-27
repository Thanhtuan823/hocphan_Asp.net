using lab2.Data;
using lab2.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Shopping.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly SignInManager<IdentityUser> signInManager;
        private readonly AppDbContext context;
        private readonly IEmailSender _emailSender;

        public AccountController(UserManager<IdentityUser> userManager,
                                 SignInManager<IdentityUser> signInManager,
                                 AppDbContext _context,
                                 IEmailSender emailSender)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            context= _context;
            _emailSender = emailSender;
        }

        [HttpGet]
        public IActionResult Auth()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Auth", "Account"); // quay về trang đăng nhập/đăng ký
        }

        // 1. Trang nhập Email để lấy lại mật khẩu
        [HttpGet]
        public IActionResult ForgotPassword() => RedirectToAction("Auth");

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            // 1. Kiểm tra định dạng Email
            if (string.IsNullOrEmpty(email) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            {
                TempData["ErrorMsg"] = "Vui lòng nhập đúng định dạng Email!";
                return RedirectToAction("Auth");
            }

            // 2. Kiểm tra đuôi Gmail
            if (!email.ToLower().EndsWith("@gmail.com"))
            {
                TempData["ErrorMsg"] = "Hệ thống chỉ hỗ trợ Gmail!";
                return RedirectToAction("Auth");
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user != null)
            {
                // 3. Tạo mật khẩu tạm và Reset trong Database
                string tempPassword = "Plant@" + new Random().Next(1000, 9999);
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var result = await userManager.ResetPasswordAsync(user, token, tempPassword);

                if (result.Succeeded)
                {
                    // 4. Gửi Mail
                    string message = $@"
                <div style='font-family: Arial; padding: 20px; border: 1px solid #eee;'>
                    <h3 style='color: #198754;'>Mật khẩu tạm thời - Life & Trees</h3>
                    <p>Chào bạn, mật khẩu mới của bạn là: <b style='font-size:18px; color:green;'>{tempPassword}</b></p>
                    <p>Vui lòng đăng nhập và đổi lại mật khẩu ngay.</p>
                </div>";
                    await _emailSender.SendEmailAsync(email, "Khôi phục mật khẩu", message);
                    TempData["SuccessMsg"] = "Mật khẩu mới đã được gửi vào Gmail của bạn!";
                }
            }
            else
            {
                // Vẫn báo thành công để bảo mật thông tin
                TempData["SuccessMsg"] = "Nếu email tồn tại, mật khẩu đã được gửi đi!";
            }

            return RedirectToAction("Auth");
        }


        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = model.Email, Email = model.Email };
                var result = await userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Gán role User
                    await userManager.AddToRoleAsync(user, "User");

                    // --- CHỈNH SỬA TẠI ĐÂY ---
                    // Bỏ dòng signInManager.SignInAsync nếu muốn họ tự đăng nhập lại
                    // Hoặc nếu muốn họ đăng nhập luôn nhưng vẫn hiện thông báo thì giữ nguyên
            
                    TempData["RegisterSuccess"] = true;
                    TempData["SuccessMsg"] = "Tài khoản " + model.Email + " đã được tạo thành công!";

                    return RedirectToAction("Index", "Home"); 
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
            }
    
            // Nếu có lỗi, trả về trang chủ và báo để mở lại Register Modal xem lỗi
            TempData["OpenRegisterModal"] = true;
            return RedirectToAction("Index", "Home");
        }
        // Đăng nhập
        [HttpGet]
        public IActionResult Login() => View();
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. Tìm User dựa trên Email trước
                var user = await userManager.FindByEmailAsync(model.Email);

                if (user != null)
                {
                    // 2. Dùng UserName thật sự của User đó để đăng nhập
                    var result = await signInManager.PasswordSignInAsync(
                        user.UserName, // Quan trọng: Dùng cái tên đã đổi trong DB
                        model.Password,
                        isPersistent: false,
                        lockoutOnFailure: false);

                    if (result.Succeeded)
                    {
                        var roles = await userManager.GetRolesAsync(user);
                        if (roles.Contains("Admin"))
                            return RedirectToAction("Dashboard", "Admin", new { area = "Management" });

                        return RedirectToAction("Index", "Product");
                    }
                }

                ModelState.AddModelError("","Sai Email hoặc mật khẩu. Vui lòng thử lại!");
            }
            return View("Auth",model);
        }
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            // 1. Lấy thông tin User hiện tại
            var user = await userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // 2. Lấy danh sách Roles
            var roles = await userManager.GetRolesAsync(user);

            // 3. Lấy thông tin từ đơn hàng gần nhất để làm địa chỉ mặc định
            var lastOrder = context.Orders
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderPlaced)
                .FirstOrDefault();

            // 4. Khởi tạo ViewModel
            var model = new UserProfileViewModel
            {
                User = new UserWithRoles
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    // SĐT này lấy từ Identity (nếu có)
                    PhoneNumber = user.PhoneNumber,
                    Roles = roles.ToList()
                },
                // Nếu chưa có đơn hàng nào (người dùng mới), tạo object Order trống 
                // để Form không bị lỗi khi render các ô Input
                OrderAddress = lastOrder ?? new Order
                {
                    UserId = user.Id,
                    Name = user.UserName, // Mặc định lấy tên User làm người nhận
                    PhoneNumber = user.PhoneNumber, // Mặc định lấy SĐT từ Identity
                    Address1 = "",
                    City = "",
                    Zip = ""
                }
            };

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFullProfile(UserProfileViewModel model)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "Không tìm thấy người dùng" });

            // Cập nhật Identity
            user.UserName = model.User.UserName;
            user.PhoneNumber = model.OrderAddress.PhoneNumber;
            var userResult = await userManager.UpdateAsync(user);

            if (userResult.Succeeded)
            {
                await signInManager.RefreshSignInAsync(user);
            }

            // Cập nhật Orders
            var userOrders = context.Orders.Where(o => o.UserId == user.Id).ToList();
            foreach (var order in userOrders)
            {
                order.Name = model.OrderAddress.Name;
                order.Address1 = model.OrderAddress.Address1;
                order.City = model.OrderAddress.City;
                order.Zip = model.OrderAddress.Zip;
                order.PhoneNumber = model.OrderAddress.PhoneNumber;
            }
            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công!";
            return RedirectToAction("Profile");
        }
        // GET: Account/ChangePass
        [Authorize]
        public IActionResult ChangePass()
        {
            return View();
        }

        // POST: Account/ChangePass
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePass(ChangePass model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 1. Lấy thông tin User đang đăng nhập dựa trên định danh của hệ thống
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // 2. Thực hiện đổi mật khẩu thông qua Identity (Hàm này tự động Hash mật khẩu mới)
            var result = await userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);

            if (result.Succeeded)
            {
                // 3. Làm mới Cookie đăng nhập để người dùng không bị văng ra ngoài (Rất quan trọng)
                await signInManager.RefreshSignInAsync(user);

                TempData["SuccessMessage"] = "Mật khẩu của bạn đã được cập nhật thành công!";
                return RedirectToAction("Profile");
            }

            // 4. Nếu có lỗi (VD: Sai mật khẩu cũ), hiển thị lỗi ra View
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

    }
}