using CS66_WebApplication1.Filters;
using CS66_WebApplication1.Models;
using CS66_WebApplication1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CS66_WebApplication1.Controllers
{
    [AdminAuthorize]
    public class AdminSuppliersController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AdminSuppliersController(ApplicationDbContext context) => _context = context;

        // 1. รายการซัพพลายเออร์ทั้งหมด
        public async Task<IActionResult> Index()
        {
            var suppliers = await _context.Suppliers
                                          .OrderByDescending(s => s.IsActive)
                                          .ThenBy(s => s.Name)
                                          .ToListAsync();

            // จำนวน PO ที่เคยสั่งกับแต่ละราย ใช้เตือนก่อนลบ
            ViewBag.PoCounts = await _context.PurchaseOrders
                                             .GroupBy(p => p.SupplierId)
                                             .Select(g => new { g.Key, Count = g.Count() })
                                             .ToDictionaryAsync(x => x.Key, x => x.Count);

            return View(suppliers);
        }

        // 2. เพิ่มซัพพลายเออร์ใหม่ (GET)
        public IActionResult Create() => View();

        // 2. บันทึกซัพพลายเออร์ใหม่ (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Supplier supplier)
        {
            if (!ModelState.IsValid) return View(supplier);

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"เพิ่มซัพพลายเออร์ \"{supplier.Name}\" เรียบร้อยแล้ว";
            return RedirectToAction(nameof(Index));
        }

        // 3. แก้ไขซัพพลายเออร์ (GET)
        public async Task<IActionResult> Edit(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound();
            return View(supplier);
        }

        // 3. บันทึกการแก้ไข (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Supplier supplier)
        {
            if (id != supplier.SupplierId) return NotFound();
            if (!ModelState.IsValid) return View(supplier);

            var existing = await _context.Suppliers.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Name = supplier.Name;
            existing.SupplierType = supplier.SupplierType;
            existing.Phone = supplier.Phone;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"แก้ไขข้อมูล \"{existing.Name}\" เรียบร้อยแล้ว";
            return RedirectToAction(nameof(Index));
        }

        // 4. เปิด/ปิดการใช้งาน (ไม่ลบถาวร เพราะอาจมีประวัติ PO ผูกอยู่)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound();

            supplier.IsActive = !supplier.IsActive;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = supplier.IsActive
                ? $"เปิดใช้งาน \"{supplier.Name}\" แล้ว"
                : $"ปิดใช้งาน \"{supplier.Name}\" แล้ว (ยังเลือกในใบสั่งซื้อเก่าดูได้ปกติ)";

            return RedirectToAction(nameof(Index));
        }

        // 5. ลบถาวร — อนุญาตเฉพาะซัพพลายเออร์ที่ไม่เคยมีใบสั่งซื้อผูกอยู่
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                TempData["ErrorMessage"] = "ไม่พบซัพพลายเออร์ที่ต้องการลบ";
                return RedirectToAction(nameof(Index));
            }

            bool hasOrders = await _context.PurchaseOrders.AnyAsync(p => p.SupplierId == id);
            if (hasOrders)
            {
                TempData["ErrorMessage"] =
                    $"ลบ \"{supplier.Name}\" ไม่ได้ เพราะมีประวัติใบสั่งซื้อผูกอยู่ กรุณาปิดการใช้งานแทน";
                return RedirectToAction(nameof(Index));
            }

            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"ลบซัพพลายเออร์ \"{supplier.Name}\" เรียบร้อยแล้ว";
            return RedirectToAction(nameof(Index));
        }
    }
}
