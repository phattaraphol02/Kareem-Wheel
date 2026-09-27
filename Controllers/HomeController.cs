using CS66_WebApplication1.Models;
using CS66_WebApplication1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace CS66_WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context; // ใช้ชื่อตามที่คุณแจ้งมา

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // ดึงสินค้า 4 รายการล่าสุดที่แอดมินเพิ่มเข้ามา
            var products = await _context.Products
                                         .Include(p => p.Brand)
                                         .OrderByDescending(p => p.Product_Id) // เรียงจากไอดีล่าสุด
                                         .Take(4)
                                         .ToListAsync();

            // แบนเนอร์ที่เปิดใช้งาน และอยู่ในช่วงวันที่ที่กำหนดไว้
            // (StartDate/EndDate เป็น null = ไม่จำกัดช่วงเวลา แสดงตลอด)
            var today = DateTime.Today;

            ViewBag.Banners = await _context.Banners
                                            .Where(b => b.IsActive
                                                     && (b.StartDate == null || b.StartDate.Value.Date <= today)
                                                     && (b.EndDate == null || b.EndDate.Value.Date >= today))
                                            .OrderBy(b => b.SortOrder)
                                            .ThenByDescending(b => b.Banner_Id)
                                            .ToListAsync();

            return View(products);
        }

        // เพิ่มฟังก์ชันนี้เพื่อให้ URL /Home/Privacy ทำงานได้
        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult Contact()
        {
            return View();
        }
    }
}