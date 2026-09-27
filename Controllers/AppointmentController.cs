using CS66_WebApplication1.Models;
using CS66_WebApplication1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic; // 👈 เพิ่ม List<> สำหรับเก็บเวลา

namespace CS66_WebApplication1.Controllers
{
    public class AppointmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AppointmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ========================================================
        // 1. หน้าแรก: เลือกว่าจะทำบริการอะไร (Create.cshtml)
        // ========================================================
        public async Task<IActionResult> Create()
        {
            var services = await _context.Services.ToListAsync();
            return View(services);
        }

        // ========================================================
        // 2. รับค่าบริการที่ลูกค้ากดเลือก แล้วส่งต่อไปหน้าเลือกเวลา
        // ========================================================
        [HttpPost]
        public IActionResult SelectService(int serviceId)
        {
            if (serviceId <= 0) return RedirectToAction("Create");
            return RedirectToAction("DateTimeSelection", new { id = serviceId });
        }

        // ========================================================
        // 3. หน้าเลือกวัน-เวลา (DateTimeSelection.cshtml)
        // ========================================================
        [HttpGet]
        public async Task<IActionResult> DateTimeSelection(int id)
        {
            // 🌟 บริการที่ใช้เวลา < 1 ชม. ไม่เปิดให้จองผ่านเว็บ (ให้ walk-in แทรกคิวหน้างานแทน)
            var service = await _context.Services.FindAsync(id);
            if (service == null || !service.IsActive || service.Duration < 60)
            {
                TempData["ErrorMessage"] = "บริการนี้ไม่เปิดให้จองผ่านเว็บ กรุณาแวะมาที่ร้านได้เลยครับ (walk-in)";
                return RedirectToAction("Create");
            }

            ViewBag.ServiceId = id;
            return View();
        }

        // ========================================================
        // 🌟 3.5 API ดึงเวลาที่ว่างของวันที่เลือก
        // ระบบใหม่: แต่ละช่วงเวลารับได้พร้อมกันหลายคิว ตามจำนวน "ช่างที่ว่าง" (ไม่ใช่ 1 คิว/เวลา แบบเดิม)
        // เพราะช่างทุกคนทำได้ทุกงาน จองซ้ำเวลาเดิมได้ตราบใดที่ยังมีช่างว่างเหลือ
        // ========================================================
        [HttpGet]
        public async Task<IActionResult> GetAvailableTimes(string selectedDate)
        {
            if (!DateTime.TryParse(selectedDate, out DateTime parsedDate))
            {
                return Json(new List<object>());
            }

            // 1. กำหนดตารางเวลาที่ร้านเปิดรับจองใน 1 วัน (สามารถปรับเพิ่มลดได้ตามจริง)
            var allTimeSlots = new List<string>
            {
                "09:00", "10:00", "11:00", "13:00", "14:00", "15:00", "16:00"
            };

            int capacity = await GetTechnicianCapacityAsync();

            // 2. นับจำนวนคิวที่ถูกจองไปแล้วในแต่ละช่วงเวลาของวันนั้น (ไม่นับคิวที่ยกเลิก)
            var bookedCounts = await _context.Appointments
                .Where(a => a.Appointment_Date.Date == parsedDate.Date && a.Status != "ยกเลิกแล้ว")
                .GroupBy(a => a.Appointment_Date)
                .Select(g => new { Time = g.Key.ToString("HH:mm"), Count = g.Count() })
                .ToListAsync();

            var bookedMap = bookedCounts.ToDictionary(x => x.Time, x => x.Count);

            // 🌟 ถ้าเลือกวันนี้ ต้องตัดช่วงเวลาที่ผ่านไปแล้วออก ไม่งั้นจะจองเวลาย้อนหลังได้
            bool isToday = parsedDate.Date == DateTime.Today;

            // 3. เวลาไหนยังมีช่างว่างเหลือ (จำนวนช่าง - จำนวนที่จองไปแล้ว > 0) และยังไม่ผ่านไปแล้ว ถึงจะให้ลูกค้าเลือกได้
            var availableSlots = allTimeSlots
                .Where(t => !isToday || TimeSpan.Parse(t) > DateTime.Now.TimeOfDay)
                .Select(t => new { time = t, remaining = capacity - (bookedMap.TryGetValue(t, out var c) ? c : 0) })
                .Where(x => x.remaining > 0)
                .ToList();

            return Json(availableSlots);
        }

        // จำนวนช่างทั้งหมดที่รับงานได้ (พนักงาน Role == 2 ที่ยังใช้งานอยู่) = ความจุสูงสุดต่อ 1 ช่วงเวลา
        private async Task<int> GetTechnicianCapacityAsync()
        {
            return await _context.Customers.CountAsync(c => c.Role == 2 && c.IsActive);
        }

        // ========================================================
        // 4. รับค่าตอนกดยืนยันปฏิทิน (บันทึกข้อมูลลง Database)
        // ========================================================
        [HttpPost]
        public async Task<IActionResult> ConfirmDateTime(int serviceId, string selectedDate, string selectedTime)
        {
            try
            {
                // 🌟 กันเคสยิง POST ตรงข้ามหน้าเลือกเวลา: บริการ < 1 ชม. จองผ่านเว็บไม่ได้เช่นกัน
                var service = await _context.Services.FindAsync(serviceId);
                if (service == null || !service.IsActive || service.Duration < 60)
                {
                    TempData["ErrorMessage"] = "บริการนี้ไม่เปิดให้จองผ่านเว็บ กรุณาแวะมาที่ร้านได้เลยครับ (walk-in)";
                    return RedirectToAction("Create");
                }

                // 1. รวมวันที่และเวลาที่ส่งมาจากหน้า View และแปลงเป็น DateTime
                DateTime appointmentDate;
                if (!DateTime.TryParse($"{selectedDate} {selectedTime}", out appointmentDate))
                {
                    appointmentDate = DateTime.Now;
                }

                // 🌟 กันจองเวลาที่ผ่านไปแล้ว (เผื่อยิง POST ตรงข้ามหน้าเลือกเวลา หรือค้างฟอร์มไว้นาน)
                if (appointmentDate <= DateTime.Now)
                {
                    TempData["ErrorMessage"] = "ไม่สามารถจองเวลาที่ผ่านไปแล้วได้ กรุณาเลือกเวลาใหม่";
                    return RedirectToAction("DateTimeSelection", new { id = serviceId });
                }

                // 🌟 2. ดักจับป้องกันคิวเกินความจุ (Capacity Check)
                // แต่ละช่วงเวลารับได้พร้อมกันได้เท่าจำนวนช่างที่ว่าง ไม่ใช่แค่ 1 คิว/เวลา แบบเดิม
                // เช็คซ้ำอีกครั้งตอนบันทึกจริง กันเคสมีคนจองแซงไปพร้อมๆ กันในช่วงที่ลูกค้ากำลังกรอกฟอร์ม
                int capacity = await GetTechnicianCapacityAsync();
                int currentlyBooked = await _context.Appointments
                    .CountAsync(a => a.Appointment_Date == appointmentDate && a.Status != "ยกเลิกแล้ว");

                if (currentlyBooked >= capacity)
                {
                    // ถ้าคิวเต็ม (ช่างว่างหมดแล้ว) ให้เด้งกลับไปหน้าปฏิทิน พร้อมแจ้งเตือนลูกค้า
                    TempData["ErrorMessage"] = $"เสียใจด้วยครับ เวลา {selectedTime} น. ของวันที่ {selectedDate} ช่างเต็มคิวแล้ว กรุณาเลือกเวลาใหม่";
                    return RedirectToAction("DateTimeSelection", new { id = serviceId });
                }

                // 3. ดึง ID ลูกค้าจาก Session
                int? sessionCustomerId = HttpContext.Session.GetInt32("Customer_Id");
                if (sessionCustomerId == null)
                {
                    TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนทำการจองคิว";
                    return RedirectToAction("Login", "Account");
                }
                int customerId = sessionCustomerId.Value;

                // 4. เตรียมสร้าง Object ข้อมูลการจองใหม่
                var newAppointment = new Appointment
                {
                    Service_Id = serviceId,
                    Appointment_Date = appointmentDate,
                    Status = "รออนุมัติ",
                    Customer_Id = customerId
                };

                // 5. สั่งเพิ่มข้อมูลและบันทึกลง Database
                _context.Appointments.Add(newAppointment);
                await _context.SaveChangesAsync();

                // 6. ฝากข้อมูลไว้ใน TempData เพื่อส่งไปโชว์หน้า Success
                TempData["BookingDate"] = selectedDate;
                TempData["BookingTime"] = selectedTime;
                TempData["BookingRef"] = $"#APT-{newAppointment.Id}";

                // สั่งให้ TempData คงอยู่สำหรับการข้ามไปหน้าถัดไป
                TempData.Keep();

                return RedirectToAction("BookingSuccess");
            }
            catch (Exception ex)
            {
                var innerError = ex.InnerException != null ? ex.InnerException.Message : "ไม่มีข้อมูลเพิ่มเติม";
                return Content($"❌ ไม่สามารถบันทึกข้อมูลลงฐานข้อมูลได้! \n\n" +
                               $"🔹 สาเหตุทั่วไป: {ex.Message} \n" +
                               $"🔹 สาเหตุลึกๆ จาก Database: {innerError}");
            }
        }

        // ========================================================
        // 5. หน้าจองสำเร็จ (BookingSuccess.cshtml)
        // ========================================================
        public IActionResult BookingSuccess()
        {
            return View();
        }

        // ========================================================
        // 6. หน้าประวัติการจอง (History.cshtml)
        // ========================================================
        public async Task<IActionResult> History()
        {
            int? customerId = HttpContext.Session.GetInt32("Customer_Id");

            if (customerId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var history = await _context.Appointments
                .Include(a => a.Service)
                .Where(a => a.Customer_Id == customerId)
                .OrderByDescending(a => a.Appointment_Date)
                .ToListAsync();

            return View(history);
        }

        // ========================================================
        // 7. อัปโหลดสลิปชำระค่าบริการ
        // ========================================================
        [HttpPost]
        public async Task<IActionResult> UploadServiceSlip(int appointmentId, IFormFile slip)
        {
            int? customerId = HttpContext.Session.GetInt32("Customer_Id");
            if (customerId == null) return RedirectToAction("Index", "Home");

            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment == null) return NotFound();
            if (appointment.Customer_Id != customerId) return Forbid();
            if (appointment.Status != "รอชำระเงิน") return RedirectToAction("History");

            if (slip == null || slip.Length == 0)
            {
                TempData["ErrorMessage"] = "กรุณาอัปโหลดสลิปการชำระเงิน";
                return RedirectToAction("History");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(slip.FileName).ToLower();
            if (!allowedExtensions.Contains(ext))
            {
                TempData["ErrorMessage"] = "อนุญาตเฉพาะไฟล์รูปภาพ (.jpg, .png)";
                return RedirectToAction("History");
            }

            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "slips");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            var fileName = Guid.NewGuid() + ext;
            using (var stream = new FileStream(Path.Combine(folderPath, fileName), FileMode.Create))
            {
                await slip.CopyToAsync(stream);
            }

            appointment.ServicePaymentSlip = fileName;
            appointment.ServicePaidAt = DateTime.Now;
            appointment.Status = "รอยืนยันชำระเงิน";

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "อัปโหลดสลิปเรียบร้อยแล้ว รอแอดมินยืนยัน";
            return RedirectToAction("History");
        }
    }
}