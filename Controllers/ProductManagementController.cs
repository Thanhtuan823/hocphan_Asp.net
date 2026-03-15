using lab2.Data;
using lab2.Models;
using Microsoft.AspNetCore.Mvc;

namespace lab2.Controllers.Management
{
    public class ProductManagementController : Controller
    {
        private readonly AppDbContext _context;

        public ProductManagementController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var products = _context.Products.ToList();
            return View("~/Views/Management/ProductManagement/Index.cshtml", products);
        }

        public IActionResult Create() => View("~/Views/Management/ProductManagement/Create.cshtml");

        [HttpPost]
        public IActionResult Create(Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Products.Add(product);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View("~/Views/Management/ProductManagement/Create.cshtml", product);
        }

        public IActionResult Edit(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null) return NotFound();
            return View("~/Views/Management/ProductManagement/Edit.cshtml", product);
        }

        [HttpPost]
        public IActionResult Edit(Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Products.Update(product);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View("~/Views/Management/ProductManagement/Edit.cshtml", product);
        }
    }
}