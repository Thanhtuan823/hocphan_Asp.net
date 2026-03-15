using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using lab2.Models;

namespace lab2.Controllers.Management
{
    public class UserManagementController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        public UserManagementController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        // Danh sách người dùng
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

        // Xóa người dùng
        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            return View("~/Views/Management/UserManagement/Delete.cshtml", user);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(user);
        }
    }
}