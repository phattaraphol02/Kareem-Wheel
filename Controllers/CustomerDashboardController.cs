using CS66_WebApplication1.Models;
using CS66_WebApplication1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CS66_WebApplication1.Controllers
{
    public class CustomerDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Action หลักสำหรับหน้า Dashboard
        public async Task<IActionResult> Index()
        {
            // 1. ดึงข้อมูลจาก Session (Customer_Id ถูกแก้เป็น int แล้ว)
            int? customerId = HttpContext.Session.GetInt32("Customer_Id");
            int? role = HttpContext.Session.GetInt32("Role");

            // 2. ตรวจสอบการ Login
            if (customerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // 3. ตรวจสอบสิทธิ์ (ถ้าไม่ใช่ Role 1 ให้เด้งออก)
            if (role != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            try
            {
                // 4. ดึงข้อมูลสถิติ โดยใช้ AsNoTracking เพื่อประสิทธิภาพที่ดีขึ้น
                ViewBag.OrderCount = await _context.Orders
                    .AsNoTracking()
                    .CountAsync(o => o.Customer_Id == customerId);

                ViewBag.PendingOrders = await _context.Orders
                    .AsNoTracking()
                    .CountAsync(o => o.Customer_Id == customerId && o.Status == "PendingVerify");

                // ======================================================
                // 🔔 5. ระบบกันลูกค้าเบี้ยวคิว: ดึงคิวที่ต้องแจ้งเตือนส่งไปให้หน้าจอ
                // ======================================================
                var pendingReminder = await _context.Appointments
                    .Include(a => a.Service)
                    .Where(a => a.Customer_Id == customerId
                             && a.IsReminderSent == true
                             && a.IsCustomerConfirmed == false
                             && a.Status != "ยกเลิกแล้ว"
                             && a.Status != "เสร็จสิ้น")
                    .OrderBy(a => a.Appointment_Date)
                    .FirstOrDefaultAsync(); // ดึงคิวที่ใกล้ที่สุดมา 1 คิว

                ViewBag.ReminderAppointment = pendingReminder;


                // 6. ดึงรายการสินค้าที่ยังขายอยู่
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsActive)
                    .AsNoTracking()
                    .ToListAsync();

                ViewBag.Categories = await _context.Categories
                    .OrderBy(c => c.Category_Id)
                    .AsNoTracking()
                    .ToListAsync();

                return View(products);
            }
            catch (Exception)
            {
                // หากเกิดข้อผิดพลาดในการดึงข้อมูล ให้ส่ง List เปล่าไปที่ View เพื่อป้องกันหน้าขาว
                return View(new List<Product>());
            }
        }

        // ==========================================
        // 🔥 หน้าสินค้าขายดี (ลูกค้าดูได้)
        // ==========================================
        public async Task<IActionResult> BestSellers()
        {
            int? customerId = HttpContext.Session.GetInt32("Customer_Id");
            if (customerId == null) return RedirectToAction("Login", "Account");

            // นับเฉพาะออเดอร์ที่ชำระเงินแล้ว ไม่งั้นออเดอร์ที่ยังไม่จ่าย/ยกเลิกจะปั่นอันดับ
            var paidStatuses = new[] { "Paid", "Processing", "Shipped", "Delivered", "Completed" };

            var ranking = await _context.OrderDetails
                .Include(d => d.Product).ThenInclude(p => p!.Brand)
                .Include(d => d.Order)
                .Where(d => d.Order != null && paidStatuses.Contains(d.Order.Status) && d.Product != null)
                .GroupBy(d => d.Product_Id)
                .Select(g => new BestSellerItem
                {
                    Product = g.First().Product!,
                    SoldQty = g.Sum(x => x.Qty)
                })
                .OrderByDescending(x => x.SoldQty)
                .Take(10)
                .ToListAsync();

            return View(ranking);
        }

        // ==========================================
        // ✅ ลูกค้ากดยืนยันคิว (รับค่าจากป๊อปอัป SweetAlert)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmAppointment(int id)
        {
            int? customerId = HttpContext.Session.GetInt32("Customer_Id");
            if (customerId == null) return RedirectToAction("Login", "Account");

            var app = await _context.Appointments.FindAsync(id);
            if (app != null && app.Customer_Id == customerId)
            {
                app.IsCustomerConfirmed = true; // บันทึกว่าลูกค้ายืนยันแล้ว
                app.Status = "ยืนยันแล้ว";
                await _context.SaveChangesAsync();

                // แจ้งเตือนให้ SweetAlert โชว์ว่าสำเร็จ
                TempData["SuccessMessage"] = "ยืนยันคิวเรียบร้อยแล้ว ขอบคุณที่ไว้วางใจใช้บริการครับ!";
            }
            return RedirectToAction("Index");
        }

        // ==========================================
        // ❌ ลูกค้ากดยกเลิกคิว (รับค่าจากป๊อปอัป SweetAlert)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            int? customerId = HttpContext.Session.GetInt32("Customer_Id");
            if (customerId == null) return RedirectToAction("Login", "Account");

            var app = await _context.Appointments.FindAsync(id);
            if (app != null && app.Customer_Id == customerId)
            {
                app.Status = "ยกเลิกแล้ว";
                app.IsCustomerConfirmed = false;
                app.Employee_Id = null; // ปลดช่างออก (เผื่อแอดมินจ่ายงานไปแล้ว ช่างจะได้คิวว่างคืน)
                await _context.SaveChangesAsync();

                TempData["ErrorMessage"] = "ยกเลิกคิวเรียบร้อยแล้ว หากต้องการใช้บริการกรุณาจองใหม่อีกครั้งครับ";
            }
            return RedirectToAction("Index");
        }

        // ==========================================
        // 🔔 API สำหรับเช็คแจ้งเตือน (ดึงไปโชว์ที่กระดิ่งในทุกหน้า)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> CheckNotification()
        {
            int? customerId = HttpContext.Session.GetInt32("Customer_Id");
            if (customerId == null) return Json(new { hasNotification = false });

            var reminder = await _context.Appointments
                .Include(a => a.Service)
                .Where(a => a.Customer_Id == customerId
                         && a.IsReminderSent == true
                         && a.IsCustomerConfirmed == false
                         && a.Status != "ยกเลิกแล้ว"
                         && a.Status != "เสร็จสิ้น")
                .OrderBy(a => a.Appointment_Date)
                .FirstOrDefaultAsync();

            if (reminder != null)
            {
                return Json(new
                {
                    hasNotification = true,
                    id = reminder.Id,
                    serviceName = reminder.Service?.Service_Name ?? "บริการทั่วไป",
                    appDate = reminder.Appointment_Date.ToString("dd MMM yyyy"),
                    appTime = reminder.Appointment_Date.ToString("HH:mm")
                });
            }

            return Json(new { hasNotification = false });
        }
    }
}