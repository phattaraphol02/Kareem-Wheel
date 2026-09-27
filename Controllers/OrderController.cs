using CS66_WebApplication1.Models;
using CS66_WebApplication1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CS66_WebApplication1.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext context;

        public OrderController(ApplicationDbContext context)
        {
            this.context = context;
        }

        // --- ลูกค้า: ดูประวัติคำสั่งซื้อของตัวเอง ---
        public async Task<IActionResult> History()
        {
            int? customerId = HttpContext.Session.GetInt32("Customer_Id");
            if (customerId == null) return RedirectToAction("Login", "Account");

            var myOrders = await context.Orders
                .Where(o => o.Customer_Id == customerId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(myOrders);
        }

        // --- Admin: หน้าจัดการคำสั่งซื้อรวม (ตรวจสอบสลิป + จัดส่ง) ---
        public async Task<IActionResult> AdminOrderHistory(string status, string search)
        {
            if (HttpContext.Session.GetInt32("Role") != 9) return RedirectToAction("Login", "Account");

            var orders = context.Orders.Include(o => o.Customer).OrderByDescending(o => o.OrderDate).AsQueryable();

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                if (status == "PendingVerify")
                    orders = orders.Where(o => o.Status == "PendingVerify");
                else
                    orders = orders.Where(o => o.ShippingStatus == status);
            }

            if (!string.IsNullOrEmpty(search))
                orders = orders.Where(o => o.Customer.Customer_Name.Contains(search));

            var pendingVerifyCount = await context.Orders.CountAsync(o => o.Status == "PendingVerify");
            ViewBag.PendingVerifyCount = pendingVerifyCount;
            ViewBag.CurrentStatus = status ?? "All";
            return View(await orders.ToListAsync());
        }

        // --- Redirect เดิมมาที่หน้ารวม ---
        public IActionResult VerifyPayments()
        {
            return RedirectToAction(nameof(AdminOrderHistory), new { status = "PendingVerify" });
        }

        // --- Admin: ยืนยันสลิป (ไม่ต้องกรอกเลขพัสดุ) → สถานะเป็น Processing (เตรียมสินค้า) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int orderId, string returnTo)
        {
            if (HttpContext.Session.GetInt32("Role") != 9) return Forbid();

            var order = await context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Order_Id == orderId);

            if (order == null) return NotFound();

            // กันกดยืนยันซ้ำ ไม่งั้นสต็อกจะถูกตัดหลายรอบ
            if (order.Status == "Processing" || order.Status == "Shipped" || order.Status == "Delivered")
            {
                TempData["Success"] = "คำสั่งซื้อนี้ยืนยันสลิปไปแล้ว";
                if (returnTo == "Details") return RedirectToAction(nameof(Details), new { id = orderId });
                return RedirectToAction(nameof(AdminOrderHistory), new { status = "PendingVerify" });
            }

            // ตัดสต็อกสินค้าตอนยืนยันสลิป (ยืนยันแล้วว่าลูกค้าจ่ายเงินจริง)
            if (!order.StockDeducted)
            {
                foreach (var item in order.OrderDetails ?? new List<OrderDetail>())
                {
                    var product = await context.Products.FindAsync(item.Product_Id);
                    if (product != null)
                    {
                        product.Quantity -= item.Qty;
                        if (product.Quantity < 0) product.Quantity = 0;
                    }
                }
                order.StockDeducted = true;
            }

            order.Status = "Processing";
            order.ShippingStatus = "Processing";

            await context.SaveChangesAsync();

            TempData["Success"] = "ยืนยันสลิปเรียบร้อยแล้ว ตัดสต็อกสินค้าและเปลี่ยนสถานะเป็น \"เตรียมสินค้า\"";

            if (returnTo == "Details")
                return RedirectToAction(nameof(Details), new { id = orderId });

            return RedirectToAction(nameof(VerifyPayments));
        }

        // --- Admin: อัปเดตเลขพัสดุ/สถานะการจัดส่ง จากหน้า AdminOrderHistory ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateShipping(int id, string shippingCompany, string trackingNo, string status, string returnTo)
        {
            if (HttpContext.Session.GetInt32("Role") != 9) return Forbid();

            var order = await context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            if (!string.IsNullOrEmpty(shippingCompany))
                order.ShippingCompany = shippingCompany;

            if (!string.IsNullOrEmpty(trackingNo))
                order.TrackingNo = trackingNo;

            order.ShippingStatus = status;
            order.Status = status;

            if (status == "Shipped" && order.ShippedAt == null)
                order.ShippedAt = DateTime.Now;

            await context.SaveChangesAsync();

            TempData["Success"] = "อัปเดตสถานะการจัดส่งเรียบร้อยแล้ว";

            if (returnTo == "Details")
                return RedirectToAction(nameof(Details), new { id });

            return RedirectToAction(nameof(AdminOrderHistory));
        }

        // --- หน้าดูรายละเอียด (ใช้ร่วมกัน) ---
        public async Task<IActionResult> Details(int id)
        {
            var order = await context.Orders
                .Include(o => o.Customer).ThenInclude(c => c.Amphur)
                .Include(o => o.Customer).ThenInclude(c => c.Province)
                .Include(o => o.OrderDetails).ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.Order_Id == id);

            if (order == null) return NotFound();

            int? customerId = HttpContext.Session.GetInt32("Customer_Id");
            int? role = HttpContext.Session.GetInt32("Role");
            if (role != 9 && order.Customer_Id != customerId) return Forbid();

            return View(order);
        }

        // --- Action สำหรับลูกค้า (อัปโหลดสลิป) ---
        [HttpPost]
        public async Task<IActionResult> UploadSlip(int orderId, IFormFile slip)
        {
            var order = await context.Orders.FindAsync(orderId);
            if (order == null || slip == null) return NotFound();

            int? customerId = HttpContext.Session.GetInt32("Customer_Id");
            if (customerId == null || order.Customer_Id != customerId)
                return Forbid();

            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "slips");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            var fileName = Guid.NewGuid() + Path.GetExtension(slip.FileName);
            using (var stream = new FileStream(Path.Combine(folderPath, fileName), FileMode.Create))
            {
                await slip.CopyToAsync(stream);
            }

            order.SlipImage = fileName;
            order.Status = "PendingVerify";
            order.PaidAt = DateTime.Now;

            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = orderId });
        }
    }
}