using CS66_WebApplication1.Filters;
using CS66_WebApplication1.Models;
using CS66_WebApplication1.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CS66_WebApplication1.Controllers
{
    [StaffAuthorize]
    public class EmployeeDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly AppointmentEmailLinkBuilder _linkBuilder;

        // สถานะที่พนักงานเปลี่ยนเองได้จากหน้าแดชบอร์ด
        private static readonly string[] AllowedTaskStatuses =
            { "รออนุมัติ", "ยืนยันแล้ว", "กำลังทำ", "เสร็จแล้ว" };

        public EmployeeDashboardController(ApplicationDbContext context, IEmailSender emailSender, AppointmentEmailLinkBuilder linkBuilder)
        {
            _context = context;
            _emailSender = emailSender;
            _linkBuilder = linkBuilder;
        }

        // ========================================================
        // 🏠 1. หน้าหลักแดชบอร์ด (Index)
        // ========================================================
        public async Task<IActionResult> Index()
        {
            int? sessionId = HttpContext.Session.GetInt32("Customer_Id");
            if (sessionId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            string empId = sessionId.ToString();
            ViewBag.EmployeeName = HttpContext.Session.GetString("Customer_Name") ?? "พนักงาน";

            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var sevenDaysAgo = today.AddDays(-7);

            // === 📊 1. คำนวณ KPI Cards ===
            ViewBag.TodayTasksCount = await _context.Appointments
                .CountAsync(a => (a.Employee_Id == empId || string.IsNullOrEmpty(a.Employee_Id)) && a.Appointment_Date.Date == today && a.Status != "ยกเลิกแล้ว");

            ViewBag.InProgressCount = await _context.Appointments
                .CountAsync(a => a.Employee_Id == empId && (a.Status == "กำลังทำ" || a.Status == "กำลังดำเนินการซ่อม"));

            ViewBag.CompletedTodayCount = await _context.Appointments
                .CountAsync(a => a.Employee_Id == empId && a.Appointment_Date.Date == today && (a.Status == "เสร็จแล้ว" || a.Status == "เสร็จสิ้น" || a.Status == "รอชำระเงิน" || a.Status == "รอยืนยันชำระเงิน"));

            ViewBag.MonthlyTasksCount = await _context.Appointments
                .CountAsync(a => (a.Employee_Id == empId || string.IsNullOrEmpty(a.Employee_Id)) && a.Appointment_Date >= startOfMonth && a.Appointment_Date.Month == today.Month && a.Status != "ยกเลิกแล้ว");

            // === 📈 2. คำนวณสถิติ Progress Bar ===
            ViewBag.WeeklyCompleted = await _context.Appointments
                .CountAsync(a => a.Employee_Id == empId && a.Appointment_Date >= sevenDaysAgo && (a.Status == "เสร็จแล้ว" || a.Status == "เสร็จสิ้น" || a.Status == "รอชำระเงิน" || a.Status == "รอยืนยันชำระเงิน"));

            // รายชื่อช่างจริงจากฐานข้อมูล (ใช้แทนรหัสพนักงานตอนแสดงผล)
            ViewBag.Mechanics = await _context.Customers
                .Where(c => c.Role == 2)
                .ToDictionaryAsync(c => c.Customer_Id.ToString(), c => c.Customer_Name);

            // === 📋 3. ดึงข้อมูลตารางงานมาแสดงหน้า Dashboard ===
            var todaysTasks = await _context.Appointments
                .Include(a => a.Service)
                .Include(a => a.Customer)
                .Where(a =>
                    (a.Employee_Id == empId &&
                        (
                            (a.Status != "เสร็จแล้ว" && a.Status != "เสร็จสิ้น" && a.Status != "ยกเลิกแล้ว")
                            ||
                            (a.Appointment_Date.Date == today)
                        )
                    )
                    ||
                    (string.IsNullOrEmpty(a.Employee_Id) && a.Appointment_Date.Date == today && a.Status != "ยกเลิกแล้ว")
                )
                .OrderBy(a => a.Status == "เสร็จแล้ว" || a.Status == "เสร็จสิ้น" || a.Status == "ยกเลิกแล้ว" ? 1 : 0)
                .ThenBy(a => a.Appointment_Date)
                .ToListAsync();

            return View(todaysTasks);
        }

        // ========================================================
        // 📋 2. หน้าจัดการสถานะงานทั้งหมด (Tasks)
        // ========================================================
        [HttpGet]
        public async Task<IActionResult> Tasks()
        {
            int? sessionId = HttpContext.Session.GetInt32("Customer_Id");
            if (sessionId == null) return RedirectToAction("Login", "Account");

            string empId = sessionId.ToString();
            ViewBag.EmployeeName = HttpContext.Session.GetString("Customer_Name") ?? "พนักงาน";

            var tasks = await _context.Appointments
                .Include(a => a.Service)
                .Include(a => a.Customer)
                .Where(a => a.Employee_Id == empId || string.IsNullOrEmpty(a.Employee_Id))
                .OrderBy(a => a.Status == "เสร็จแล้ว" || a.Status == "เสร็จสิ้น" || a.Status == "ยกเลิกแล้ว" ? 1 : 0)
                .ThenBy(a => a.Appointment_Date)
                .ToListAsync();

            return View(tasks);
        }

        // ========================================================
        // 🔄 3. ฟังก์ชันสำหรับอัปเดตสถานะ 
        // ========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTaskStatus(int appointmentId, string newStatus)
        {
            int? sessionId = HttpContext.Session.GetInt32("Customer_Id");

            // กันสถานะแปลกปลอมที่หน้าอื่นอ่านไม่รู้เรื่อง
            if (!AllowedTaskStatuses.Contains(newStatus))
            {
                TempData["ErrorMessage"] = "สถานะงานไม่ถูกต้อง";
                return RedirectToAction("Tasks");
            }

            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment != null)
            {
                if (newStatus == "เสร็จแล้ว")
                    newStatus = "รอชำระเงิน";

                appointment.Status = newStatus;

                if (string.IsNullOrEmpty(appointment.Employee_Id) && sessionId != null)
                {
                    appointment.Employee_Id = sessionId.ToString();
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"อัปเดตสถานะงานสำเร็จ!";
            }
            else
            {
                TempData["ErrorMessage"] = "ไม่พบข้อมูลงานที่ต้องการอัปเดต";
            }

            string referer = Request.Headers["Referer"].ToString();
            return !string.IsNullOrEmpty(referer) ? Redirect(referer) : RedirectToAction("Index");
        }

        // ========================================================
        // 🔔 4. ฟังก์ชันส่งแจ้งเตือนหาลูกค้า
        // ========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendReminder(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Service)
                .Include(a => a.Customer)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment != null && appointment.Customer != null && !string.IsNullOrWhiteSpace(appointment.Customer.Customer_Email))
            {
                appointment.ConfirmToken = Guid.NewGuid().ToString("N");
                appointment.TokenExpiry = appointment.Appointment_Date.AddHours(-2);
                appointment.IsReminderSent = true;
                await _context.SaveChangesAsync();

                var (confirmUrl, declineUrl) = _linkBuilder.Build(appointment.ConfirmToken);
                var html = AppointmentEmailTemplate.BuildReminderEmail(
                    appointment.Customer.Customer_Name,
                    appointment.Service?.Service_Name ?? "บริการทั่วไป",
                    appointment.Appointment_Date,
                    confirmUrl,
                    declineUrl);

                await _emailSender.SendEmailAsync(
                    appointment.Customer.Customer_Email,
                    appointment.Customer.Customer_Name,
                    "แจ้งเตือนนัดหมาย - กรุณายืนยันคิวของท่าน",
                    html);

                TempData["SuccessMessage"] = "ส่งอีเมลแจ้งเตือนไปยังลูกค้าเรียบร้อยแล้ว!";
            }
            else if (appointment != null)
            {
                TempData["ErrorMessage"] = "ลูกค้ารายนี้ไม่มีอีเมลในระบบ ไม่สามารถส่งแจ้งเตือนได้";
            }

            string referer = Request.Headers["Referer"].ToString();
            return !string.IsNullOrEmpty(referer) ? Redirect(referer) : RedirectToAction("Index");
        }

        // ========================================================
        // 📅 5. หน้าตารางงานรวม (ดูงานของทุกคนในร้าน)
        // ========================================================
        [HttpGet]
        public async Task<IActionResult> Schedule()
        {
            int? sessionId = HttpContext.Session.GetInt32("Customer_Id");
            if (sessionId == null) return RedirectToAction("Login", "Account");

            string empId = sessionId.ToString();
            ViewBag.EmployeeName = HttpContext.Session.GetString("Customer_Name") ?? "พนักงาน";

            var today = DateTime.Today;

            var mechanics = await _context.Customers
                .Where(c => c.Role == 2)
                .ToDictionaryAsync(c => c.Customer_Id.ToString(), c => c.Customer_Name);

            ViewBag.Mechanics = mechanics;

            var myTasks = await _context.Appointments
                .Include(a => a.Service)
                .Include(a => a.Customer)
                .Where(a => a.Employee_Id == empId)
                .OrderByDescending(a => a.Appointment_Date.Date == today)
                .ThenBy(a => a.Status == "เสร็จแล้ว" || a.Status == "เสร็จสิ้น" || a.Status == "ยกเลิกแล้ว" ? 1 : 0)
                .ThenByDescending(a => a.Appointment_Date)
                .ToListAsync();

            return View(myTasks);
        }

        // ========================================================
        // ⚙️ 6. หน้าเบิกอะไหล่ (SpareParts)
        // ========================================================
        public async Task<IActionResult> SpareParts()
        {
            int? empId = HttpContext.Session.GetInt32("Customer_Id");
            if (empId == null) return RedirectToAction("Login", "Account");

            ViewBag.EmployeeName = HttpContext.Session.GetString("Customer_Name") ?? "พนักงาน";

            // งานที่ยังทำอยู่ของช่างคนนี้ ให้เลือกผูกกับการเบิกได้
            ViewBag.MyJobs = await _context.Appointments
                .Include(a => a.Service)
                .Include(a => a.Customer)
                .Where(a => a.Employee_Id == empId.ToString()
                         && a.Status != "ยกเลิกแล้ว"
                         && a.Status != "เสร็จสิ้น")
                .OrderBy(a => a.Appointment_Date)
                .ToListAsync();

            // ประวัติการเบิกของช่างคนนี้ (ล่าสุด 20 รายการ)
            ViewBag.MyHistory = await _context.SparePartRequisitions
                .Include(r => r.SparePart)
                .Include(r => r.Appointment).ThenInclude(a => a!.Service)
                .Where(r => r.Employee_Id == empId)
                .OrderByDescending(r => r.Id)
                .Take(20)
                .ToListAsync();

            var parts = await _context.SpareParts.OrderBy(p => p.Name).ToListAsync();
            return View(parts);
        }

        // ========================================================
        // ⚙️ 7. บันทึกการเบิกอะไหล่ (ตัดสต็อกทันที)
        // ========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WithdrawSparePart(int sparePartId, int qty, int? appointmentId, string? note)
        {
            int? empId = HttpContext.Session.GetInt32("Customer_Id");
            if (empId == null) return RedirectToAction("Login", "Account");

            if (qty <= 0)
            {
                TempData["ErrorMessage"] = "จำนวนที่เบิกต้องมากกว่า 0";
                return RedirectToAction(nameof(SpareParts));
            }

            var part = await _context.SpareParts.FindAsync(sparePartId);
            if (part == null)
            {
                TempData["ErrorMessage"] = "ไม่พบอะไหล่ที่ต้องการเบิก";
                return RedirectToAction(nameof(SpareParts));
            }

            if (part.StockQuantity < qty)
            {
                TempData["ErrorMessage"] = $"อะไหล่ \"{part.Name}\" มีไม่พอ (เหลือ {part.StockQuantity} ชิ้น)";
                return RedirectToAction(nameof(SpareParts));
            }

            part.StockQuantity -= qty;

            _context.SparePartRequisitions.Add(new SparePartRequisition
            {
                SparePart_Id = sparePartId,
                Employee_Id = empId.Value,
                Qty = qty,
                RequestedAt = DateTime.Now,
                Appointment_Id = appointmentId,
                Note = note
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"เบิก \"{part.Name}\" จำนวน {qty} ชิ้น เรียบร้อยแล้ว";
            return RedirectToAction(nameof(SpareParts));
        }
    }
}