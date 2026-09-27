using CS66_WebApplication1.Models;
using CS66_WebApplication1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace CS66_WebApplication1.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _db;
        private const string CART_KEY = "CART";

        public CheckoutController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var cid = HttpContext.Session.GetInt32("Customer_Id");
            if (cid == null) return RedirectToAction("Login", "Account");

            var customer = _db.Customers
                .Include(c => c.Amphur)
                .Include(c => c.Province)
                .FirstOrDefault(c => c.Customer_Id == cid);

            ViewBag.CustomerInfo = customer;

            var cart = HttpContext.Session.GetObject<List<CartItem>>(CART_KEY) ?? new List<CartItem>();
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(string DeliveryMethod, string AppointmentDate)
        {
            var cid = HttpContext.Session.GetInt32("Customer_Id");
            if (cid == null) return RedirectToAction("Login", "Account");

            var cart = HttpContext.Session.GetObject<List<CartItem>>(CART_KEY);
            if (cart == null || !cart.Any()) return RedirectToAction("Index", "Cart");

            // กันสั่งเกินสต็อก (สต็อกจริงจะถูกตัดตอนแอดมินยืนยันสลิป)
            foreach (var item in cart)
            {
                var product = await _db.Products.FindAsync(item.Product_Id);
                if (product == null || product.Quantity < item.Qty)
                {
                    TempData["ErrorMessage"] = $"สินค้า \"{item.Product_Name}\" มีไม่พอ (เหลือ {product?.Quantity ?? 0} ชิ้น) กรุณาปรับจำนวนในตะกร้า";
                    return RedirectToAction("Index", "Cart");
                }
            }

            // แปลงวันที่อย่างปลอดภัย
            DateTime? finalAppDate = null;
            if (DeliveryMethod == "Pickup" && !string.IsNullOrEmpty(AppointmentDate))
            {
                // แปลง string จาก HTML (yyyy-MM-ddTHH:mm) เป็น DateTime
                if (DateTime.TryParse(AppointmentDate, out DateTime parsedDate))
                {
                    finalAppDate = parsedDate;
                }
            }

            var order = new Order
            {
                Customer_Id = cid.Value,
                OrderNumber = await _db.GenerateOrderNumberAsync(),
                OrderDate = DateTime.Now,
                GrandTotal = cart.Sum(c => c.Price * c.Qty),
                Status = "New",
                ShippingStatus = "Pending",
                DeliveryMethod = DeliveryMethod ?? "Delivery",
                AppointmentDate = finalAppDate
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            foreach (var item in cart)
            {
                _db.OrderDetails.Add(new OrderDetail
                {
                    Order_Id = order.Order_Id,
                    Product_Id = item.Product_Id,
                    Price = item.Price,
                    Qty = item.Qty,
                    SubTotal = item.Price * item.Qty
                });
            }

            _db.SaveChanges();
            HttpContext.Session.Remove(CART_KEY);

            return RedirectToAction("Success", new { id = order.Order_Id });
        }

        public IActionResult Success(int id)
        {
            var order = _db.Orders
                .Include(o => o.Customer).ThenInclude(c => c.Amphur)
                .Include(o => o.Customer).ThenInclude(c => c.Province)
                .FirstOrDefault(o => o.Order_Id == id);

            if (order == null) return RedirectToAction("History", "Order");
            if (!CanAccessOrder(order)) return Forbid();

            ViewBag.Details = _db.OrderDetails.Include(d => d.Product).Where(d => d.Order_Id == id).ToList();
            return View(order);
        }

        // ✓ แสดงใบเสร็จให้ลูกค้า
        public IActionResult Receipt(int id)
        {
            var order = _db.Orders
                .Include(o => o.Customer).ThenInclude(c => c.Amphur)
                .Include(o => o.Customer).ThenInclude(c => c.Province)
                .FirstOrDefault(o => o.Order_Id == id);

            if (order == null) return RedirectToAction("History", "Order");
            if (!CanAccessOrder(order)) return Forbid();

            ViewBag.Details = _db.OrderDetails.Include(d => d.Product).Where(d => d.Order_Id == id).ToList();
            ViewBag.Payment = _db.Payments.FirstOrDefault(p => p.Order_Id == id);
            return View(order);
        }

        // เจ้าของออเดอร์ดูได้ แอดมินดูได้ทุกใบ
        private bool CanAccessOrder(Order order)
        {
            if (HttpContext.Session.GetInt32("Role") == 9) return true;
            return order.Customer_Id == HttpContext.Session.GetInt32("Customer_Id");
        }
    }
}