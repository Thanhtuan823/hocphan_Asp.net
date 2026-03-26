using lab2.Data;
using lab2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lab2.Controllers
{
    public class CategoryController : Controller
    {
        private readonly AppDbContext _context;

        public CategoryController(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Categories());
        }


        [HttpPut] // thêm HttpPut để rõ ràng
        public IActionResult UpdateCategory(int id, Categories category)
        {
            if (id != category.CategoryId) return BadRequest();

            var existing = _context.Category.Find(id);
            if (existing == null) return NotFound();

            existing.CategoryName = category.CategoryName;
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Cập nhật danh mục thành công!";
            return RedirectToAction("Index");
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Categories category)
        {
            if (ModelState.IsValid)
            {
                _context.Category.Add(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Danh mục đã được thêm thành công!";
                return RedirectToAction("Index");
            }
            return View(category);
        }

        public async Task<IActionResult> Index()
        {
            // Lấy danh mục và nạp kèm danh sách sản phẩm để đếm số lượng (Include)
            var categories = await _context.Category
                .Include(c => c.Products)
                .ToListAsync();

            return View(categories);
        }

    }
}