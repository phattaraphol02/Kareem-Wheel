using CS66_WebApplication1.Filters;
using CS66_WebApplication1.Models;
using CS66_WebApplication1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CS66_WebApplication1.Controllers
{
    [AdminAuthorize]
    public class BrandController : Controller
    {
        private readonly ApplicationDbContext context;

        public BrandController(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 5)
        {
            if (page < 1) page = 1;
            var query = context.Brands.OrderBy(p => p.Brand_Id).AsNoTracking();
            var totalItems = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;

            return View(items);
        }

        // ==========================================
        // CREATE
        // ==========================================
        public IActionResult Create()
        {
            // ✅ ต้องส่ง Brand Object ไปเพื่อให้ View ใช้งานได้
            return View(new Brand());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(BrandDto brandDto)
        {
            if (!ModelState.IsValid)
            {
                // ❌ ถ้าพลาด ต้องแปลงกลับเป็น Brand ก่อนส่งคืน View
                var brand = new Brand { Brand_Name = brandDto.Brand_Name };
                return View(brand);
            }

            var brandNew = new Brand { Brand_Name = brandDto.Brand_Name };
            context.Brands.Add(brandNew);
            context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
        // POST: /Brand/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var brand = await context.Brands.FindAsync(id);
            if (brand == null)
            {
                return RedirectToAction(nameof(Index));
            }

            // กันลบแบรนด์ที่ยังมีสินค้าผูกอยู่ ไม่งั้นสินค้าและประวัติการสั่งซื้อจะหายตาม
            int productCount = await context.Products.CountAsync(p => p.Brand_Id == id);
            if (productCount > 0)
            {
                TempData["ErrorMessage"] = $"ไม่สามารถลบแบรนด์ \"{brand.Brand_Name}\" ได้ เพราะมีสินค้าอยู่ {productCount} รายการ กรุณาย้ายหรือลบสินค้าก่อน";
                return RedirectToAction(nameof(Index));
            }

            context.Brands.Remove(brand);
            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"ลบแบรนด์ \"{brand.Brand_Name}\" เรียบร้อยแล้ว";
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // EDIT
        // ==========================================
        public IActionResult Edit(int id)
        {
            var brand = context.Brands.Find(id);
            if (brand == null) return RedirectToAction(nameof(Index));

            // ✅ ส่ง Model 'Brand' ไปตรงๆ (แก้ Error ใน image_a8ddec.png)
            return View(brand);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, BrandDto brandDto)
        {
            var brand = context.Brands.Find(id);
            if (brand == null) return RedirectToAction(nameof(Index));

            if (!ModelState.IsValid)
            {
                return View(brand); // ส่ง Brand กลับไปถ้าข้อมูลไม่ถูกต้อง
            }

            brand.Brand_Name = brandDto.Brand_Name;
            context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
    }
}