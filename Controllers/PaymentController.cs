using CS66_WebApplication1.Filters;
using CS66_WebApplication1.Models;
using CS66_WebApplication1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CS66_WebApplication1.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment env;

        public PaymentController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            this.context = context;
            this.env = env;
        }

        // ================================
        // ADMIN LIST PAYMENTS
        // ================================
        [AdminAuthorize]
        public IActionResult Index()
        {
            var payments = context.Payments
                .Include(p => p.Order)
                .OrderByDescending(p => p.PaidAt)
                .ToList();

            return View(payments);
        }

        // ================================
        // CUSTOMER UPLOAD SLIP PAGE
        // ================================
        public IActionResult Create(int orderId)
        {
            int? customerId = HttpContext.Session.GetInt32("Customer_Id");
            if (customerId == null) return RedirectToAction("Login", "Account");

            var order = context.Orders
                .FirstOrDefault(o => o.Order_Id == orderId);

            if (order == null)
                return NotFound();

            if (!CanAccessOrder(order)) return Forbid();

            ViewBag.Order = order;
            ViewBag.OrderId = orderId;

            return View();
        }

        // ================================
        // SAVE PAYMENT
        // ================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int orderId,
            decimal amount,
            DateTime paidAt,
            IFormFile slip)
        {
            int? customerId = HttpContext.Session.GetInt32("Customer_Id");
            if (customerId == null) return RedirectToAction("Login", "Account");

            var order = await context.Orders
                .FirstOrDefaultAsync(o => o.Order_Id == orderId);

            if (order == null)
                return NotFound();

            if (!CanAccessOrder(order)) return Forbid();

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "ข้อมูลไม่ถูกต้อง";
                return RedirectToAction("Create", new { orderId });
            }

            if (amount <= 0)
            {
                TempData["Error"] = "จำนวนเงินไม่ถูกต้อง";
                return RedirectToAction("Create", new { orderId });
            }

            if (slip == null || slip.Length == 0)
            {
                TempData["Error"] = "กรุณาอัปโหลดสลิป";
                return RedirectToAction("Create", new { orderId });
            }

            // ป้องกันแจ้งชำระซ้ำ
            var existingPayment = await context.Payments
                .FirstOrDefaultAsync(p => p.Order_Id == orderId);

            if (existingPayment != null)
            {
                TempData["Error"] = "คำสั่งซื้อนี้มีการแจ้งชำระเงินแล้ว";
                return RedirectToAction("History", "Order");
            }

            // ตรวจสอบประเภทไฟล์
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(slip.FileName).ToLower();

            if (!allowedExtensions.Contains(ext))
            {
                TempData["Error"] = "อนุญาตเฉพาะไฟล์รูปภาพ";
                return RedirectToAction("Create", new { orderId });
            }

            // สร้างชื่อไฟล์
            string fileName = Guid.NewGuid().ToString() + ext;

            // สร้างโฟลเดอร์
            string folder = Path.Combine(env.WebRootPath, "slips");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            // path
            string path = Path.Combine(folder, fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await slip.CopyToAsync(stream);
            }

            // บันทึก Payment
            Payment payment = new Payment()
            {
                Order_Id = orderId,
                PaidAt = paidAt,
                Amount = amount,
                SlipFileName = fileName,
                Status = "Pending"
            };

            context.Payments.Add(payment);

            // อัปเดตสถานะ Order
            order.Status = "PendingVerify";

            await context.SaveChangesAsync();

            return RedirectToAction("History", "Order");
        }

        // ================================
        // VIEW SLIP DETAILS
        // ================================
        public IActionResult Details(int id)
        {
            int? customerId = HttpContext.Session.GetInt32("Customer_Id");
            if (customerId == null) return RedirectToAction("Login", "Account");

            var payment = context.Payments
                .Include(p => p.Order)
                .FirstOrDefault(p => p.Payment_Id == id);

            if (payment == null)
                return NotFound();

            if (payment.Order != null && !CanAccessOrder(payment.Order)) return Forbid();

            return View(payment);
        }

        // เจ้าของออเดอร์ดูได้ แอดมินดูได้ทุกใบ
        private bool CanAccessOrder(Order order)
        {
            if (HttpContext.Session.GetInt32("Role") == 9) return true;
            return order.Customer_Id == HttpContext.Session.GetInt32("Customer_Id");
        }

        // ================================
        // ADMIN APPROVE PAYMENT
        // ================================
        [HttpPost]
        [AdminAuthorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var payment = await context.Payments.FindAsync(id);

            if (payment == null)
                return NotFound();

            payment.Status = "Approved";
            payment.VerifiedAt = DateTime.Now;

            var order = await context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Order_Id == payment.Order_Id);

            if (order != null && order.Status != "Processing")
            {
                // ตัดสต็อกตอนอนุมัติการชำระเงิน (ครั้งเดียวต่อออเดอร์)
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
            }

            await context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // ================================
        // ADMIN REJECT PAYMENT
        // ================================
        [HttpPost]
        [AdminAuthorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var payment = await context.Payments.FindAsync(id);

            if (payment == null)
                return NotFound();

            payment.Status = "Rejected";
            payment.VerifiedAt = DateTime.Now;

            await context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}