using lab2.Data;
using lab2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace lab2.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserManagementController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AppDbContext context;

        public UserManagementController(UserManager<IdentityUser> userManager,
                                        AppDbContext _context)
        {
            _userManager = userManager;
            context = _context;
        }

        // 1. DANH SÁCH NGƯỜI DÙNG
        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            var userList = new List<UserWithRoles>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new UserWithRoles
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Roles = roles.ToList()
                });
            }

            return View("~/Views/Management/UserManagement/Index.cshtml", userList);
        }
        // GET: Hiển thị form sửa Profile cho User cụ thể
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            // Tìm User theo ID truyền từ danh sách
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            // Lấy thông tin địa chỉ từ bảng Orders (giống trang Profile của User)
            var lastOrder = await context.Orders
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderPlaced)
                .FirstOrDefaultAsync();

            var model = new UserProfileViewModel
            {
                User = new UserWithRoles
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Roles = roles.ToList()
                },
                OrderAddress = lastOrder ?? new Order
                {
                    UserId = user.Id,
                    Name = user.UserName
                }
            };

            return View("~/Views/Management/UserManagement/Edit.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserProfileViewModel model)
        {
            if (model.User == null || string.IsNullOrEmpty(model.User.Id))
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });
            }

            var user = await _userManager.FindByIdAsync(model.User.Id);
            if (user == null) return Json(new { success = false, message = "Người dùng không tồn tại!" });

            // --- CẬP NHẬT CÁC TRƯỜNG CỦA IDENTITY ---

            // 1. Cập nhật UserName
            user.UserName = model.User.UserName;

            // 2. CẬP NHẬT EMAIL (Dòng này bạn đang thiếu nè)
            user.Email = model.User.Email;

            // 3. Cập nhật SĐT (Lấy từ phần địa chỉ hoặc model tùy bạn)
            user.PhoneNumber = model.OrderAddress?.PhoneNumber;

            // QUAN TRỌNG: Đồng bộ lại bản Normalized để Identity tìm thấy khi Đăng nhập
            await _userManager.UpdateNormalizedUserNameAsync(user);
            await _userManager.UpdateNormalizedEmailAsync(user);

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                // --- CẬP NHẬT THÔNG TIN ĐỊA CHỈ TRONG BẢNG ORDERS ---
                var lastOrder = await context.Orders
                    .Where(o => o.UserId == user.Id)
                    .OrderByDescending(o => o.OrderPlaced)
                    .FirstOrDefaultAsync();

                if (lastOrder != null && model.OrderAddress != null)
                {
                    lastOrder.Name = model.OrderAddress.Name;
                    lastOrder.Address1 = model.OrderAddress.Address1;
                    lastOrder.City = model.OrderAddress.City;
                    lastOrder.Zip = model.OrderAddress.Zip;
                    lastOrder.PhoneNumber = model.OrderAddress.PhoneNumber;

                    context.Orders.Update(lastOrder);
                    await context.SaveChangesAsync();
                }

                return Json(new { success = true, message = "Cập nhật hồ sơ thành công!" });
            }

            // Trả về lỗi nếu Identity từ chối (vd: Email đã tồn tại)
            var errors = string.Join("<br/>", result.Errors.Select(e => e.Description));
            return Json(new { success = false, message = errors });
        }
        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Nếu muốn hiển thị tên Role cho đẹp trên trang xác nhận xóa
            var roles = await _userManager.GetRolesAsync(user);
            var model = new UserWithRoles
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Roles = roles.ToList()
            };

            return View("~/Views/Management/UserManagement/Delete.cshtml", model);
        }
        [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> DeleteConfirmed(string id)
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null) return NotFound();

                // Bảo vệ Admin: Không cho tự xóa chính mình
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null && currentUser.Id == id)
                {
                    TempData["Error"] = "Bạn không thể tự xóa tài khoản của chính mình!";
                    return RedirectToAction(nameof(Index));
                }

                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    TempData["Success"] = $"Đã xóa thành công người dùng {user.UserName}!";
                }
                else
                {
                    TempData["Error"] = "Lỗi hệ thống, không thể xóa người dùng này.";
                }

                return RedirectToAction(nameof(Index));
            }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string UserName, string Email, string PhoneNumber, string Password, string ConfirmPassword)
        {
            if (string.IsNullOrEmpty(UserName) || string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ các thông tin bắt buộc.";
                TempData["OpenCreateModal"] = true; // Phải để ở đây để Modal mở lại ngay
                return RedirectToAction("Index");
            }

            if (Password != ConfirmPassword)
            {
                TempData["Error"] = "Mật khẩu xác nhận không khớp.";
                TempData["OpenCreateModal"] = true;
                return RedirectToAction("Index");
            }

            var user = new IdentityUser
            {
                UserName = UserName,
                Email = Email,
                PhoneNumber = PhoneNumber,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, Password);

            if (result.Succeeded)
            {
                // FIX LỖI ROLE: Kiểm tra an toàn
                // Dùng _context để check trực tiếp trong bảng Roles
                var roleExists = await context.Roles.AnyAsync(r => r.Name == "User");
                if (roleExists)
                {
                    await _userManager.AddToRoleAsync(user, "User");
                }

                TempData["Success"] = $"Tài khoản {UserName} đã được tạo thành công!";
                return RedirectToAction("Index");
            }

            // Xử lý lỗi từ Identity (vd: Mật khẩu quá ngắn, trùng Email...)
            TempData["Error"] = string.Join("<br/>", result.Errors.Select(e => e.Description));
            TempData["OpenCreateModal"] = true;
            return RedirectToAction("Index");
        }

    }
}