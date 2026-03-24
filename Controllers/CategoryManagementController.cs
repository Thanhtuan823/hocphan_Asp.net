using lab2.Data;
using lab2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace lab2.Controllers.Management
{
    [Authorize]
    public class CategoryManagementController : Controller
    {
        private readonly AppDbContext _context;

        public CategoryManagementController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var categories = _context.Category.ToList();
            return View("~/Views/Management/CategoryManagement/Index.cshtml", categories);
        }

        public IActionResult Create() => View("~/Views/Management/CategoryManagement/Create.cshtml");

        [HttpPost]
        public IActionResult Create(Categories category)
        {
            if (ModelState.IsValid)
            {
                _context.Category.Add(category);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View("~/Views/Management/CategoryManagement/Create.cshtml", category);
        }

        public IActionResult Edit(int id)
        {
            var category = _context.Category.Find(id);
            if (category == null) return NotFound();
            return View("~/Views/Management/CategoryManagement/UpdateCategory.cshtml", category);
        }

        [HttpPost]
        public IActionResult Edit(Categories category)
        {
            if (ModelState.IsValid)
            {
                _context.Category.Update(category);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View("~/Views/Management/CategoryManagement/Edit.cshtml", category);
        }
    }
}