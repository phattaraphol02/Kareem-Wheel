using CS66_WebApplication1.Filters;
using CS66_WebApplication1.Models;
using CS66_WebApplication1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CS66_WebApplication1.Controllers
{
    [AdminAuthorize]
    public class AdminFinanceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminFinanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // สถานะที่ถือว่าเก็บเงินได้แล้ว (ใช้ชุดเดียวกับ Dashboard/รายงาน)
        private static readonly string[] PaidStatuses =
            { "Paid", "Processing", "Shipped", "Delivered", "Completed" };

        // ==========================================
        // หน้าหลัก: สรุปรายรับ-รายจ่าย ตามช่วงเวลา
        // ==========================================
        public async Task<IActionResult> Index(string period = "month")
        {
            var (from, to, label) = ResolveRange(period);

            // ---------- รายรับ ----------
            var productIncome = await _context.Orders
                .Where(o => PaidStatuses.Contains(o.Status)
                         && o.OrderDate >= from && o.OrderDate < to)
                .SumAsync(o => (decimal?)o.GrandTotal) ?? 0;

            // ค่าบริการนับเมื่องานเสร็จสิ้น (ลูกค้าจ่ายและแอดมินยืนยันแล้ว)
            var serviceIncome = await _context.Appointments
                .Where(a => a.Status == "เสร็จสิ้น"
                         && a.ServicePaidAt != null
                         && a.ServicePaidAt >= from && a.ServicePaidAt < to)
                .SumAsync(a => (decimal?)(a.Service != null ? a.Service.Price : 0)) ?? 0;

            // ---------- รายจ่าย ----------
            // 1) ต้นทุนสินค้าเข้าร้าน (จากใบสั่งซื้อที่รับของแล้ว)
            var procurementCost = await _context.PurchaseOrderDetails
                .Include(d => d.PurchaseOrder)
                .Where(d => d.PurchaseOrder != null
                         && d.PurchaseOrder.Status == "Completed"
                         && d.PurchaseOrder.OrderDate >= from && d.PurchaseOrder.OrderDate < to)
                .SumAsync(d => (decimal?)(d.CostPrice * d.Quantity)) ?? 0;

            // 2) รายจ่ายที่บันทึกเอง
            var expenses = await _context.Expenses
                .Where(e => e.ExpenseDate >= from && e.ExpenseDate < to)
                .OrderByDescending(e => e.ExpenseDate)
                .ToListAsync();

            var manualExpense = expenses.Sum(e => e.Amount);

            var totalIncome = productIncome + serviceIncome;
            var totalExpense = procurementCost + manualExpense;

            ViewBag.Period = period;
            ViewBag.PeriodLabel = label;
            ViewBag.ProductIncome = productIncome;
            ViewBag.ServiceIncome = serviceIncome;
            ViewBag.TotalIncome = totalIncome;
            ViewBag.ProcurementCost = procurementCost;
            ViewBag.ManualExpense = manualExpense;
            ViewBag.TotalExpense = totalExpense;
            ViewBag.NetProfit = totalIncome - totalExpense;

            // แยกรายจ่ายตามหมวดหมู่ ไว้โชว์ว่าเงินหมดไปกับอะไรมากที่สุด
            ViewBag.ByCategory = expenses
                .GroupBy(e => e.Category)
                .Select(g => new Expense.CategoryTotal { Category = g.Key, Total = g.Sum(x => x.Amount) })
                .OrderByDescending(x => x.Total)
                .ToList();

            ViewBag.Categories = Expense.Categories;

            return View(expenses);
        }

        // ==========================================
        // บันทึกรายจ่ายใหม่
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Expense expense, string period = "month")
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "กรอกข้อมูลไม่ครบหรือไม่ถูกต้อง กรุณาตรวจสอบอีกครั้ง";
                return RedirectToAction(nameof(Index), new { period });
            }

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"บันทึกรายจ่าย \"{expense.Title}\" เรียบร้อยแล้ว";
            return RedirectToAction(nameof(Index), new { period });
        }

        // ==========================================
        // ลบรายจ่าย
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string period = "month")
        {
            var expense = await _context.Expenses.FindAsync(id);
            if (expense != null)
            {
                _context.Expenses.Remove(expense);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "ลบรายจ่ายเรียบร้อยแล้ว";
            }
            return RedirectToAction(nameof(Index), new { period });
        }

        // ==========================================
        // แปลงช่วงเวลาเป็นวันเริ่ม-วันสิ้นสุด
        // ==========================================
        private static (DateTime from, DateTime to, string label) ResolveRange(string period)
        {
            var today = DateTime.Today;

            return period switch
            {
                "day" => (today, today.AddDays(1), $"วันนี้ ({today:dd MMM yyyy})"),
                "year" => (new DateTime(today.Year, 1, 1),
                           new DateTime(today.Year + 1, 1, 1),
                           $"ปี {today.Year}"),
                _ => (new DateTime(today.Year, today.Month, 1),
                      new DateTime(today.Year, today.Month, 1).AddMonths(1),
                      $"เดือน{today:MMMM yyyy}")
            };
        }
    }
}
