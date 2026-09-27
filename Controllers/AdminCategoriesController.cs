using CS66_WebApplication1.Filters;
using CS66_WebApplication1.Models;
using CS66_WebApplication1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CS66_WebApplication1.Controllers
{
    [AdminAuthorize]
    public class AdminCategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminCategoriesController(ApplicationDbContext context) => _context = context;

        // แสดงรายการ
        public async Task<IActionResult> Index() => View(await _context.Categories.ToListAsync());

        // สร้าง (GET)
        public IActionResult Create() => View();

        // สร้าง (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
                TempData["Success"] = "เพิ่มประเภทสินค้าสำเร็จ";
                return RedirectToAction("Index");
            }
            return View(category);
        }

        // แก้ไข (GET)
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        // แก้ไข (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id != category.Category_Id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(category);
                await _context.SaveChangesAsync();
                TempData["Success"] = "แก้ไขข้อมูลสำเร็จ";
                return RedirectToAction("Index");
            }
            return View(category);
        }

        // ลบ (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var cat = await _context.Categories.FindAsync(id);
            if (cat == null)
            {
                TempData["Error"] = "ไม่พบข้อมูลที่ต้องการลบ";
                return RedirectToAction("Index");
            }

            // กันลบประเภทที่ยังมีสินค้าผูกอยู่ ไม่งั้นสินค้าและประวัติการสั่งซื้อจะหายตาม
            int productCount = await _context.Products.CountAsync(p => p.Category_Id == id);
            if (productCount > 0)
            {
                TempData["Error"] = $"ไม่สามารถลบประเภท \"{cat.Category_Name}\" ได้ เพราะมีสินค้าอยู่ {productCount} รายการ กรุณาย้ายหรือลบสินค้าก่อน";
                return RedirectToAction("Index");
            }

            _context.Categories.Remove(cat);
            await _context.SaveChangesAsync();
            TempData["Success"] = "ลบข้อมูลสำเร็จ";
            return RedirectToAction("Index");
        }
    }
}