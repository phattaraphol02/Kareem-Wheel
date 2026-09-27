using CS66_WebApplication1.Filters;
using CS66_WebApplication1.Models;
using CS66_WebApplication1.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CS66_WebApplication1.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext context;

        public CustomerController(ApplicationDbContext context)
        {
            this.context = context;
        }

        // =================================================================
        // 🛡️ ฟังก์ชันพิเศษ: ส่งข้อมูลที่อยู่ + สิทธิ์การใช้งาน ดักไว้กันแครช
        // =================================================================
        private async Task InitializeLocationViews(Customer customer = null)
        {
            // 1. จังหวัด
            var provinces = await context.Provinces.ToListAsync() ?? new List<Province>();
            ViewBag.Provinces = provinces;
            ViewBag.ProvinceList = provinces;
            ViewBag.AllProvinces = provinces;
            ViewBag.ProvinceSelectList = new SelectList(provinces, "Province_Id", "Province_Name");

            // 2. อำเภอ
            var amphurs = new List<Amphur>();
            if (customer != null && !string.IsNullOrEmpty(customer.Province_Id))
            {
                amphurs = await context.Amphurs
                    .Where(a => a.Province_Id == customer.Province_Id)
                    .ToListAsync() ?? new List<Amphur>();
            }
            ViewBag.Amphurs = amphurs;
            ViewBag.AmphurList = amphurs;
            ViewBag.AllAmphurs = amphurs;
            ViewBag.AmphurSelectList = new SelectList(amphurs, "Amphur_Id", "Amphur_Name");

            // 💡 3. เพิ่มการจัดการข้อมูล ตำบล (District)
            var districts = new List<District>();
            if (customer != null && !string.IsNullOrEmpty(customer.Amphur_Id))
            {
                districts = await context.Districts
                    .Where(d => d.Amphur_Id == customer.Amphur_Id)
                    .ToListAsync() ?? new List<District>();
            }
            ViewBag.Districts = districts;
            ViewBag.DistrictList = districts;
            ViewBag.AllDistricts = districts;
            ViewBag.DistrictSelectList = new SelectList(districts, "District_Id", "District_Name");

            // 4. สิทธิ์การใช้งาน (Role)
            var roles = new List<object>
            {
                new { Id = 1, Name = "Customer (ลูกค้าทั่วไป)" },
                new { Id = 2, Name = "Employee (พนักงาน)" },
                new { Id = 9, Name = "Admin (ผู้ดูแลระบบ)" }
            };
            ViewBag.Roles = roles;
            ViewBag.RoleList = roles;
            ViewBag.RoleSelectList = new SelectList(roles, "Id", "Name");
        }

        // =================================================================
        // 📋 1. หน้าแสดงรายชื่อลูกค้าทั้งหมด (GET: /Customer/Index)
        // =================================================================
        [HttpGet]
        [AdminAuthorize]
        public async Task<IActionResult> Index()
        {
            var customers = await context.Customers.ToListAsync();
            return View(customers);
        }

        // =================================================================
        // ➕ 2. หน้าฟอร์มเพิ่มข้อมูลลูกค้า (GET: /Customer/Create)
        // =================================================================
        [HttpGet]
        [AdminAuthorize]
        public async Task<IActionResult> Create()
        {
            await InitializeLocationViews();
            return View();
        }

        [HttpPost]
        [AdminAuthorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer model)
        {
            if (ModelState.IsValid)
            {
                var hasher = new PasswordHasher<Customer>();
                if (!string.IsNullOrEmpty(model.Password))
                {
                    model.Password = hasher.HashPassword(model, model.Password);
                }

                context.Customers.Add(model);
                await context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await InitializeLocationViews(model);
            return View(model);
        }

        // =================================================================
        // ✏️ 3. หน้าฟอร์มแก้ไขข้อมูลลูกค้าตาม ID (GET: /Customer/Edit/10)
        // =================================================================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            int? sessionId = HttpContext.Session.GetInt32("Customer_Id");
            if (sessionId == null) return RedirectToAction("Login", "Account");

            // เจ้าของบัญชีแก้ของตัวเองได้ ส่วนคนอื่นต้องเป็นแอดมินเท่านั้น
            bool isAdmin = HttpContext.Session.GetInt32("Role") == 9;
            if (!isAdmin && sessionId != id) return Forbid();

            var customer = await context.Customers.FindAsync(id);
            if (customer == null) return NotFound();

            await InitializeLocationViews(customer);
            return View(customer);
        }

        // ✏️ 4. ระบบบันทึกข้อมูลที่แก้ไขเสร็จแล้ว (POST: /Customer/Edit/10)
        [HttpPost]
        [AdminAuthorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Customer model)
        {
            if (id != model.Customer_Id) return NotFound();

            // ลบฟิลด์ที่เราไม่ได้อัปเดตออกจากระบบตรวจสอบข้อมูล
            ModelState.Remove("Password");
            ModelState.Remove("Province");
            ModelState.Remove("Amphur");
            ModelState.Remove("District"); // 💡 กัน Error ModelState ตำบล

            if (!ModelState.IsValid)
            {
                await InitializeLocationViews(model);
                return View(model);
            }

            var existCustomer = await context.Customers.FindAsync(id);
            if (existCustomer == null) return NotFound();

            existCustomer.Customer_Name = model.Customer_Name;
            existCustomer.Customer_Email = model.Customer_Email;
            existCustomer.Phone = model.Phone;
            existCustomer.Address = model.Address;
            existCustomer.Province_Id = model.Province_Id;
            existCustomer.Amphur_Id = model.Amphur_Id;
            existCustomer.District_Id = model.District_Id; // 💡 บันทึกตำบล
            existCustomer.PostalCode = model.PostalCode;
            existCustomer.Role = model.Role;

            if (!string.IsNullOrEmpty(model.Password))
            {
                var hasher = new PasswordHasher<Customer>();
                existCustomer.Password = hasher.HashPassword(existCustomer, model.Password);
            }

            try
            {
                context.Customers.Update(existCustomer);
                await context.SaveChangesAsync();

                TempData["SuccessMessage"] = "แก้ไขข้อมูลสมาชิกเรียบร้อยแล้ว!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "เกิดข้อผิดพลาดในการอัปเดตข้อมูล กรุณลองใหมี่อีกครั้ง");
            }

            await InitializeLocationViews(model);
            return View(model);
        }

        // 🗑️ ปิดการใช้งานบัญชี (POST: /Customer/Delete/10)
        // ไม่ลบจริงเพราะออเดอร์และนัดหมายอ้างอิงลูกค้าอยู่ ลบแล้วประวัติจะหายตาม
        [HttpPost]
        [AdminAuthorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await context.Customers.FindAsync(id);
            if (customer == null)
            {
                TempData["ErrorMessage"] = "ไม่พบข้อมูลสมาชิกที่ต้องการลบ";
                return RedirectToAction(nameof(Index));
            }

            if (id == HttpContext.Session.GetInt32("Customer_Id"))
            {
                TempData["ErrorMessage"] = "ไม่สามารถปิดการใช้งานบัญชีของตัวเองได้";
                return RedirectToAction(nameof(Index));
            }

            customer.IsActive = false;
            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"ปิดการใช้งานบัญชี \"{customer.Customer_Name}\" เรียบร้อยแล้ว";
            return RedirectToAction(nameof(Index));
        }

        // =================================================================
        // 👤 5. หน้าแสดงข้อมูลส่วนตัว (GET: /Customer/Profile)
        // =================================================================
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            int? customerId = HttpContext.Session.GetInt32("Customer_Id");
            if (customerId == null) return RedirectToAction("Login", "Account");

            // 💡 อัปเดตคำสั่งเพื่อดึงตาราง จังหวัด, อำเภอ, ตำบล มาแสดงผลชื่อได้
            var customer = await context.Customers
                .Include(c => c.Province)
                .Include(c => c.Amphur)
                .Include(c => c.District) // 💡 ดึงชื่อตำบลมาแสดง
                .FirstOrDefaultAsync(c => c.Customer_Id == customerId);

            if (customer == null) return NotFound();

            await InitializeLocationViews(customer);
            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(Customer model)
        {
            int? customerId = HttpContext.Session.GetInt32("Customer_Id");
            if (customerId == null) return RedirectToAction("Login", "Account");

            // ลบข้อมูลฟิลด์ที่ไม่ได้ส่งมาตรวจสอบ เพื่อไม่ให้ ModelState.IsValid ติดขัด
            ModelState.Remove("Password");
            ModelState.Remove("Province");
            ModelState.Remove("Amphur");
            ModelState.Remove("District"); // 💡 กัน Error ModelState ตำบล

            if (!ModelState.IsValid)
            {
                await InitializeLocationViews(model);
                return View(model);
            }

            var existCustomer = await context.Customers.FirstOrDefaultAsync(c => c.Customer_Id == customerId);
            if (existCustomer == null) return NotFound();

            // นำค่าใหม่ไปทับค่าเดิมในฐานข้อมูล
            existCustomer.Customer_Name = model.Customer_Name;
            existCustomer.Phone = model.Phone;
            existCustomer.Address = model.Address;
            existCustomer.Province_Id = model.Province_Id;
            existCustomer.Amphur_Id = model.Amphur_Id;
            existCustomer.District_Id = model.District_Id; // 💡 บันทึกตำบล
            existCustomer.PostalCode = model.PostalCode;

            string newPassword = Request.Form["Password"];
            if (!string.IsNullOrEmpty(newPassword))
            {
                var hasher = new PasswordHasher<Customer>();
                existCustomer.Password = hasher.HashPassword(existCustomer, newPassword);
            }

            try
            {
                context.Customers.Update(existCustomer);
                await context.SaveChangesAsync();

                HttpContext.Session.SetString("Customer_Name", existCustomer.Customer_Name ?? "");
                TempData["SuccessMessage"] = "อัปเดตข้อมูลส่วนตัวของคุณเรียบร้อยแล้ว!";
                return RedirectToAction("Profile");
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "เกิดข้อผิดพลาดทางเทคนิคในการบันทึกข้อมูล");
            }

            await InitializeLocationViews(model);
            return View(model);
        }
    }
}