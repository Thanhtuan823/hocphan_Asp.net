using lab2.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Shopping.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly SignInManager<IdentityUser> signInManager;

        public AccountController(UserManager<IdentityUser> userManager,
                                 SignInManager<IdentityUser> signInManager)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
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
                    // Gán role User cho tài khoản mới
                    await userManager.AddToRoleAsync(user, "User");

                    await signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Product");
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
            }
            return View(model);
        }

        // Đăng nhập
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await signInManager.PasswordSignInAsync(
                    model.Email, model.Password, isPersistent: false, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    var user = await userManager.FindByEmailAsync(model.Email);
                    var roles = await userManager.GetRolesAsync(user);

                    if (roles.Contains("Admin"))
                        return RedirectToAction("Dashboard", "Admin", new { area = "Management" });

                    if (roles.Contains("User"))
                        return RedirectToAction("Index", "Product"); // Layout user
                }

                ModelState.AddModelError("", "Sai thông tin đăng nhập");
            }
            return View(model);
        }


    }
}