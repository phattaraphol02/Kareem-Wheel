using CS66_WebApplication1.Filters;
using CS66_WebApplication1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CS66_WebApplication1.Controllers
{
    [AdminAuthorize]
    public class AdminDashboardController : Controller
    {
        private readonly ApplicationDbContext context;

        public AdminDashboardController(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 💡 1. กำหนดกลุ่มสถานะที่ถือว่าเป็น "ยอดขายสำเร็จ" ไว้ที่เดียว
            // รวมสถานะ "Completed" เผื่อไว้ในกรณีที่ออเดอร์ Walk-in ใช้สถานะนี้
            var successStatuses = new[] { "Paid", "Processing", "Shipped", "Delivered", "Completed" };

            // ==========================================
            // ส่วนที่ 1: สถิติยอดขายรวม (ทั้งหมดที่เคยมีมา)
            // ==========================================
            var totalSales = await context.Orders
                .Where(o => successStatuses.Contains(o.Status))
                .SumAsync(o => (decimal?)o.GrandTotal) ?? 0;

            var pendingVerify = await context.Orders.CountAsync(o => o.Status == "PendingVerify");
            // "เตรียมสินค้า" = ยืนยันสลิปแล้ว รอกรอกเลขพัสดุ
            var readyToShip = await context.Orders.CountAsync(o => o.Status == "Processing" || o.Status == "Paid");

            ViewBag.TotalSales = totalSales;
            ViewBag.PendingVerify = pendingVerify;
            ViewBag.ReadyToShip = readyToShip;

            // ==========================================
            // ส่วนที่ 2: ข้อมูลกราฟ (7 วันย้อนหลัง)
            // ==========================================
            var startDate = DateTime.Today.AddDays(-6);
            var last7Days = Enumerable.Range(0, 7)
                .Select(i => startDate.AddDays(i))
                .ToList();

            // ดึงข้อมูลออกมาก่อน 1 ชั้น ป้องกัน Error การแปลงค่าวันที่ (Date) ของ EF Core
            var rawSalesData = await context.Orders
                .Where(o => o.OrderDate >= startDate && successStatuses.Contains(o.Status))
                .Select(o => new { o.OrderDate, o.GrandTotal })
                .ToListAsync();

            var salesData = rawSalesData
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new {
                    Date = g.Key,
                    Total = g.Sum(o => o.GrandTotal)
                })
                .ToList();

            var chartLabels = last7Days.Select(d => d.ToString("dd MMM")).ToList();
            var chartValues = last7Days.Select(d => salesData.FirstOrDefault(s => s.Date == d.Date)?.Total ?? 0).ToList();

            ViewBag.ChartLabels = JsonSerializer.Serialize(chartLabels);
            ViewBag.ChartData = JsonSerializer.Serialize(chartValues);

            // ==========================================
            // ส่วนที่ 3: สินค้าขายดี 5 อันดับแรก
            // ==========================================
            var topProducts = await context.OrderDetails
                .Include(od => od.Product)
                .GroupBy(od => new { od.Product_Id, od.Product.Product_Name })
                .Select(g => new {
                    ProductId = g.Key.Product_Id,
                    Name = g.Key.Product_Name ?? "ไม่มีชื่อสินค้า",
                    Qty = g.Sum(od => od.Qty),
                    Total = g.Sum(od => (decimal)od.Price * od.Qty)
                })
                .OrderByDescending(x => x.Qty)
                .Take(5)
                .ToListAsync();

            // ✅ ไม่ต้องเช็ค ?? แล้ว เพราะ ToListAsync() จะคืนค่าเป็น List ว่างให้เสมอถ้าไม่มีข้อมูล
            ViewBag.TopProducts = topProducts;

            return View();
        }
    }
}