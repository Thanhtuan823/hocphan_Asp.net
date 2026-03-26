using lab2.Data;
using lab2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lab2.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var products = _context.Products.Include(p => p.Category).ToList();
            ViewBag.SuccessMessage = TempData["SuccessMessage"];
            return View(products);
        }

        public IActionResult About()
        {
            return View();
        }
        public IActionResult Contact()
        {
            return View();
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

    }
}