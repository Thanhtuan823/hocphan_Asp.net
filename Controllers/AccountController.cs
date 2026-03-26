using lab2.Data;
using lab2.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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

        public AccountController(UserManager<IdentityUser> userManager,
                                 SignInManager<IdentityUser> signInManager,
                                 AppDbContext _context)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            context= _context;

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

                ModelState.AddModelError("", "Sai Email hoặc mật khẩu");
            }
            return View(model);
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