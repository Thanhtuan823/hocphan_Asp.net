using lab2.Data;
using lab2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Quan trọng: Phải có dòng này để dùng .Include()

namespace lab2.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoryManagementController : Controller
    {
        private readonly AppDbContext _context;

        public CategoryManagementController(AppDbContext context)
        {
            _context = context;
        }

        // 1. Hiển thị danh sách - Sửa lỗi đếm sản phẩm
        public IActionResult Index()
        {
            // Sử dụng .Include(c => c.Products) để nạp danh sách sản phẩm vào từng danh mục
            var categories = _context.Category
                                     .Include(c => c.Products)
                                     .ToList();

            return View("~/Views/Management/CategoryManagement/Index.cshtml", categories);
        }

        // 2. Thêm mới danh mục
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Categories category)
        {
            if (ModelState.IsValid)
            {
                _context.Category.Add(category);
                _context.SaveChanges();
                TempData["Success"] = "Thêm danh mục mới thành công!";
                return RedirectToAction(nameof(Index));
            }

            // Nếu lỗi, quay lại trang Index để hiển thị lỗi (vì ta dùng Modal trên trang Index)
            return RedirectToAction(nameof(Index));
        }

        // 3. Cập nhật danh mục
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Categories category)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Category.Update(category);
                    _context.SaveChanges();
                    TempData["Success"] = "Cập nhật danh mục thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Category.Any(e => e.CategoryId == category.CategoryId))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return RedirectToAction(nameof(Index));
        }

        // 4. Xóa danh mục
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var category = _context.Category.Find(id);
            if (category != null)
            {
                // Lưu ý: Nếu có ràng buộc khóa ngoại, bạn có thể cần xử lý xóa sản phẩm trước 
                // hoặc báo lỗi nếu danh mục đang có sản phẩm.
                _context.Category.Remove(category);
                _context.SaveChanges();
                TempData["Success"] = "Xóa danh mục thành công!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}