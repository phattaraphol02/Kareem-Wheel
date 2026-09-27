using CS66_WebApplication1.Filters;
using CS66_WebApplication1.Models;
using CS66_WebApplication1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CS66_WebApplication1.Controllers
{
    [AdminAuthorize]
    public class WalkInController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WalkInController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.Product_Name)
                .ToListAsync();
            ViewBag.Customers = await _context.Customers.ToListAsync();
            return View(products);
        }

        [HttpPost]
        public async Task<IActionResult> Checkout([FromBody] WalkInOrderDto dto)
        {
            if (dto == null || !dto.CartItems.Any())
            {
                return BadRequest(new { success = false, message = "ไม่มีสินค้าในตะกร้า" });
            }

            // ✓ Validate customerId มีอยู่จริง
            var customer = await _context.Customers.FindAsync(dto.CustomerId);
            if (customer == null)
            {
                return BadRequest(new { success = false, message = "ไม่พบข้อมูลลูกค้า" });
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. สร้าง Order ใหม่ (walk-in = เงินสดทันที)
                    var order = new Order
                    {
                        Customer_Id = dto.CustomerId,
                        OrderNumber = await _context.GenerateOrderNumberAsync(),
                        OrderDate = DateTime.Now,
                        Status = "Paid",
                        DeliveryMethod = "Pickup"
                    };

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync(); // บันทึกเพื่อให้ได้ Order_Id

                    decimal grandTotal = 0;

                    // 2. วนลูปบันทึกรายละเอียดสินค้า + ตัดสต็อก
                    foreach (var item in dto.CartItems)
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product == null) continue;

                        if (product.Quantity < item.Qty)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest(new { success = false, message = $"สินค้า \"{product.Product_Name}\" มีไม่พอ (เหลือ {product.Quantity} ชิ้น)" });
                        }

                        product.Quantity -= item.Qty;
                        decimal subTotal = (decimal)product.Price * item.Qty;
                        grandTotal += subTotal;

                        // ✓ บันทึก SubTotal ให้ OrderDetail ครบ
                        var orderDetail = new OrderDetail
                        {
                            Order_Id = order.Order_Id,
                            Product_Id = item.ProductId,
                            Qty = item.Qty,
                            Price = product.Price,
                            SubTotal = subTotal
                        };
                        _context.OrderDetails.Add(orderDetail);
                    }

                    order.GrandTotal = grandTotal;
                    _context.Orders.Update(order);

                    // ✓ บันทึก Payment record (walk-in = เงินสดทันที)
                    var payment = new Payment
                    {
                        Order_Id = order.Order_Id,
                        PaidAt = DateTime.Now,
                        Amount = grandTotal,
                        Status = "Verified",
                        VerifiedAt = DateTime.Now
                    };
                    _context.Payments.Add(payment);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(new { success = true, message = "บันทึกการขายสำเร็จ!", orderId = order.Order_Id });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { success = false, message = "เกิดข้อผิดพลาด: " + ex.Message });
                }
            }
        }
    }
}