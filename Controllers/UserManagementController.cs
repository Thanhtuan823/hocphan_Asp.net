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

        // 2. CHỈNH SỬA HỒ SƠ (GET): Load dữ liệu lên trang Edit
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            var model = new UserWithRoles
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Roles = roles.ToList()
            };

            return View("~/Views/Management/UserManagement/Edit.cshtml", model);
        }

        
        // 4. XÓA NGƯỜI DÙNG (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Bảo vệ Admin: Không cho phép tự xóa chính mình
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && currentUser.Id == id)
            {
                TempData["ErrorMessage"] = "Bạn không thể tự xóa tài khoản của chính mình!";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["Message"] = "Đã xóa người dùng thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Lỗi hệ thống khi thực hiện xóa người dùng.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}