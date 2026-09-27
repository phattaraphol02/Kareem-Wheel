using CS66_WebApplication1.Filters;
using CS66_WebApplication1.Models;
using CS66_WebApplication1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

[AdminAuthorize]
public class AdminServicesController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminServicesController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index() => View(await _context.Services.ToListAsync());

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Service service)
    {
        if (ModelState.IsValid)
        {
            _context.Services.Add(service);
            await _context.SaveChangesAsync();
            TempData["Success"] = "เพิ่มบริการสำเร็จ";
            return RedirectToAction("Index");
        }
        return View(service);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service == null) return NotFound();
        return View(service);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Service service)
    {
        if (id != service.Service_Id) return NotFound();
        if (ModelState.IsValid)
        {
            _context.Update(service);
            await _context.SaveChangesAsync();
            TempData["Success"] = "แก้ไขบริการสำเร็จ";
            return RedirectToAction("Index");
        }
        return View(service);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service != null)
        {
            _context.Services.Remove(service);
            await _context.SaveChangesAsync();
            TempData["Success"] = "ลบบริการสำเร็จ";
        }
        return RedirectToAction("Index");
    }

    // =================================================================
    // 🔘 API สลับสถานะการเปิด/ปิดบริการ (ใช้ _context และรองรับฟิลด์ IsActive ใน DB แล้ว)
    // =================================================================
    [HttpPost]
    public async Task<IActionResult> ToggleStatus(int id, bool status)
    {
        var service = await _context.Services.FindAsync(id);
        if (service == null)
        {
            return Json(new { success = false, message = "ไม่พบข้อมูลบริการนี้ในระบบ" });
        }

        // อัปเดตฟิลด์สถานะ
        service.IsActive = status;

        try
        {
            _context.Services.Update(service);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        catch (Exception)
        {
            return Json(new { success = false, message = "เกิดข้อผิดพลาดในการบันทึกข้อมูลเข้าสู่ฐานข้อมูล" });
        }
    }
}