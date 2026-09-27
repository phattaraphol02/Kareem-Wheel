using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using CS66_WebApplication1.Filters;
using CS66_WebApplication1.Models;
using CS66_WebApplication1.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CS66_WebApplication1.Controllers
{
    [AdminAuthorize]
    public class AdminAppointmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly AppointmentEmailLinkBuilder _linkBuilder;

        public AdminAppointmentController(ApplicationDbContext context, IEmailSender emailSender, AppointmentEmailLinkBuilder linkBuilder)
        {
            _context = context;
            _emailSender = emailSender;
            _linkBuilder = linkBuilder;
        }

        // ==========================================
        // 📋 หน้า Index: แสดงรายการจองทั้งหมด
        // ==========================================
        public IActionResult Index(string sort = "new")
        {
            // ดึงรายการจองพร้อมข้อมูล Service และ Customer
            // 🌟 เรียงตามคิวที่จองเข้ามาล่าสุด (Id เป็น identity เพิ่มขึ้นตามลำดับการจองจริง)
            //    หรือสลับไปเรียงตามวันนัดที่ใกล้ถึงก่อน (สำหรับดูคิวงานประจำวัน) ผ่านปุ่มบนหน้าเว็บ
            var query = _context.Appointments
                                 .Include(a => a.Service)
                                 .Include(a => a.Customer)
                                 .AsQueryable();

            query = sort == "date"
                ? query.OrderBy(a => a.Appointment_Date)
                : query.OrderByDescending(a => a.Id);

            var appointments = query.ToList();

            ViewBag.CurrentSort = sort;

            // 🌟 ดึงรายชื่อพนักงานที่มี Role == 2 และยังใช้งานอยู่ จากฐานข้อมูลจริงมาทำ Dropdown
            var mechanicsList = _context.Customers
                .Where(c => c.Role == 2 && c.IsActive)
                .Select(c => new SelectListItem
                {
                    Value = c.Customer_Id.ToString(), // รหัสพนักงาน (เก็บลง Appointment.Employee_Id)
                    Text = c.Customer_Name            // ชื่อพนักงานที่จะแสดงให้แอดมินเลือก
                })
                .ToList();

            ViewBag.Mechanics = mechanicsList;

            // 🌟 ช่างที่ถูกจ่ายงานไปแล้วในแต่ละช่วงเวลา (กันแอดมินจ่ายช่างคนเดิมซ้ำในเวลาเดียวกัน)
            // เพราะตอนนี้ 1 ช่วงเวลารับได้หลายคิวพร้อมกัน แต่ช่าง 1 คนยังทำได้แค่ 1 งานต่อครั้ง
            var busyByDateTime = appointments
                .Where(a => !string.IsNullOrEmpty(a.Employee_Id) && a.Status != "ยกเลิกแล้ว")
                .GroupBy(a => a.Appointment_Date)
                .ToDictionary(g => g.Key, g => g.Select(a => a.Employee_Id!).ToHashSet());

            ViewBag.BusyByDateTime = busyByDateTime;

            return View(appointments);
        }

        // ==========================================
        // 🔧 ระบบจ่ายงานให้ช่าง (Assign Task)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AssignTask(int id, string employeeId)
        {
            var app = _context.Appointments.Find(id);
            if (app != null && !string.IsNullOrEmpty(employeeId))
            {
                // 🌟 กันจ่ายงานให้ช่างคนเดิมซ้ำในช่วงเวลาเดียวกัน (ช่าง 1 คนทำได้แค่ 1 งานต่อครั้ง)
                bool alreadyBusy = _context.Appointments.Any(a =>
                    a.Id != id &&
                    a.Employee_Id == employeeId &&
                    a.Appointment_Date == app.Appointment_Date &&
                    a.Status != "ยกเลิกแล้ว");

                if (alreadyBusy)
                {
                    TempData["ErrorMessage"] = "ช่างคนนี้มีคิวช่วงเวลานี้อยู่แล้ว กรุณาเลือกช่างคนอื่น";
                    return RedirectToAction("Index");
                }

                app.Employee_Id = employeeId; // บันทึกรหัสพนักงานที่รับงาน

                // ถ้าระบบยัง "รออนุมัติ" อยู่ การที่แอดมินจ่ายงาน ถือว่าเป็นการ "ยืนยัน" คิวไปในตัว
                if (app.Status == "รออนุมัติ")
                {
                    app.Status = "ยืนยันแล้ว";
                }

                _context.SaveChanges();
                TempData["SuccessMessage"] = "จ่ายงานให้ช่างเรียบร้อยแล้ว";
            }
            else
            {
                TempData["ErrorMessage"] = "กรุณาเลือกช่างก่อนทำการจ่ายงาน";
            }
            return RedirectToAction("Index");
        }

        // ==========================================
        // 🔔 ระบบกันลูกค้าเบี้ยวคิว (ส่งข้อความสะกิด)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendReminder(int id)
        {
            var app = await _context.Appointments
                .Include(a => a.Service)
                .Include(a => a.Customer)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (app != null && app.Customer != null && !string.IsNullOrWhiteSpace(app.Customer.Customer_Email))
            {
                app.ConfirmToken = Guid.NewGuid().ToString("N");
                app.TokenExpiry = app.Appointment_Date.AddHours(-2);
                app.IsReminderSent = true;
                await _context.SaveChangesAsync();

                var (confirmUrl, declineUrl) = _linkBuilder.Build(app.ConfirmToken);
                var html = AppointmentEmailTemplate.BuildReminderEmail(
                    app.Customer.Customer_Name,
                    app.Service?.Service_Name ?? "บริการทั่วไป",
                    app.Appointment_Date,
                    confirmUrl,
                    declineUrl);

                await _emailSender.SendEmailAsync(
                    app.Customer.Customer_Email,
                    app.Customer.Customer_Name,
                    "แจ้งเตือนนัดหมาย - กรุณายืนยันคิวของท่าน",
                    html);

                TempData["SuccessMessage"] = "ส่งอีเมลแจ้งเตือนไปยังลูกค้าเรียบร้อยแล้ว";
            }
            else if (app != null)
            {
                TempData["ErrorMessage"] = "ลูกค้ารายนี้ไม่มีอีเมลในระบบ ไม่สามารถส่งแจ้งเตือนได้";
            }
            return RedirectToAction("Index");
        }

        // ==========================================
        // ✅ อนุมัติคิว (แบบแมนนวล)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Confirm(int id)
        {
            var app = _context.Appointments.Find(id);
            if (app != null)
            {
                app.Status = "ยืนยันแล้ว";
                _context.SaveChanges();
                TempData["SuccessMessage"] = "ยืนยันการจองเรียบร้อยแล้ว";
            }
            return RedirectToAction("Index");
        }

        // ==========================================
        // ❌ ยกเลิกคิวงาน
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancel(int id)
        {
            var app = _context.Appointments.Find(id);
            if (app != null)
            {
                app.Status = "ยกเลิกแล้ว";
                app.Employee_Id = null; // เคลียร์ช่างออกเมื่อยกเลิกคิว
                _context.SaveChanges();
                TempData["SuccessMessage"] = "ยกเลิกการจองเรียบร้อยแล้ว";
            }
            return RedirectToAction("Index");
        }

        // ==========================================
        // 💰 ยืนยันการชำระเงินค่าบริการ
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmServicePayment(int id)
        {
            var app = _context.Appointments.Find(id);
            if (app != null && app.Status == "รอยืนยันชำระเงิน")
            {
                app.Status = "เสร็จสิ้น";
                _context.SaveChanges();
                TempData["SuccessMessage"] = "ยืนยันการชำระเงินค่าบริการเรียบร้อยแล้ว";
            }
            return RedirectToAction("Index");
        }
    }
}