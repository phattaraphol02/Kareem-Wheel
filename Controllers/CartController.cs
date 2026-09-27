using CS66_WebApplication1.Models;
using CS66_WebApplication1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CS66_WebApplication1.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;
        private const string CART_KEY = "CART";

        public CartController(ApplicationDbContext db) { _db = db; }

        // แสดงตะกร้าสินค้า
        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>(CART_KEY) ?? new List<CartItem>();
            return View(cart);
        }

        // ดึงจำนวนสินค้าไปโชว์ที่ไอคอน (AJAX)
        [HttpGet]
        public IActionResult GetCartCount()
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>(CART_KEY) ?? new List<CartItem>();
            return Json(cart.Sum(x => x.Qty));
        }

        // เพิ่มสินค้า (AJAX)
        [HttpPost]
        public IActionResult AddToCart(int id)
        {
            var product = _db.Products.FirstOrDefault(p => p.Product_Id == id);
            if (product == null) return Json(new { success = false, message = "ไม่พบสินค้า" });

            if (product.Quantity <= 0)
                return Json(new { success = false, message = $"สินค้า \"{product.Product_Name}\" หมดแล้ว" });

            var cart = HttpContext.Session.GetObject<List<CartItem>>(CART_KEY) ?? new List<CartItem>();
            var item = cart.FirstOrDefault(c => c.Product_Id == id);

            if (item == null)
            {
                cart.Add(new CartItem
                {
                    Product_Id = product.Product_Id,
                    Product_Name = product.Product_Name,
                    Price = product.Price,
                    Qty = 1,
                    ImageFileName = product.ImageFileName
                });
            }
            else
            {
                // ห้ามใส่ตะกร้าเกินจำนวนที่มีในคลัง
                if (item.Qty >= product.Quantity)
                    return Json(new { success = false, message = $"สินค้า \"{product.Product_Name}\" มีในคลังเพียง {product.Quantity} ชิ้น" });

                item.Qty++;
            }

            HttpContext.Session.SetObject(CART_KEY, cart);
            return Json(new { success = true, count = cart.Sum(x => x.Qty) });
        }

        // สั่งซื้อทันที (CheckoutNow)
        public IActionResult CheckoutNow(int id)
        {
            var product = _db.Products.FirstOrDefault(p => p.Product_Id == id);
            if (product != null)
            {
                var cart = HttpContext.Session.GetObject<List<CartItem>>(CART_KEY) ?? new List<CartItem>();
                var item = cart.FirstOrDefault(c => c.Product_Id == id);
                if (item == null)
                {
                    cart.Add(new CartItem { Product_Id = product.Product_Id, Product_Name = product.Product_Name, Price = product.Price, Qty = 1, ImageFileName = product.ImageFileName });
                }
                else { item.Qty++; }
                HttpContext.Session.SetObject(CART_KEY, cart);
            }
            return RedirectToAction("Checkout");
        }

        // หน้า Review สินค้าก่อนสั่งซื้อ
        public IActionResult Checkout()
        {
            var cid = HttpContext.Session.GetInt32("Customer_Id");
            if (cid == null) return RedirectToAction("Login", "Account");

            var cart = HttpContext.Session.GetObject<List<CartItem>>(CART_KEY);
            if (cart == null || !cart.Any()) return RedirectToAction("Index");

            var customer = _db.Customers
                .Include(c => c.Province)
                .Include(c => c.Amphur)
                .FirstOrDefault(c => c.Customer_Id == cid);

            ViewBag.CustomerInfo = customer;
            return View(cart);
        }

        // ดำเนินการสั่งซื้อ (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder()
        {
            var cid = HttpContext.Session.GetInt32("Customer_Id");
            if (cid == null) return RedirectToAction("Login", "Account");

            var cart = HttpContext.Session.GetObject<List<CartItem>>(CART_KEY);
            if (cart == null || !cart.Any()) return RedirectToAction("Index");

            // กันสั่งเกินสต็อก (สต็อกจริงจะถูกตัดตอนแอดมินยืนยันสลิป)
            foreach (var item in cart)
            {
                var product = await _db.Products.FindAsync(item.Product_Id);
                if (product == null || product.Quantity < item.Qty)
                {
                    TempData["Error"] = $"สินค้า \"{item.Product_Name}\" มีไม่พอ (เหลือ {product?.Quantity ?? 0} ชิ้น) กรุณาปรับจำนวนในตะกร้า";
                    return RedirectToAction("Index");
                }
            }

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var order = new Order
                {
                    Customer_Id = cid.Value,
                    OrderNumber = await _db.GenerateOrderNumberAsync(),
                    OrderDate = DateTime.Now,
                    GrandTotal = cart.Sum(c => c.Price * c.Qty),
                    Status = "New"
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
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                HttpContext.Session.Remove(CART_KEY);
                return RedirectToAction("Details", "Order", new { id = order.Order_Id });
            }
            catch { await transaction.RollbackAsync(); return RedirectToAction("Index"); }
        }

        public IActionResult UpdateQty(int id, int qty)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>(CART_KEY);
            if (cart != null)
            {
                var item = cart.FirstOrDefault(c => c.Product_Id == id);
                if (item != null)
                {
                    int newQty = item.Qty + qty;

                    // ห้ามเกินจำนวนที่มีในคลังจริง
                    int stock = _db.Products
                        .Where(p => p.Product_Id == id)
                        .Select(p => p.Quantity)
                        .FirstOrDefault();

                    if (newQty > stock)
                    {
                        newQty = stock;
                        TempData["ErrorMessage"] = $"สินค้า \"{item.Product_Name}\" มีในคลังเพียง {stock} ชิ้น";
                    }

                    item.Qty = newQty;
                    if (item.Qty <= 0) cart.Remove(item);
                }
                HttpContext.Session.SetObject(CART_KEY, cart);
            }
            return RedirectToAction("Index");
        }

        public IActionResult Remove(int id)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>(CART_KEY);
            if (cart != null)
            {
                var item = cart.FirstOrDefault(c => c.Product_Id == id);
                if (item != null) cart.Remove(item);
                HttpContext.Session.SetObject(CART_KEY, cart);
            }
            return RedirectToAction("Index");
        }
    }
}