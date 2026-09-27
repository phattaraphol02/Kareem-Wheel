using CS66_WebApplication1.Models; // เปลี่ยนให้ตรงกับ Namespace ของโปรเจกต์คุณ
using CS66_WebApplication1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace CS66_WebApplication1.Controllers
{
    public class DistrictController : Controller
    {
        // กำหนดตัวแปรสำหรับเชื่อมต่อฐานข้อมูล (อ้างอิงชื่อ DbContext ของคุณ)
        // หาก DbContext ของคุณชื่ออื่น เช่น AppDbContext ให้เปลี่ยนให้ตรงกันนะครับ
        private readonly ApplicationDbContext _context;

        public DistrictController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /District/Index
        // สำหรับแสดงหน้ารายการตำบลทั้งหมด (ถ้ามี)
        public async Task<IActionResult> Index()
        {
            // รวม (Include) ข้อมูลอำเภอมาด้วย เผื่อต้องการแสดงชื่ออำเภอในตาราง
            var districts = await _context.Districts
                .Include(d => d.Amphur)
                .ToListAsync();

            return View(districts);
        }

        // ========================================================
        // API Endpoint สำหรับให้ AJAX เรียกใช้ตอนทำ Dropdown
        // ========================================================

        // GET: /District/GetDistrictsByAmphur/?amphurId=1001
        [HttpGet]
        public async Task<JsonResult> GetDistrictsByAmphur(string amphurId)
        {
            if (string.IsNullOrEmpty(amphurId))
            {
                return Json(new { success = false, message = "Amphur ID is required" });
            }

            // ดึงข้อมูลตำบลที่ตรงกับรหัสอำเภอที่ส่งมา
            var districts = await _context.Districts
                .Where(d => d.Amphur_Id == amphurId)
                .Select(d => new
                {
                    Value = d.District_Id,
                    Text = d.District_Name,
                    PostCode = d.PostCodeMain // ส่งรหัสไปรษณีย์กลับไปด้วยเผื่อต้องการ Auto-fill
                })
                .OrderBy(d => d.Text) // เรียงตามตัวอักษร
                .ToListAsync();

            return Json(districts);
        }
    }
}