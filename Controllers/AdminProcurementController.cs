using CS66_WebApplication1.Filters;
using CS66_WebApplication1.Models;
using CS66_WebApplication1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CS66_WebApplication1.Controllers
{
    [AdminAuthorize]
    public class AdminProcurementController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AdminProcurementController(ApplicationDbContext context) => _context = context;

        // 1. หน้าแสดงรายการใบสั่งซื้อ
        public async Task<IActionResult> Index()
        {
            var list = await _context.PurchaseOrders
                                     .Include(p => p.Supplier)
                                     .OrderByDescending(p => p.PO_Id)
                                     .ToListAsync();

            ViewBag.ItemCounts = await _context.PurchaseOrderDetails
                                               .GroupBy(d => d.PO_Id)
                                               .Select(g => new { g.Key, Count = g.Count() })
                                               .ToDictionaryAsync(x => x.Key, x => x.Count);

            return View(list);
        }

        // 2. หน้าสร้างใบสั่งซื้อ (GET)
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new CreatePOViewModel { OrderDate = DateTime.Now });
        }

        // 2. บันทึกข้อมูลใบสั่งซื้อ (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePOViewModel model)
        {
            // ✓ ต้องเลือกซัพพลายเออร์และมีอย่างน้อย 1 รายการ
            if (model.SupplierId <= 0)
            {
                TempData["ErrorMessage"] = "กรุณาเลือกซัพพลายเออร์";
                await LoadDropdowns();
                return View(model);
            }

            var validItems = model.Items?
                .Where(i => (i.ProductId.HasValue || i.SparePartId.HasValue) && i.Quantity > 0)
                .ToList() ?? new List<POItemDto>();

            if (!validItems.Any())
            {
                TempData["ErrorMessage"] = "กรุณาเพิ่มรายการสินค้าอย่างน้อย 1 รายการ พร้อมระบุจำนวน";
                await LoadDropdowns();
                return View(model);
            }

            // ✓ กันเลือกสินค้า/อะไหล่ชิ้นเดียวกันซ้ำหลายแถวในใบเดียวกัน
            var duplicateKeys = validItems
                .GroupBy(i => i.ItemType == "SparePart" ? $"SP-{i.SparePartId}" : $"P-{i.ProductId}")
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateKeys.Any())
            {
                TempData["ErrorMessage"] = "มีรายการสินค้าซ้ำกันในใบสั่งซื้อเดียวกัน กรุณารวมจำนวนไว้แถวเดียว";
                await LoadDropdowns();
                return View(model);
            }

            var supplier = await _context.Suppliers.FindAsync(model.SupplierId);
            if (supplier == null)
            {
                TempData["ErrorMessage"] = "ไม่พบซัพพลายเออร์ที่เลือก";
                await LoadDropdowns();
                return View(model);
            }

            var po = new PurchaseOrder
            {
                SupplierId = model.SupplierId,
                OrderDate = model.OrderDate,
                Status = "Pending"
            };
            _context.PurchaseOrders.Add(po);
            await _context.SaveChangesAsync(); // ให้ได้ PO_Id ก่อน

            decimal grandTotal = 0;

            foreach (var item in validItems)
            {
                // ✓ อะไหล่บังคับเป็นมือ1เสมอ ตามสเปก (ล้อ/ยางเลือกได้)
                string condition = item.ItemType == "SparePart" ? "มือ1" : (item.Condition == "มือ2" ? "มือ2" : "มือ1");

                var detail = new PurchaseOrderDetail
                {
                    PO_Id = po.PO_Id,
                    ItemType = item.ItemType == "SparePart" ? "SparePart" : "Product",
                    ProductId = item.ItemType == "SparePart" ? null : item.ProductId,
                    SparePartId = item.ItemType == "SparePart" ? item.SparePartId : null,
                    Quantity = item.Quantity,
                    CostPrice = item.CostPrice,
                    Condition = condition
                };
                _context.PurchaseOrderDetails.Add(detail);

                grandTotal += item.Quantity * item.CostPrice;
            }

            po.GrandTotal = grandTotal;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"สร้างใบสั่งซื้อ PO-{po.PO_Id:D4} เรียบร้อยแล้ว ยอดรวม ฿{grandTotal:N2}";
            return RedirectToAction(nameof(Index));
        }

        // 3. หน้าตรวจสอบและรับสินค้าเข้าร้าน (GET) — กรอกจำนวนที่ได้รับจริงทีละรายการ
        public async Task<IActionResult> Receive(int id)
        {
            var po = await _context.PurchaseOrders
                                   .Include(p => p.Supplier)
                                   .FirstOrDefaultAsync(p => p.PO_Id == id);
            if (po == null) return NotFound();

            var details = await _context.PurchaseOrderDetails
                                        .Include(d => d.Product)
                                        .Include(d => d.SparePart)
                                        .Where(d => d.PO_Id == id)
                                        .ToListAsync();

            ViewBag.PO = po;
            return View(details);
        }

        // 3. บันทึกจำนวนที่ตรวจนับได้จริง แล้วเพิ่มสต็อกตามนั้น (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Receive(int poId, Dictionary<int, int> receivedQty)
        {
            var po = await _context.PurchaseOrders.FindAsync(poId);
            if (po == null || po.Status == "Completed") return NotFound();

            var details = await _context.PurchaseOrderDetails
                                        .Where(d => d.PO_Id == poId)
                                        .ToListAsync();

            bool allFullyReceived = true;

            foreach (var item in details)
            {
                // ✓ จำนวนที่ตรวจนับได้จริง (ถ้าไม่ได้กรอกมา ถือว่าได้ 0 ชิ้น ไม่ใช่เดาว่าได้ครบ)
                int qty = receivedQty != null && receivedQty.TryGetValue(item.DetailId, out var q) ? q : 0;

                // ป้องกันกรอกเกินจำนวนที่สั่ง หรือติดลบ
                qty = Math.Max(0, Math.Min(qty, item.Quantity));
                item.ReceivedQty = qty;

                if (qty < item.Quantity) allFullyReceived = false;

                if (qty > 0)
                {
                    if (item.ItemType == "SparePart" && item.SparePartId.HasValue)
                    {
                        var part = await _context.SpareParts.FindAsync(item.SparePartId.Value);
                        if (part != null) part.StockQuantity += qty;
                    }
                    else if (item.ProductId.HasValue)
                    {
                        var product = await _context.Products.FindAsync(item.ProductId.Value);
                        if (product != null) product.Quantity += qty;
                    }
                }
            }

            po.Status = allFullyReceived ? "Completed" : "PartiallyReceived";
            po.ReceivedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = allFullyReceived
                ? $"รับสินค้าครบตามใบสั่งซื้อ PO-{po.PO_Id:D4} เรียบร้อยแล้ว"
                : $"บันทึกการรับสินค้า PO-{po.PO_Id:D4} แล้ว (ได้รับไม่ครบ — สถานะ: รับของบางส่วน)";

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDropdowns()
        {
            ViewBag.Suppliers = await _context.Suppliers
                                              .Where(s => s.IsActive)
                                              .OrderBy(s => s.Name)
                                              .ToListAsync();

            // ส่ง Category name ไปด้วย เพื่อให้ JS ฝั่งหน้าเว็บกรอง/บังคับ Condition ได้
            ViewBag.Products = await _context.Products
                                             .Include(p => p.Category)
                                             .Where(p => p.IsActive)
                                             .OrderBy(p => p.Product_Name)
                                             .Select(p => new { p.Product_Id, p.Product_Name, CategoryName = p.Category != null ? p.Category.Category_Name : "" })
                                             .ToListAsync();

            ViewBag.SpareParts = await _context.SpareParts
                                               .OrderBy(s => s.Name)
                                               .ToListAsync();
        }
    }
}
