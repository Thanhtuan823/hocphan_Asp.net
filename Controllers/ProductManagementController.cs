using lab2.Data;
using lab2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace lab2.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductManagementController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProductManagementController(AppDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // 1. Danh sách sản phẩm
        public IActionResult Index()
        {
            var products = _context.Products.Include(p => p.Category).ToList();
            ViewBag.CategoryId = new SelectList(_context.Category, "CategoryId", "CategoryName");
            return View("~/Views/Management/ProductManagement/Index.cshtml", products);
        }

        // 2. Trang Thêm mới
        public IActionResult Create()
        {
            ViewBag.CategoryId = new SelectList(_context.Category, "CategoryId", "CategoryName");
            return View("~/Views/Management/ProductManagement/Create.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? ImageFile)
        {
            if (ModelState.IsValid)
            {
                if (ImageFile != null)
                {
                    product.ImageUrl = await SaveImage(ImageFile);
                }

                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm sản phẩm thành công!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.CategoryId = new SelectList(_context.Category, "CategoryId", "CategoryName", product.CategoryId);
            return View("~/Views/Management/ProductManagement/Create.cshtml", product);
        }

        // 3. Trang Chỉnh sửa (Đã đổi tên từ EditProduct thành Edit)
        public IActionResult Edit(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null) return NotFound();

            ViewBag.CategoryId = new SelectList(_context.Category, "CategoryId", "CategoryName", product.CategoryId);
            return View("~/Views/Management/ProductManagement/Edit.cshtml", product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, IFormFile? ImageFile)
        {
            if (id != product.ProductId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Truy vấn không theo dõi để tránh xung đột khi Update
                    var productInDb = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == id);
                    if (productInDb == null) return NotFound();

                    if (ImageFile != null)
                    {
                        product.ImageUrl = await SaveImage(ImageFile);
                    }
                    else
                    {
                        // Giữ lại ảnh cũ nếu người dùng không chọn file mới
                        product.ImageUrl = productInDb.ImageUrl;
                    }

                    _context.Update(product);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Products.Any(e => e.ProductId == product.ProductId)) return NotFound();
                    else throw;
                }
            }
            ViewBag.CategoryId = new SelectList(_context.Category, "CategoryId", "CategoryName", product.CategoryId);
            return View("~/Views/Management/ProductManagement/Edit.cshtml", product);
        }

        // 4. Xóa sản phẩm
        [HttpPost]
        public IActionResult DeleteConfirmed(int id)
        {
            var product = _context.Products.Find(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                _context.SaveChanges();
                TempData["Success"] = "Đã xóa sản phẩm!";
            }
            return RedirectToAction("Index");
        }

        // Hàm phụ lưu file vào thư mục wwwroot/images
        private async Task<string> SaveImage(IFormFile imageFile)
        {
            string wwwRootPath = _hostEnvironment.WebRootPath;
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            string path = Path.Combine(wwwRootPath, "images", fileName);

            // Đảm bảo thư mục images tồn tại
            if (!Directory.Exists(Path.Combine(wwwRootPath, "images")))
            {
                Directory.CreateDirectory(Path.Combine(wwwRootPath, "images"));
            }

            using (var fileStream = new FileStream(path, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }
            return "/images/" + fileName;
        }
    }
}