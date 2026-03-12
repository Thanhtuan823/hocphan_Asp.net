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

        // Trang giới thiệu
        public IActionResult About()
        {
            ViewBag.Message = "Tree Store - Mang thiên nhiên vào không gian sống.";
            return View();
        }

        // Trang liên hệ
        public IActionResult Contact()
        {
            ViewBag.Message = "Liên hệ với Tree Store qua email: treestore@gmail.com";
            return View();
        }
    }
}