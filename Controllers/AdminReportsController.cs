using CS66_WebApplication1.Filters;
using CS66_WebApplication1.Models;
using CS66_WebApplication1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CS66_WebApplication1.Controllers
{
    [AdminAuthorize]
    public class AdminReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // สถานะที่ถือว่าเก็บเงินได้แล้ว (ชุดเดียวกับ Dashboard/Finance)
        private static readonly string[] PaidStatuses =
            { "Paid", "Processing", "Shipped", "Delivered", "Completed" };

        // สินค้าที่เหลือน้อยกว่าหรือเท่านี้ ถือว่า "ใกล้หมด"
        private const int LowStockThreshold = 5;

        public async Task<IActionResult> Index(string period = "day")
        {
            var (from, to, label) = ResolveRange(period);

            // ---------- รายการสั่งซื้อในช่วงเวลา ----------
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                .Where(o => PaidStatuses.Contains(o.Status)
                         && o.OrderDate >= from && o.OrderDate < to)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            ViewBag.Period = period;
            ViewBag.PeriodLabel = label;
            ViewBag.TotalRevenue = orders.Sum(o => o.GrandTotal);
            ViewBag.OrderCount = orders.Count;

            // ---------- สินค้าขายดี (ทั้งหมดที่เคยขาย) ----------
            ViewBag.BestSellers = await _context.OrderDetails
                .Include(d => d.Product)
                .Include(d => d.Order)
                .Where(d => d.Order != null && PaidStatuses.Contains(d.Order.Status) && d.Product != null)
                .GroupBy(d => d.Product_Id)
                .Select(g => new BestSellerItem
                {
                    Product = g.First().Product!,
                    SoldQty = g.Sum(x => x.Qty)
                })
                .OrderByDescending(x => x.SoldQty)
                .Take(10)
                .ToListAsync();

            // ---------- สินค้าหมด / ใกล้หมด ----------
            ViewBag.LowStock = await _context.Products
                .Include(p => p.Brand)
                .Where(p => p.Quantity <= LowStockThreshold)
                .OrderBy(p => p.Quantity)
                .ToListAsync();

            ViewBag.LowStockThreshold = LowStockThreshold;

            return View(orders);
        }

        [HttpGet]
        public async Task<IActionResult> ProductSalesDetail(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            var now = DateTime.Now;
            var startMonth = new DateTime(now.Year, now.Month, 1).AddMonths(-11);

            var monthlySales = await _context.OrderDetails
                .Include(d => d.Order)
                .Where(d => d.Product_Id == productId
                         && d.Order != null
                         && PaidStatuses.Contains(d.Order.Status)
                         && d.Order.OrderDate >= startMonth)
                .GroupBy(d => new { d.Order!.OrderDate.Year, d.Order.OrderDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Qty = g.Sum(x => x.Qty),
                    Revenue = g.Sum(x => x.SubTotal)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            int maxQty = monthlySales.Any() ? monthlySales.Max(x => x.Qty) : 0;

            var months = new List<object>();
            for (int i = 0; i < 12; i++)
            {
                var m = startMonth.AddMonths(i);
                var data = monthlySales.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month);
                int qty = data?.Qty ?? 0;

                months.Add(new
                {
                    label = m.ToString("MMM yy"),
                    fullLabel = m.ToString("MMMM yyyy"),
                    quarter = $"Q{(m.Month - 1) / 3 + 1}",
                    qty,
                    revenue = data?.Revenue ?? 0m,
                    level = PopularityLevel(qty, maxQty),
                    ratio = maxQty > 0 ? Math.Round((double)qty / maxQty, 3) : 0
                });
            }

            var quarters = Enumerable.Range(0, 4)
                .Select(q =>
                {
                    var qMonths = Enumerable.Range(0, 12)
                        .Select(i => startMonth.AddMonths(i))
                        .Where(m => (m.Month - 1) / 3 == q)
                        .ToList();

                    int qQty = qMonths.Sum(m =>
                        monthlySales.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month)?.Qty ?? 0);

                    return new { name = $"ไตรมาส {q + 1}", qty = qQty };
                })
                .ToList();

            int maxQuarter = quarters.Any() ? quarters.Max(x => x.qty) : 0;
            var quarterData = quarters.Select(x => new
            {
                x.name,
                x.qty,
                ratio = maxQuarter > 0 ? Math.Round((double)x.qty / maxQuarter, 3) : 0
            }).ToList();

            var peakMonth = monthlySales.OrderByDescending(x => x.Qty).FirstOrDefault();
            string peakLabel = peakMonth != null
                ? new DateTime(peakMonth.Year, peakMonth.Month, 1).ToString("MMMM yyyy")
                : "-";

            return Json(new
            {
                productName = product.Product_Name,
                totalSold = monthlySales.Sum(x => x.Qty),
                totalRevenue = monthlySales.Sum(x => x.Revenue),
                peakMonth = peakLabel,
                peakQty = peakMonth?.Qty ?? 0,
                activeMonths = monthlySales.Count(x => x.Qty > 0),
                months,
                quarters = quarterData
            });
        }

        // จัดระดับความนิยมเทียบกับเดือนที่ขายดีที่สุดของสินค้าชิ้นนั้น
        private static string PopularityLevel(int qty, int maxQty)
        {
            if (qty <= 0 || maxQty <= 0) return "none";
            double ratio = (double)qty / maxQty;
            if (ratio >= 0.7) return "hot";
            if (ratio >= 0.35) return "warm";
            return "cool";
        }

        private static (DateTime from, DateTime to, string label) ResolveRange(string period)
        {
            var today = DateTime.Today;

            return period switch
            {
                "month" => (new DateTime(today.Year, today.Month, 1),
                            new DateTime(today.Year, today.Month, 1).AddMonths(1),
                            $"เดือน{today:MMMM yyyy}"),
                "year" => (new DateTime(today.Year, 1, 1),
                           new DateTime(today.Year + 1, 1, 1),
                           $"ปี {today.Year}"),
                _ => (today, today.AddDays(1), $"วันนี้ ({today:dd MMM yyyy})")
            };
        }
    }
}
