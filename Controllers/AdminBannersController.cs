using CS66_WebApplication1.Filters;
using CS66_WebApplication1.Models;
using CS66_WebApplication1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CS66_WebApplication1.Controllers
{
    [AdminAuthorize]
    public class AdminBannersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        private const string BannerFolder = "banners";
        private static readonly string[] AllowedExt = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

        public AdminBannersController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ==========================================
        // รายการแบนเนอร์ทั้งหมด
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var banners = await _context.Banners
                .OrderBy(b => b.SortOrder)
                .ThenByDescending(b => b.Banner_Id)
                .ToListAsync();

            ViewBag.ActiveCount = banners.Count(b => b.IsLiveNow);
            return View(banners);
        }

        // ==========================================
        // เพิ่มแบนเนอร์ใหม่
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Banner banner, IFormFile image)
        {
            if (string.IsNullOrWhiteSpace(banner.Title))
            {
                TempData["ErrorMessage"] = "กรุณาระบุหัวข้อแบนเนอร์";
                return RedirectToAction(nameof(Index));
            }

            if (image == null || image.Length == 0)
            {
                TempData["ErrorMessage"] = "กรุณาเลือกรูปภาพ";
                return RedirectToAction(nameof(Index));
            }

            if (banner.StartDate != null && banner.EndDate != null && banner.EndDate < banner.StartDate)
            {
                TempData["ErrorMessage"] = "วันที่สิ้นสุดต้องไม่มาก่อนวันที่เริ่ม";
                return RedirectToAction(nameof(Index));
            }

            if (image.Length > MaxFileSize)
            {
                TempData["ErrorMessage"] = "ไฟล์ใหญ่เกินไป (จำกัด 5 MB)";
                return RedirectToAction(nameof(Index));
            }

            var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!AllowedExt.Contains(ext))
            {
                TempData["ErrorMessage"] = "อนุญาตเฉพาะไฟล์ .jpg .png .webp";
                return RedirectToAction(nameof(Index));
            }

            var folder = Path.Combine(_env.WebRootPath, BannerFolder);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            var fileName = Guid.NewGuid().ToString("N") + ext;
            using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            banner.ImageFileName = fileName;
            banner.CreatedAt = DateTime.Now;

            _context.Banners.Add(banner);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"เพิ่มแบนเนอร์ \"{banner.Title}\" เรียบร้อยแล้ว";
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // เปิด/ปิดการใช้งาน
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null) return NotFound();

            banner.IsActive = !banner.IsActive;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = banner.IsActive
                ? $"เปิดใช้งาน \"{banner.Title}\" แล้ว"
                : $"ปิดใช้งาน \"{banner.Title}\" แล้ว (ยังไม่ถูกลบ เปิดกลับได้)";

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // แก้ไขข้อความ / ลำดับ (ไม่เปลี่ยนรูป)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateText(int id, string title, string? subtitle, int sortOrder,
                                                    DateTime? startDate, DateTime? endDate)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null) return NotFound();

            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["ErrorMessage"] = "หัวข้อห้ามว่าง";
                return RedirectToAction(nameof(Index));
            }

            if (startDate != null && endDate != null && endDate < startDate)
            {
                TempData["ErrorMessage"] = "วันที่สิ้นสุดต้องไม่มาก่อนวันที่เริ่ม";
                return RedirectToAction(nameof(Index));
            }

            banner.Title = title;
            banner.Subtitle = subtitle;
            banner.SortOrder = sortOrder;
            banner.StartDate = startDate;
            banner.EndDate = endDate;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "บันทึกการแก้ไขเรียบร้อยแล้ว";
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // ลบถาวร (ลบไฟล์รูปด้วย)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null) return NotFound();

            // ลบไฟล์รูปออกจากเซิร์ฟเวอร์ ไม่ให้ค้างกินพื้นที่
            if (!string.IsNullOrEmpty(banner.ImageFileName))
            {
                var path = Path.Combine(_env.WebRootPath, BannerFolder, banner.ImageFileName);
                if (System.IO.File.Exists(path))
                {
                    try { System.IO.File.Delete(path); }
                    catch { /* ลบไฟล์ไม่สำเร็จก็ให้ลบข้อมูลใน DB ต่อไป */ }
                }
            }

            _context.Banners.Remove(banner);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"ลบแบนเนอร์ \"{banner.Title}\" เรียบร้อยแล้ว";
            return RedirectToAction(nameof(Index));
        }
    }
}
