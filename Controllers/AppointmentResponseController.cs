using CS66_WebApplication1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace CS66_WebApplication1.Controllers
{
    // ควบคุมลิงก์ "สะดวกมา / ไม่สะดวกมา" ที่แนบไปกับอีเมล — ตั้งใจไม่ต้อง login
    // เพราะลูกค้ากดจากกล่องอีเมลโดยตรง ใช้ ConfirmToken แทนการยืนยันตัวตนผ่าน session
    public class AppointmentResponseController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AppointmentResponseController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /AppointmentResponse/Respond?token=...&response=confirm|decline
        // แสดงหน้ายืนยันก่อนดำเนินการจริง กันกรณี email client แอบเปิดลิงก์ล่วงหน้า
        [HttpGet]
        public async Task<IActionResult> Respond(string token, string response)
        {
            bool isValidResponse = response == "confirm" || response == "decline";

            var appointment = isValidResponse
                ? await _context.Appointments
                    .Include(a => a.Service)
                    .FirstOrDefaultAsync(a => a.ConfirmToken == token)
                : null;

            bool isValid = appointment != null
                         && appointment.TokenExpiry.HasValue
                         && appointment.TokenExpiry.Value >= DateTime.Now
                         && appointment.Status != "ยกเลิกแล้ว";

            if (!isValid)
            {
                return View("Invalid");
            }

            ViewBag.Token = token;
            ViewBag.Response = response;
            return View(appointment);
        }

        // POST: /AppointmentResponse/Respond
        // กดปุ่มยืนยันจริงในหน้า landing page แล้วค่อยบันทึกผล
        [HttpPost]
        [ActionName("Respond")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RespondSubmit(string token, string response)
        {
            bool isValidResponse = response == "confirm" || response == "decline";

            var appointment = isValidResponse
                ? await _context.Appointments
                    .Include(a => a.Service)
                    .FirstOrDefaultAsync(a => a.ConfirmToken == token)
                : null;

            bool isValid = appointment != null
                         && appointment.TokenExpiry.HasValue
                         && appointment.TokenExpiry.Value >= DateTime.Now
                         && appointment.Status != "ยกเลิกแล้ว";

            if (!isValid)
            {
                return View("Invalid");
            }

            if (response == "confirm")
            {
                appointment!.IsCustomerConfirmed = true;
                appointment.Status = "ยืนยันแล้ว";
            }
            else
            {
                appointment!.Status = "ยกเลิกแล้ว";
                appointment.IsCustomerConfirmed = false;
                appointment.Employee_Id = null; // คืนคิวว่างให้ลูกค้าคนอื่นจองได้ทันที
            }

            // โทเค็นใช้ได้ครั้งเดียว กันกดลิงก์ซ้ำ/ลิงก์รั่วไหล
            appointment.ConfirmToken = null;
            appointment.TokenExpiry = null;
            await _context.SaveChangesAsync();

            ViewBag.Response = response;
            return View("Result", appointment);
        }
    }
}
