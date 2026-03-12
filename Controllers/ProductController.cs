using lab2.Data;
using lab2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace lab2.Controllers
{
    public class ProductController : Controller
    {
        private readonly AppDbContext _context;

        public ProductController(AppDbContext context)
        {
            _context = context;
        }

        // Hiển thị form thêm sản phẩm
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Category = new SelectList(_context.Category.ToList(), "CategoryId", "CategoryName");
            return View(new Product());
        }

        // Xử lý thêm sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
  
            if (ModelState.IsValid)
            {
                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Sản phẩm đã được thêm thành công!";
                return RedirectToAction("Index");
            }

            ViewBag.Category = new SelectList(_context.Category.ToList(), "CategoryId", "CategoryName");
            return View(product);
        }

        // Hiển thị danh sách sản phẩm
        public IActionResult Index()
        {
            var products = _context.Products.Include(p => p.Category).ToList();
            ViewBag.SuccessMessage = TempData["SuccessMessage"];
            return View(products);
        }
        // Hiển thị chi tiết sản phẩm
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
        public IActionResult ProductByCategory(int categoryId)
        {
            var products = _context.Products
                                   .Include(p => p.Category)
                                   .Where(p => p.CategoryId == categoryId)
                                   .ToList();

            if (!products.Any())
            {
                return View(new ProductByCategoryViewModel
                {
                    CategoryId = categoryId,
                    CategoryName = "Không tìm thấy loại",
                    Products = new List<Product>()
                });
            }

            var vm = new ProductByCategoryViewModel
            {
                CategoryId = categoryId,
                CategoryName = products.First().Category.CategoryName,
                Products = products
            };

            return View(vm);
        }
    }
}