using CS66_WebApplication1.Filters;
using CS66_WebApplication1.Models;
using CS66_WebApplication1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CS66_WebApplication1.Controllers
{
    [AdminAuthorize]
    public class AdminSparePartsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminSparePartsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // อะไหล่ที่เหลือน้อยกว่าหรือเท่านี้ ถือว่า "ใกล้หมด" (ใช้เกณฑ์เดียวกับหน้าพนักงาน)
        public const int LowStockThreshold = 5;

        // 1. หน้าแสดงรายการ (Index)
        public async Task<IActionResult> Index()
        {
            var parts = await _context.SpareParts
                                      .OrderBy(p => p.StockQuantity)   // ของใกล้หมดขึ้นก่อน
                                      .ThenBy(p => p.Name)
                                      .ToListAsync();

            ViewBag.LowStockThreshold = LowStockThreshold;
            ViewBag.OutOfStock = parts.Count(p => p.StockQuantity <= 0);
            ViewBag.LowStock = parts.Count(p => p.StockQuantity > 0 && p.StockQuantity <= LowStockThreshold);
            ViewBag.TotalValue = parts.Sum(p => p.Price * p.StockQuantity);

            return View(parts);
        }

        // 1.5 ประวัติการเบิกอะไหล่ของพนักงานทุกคน
        public async Task<IActionResult> Requisitions()
        {
            var list = await _context.SparePartRequisitions
                .Include(r => r.SparePart)
                .Include(r => r.Employee)
                .Include(r => r.Appointment).ThenInclude(a => a!.Service)
                .OrderByDescending(r => r.Id)
                .Take(200)
                .ToListAsync();

            ViewBag.TotalValue = list.Sum(r => r.Qty * (r.SparePart != null ? r.SparePart.Price : 0));
            return View(list);
        }

        // 2. หน้าเพิ่มอะไหล่ (Create) - GET
        public IActionResult Create() => View();

        // 2. บันทึกการเพิ่มอะไหล่ (Create) - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SparePart sparePart)
        {
            if (ModelState.IsValid)
            {
                _context.Add(sparePart);
                await _context.SaveChangesAsync();
                TempData["Success"] = "เพิ่มอะไหล่ใหม่เรียบร้อยแล้ว!";
                return RedirectToAction(nameof(Index));
            }
            return View(sparePart);
        }

        // 3. หน้าแก้ไขอะไหล่ (Edit) - GET
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var sparePart = await _context.SpareParts.FindAsync(id);
            if (sparePart == null) return NotFound();
            return View(sparePart);
        }

        // 3. บันทึกการแก้ไข (Edit) - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SparePart sparePart)
        {
            if (id != sparePart.Id) return NotFound();

            if (!ModelState.IsValid) return View(sparePart);

            // ดึงตัวจริงจากฐานข้อมูลมาแก้ทีละฟิลด์
            // ไม่ใช้ _context.Update() ทั้งก้อน เพราะจะเขียนทับทุกคอลัมน์
            var existing = await _context.SpareParts.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Name = sparePart.Name;
            existing.Price = sparePart.Price;
            existing.StockQuantity = sparePart.StockQuantity;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"แก้ไขข้อมูล \"{existing.Name}\" เรียบร้อยแล้ว";
            return RedirectToAction(nameof(Index));
        }

        // 4. ลบอะไหล่ (Delete) - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var sparePart = await _context.SpareParts.FindAsync(id);
            if (sparePart == null)
            {
                TempData["ErrorMessage"] = "ไม่พบอะไหล่ที่ต้องการลบ";
                return RedirectToAction(nameof(Index));
            }

            // อะไหล่ที่เคยถูกเบิกไปแล้วห้ามลบ ไม่งั้นประวัติการเบิกจะเสียและระบบจะพัง
            // (ติด Foreign Key FK_Req_SparePart)
            int usedCount = await _context.SparePartRequisitions
                                          .CountAsync(r => r.SparePart_Id == id);

            if (usedCount > 0)
            {
                TempData["ErrorMessage"] =
                    $"ลบ \"{sparePart.Name}\" ไม่ได้ เพราะมีประวัติการเบิกอยู่ {usedCount} รายการ " +
                    "หากไม่ใช้แล้วให้ปรับสต็อกเป็น 0 แทนการลบ เพื่อเก็บประวัติไว้";
                return RedirectToAction(nameof(Index));
            }

            _context.SpareParts.Remove(sparePart);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"ลบอะไหล่ \"{sparePart.Name}\" เรียบร้อยแล้ว";
            return RedirectToAction(nameof(Index));
        }

        // 5. ปรับสต็อกทีละชิ้นแบบไม่ต้องโหลดหน้า (Ajax) — ใช้ได้ทั้งเพิ่มและลด
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStock(int id, int amount)
        {
            // จำกัดช่วงที่ปรับได้ต่อครั้ง กันยิงค่ามหาศาลหรือค่าเพี้ยนเข้ามา
            if (amount == 0 || amount < -100 || amount > 100)
                return Json(new { success = false, message = "จำนวนที่ปรับต้องอยู่ระหว่าง -100 ถึง 100" });

            var sparePart = await _context.SpareParts.FindAsync(id);
            if (sparePart == null)
                return Json(new { success = false, message = "ไม่พบอะไหล่ชิ้นนี้" });

            int newQty = sparePart.StockQuantity + amount;
            if (newQty < 0)
                return Json(new { success = false, message = $"สต็อกติดลบไม่ได้ (คงเหลือ {sparePart.StockQuantity} ชิ้น)" });

            sparePart.StockQuantity = newQty;
            await _context.SaveChangesAsync();

            return Json(new { success = true, newQuantity = sparePart.StockQuantity });
        }

        private bool SparePartExists(int id) => _context.SpareParts.Any(e => e.Id == id);
    }
}